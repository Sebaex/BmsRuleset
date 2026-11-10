using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps.Objects;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Judgement;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Gameplay.Judgement;

[TestFixture]
public class BmsFixedJudgeJavaParityTest
{
    public static IEnumerable<TestCaseData> JavaCases()
    {
        using var stream = typeof(BmsFixedJudgeJavaParityTest).Assembly.GetManifestResourceStream(
            "osu.Game.Rulesets.BmsRuleset.Tests.reference_fixtures.fixed_judge_parity.selection.csv")
                           ?? throw new InvalidOperationException("Missing fixed-judge Java oracle.");
        using var reader = new StreamReader(stream);
        var rows = reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Skip(1).ToArray();
        Assert.That(rows, Has.Length.EqualTo(536));
        foreach (var line in rows)
        {
            var row = line.Split(',');
            yield return new TestCaseData(new object[] { row }).SetName($"FixedJudge_{string.Join('_', row.Take(6))}");
        }
    }

    [Test]
    public void VariableChartKeepsExistingNoGoodTieOrderAcrossColumns()
    {
        using var scope = BmsJudgementProfileProvider.UseWindowMods([new BmsModNoGood()]);
        var beatmap = new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K, Rank = 3 };
        var notes = new[] { 1000d, 1000, 2000, 2000 }.Select(start => new BmsNote { Beatmap = beatmap, Column = 1, StartTime = start }).ToArray();
        var other = new BmsNote { Beatmap = beatmap, Column = 2, StartTime = 6000, JudgementRate = .75 };
        var output = new BmsRecordingJudgementOutput();
        var engine = new BmsJudgementEngine(new(notes, beatmap.LayoutVariant, 1, output),
            new([other], beatmap.LayoutVariant, 2, new BmsRecordingJudgementOutput()));
        var order = new List<int>();
        output.BeforeResultCheckpoint += (note, _) => order.Add(Array.IndexOf(notes, note));
        engine.GetColumn(1).HandleNotePress(new BmsPressContext(1030, new BmsFrameContext(1030, 1)), BmsJudgementAlgorithm.Combo);
        engine.GetColumn(1).HandleNotePress(new BmsPressContext(2030, new BmsFrameContext(2030, 1)), BmsJudgementAlgorithm.Combo);
        Assert.That(order, Is.EqualTo(new[] { 1, 3 }), "The different rate in another column preserves the existing variable-chart policy.");
        Assert.That(output.GetMainResult(notes[0]).HasResult || output.GetMainResult(notes[2]).HasResult, Is.False);
    }

    [Test]
    public void ReconfigurationRefreshesFixedJudgeContextOnlyWhenRateChanges()
    {
        using var scope = BmsJudgementProfileProvider.UseWindowMods([new BmsModNoGood()]);
        var beatmap = new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K, Rank = 3 };
        var notes = new[] { 1000d, 1000, 2000, 2000, 3000, 3000 }.Select(start => new BmsNote { Beatmap = beatmap, Column = 1, StartTime = start }).ToArray();
        var other = new BmsNote { Beatmap = beatmap, Column = 2, StartTime = 6000, JudgementRate = .75 };
        var output = new BmsRecordingJudgementOutput();
        var engine = new BmsJudgementEngine(new(notes, beatmap.LayoutVariant, 1, output),
            new([other], beatmap.LayoutVariant, 2, new BmsRecordingJudgementOutput()));
        var order = new List<int>();
        output.BeforeResultCheckpoint += (note, _) => order.Add(Array.IndexOf(notes, note));
        Assert.That(engine.HasFixedJudge, Is.False);
        engine.GetColumn(1).HandleNotePress(new BmsPressContext(1030, new BmsFrameContext(1030, 1)));
        other.JudgementRate = 1;
        engine.GetColumn(2).RefreshNoteConfiguration(other);
        Assert.That(engine.HasFixedJudge, Is.True);
        engine.GetColumn(1).HandleNotePress(new BmsPressContext(2030, new BmsFrameContext(2030, 1)));
        other.JudgementRate = .75;
        engine.GetColumn(2).RefreshNoteConfiguration(other);
        Assert.That(engine.HasFixedJudge, Is.False);
        engine.GetColumn(1).HandleNotePress(new BmsPressContext(3030, new BmsFrameContext(3030, 1)));
        Assert.That(order, Is.EqualTo(new[] { 1, 2, 5 }));
    }

    [Test]
    public void VariablePrimaryRemainsStableWhileDeferredCollectorIsReported()
    {
        var beatmap = new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K, Rank = 3 };
        var notes = new[] { new BmsNote { Beatmap = beatmap, Column = 1, StartTime = 1000, JudgementRate = .05 },
            new BmsNote { Beatmap = beatmap, Column = 1, StartTime = 1150, JudgementRate = 1 } };
        var output = new BmsRecordingJudgementOutput();
        var engine = new BmsJudgementEngine(notes, beatmap.LayoutVariant, 1, output);
        Assert.That(engine.HasFixedJudge, Is.False);
        var ownWindow = BmsJudgementProfileProvider.GetTable(beatmap.LayoutVariant, 1, notes[1].EffectiveJudgementRate, false);
        Assert.That(ownWindow.ResultForOffset(-120), Is.EqualTo(HitResult.Good));
        engine.GetColumn(1).HandleNotePress(new BmsPressContext(1030, new BmsFrameContext(1030, 1)), BmsJudgementAlgorithm.Duration);
        Assert.That(output.GetMainResult(notes[0]).Type, Is.EqualTo(HitResult.Ok));
        // The known additional-result defect is diagnostic evidence, never a blessed expected result.
        TestContext.WriteLine($"JF-VARIABLE-01 secondOwn=Good secondObserved={output.GetMainResult(notes[1]).Type} consumed={output.GetMainResult(notes[1]).HasResult}");
    }

    [TestCaseSource(nameof(JavaCases))]
    public void SelectionAndRuntimeApplicationMatchOriginalJavaComponents(string[] row)
    {
        var scratch = bool.Parse(row[3]);
        var columnIndex = scratch ? 0 : 1;
        var parameter = int.Parse(row[2], CultureInfo.InvariantCulture);
        var beatmap = new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K, Rank = row[1] == "rank" ? parameter : 2,
            ExRank = row[1] == "exrank" ? parameter : null, LockedLongNoteMode = BmsLongNoteMode.ChargeNote };
        IApplicableToJudgementWindow[] mods = row[4] switch
        {
            "no-good" => [new BmsModNoGood()],
            "no-great" => [new BmsModNoGreat()],
            _ => [],
        };
        using var scope = BmsJudgementProfileProvider.UseWindowMods(mods);
        var algorithm = Enum.Parse<BmsJudgementAlgorithm>(row[5]);
        var time = long.Parse(row[6], CultureInfo.InvariantCulture) / 1000d;
        var times = row[8].Split('|').Select(value => long.Parse(value, CultureInfo.InvariantCulture) / 1000d).ToArray();
        var notes = times.Select((start, i) =>
        {
            BmsHitObject note = row[7][i] == 'L' ? new BmsLongNote { Duration = 500 } : new BmsNote();
            note.Beatmap = beatmap; note.Column = columnIndex; note.StartTime = start;
            return note;
        }).ToArray();
        var candidates = notes.Select((note, i) => new BmsJudgementCandidate(note.StartTime, note.GetEndTime(), columnIndex,
            note.EffectiveJudgementRate, note is BmsLongNote, row[9][i] == '1')).ToArray();
        var primary = int.Parse(row[10], CultureInfo.InvariantCulture);
        var javaJudge = int.Parse(row[11], CultureInfo.InvariantCulture);
        var expectedResult = javaJudge switch { 0 => HitResult.Perfect, 1 => HitResult.Great, 2 => HitResult.Good, 3 => HitResult.Ok, 5 => HitResult.Miss, _ => HitResult.None };
        var selected = BmsJudgementSelector.SelectPress(beatmap.LayoutVariant, columnIndex, candidates, time, algorithm);
        // The public selection is a value: identical stacked candidates have no distinct identity.
        // The real runtime's result-order assertion below verifies the consumed object identity.
        Assert.That(selected.Candidate, Is.EqualTo(primary < 0 ? (BmsJudgementCandidate?)null : candidates[primary]));
        Assert.That(selected.Result, Is.EqualTo(expectedResult));
        Assert.That(selected.IsEmptyPoor, Is.EqualTo(javaJudge == 5));

        var output = new BmsRecordingJudgementOutput();
        var column = new BmsJudgementEngine(notes, beatmap.LayoutVariant, columnIndex, output).GetColumn(columnIndex);
        for (var i = 0; i < notes.Length; i++)
            if (row[9][i] == '1') column.TryPressRegisteredNote(notes[i], HitResult.Perfect, new BmsPressContext(notes[i].StartTime, new BmsFrameContext(notes[i].StartTime, 1)));
        var order = new List<int>();
        output.BeforeResultCheckpoint += (note, _) => order.Add(Array.IndexOf(notes, note));
        column.HandleNotePress(new BmsPressContext(time, new BmsFrameContext(time, 1)), algorithm);
        var extras = primary < 0 || row[12] == "-" ? [] : row[12].Split('|').Select(value => int.Parse(value, CultureInfo.InvariantCulture)).ToArray();
        var expectedOrder = javaJudge < 4 ? extras.Append(primary).ToArray() : extras;
        Assert.That(order, Is.EqualTo(expectedOrder));
        foreach (var index in extras)
        {
            Assert.That(output.GetMainResult(notes[index]).Type, Is.EqualTo(HitResult.Ok));
            if (notes[index] is BmsLongNote hold)
                Assert.That(column.GetLongNoteState(hold).IsHolding, Is.False, "An additional BAD head cannot start a hold.");
        }
        for (var i = 0; i < notes.Length; i++)
            Assert.That(output.GetMainResult(notes[i]).HasResult, Is.EqualTo(row[9][i] == '1' || expectedOrder.Contains(i)));
    }
}
