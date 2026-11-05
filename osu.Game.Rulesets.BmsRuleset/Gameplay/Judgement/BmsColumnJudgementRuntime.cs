using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.BmsRuleset.Beatmaps.Objects;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Judgement.Rules;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Judgement.State;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Gameplay.Judgement;

/// <summary>
/// Resolves one column's input and exact rule deadlines without a graphical host.
/// </summary>
internal sealed class BmsColumnJudgementRuntime
{
    private readonly BmsNotePressState pressState = new();
    private readonly List<BmsHitObject> multiBadNotes = [];
    private readonly BmsHitObject[] columnNotes;
    private readonly Dictionary<BmsHitObject, int> noteSlots = new(ReferenceEqualityComparer.Instance);
    private readonly IBmsJudgementOutput noteOutput;
    private readonly BmsHoldRegistry holds;
    private readonly BmsLandmineRegistry landmines;
    private readonly BmsHitObject[] chartObjects;
    private readonly Dictionary<BmsHitObject, int> chartOrders = new(ReferenceEqualityComparer.Instance);
    private readonly SortedSet<(double Time, int Order)> automaticEvents = [];
    private readonly double[] automaticTimes;
    private (double Time, int Order) nextAutomaticEvent = (double.PositiveInfinity, -1);

    internal BmsJudgementRules Rules { get; }

    internal int Index { get; }

    internal bool IsScratch => BmsLayout.IsScratchColumn(Index, LayoutVariant);

    internal BmsLayoutVariant LayoutVariant { get; }

    internal BmsColumnNoteIndex Notes { get; }

    internal BmsNoteRegistration[] NoteRegistrations { get; }

    internal BmsNoteJudgementWindows[] NoteWindows { get; }

    internal void ApplyEmptyPoor(double expectedTime, in BmsFrameContext frame) => noteOutput.ApplyEmptyPoor(expectedTime, Index, frame);

    internal bool HasPmsMistake(BmsHitObject note) => BmsJudgementProfileProvider.IsPms(LayoutVariant) && pressState.Get(note).Mistake;

    internal bool IsKeySoundFinished(BmsHitObject note, double time)
        => noteSlots.ContainsKey(note) ? GetNoteResult(note).HasResult : time > note.StartTime;

    internal void RecordResult(BmsHitObject note) => pressState.Record(note, true);

    internal void PublishExecutionState(BmsLongNote note, in BmsLongNoteState state, in BmsFrameContext frame)
    {
        noteOutput.PublishState(note, state, frame);
        RefreshExecutionState(note);
    }

    internal void CompleteLongNotesAtEnd(in BmsFrameContext frame)
    {
        foreach (var note in GetHoldingNotes().ToArray())
            holds.CompleteAtEnd(noteSlots[note], frame);
    }

    internal int PressedDirections { get; set; }

    internal BmsColumnJudgementRuntime(BmsHitObject[] notes, BmsLayoutVariant layout, int column, IBmsJudgementOutput output, BmsJudgementRules rules)
    {
        Rules = rules;
        Index = column;
        LayoutVariant = layout;
        chartObjects = notes.Where(n => n.Column == column && n is BmsNote or BmsLongNote or BmsLandmine).OrderBy(n => n.StartTime).ToArray();
        automaticTimes = new double[chartObjects.Length];
        Array.Fill(automaticTimes, double.PositiveInfinity);
        for (var i = 0; i < chartObjects.Length; i++) chartOrders.Add(chartObjects[i], i);
        columnNotes = notes.Where(n => n.Column == column && n is BmsNote or BmsLongNote).OrderBy(n => n.StartTime).ToArray();
        landmines = new BmsLandmineRegistry(notes, column, rules.Landmine, output);
        noteOutput = output;
        var binding = new BmsNoteOperationBinding(this, output);
        NoteRegistrations = new BmsNoteRegistration[columnNotes.Length];
        NoteWindows = new BmsNoteJudgementWindows[columnNotes.Length];
        for (var i = 0; i < columnNotes.Length; i++)
            noteSlots.Add(columnNotes[i], i);

        holds = new BmsHoldRegistry(columnNotes, Rules, new BmsJudgementExecutionContext(this, output));
        for (var i = 0; i < columnNotes.Length; i++)
        {
            var note = columnNotes[i];
            if (note is BmsLongNote)
                NoteRegistrations[i] = new BmsNoteRegistration(note, i, holds.PressOperation, holds.PassiveOperation);
            else
            {
                NoteRegistrations[i] = new BmsNoteRegistration(note, i, binding.Press, binding.Passive);
                NoteWindows[i] = new BmsNoteJudgementWindows(note);
            }
        }

        Notes = new BmsColumnNoteIndex(noteSlots, NoteRegistrations, holds, output);
        output.SetResultCheckpoints(RecordResult, note =>
        {
            Notes.Refresh(note);
            refreshAutomaticEvent(note);
        });
        foreach (var note in chartObjects) refreshAutomaticEvent(note);
    }

    internal BmsLongNoteState GetLongNoteState(BmsLongNote note) => holds.GetState(noteSlots[note]);

    internal IEnumerable<BmsLongNote> GetPendingLongNoteTails()
    {
        for (var i = 0; i < Notes.PendingTails.Count; i++)
            yield return holds.GetNote(Notes.PendingTails[i]);
    }

    internal IEnumerable<BmsLongNote> GetActiveBodies()
    {
        for (var i = 0; i < Notes.ActiveBodies.Count; i++)
            yield return holds.GetNote(Notes.ActiveBodies[i]);
    }

    internal IEnumerable<BmsLongNote> GetHoldingNotes()
    {
        for (var i = 0; i < Notes.PendingTails.Count; i++)
        {
            var slot = Notes.PendingTails[i];
            var longNote = holds.GetNote(slot);
            if (GetLongNoteState(longNote).IsHolding)
                yield return longNote;
        }
    }

    internal void RefreshExecutionState(BmsLongNote note)
    {
        Notes.Refresh(note);
        refreshAutomaticEvent(note);
    }

    internal BmsMainResultState GetNoteResult(BmsHitObject note) => noteOutput.GetMainResult(note);

    internal BmsHitObject? FirstPendingNote => Notes.FirstPendingNote;

    internal bool IsNoteCandidate(BmsHitObject note) => Notes.IsNoteCandidate(note);

    internal IEnumerable<BmsHitObject> GetNoteCandidates() => Notes.GetNoteCandidates();

    internal void RefreshNoteTiming(BmsHitObject note)
    {
        if (noteSlots.ContainsKey(note))
        {
            if (!Notes.RefreshTiming(note)) return;

            // Configuration changes keep the existing relative order of equal-time chart objects.
            columnNotes.OrderBy(n => n.StartTime).ToArray().CopyTo(columnNotes, 0);
        }

        refreshAutomaticEvent(note);
    }

    internal void RefreshNoteConfiguration(BmsHitObject note)
    {
        if (!noteSlots.TryGetValue(note, out var slot))
        {
            refreshAutomaticEvent(note);
            return;
        }

        if (note is BmsLongNote) holds.Configure(slot);
        else NoteWindows[slot] = new BmsNoteJudgementWindows(note);
        RefreshNoteTiming(note);
        Notes.Refresh(note);
        refreshAutomaticEvent(note);
    }

    internal bool TryPressRegisteredNote(BmsHitObject note, HitResult result, in BmsPressContext press)
    {
        var registration = NoteRegistrations[noteSlots[note]];
        return registration.Press(registration.Slot, result, press);
    }

    internal void CheckRegisteredNotePassive(BmsHitObject note, in BmsFrameContext frame)
    {
        var registration = NoteRegistrations[noteSlots[note]];
        registration.Passive(registration.Slot, frame);
    }

    internal PressOutcome HandleNotePress(in BmsPressContext press, BmsJudgementAlgorithm? algorithm = BmsJudgementAlgorithm.Combo)
    {
        if (!press.Frame.IsRewinding && Notes.ActiveBodies.Count > 0)
            UpdateRegisteredBodies(press.Frame with { SimulationTime = press.InputTime }, 0, PressedDirections != 0);
        PressedDirections |= IsScratch && press.ReverseScratch ? 2 : 1;
        if (press.Frame.IsRewinding)
            return PressOutcome.Empty;

        for (var i = 0; i < Notes.PendingTails.Count; i++)
        {
            var slot = Notes.PendingTails[i];
            if (holds.Repress(slot, press))
            {
                var longNote = holds.GetNote(slot);
                if (!GetLongNoteState(longNote).IsHolding)
                    noteOutput.PlayTailSample(longNote);
                return PressOutcome.Hit;
            }
        }

        var search = selectPress(press.InputTime, algorithm);
        var selection = search.Selection;
        if (selection.Candidate != null)
            applyMultiBad(selection.Candidate.Value, search.Slot, press);
        if (!selection.IsEmptyPoor && selection.Candidate != null && search.Slot >= 0)
        {
            var note = NoteRegistrations[search.Slot].Object;
            if (TryPressRegisteredNote(note, selection.Result, press))
            {
                pressState.Record(note, true);
                noteOutput.PlaySample(note);
                return PressOutcome.Hit;
            }
        }

        noteOutput.PlayKeySound();
        if (selection is { IsEmptyPoor: true, Candidate: { } emptyPoor })
        {
            // Identical stacked heads share the old empty-POOR mistake marker.
            for (var i = findFirstNote(emptyPoor.StartTime); i < columnNotes.Length && columnNotes[i].StartTime == emptyPoor.StartTime; i++)
            {
                var note = columnNotes[i];
                if (!IsNoteCandidate(note)) continue;
                var candidate = new BmsJudgementCandidate(note.StartTime, note.GetEndTime(), Index, note.EffectiveJudgementRate,
                    note is BmsLongNote, note is BmsLongNote hold && GetLongNoteState(hold).HeadJudged, pressState.Get(note).Mistake);
                if (candidate == emptyPoor) pressState.Record(note, false, true);
            }
            return PressOutcome.ForEmptyPoor(emptyPoor.StartTime, emptyPoor.Column);
        }

        return PressOutcome.Empty;
    }

    private BmsJudgementSelector.PressSearch selectPress(double inputTime, BmsJudgementAlgorithm? algorithm)
    {
        var search = new BmsJudgementSelector.PressSearch(LayoutVariant, Index, inputTime, algorithm);
        // Chronological traversal preserves invalidation by later LN heads for every algorithm.
        for (var i = findFirstNote(inputTime - 1000); i < columnNotes.Length && columnNotes[i].StartTime < inputTime + 1000; i++)
        {
            var note = columnNotes[i];
            var history = pressState.Get(note);
            var completed = noteOutput.GetMainResult(note).HasResult;
            if (completed && !history.Judged) continue;
            var candidate = new BmsJudgementCandidate(note.StartTime, note.GetEndTime(), Index, note.EffectiveJudgementRate,
                note is BmsLongNote, completed || (note is BmsLongNote hold && GetLongNoteState(hold).HeadJudged), history.Mistake);
            if (!search.Consider(candidate, completed ? -1 : noteSlots[note])) break;
        }
        return search;
    }
    private void applyMultiBad(BmsJudgementCandidate primary, int primarySlot, in BmsPressContext press)
    {
        multiBadNotes.Clear();
        var table = BmsJudgementProfileProvider.GetTable(LayoutVariant, Index, primary.JudgementRate, false);
        // Java's collector only contains unjudged heads; a judged primary retains its -1us sentinel.
        var primaryTime = primary.IsJudged ? press.InputTime - 0.001 : primary.StartTime;
        var primaryBad = table.ResultForOffset(press.InputTime - primaryTime) == HitResult.Ok;
        for (var i = findFirstNote(press.InputTime - table.SlowWindowFor(HitResult.Ok));
             i < columnNotes.Length && columnNotes[i].StartTime <= press.InputTime + table.FastWindowFor(HitResult.Ok); i++)
        {
            var note = columnNotes[i];
            if (noteSlots[note] == primarySlot || noteOutput.GetMainResult(note).HasResult
                || (note is BmsLongNote hold && GetLongNoteState(hold).HeadJudged)) continue;
            var offset = press.InputTime - note.StartTime;
            if (table.ResultForOffset(offset) != HitResult.Ok) continue;
            if ((!primaryBad || primary.IsLongNote) && note.StartTime >= primaryTime) continue;
            multiBadNotes.Add(note);
        }

        // Match MultiBadCollector's prefix exclusion, rather than excluding every earlier LN.
        var first = 0;
        while (first < multiBadNotes.Count && multiBadNotes[first] is BmsLongNote && multiBadNotes[first].StartTime < primaryTime)
            first++;
        for (var i = first; i < multiBadNotes.Count; i++)
        {
            var note = multiBadNotes[i];
            if (note is BmsLongNote)
                holds.ApplyMultiBad(noteSlots[note], press);
            else
                TryPressRegisteredNote(note, HitResult.Ok, press);
            pressState.Record(note, true);
        }
    }

    private int findFirstNote(double time)
    {
        var low = 0;
        var high = columnNotes.Length;
        while (low < high)
        {
            var middle = low + (high - low) / 2;
            if (columnNotes[middle].StartTime < time) low = middle + 1;
            else high = middle;
        }
        return low;
    }

    internal void HandleExecutionRelease(double inputTime, in BmsFrameContext frame, bool reverseScratch = false)
    {
        var direction = IsScratch && reverseScratch ? 2 : 1;
        if ((PressedDirections & direction) == 0)
            return;

        if (!frame.IsRewinding && Notes.ActiveBodies.Count > 0)
            UpdateRegisteredBodies(frame with { SimulationTime = inputTime }, 0, PressedDirections != 0);
        PressedDirections &= ~direction;
        if (frame.IsRewinding)
            return;

        BmsLongNote? held = null;
        for (var i = 0; i < Notes.PendingTails.Count; i++)
        {
            var slot = Notes.PendingTails[i];
            var longNote = holds.GetNote(slot);
            if (!GetLongNoteState(longNote).IsHolding)
                continue;

            var note = longNote;
            if (held == null || note.EndTime < held.EndTime)
                held = note;
        }

        if (held != null && holds.Release(noteSlots[held], new BmsReleaseContext(inputTime, frame, reverseScratch)))
            noteOutput.PlayTailSample(held);
    }

    internal void UpdateRegisteredLongNote(BmsLongNote note, in BmsFrameContext frame, double elapsed, bool holding)
        => holds.Finish(noteSlots[note], frame, elapsed, holding);

    internal void UpdateRegisteredBodies(in BmsFrameContext frame, double elapsed, bool holding)
    {
        for (var i = 0; i < Notes.ActiveBodies.Count;)
        {
            var slot = Notes.ActiveBodies[i];
            holds.UpdateBody(slot, frame, elapsed, holding);
            // Ending a body removes only that member while preserving the next object's turn.
            if (i < Notes.ActiveBodies.Count && Notes.ActiveBodies[i] == slot)
                i++;
        }
    }


    internal int GetNoteSlot(BmsHitObject note) => noteSlots[note];

    internal double NextBodyTime(bool holding)
    {
        var next = double.PositiveInfinity;
        for (var i = 0; i < Notes.ActiveBodies.Count; i++)
            next = Math.Min(next, holds.NextBodyTime(Notes.ActiveBodies[i], holding));
        return next;
    }

    internal BmsLandmineState GetLandmineState(BmsLandmine note) => landmines.GetLandmineState(note);

    internal void UpdateLandmines(in BmsFrameContext frame, bool holding) => landmines.UpdateLandmines(frame, holding);

    internal double NextAutomaticTime => nextAutomaticEvent.Time;

    internal void ProcessAutomatic(in BmsFrameContext frame)
    {
        while (automaticEvents.Count > 0 && nextAutomaticEvent.Time <= frame.SimulationTime)
        {
            var note = chartObjects[nextAutomaticEvent.Order];
            if (note is BmsLandmine mine)
                landmines.UpdateLandmine(mine, frame, PressedDirections != 0);
            else
            {
                CheckRegisteredNotePassive(note, frame);
                if (note is BmsLongNote longNote)
                {
                    UpdateRegisteredLongNote(longNote, frame, 0, PressedDirections != 0);
                }
            }

            refreshAutomaticEvent(note);
        }
    }

    private void refreshAutomaticEvent(BmsHitObject note)
    {
        if (!chartOrders.TryGetValue(note, out var order)) return;

        automaticEvents.Remove((automaticTimes[order], order));
        var next = note switch
        {
            BmsLandmine mine => landmines.GetLandmineState(mine).HandledTime.HasValue ? double.PositiveInfinity : mine.StartTime,
            BmsLongNote => holds.NextEventTime(noteSlots[note]),
            _ => noteOutput.GetMainResult(note).HasResult
                ? double.PositiveInfinity
                : BmsJudgementWindowTable.TimeAfter(note.StartTime, NoteWindows[noteSlots[note]].PassivePoorOffset),
        };
        automaticTimes[order] = next;
        if (double.IsFinite(next)) automaticEvents.Add((next, order));
        nextAutomaticEvent = automaticEvents.Count > 0 ? automaticEvents.Min : (double.PositiveInfinity, -1);
    }
}
