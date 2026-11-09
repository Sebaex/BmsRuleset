using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Gameplay.Judgement;

public static class BmsJudgementSelector
{
    private static readonly CandidateComparer candidate_comparer = new();

    public static BmsJudgementSelection SelectPress(
        BmsLayoutVariant layout,
        int column,
        IEnumerable<BmsJudgementCandidate> candidates,
        double inputTime,
        BmsJudgementAlgorithm? algorithm = BmsJudgementAlgorithm.Combo)
    {
        var initialCapacity = candidates.TryGetNonEnumeratedCount(out var candidateCount)
            ? Math.Max(1, candidateCount)
            : 16;
        var sortedCandidates = ArrayPool<BmsJudgementCandidate>.Shared.Rent(initialCapacity);
        var count = 0;
        var fixedJudge = true;

        try
        {
            foreach (var candidate in candidates)
            {
                if (count == sortedCandidates.Length)
                {
                    var expanded = ArrayPool<BmsJudgementCandidate>.Shared.Rent(sortedCandidates.Length * 2);
                    Array.Copy(sortedCandidates, expanded, count);
                    ArrayPool<BmsJudgementCandidate>.Shared.Return(sortedCandidates);
                    sortedCandidates = expanded;
                }

                if (count > 0 && candidate.JudgementRate != sortedCandidates[0].JudgementRate)
                    fixedJudge = false;
                sortedCandidates[count++] = candidate;
            }

            Array.Sort(sortedCandidates, 0, count, candidate_comparer);
            var search = new PressSearch(layout, column, inputTime, algorithm, fixedJudge);
            for (var i = 0; i < count; i++)
            {
                if (!search.Consider(sortedCandidates[i], i)) break;
            }

            return search.Selection;
        }
        finally
        {
            ArrayPool<BmsJudgementCandidate>.Shared.Return(sortedCandidates);
        }
    }

    // Gameplay already owns ordered notes. Feed them directly, retaining the selected slot
    // instead of copying, sorting and finding the chosen note a second time.
    internal struct PressSearch(BmsLayoutVariant layout, int column, double inputTime, BmsJudgementAlgorithm? algorithm, bool fixedJudge)
    {
        private BmsJudgementCandidate? selected;
        private HitResult selectedResult;
        private BmsJudgementWindowTable? selectedTable;
        private BmsJudgementCandidate? emptyPoorCandidate;
        private int selectedSlot;
        private int emptyPoorSlot;

        internal readonly BmsJudgementSelection Selection => selected != null
            ? new BmsJudgementSelection(selected, selectedResult, false)
            : new BmsJudgementSelection(emptyPoorCandidate, emptyPoorCandidate != null ? HitResult.Miss : HitResult.None, emptyPoorCandidate != null);

        internal readonly int Slot => selected != null ? selectedSlot : emptyPoorCandidate != null ? emptyPoorSlot : -1;

        internal bool Consider(in BmsJudgementCandidate candidate, int slot)
        {
            if (candidate.Column != column)
                return true;

            var offset = inputTime - candidate.StartTime;
            // beatoraja stops its candidate search before the inclusive E-POOR table edge.
            if (offset <= -1000)
                return false;

            if (selected is { } current && algorithm == BmsJudgementAlgorithm.Duration
                                        && candidate.StartTime >= inputTime && Math.Abs(current.StartTime - inputTime) <= -offset)
                return false;

            var table = BmsJudgementProfileProvider.GetTable(layout, candidate.Column, candidate.JudgementRate, tail: false);
            // LR2 excludes late BAD heads without starting the hold or emitting empty POOR.
            if (candidate.IsLongNote && !candidate.IsJudged && offset > selectionWindow(table, HitResult.Good, fixedJudge, false))
            {
                // Java invalidates tnote only after this candidate wins the algorithm comparison.
                if (selected == null || shouldReplaceSelected(selected.Value, candidate, inputTime, selectedTable!, table, algorithm, fixedJudge))
                {
                    selected = null;
                    emptyPoorCandidate = null;
                }
                return true;
            }
            var result = candidate.IsJudged ? HitResult.None : table.ResultForOffset(offset);
            if (result != HitResult.None)
            {
                if (selected == null || shouldReplaceSelected(selected.Value, candidate, inputTime, selectedTable!, table, algorithm, fixedJudge))
                {
                    selected = candidate;
                    selectedResult = result;
                    selectedTable = table;
                    selectedSlot = slot;
                }

                // Once the current choice cannot be replaced, later notes and empty POOR
                // candidates cannot change the result, even when judgement rates differ.
                if (selectedResult == HitResult.Perfect && selected.Value.StartTime == inputTime)
                    return false;

                return (algorithm ?? BmsJudgementAlgorithm.Combo) switch
                {
                    BmsJudgementAlgorithm.Lowest => false,
                    BmsJudgementAlgorithm.Duration => selected.Value.StartTime < inputTime,
                    BmsJudgementAlgorithm.Combo => selected.Value.StartTime - inputTime < -selectionWindow(selectedTable!, HitResult.Good, fixedJudge, false),
                    BmsJudgementAlgorithm.Score => selected.Value.StartTime - inputTime < -selectionWindow(selectedTable!, HitResult.Great, fixedJudge, false),
                    _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null),
                };
            }

            if (table.EPoorWindow?.ContainsOffset(offset) == true
                && (emptyPoorCandidate == null || Math.Abs(offset) < Math.Abs(inputTime - emptyPoorCandidate.Value.StartTime)))
            {
                emptyPoorCandidate = candidate;
                emptyPoorSlot = slot;
            }

            return true;
        }
    }

    private static bool shouldReplaceSelected(
        BmsJudgementCandidate current,
        BmsJudgementCandidate next,
        double inputTime,
        BmsJudgementWindowTable currentTable,
        BmsJudgementWindowTable nextTable,
        BmsJudgementAlgorithm? algorithm,
        bool fixedJudge)
    {
        // Match beatoraja's JudgeAlgorithm comparisons, including strict late and inclusive early
        // boundaries. Use each note's own table because BMS judgement rates can change mid-chart.
        return (algorithm ?? BmsJudgementAlgorithm.Combo) switch
        {
            BmsJudgementAlgorithm.Combo => isPastWindow(HitResult.Good),
            BmsJudgementAlgorithm.Duration => Math.Abs(current.StartTime - inputTime) > Math.Abs(next.StartTime - inputTime),
            BmsJudgementAlgorithm.Lowest => false,
            BmsJudgementAlgorithm.Score => isPastWindow(HitResult.Great),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null),
        };

        bool isPastWindow(HitResult threshold) =>
            current.StartTime - inputTime < -selectionWindow(currentTable, threshold, fixedJudge, false)
            && next.StartTime - inputTime <= selectionWindow(nextTable, threshold, fixedJudge, true);
    }

    private static double selectionWindow(BmsJudgementWindowTable table, HitResult result, bool fixedJudge, bool fast)
    {
        var window = fast ? table.FastWindowFor(result) : table.SlowWindowFor(result);
        if (!fixedJudge) return window;

        // Java nests disabled rows into the preceding row. The result is unchanged, but
        // selection and late-LN eligibility must still use those bounds on fixed charts.
        window = Math.Max(window, fast ? table.FastWindowFor(HitResult.Perfect) : table.SlowWindowFor(HitResult.Perfect));
        if (result == HitResult.Good)
            window = Math.Max(window, fast ? table.FastWindowFor(HitResult.Great) : table.SlowWindowFor(HitResult.Great));
        return window;
    }

    private sealed class CandidateComparer : IComparer<BmsJudgementCandidate>
    {
        public int Compare(BmsJudgementCandidate x, BmsJudgementCandidate y)
        {
            var startTimeComparison = x.StartTime.CompareTo(y.StartTime);
            return startTimeComparison != 0 ? startTimeComparison : x.Column.CompareTo(y.Column);
        }
    }
}
