using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Game.IO;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps.Objects;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Judgement;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Mods;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Replays;
using osu.Game.Rulesets.BmsRuleset.IO.Input;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Gameplay.Judgement;

[TestFixture]
public class BmsJudgeRateCompatibilityTest
{
    public static IEnumerable<TestCaseData> RateCases()
    {
        yield return test("rank", "#RANK 3\n#00111:0101", [1, 1], "fixed");
        yield return test("global-nonstandard", "#DEFEXRANK 143\n#00111:0101", [1.07, 1.07], "fixed");
        yield return test("bare-global-alias", "#EXRANK 150\n#00111:0101", [1.12, 1.12], "fixed");
        yield return test("unused-definition", "#EXRANKAA 200\n#00111:0101", [.75, .75], "fixed");
        yield return test("redundant-events", "#EXRANKAA 100\n#001A0:AA00AA00\n#00111:01010101", [.75, .75, .75, .75], "fixed");
        yield return test("undefined-reference", "#EXRANKAA 200\n#001A0:AA00CC00\n#00111:01010101", [1.5, 1.5, 1.5, 1.5], "fixed");
        yield return test("before-first-head", "#EXRANKAA 200\n#000A0:AA\n#00111:0101", [1.5, 1.5], "fixed");
        yield return test("after-last-head", "#EXRANKAA 200\n#00111:0101\n#002A0:AA", [.75, .75], "fixed");
        yield return test("no-head-during-change", "#EXRANKAA 200\n#EXRANKBB 100\n#001A0:00AABB00\n#00111:01000001", [.75, .75], "fixed");
        yield return test("actual-change", "#EXRANKAA 200\n#EXRANKBB 50\n#001A0:AA00BB00\n#00111:01010101", [1.5, 1.5, .37, .37], "variable");
        yield return test("empty", "#EXRANKAA 200\n#001A0:AA", [], "empty");
        yield return test("event-inside-only-hold", "#EXRANKAA 200\n#00151:01000001\n#001A0:00AA0000", [.75], "fixed");
        yield return test("hold-and-later-head", "#EXRANKAA 200\n#00151:01000001\n#001A0:00AA0000\n#00211:01", [.75, 1.5], "variable");
        yield return test("same-tick-last-wins", "#EXRANKAA 200\n#EXRANKBB 50\n#001A0:AA\n#001A0:BB\n#00111:0101", [.37, .37], "fixed");

        static TestCaseData test(string name, string chart, double[] rates, string classification)
            => new TestCaseData(chart, rates, classification).SetName("JudgeRate_" + name);
    }

    [TestCaseSource(nameof(RateCases))]
    public void ParsedHeadAssignmentsDefineContextWithoutReinterpretingEvents(string chart, double[] expectedRates, string expectedClassification)
    {
        var beatmap = decode(chart);
        var notes = beatmap.HitObjects.Where(note => note is BmsNote or BmsLongNote).ToArray();
        var rates = notes.Select(note => note.EffectiveJudgementRate).ToArray();
        Assert.That(rates, Is.EqualTo(expectedRates).Within(1e-12));
        var classification = rates.Length == 0 ? "empty" : rates.Distinct().Count() == 1 ? "fixed" : "variable";
        Assert.That(classification, Is.EqualTo(expectedClassification));
        var engine = new BmsJudgementEngine(notes, beatmap.LayoutVariant, 1, new BmsRecordingJudgementOutput());
        Assert.That(engine.HasFixedJudge, Is.EqualTo(expectedClassification != "variable"));
        if (notes.FirstOrDefault() is BmsLongNote hold)
        {
            Assert.That(hold.Duration, Is.EqualTo(1500));
            Assert.That(BmsJudgementProfileProvider.GetTable(beatmap.LayoutVariant, hold.Column, hold.EffectiveJudgementRate, true)
                .SlowWindowFor(HitResult.Perfect), Is.EqualTo(100), "The tail retains the captured head rate across a later A0 event.");
        }
    }

    [Test]
    public void VariableRateReplayPreservesAssignedRatesAndPrefixResults()
    {
        var beatmap = decode("#EXRANKAA 200\n#EXRANKBB 50\n#001A0:AA00BB00\n#00111:01010101");
        var notes = beatmap.HitObjects.OfType<BmsNote>().ToArray();
        var before = notes.Select(note => note.JudgementRate).ToArray();
        var replay = new osu.Game.Replays.Replay { HasReceivedAllFrames = true };
        replay.Frames.Add(new BmsReplayFrame(0) { Algorithm = BmsJudgementAlgorithm.Duration });
        foreach (var note in notes)
        {
            replay.Frames.Add(new BmsReplayFrame(note.StartTime + 5, BmsAction.Key1));
            replay.Frames.Add(new BmsReplayFrame(note.StartTime + 6));
        }
        replay.Frames.Add(new BmsReplayFrame(notes[^1].StartTime + 250));
        var full = BmsReplaySimulation.Run(beatmap, replay, new ScoreInfo());
        Assert.That(full.Judgements.Select(item => item.Result), Is.EqualTo(Enumerable.Repeat(HitResult.Perfect, 4)));
        Assert.That(full.Judgements.SelectMany(item => item.TimingObservations).Select(item => item.TimeOffset), Is.EqualTo(new[] { 5d, 5, 5, 5 }));
        var prefix = new osu.Game.Replays.Replay { HasReceivedAllFrames = true,
            Frames = replay.Frames.Where(frame => frame.Time <= notes[1].StartTime + 6).ToList() };
        var restoredPrefix = BmsReplaySimulation.Run(beatmap, prefix, new ScoreInfo { Passed = false });
        Assert.That(restoredPrefix.Judgements.SelectMany(item => item.TimingObservations),
            Is.EqualTo(full.Judgements.Take(2).SelectMany(item => item.TimingObservations)));
        Assert.That(notes.Select(note => note.JudgementRate), Is.EqualTo(before));
        Assert.That(before, Is.EqualTo(new[] { 1.5, 1.5, .37, .37 }).Within(1e-12));
    }

    [TestCase(false, 10, HitResult.Perfect)]
    [TestCase(false, 30, HitResult.Great)]
    [TestCase(true, 10, HitResult.Perfect)]
    public void FixedModifiedLateHeadReplayReachesItsTail(bool noGreat, double offset, HitResult expectedHead)
    {
        var beatmap = new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K, TotalColumns = 8,
            Rank = 3, LockedLongNoteMode = BmsLongNoteMode.ChargeNote };
        beatmap.HitObjects.Add(new BmsLongNote { Beatmap = beatmap, Column = 1, StartTime = 1000, Duration = 500 });
        var replay = new osu.Game.Replays.Replay { HasReceivedAllFrames = true,
            Frames = [new BmsReplayFrame(0), new BmsReplayFrame(1000 + offset, BmsAction.Key1), new BmsReplayFrame(1500), new BmsReplayFrame(1800)] };
        var result = BmsReplaySimulation.Run(beatmap, replay, new ScoreInfo
        {
            Passed = true, Mods = noGreat ? [new BmsModNoGreat()] : [new BmsModNoGood()],
        });
        // Head expectations are independently captured by positive-long-* Java vectors.
        // Tail completion and replay application are separate C# state-continuity checks.
        Assert.That(result.Judgements.Select(item => item.Result), Is.EqualTo(new[] { expectedHead, HitResult.Perfect }));
        Assert.That(result.Judgements.SelectMany(item => item.TimingObservations).Select(item => item.TimeOffset), Is.EqualTo(new[] { offset, 0 }));
    }

    private static BmsBeatmap decode(string chart)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("#BPM 120\n" + chart + "\n"));
        using var reader = new LineBufferedReader(stream);
        var decoded = new BmsBeatmapDecoder().Decode(reader);
        return (BmsBeatmap)new BmsBeatmapConverter(decoded, new BmsRuleset()).Convert();
    }
}
