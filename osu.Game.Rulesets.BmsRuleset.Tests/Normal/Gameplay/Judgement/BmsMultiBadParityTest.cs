using System.Linq;
using NUnit.Framework;
using osu.Game.Replays;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps.Objects;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Judgement;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Replays;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring;
using osu.Game.Rulesets.BmsRuleset.IO.Input;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Gameplay.Judgement;

[TestFixture]
public class BmsMultiBadParityTest
{
    [TestCase(BmsLongNoteMode.LongNote)]
    [TestCase(BmsLongNoteMode.ChargeNote)]
    [TestCase(BmsLongNoteMode.HellChargeNote)]
    public void AdditionalLongHeadIsConsumedWithoutStartingHold(BmsLongNoteMode mode)
    {
        var beatmap = new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K, Rank = 2, LockedLongNoteMode = mode };
        var first = new BmsNote { Beatmap = beatmap, Column = 1, StartTime = 1000 };
        var hold = new BmsLongNote { Beatmap = beatmap, Column = 1, StartTime = 1020, Duration = 1000 };
        var output = new BmsRecordingJudgementOutput();
        var column = new BmsJudgementEngine([first, hold], beatmap.LayoutVariant, 1, output).GetColumn(1);
        column.HandleNotePress(new BmsPressContext(850, new BmsFrameContext(850, 1)));
        Assert.That(output.MainResults, Is.EqualTo(2));
        Assert.That(column.GetLongNoteState(hold).HeadResult, Is.EqualTo(HitResult.Ok));
        Assert.That(column.GetLongNoteState(hold).IsHolding, Is.False);
        column.CheckRegisteredNotePassive(hold, new BmsFrameContext(2221, 1));
        column.UpdateRegisteredLongNote(hold, new BmsFrameContext(2221, 1), 1371, false);
        Assert.That(column.GetLongNoteState(hold).TailJudged, Is.True);
        Assert.That(output.SyntheticEndpoints.Count, Is.EqualTo(mode == BmsLongNoteMode.LongNote ? 0 : 1));
        if (mode != BmsLongNoteMode.LongNote)
            Assert.That(output.SyntheticEndpoints.Single().Result, Is.EqualTo(HitResult.Meh));
        beatmap.TotalColumns = 8;
        beatmap.Total = 300;
        beatmap.HitObjects.AddRange([first, hold]);
        var replay = new osu.Game.Replays.Replay { HasReceivedAllFrames = true };
        replay.Frames.Add(new BmsReplayFrame(0));
        replay.Frames.Add(new BmsReplayFrame(850, BmsAction.Key1));
        replay.Frames.Add(new BmsReplayFrame(851));
        replay.Frames.Add(new BmsReplayFrame(2221));
        var simulated = BmsReplaySimulation.Run(beatmap, replay, new ScoreInfo());
        Assert.That(simulated.Judgements.Count(j => j.Result == HitResult.Ok), Is.EqualTo(2));
        Assert.That(simulated.Judgements.Count(j => j.Result == HitResult.Meh), Is.EqualTo(mode == BmsLongNoteMode.LongNote ? 0 : 1));
        Assert.That(BmsExScore.CreateProgression(simulated.Judgements).Last(), Is.Zero);
        Assert.That(simulated.Gauges.Last().States.Single().Health,
            Is.EqualTo(mode == BmsLongNoteMode.LongNote ? 0.12 : mode == BmsLongNoteMode.ChargeNote ? 0.06 : 0.02).Within(1e-9));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void LongPrimaryOrGoodPrimaryLimitsAdditionalBadToEarlierNotes(bool longPrimary)
    {
        var beatmap = new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K, Rank = 2, LockedLongNoteMode = BmsLongNoteMode.ChargeNote };
        BmsHitObject primary = longPrimary
            ? new BmsLongNote { Beatmap = beatmap, Column = 1, StartTime = 1000, Duration = 1000 }
            : new BmsNote { Beatmap = beatmap, Column = 1, StartTime = 1000 };
        var later = new BmsNote { Beatmap = beatmap, Column = 1, StartTime = longPrimary ? 1020 : 1150 };
        var output = new BmsRecordingJudgementOutput();
        var column = new BmsJudgementEngine([primary, later], beatmap.LayoutVariant, 1, output).GetColumn(1);
        column.HandleNotePress(new BmsPressContext(longPrimary ? 850 : 1000, new BmsFrameContext(longPrimary ? 850 : 1000, 1)));
        Assert.That(output.MainResults, Is.EqualTo(1));
        Assert.That(output.GetMainResult(later).HasResult, Is.False);
    }

    [Test]
    public void BetterPrimaryStillConsumesPrecedingBadAndSkipsGoodNeighbours()
    {
        var beatmap = new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K, Rank = 2 };
        var notes = new[] { 1000d, 1080d, 1100d }.Select(t => new BmsNote { Beatmap = beatmap, Column = 1, StartTime = t }).ToArray();
        var output = new BmsRecordingJudgementOutput();
        var column = new BmsJudgementEngine(notes, beatmap.LayoutVariant, 1, output).GetColumn(1);
        column.HandleNotePress(new BmsPressContext(1150, new BmsFrameContext(1150, 1)));
        Assert.That(output.GetMainResult(notes[0]).Type, Is.EqualTo(HitResult.Ok));
        Assert.That(output.GetMainResult(notes[1]).Type, Is.EqualTo(HitResult.Good));
        Assert.That(output.GetMainResult(notes[2]).HasResult, Is.False);
        Assert.That(output.MainResults, Is.EqualTo(2));
    }

    // Java filter: normal BAD primary keeps both BAD candidates, including equal timestamps.
    [TestCase(850, 1020)]
    [TestCase(1150, 1020)]
    [TestCase(850, 1000)]
    public void TwoBadNotesAreConsumedByOnePress(double time, double secondTime)
    {
        foreach (var algorithm in System.Enum.GetValues<BmsJudgementAlgorithm>())
        {
            var beatmap = new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K, TotalColumns = 8, Rank = 2, Total = 300 };
            var first = new BmsNote { Beatmap = beatmap, Column = 1, StartTime = 1000 };
            var second = new BmsNote { Beatmap = beatmap, Column = 1, StartTime = secondTime };
            beatmap.HitObjects.AddRange([first, second]);
            using var score = new BmsScoreProcessor();
            using var health = new BmsHealthProcessor();
            score.ApplyBeatmap(beatmap);
            health.ApplyBeatmap(beatmap);
            score.Combo.Value = 3;
            var output = new BmsRecordingJudgementOutput();
            output.BeforeResultCheckpoint = (note, _) =>
            {
                var result = new JudgementResult(note, note.CreateJudgement()) { Type = output.GetMainResult(note).Type };
                score.ApplyResult(result);
                health.ApplyResult(result);
            };
            var column = new BmsJudgementEngine([first, second], beatmap.LayoutVariant, 1, output).GetColumn(1);
            column.HandleNotePress(new BmsPressContext(time, new BmsFrameContext(time, 1)), algorithm);
            Assert.That(output.MainResults, Is.EqualTo(2), algorithm.ToString());
            Assert.That(column.GetNoteCandidates(), Is.Empty);
            Assert.That(score.Statistics[HitResult.Ok], Is.EqualTo(2));
            Assert.That(score.Combo.Value, Is.Zero);
            Assert.That(BmsExScore.CreateProgression(score.JudgementEvents).Last(), Is.Zero);
            Assert.That(health.Health.Value, Is.EqualTo(0.12).Within(1e-9));
            var replay = new osu.Game.Replays.Replay { HasReceivedAllFrames = true };
            replay.Frames.Add(new BmsReplayFrame(0) { Algorithm = algorithm });
            replay.Frames.Add(new BmsReplayFrame(time, BmsAction.Key1));
            replay.Frames.Add(new BmsReplayFrame(time + 1));
            var simulated = BmsReplaySimulation.Run(beatmap, replay, new ScoreInfo());
            Assert.That(simulated.Judgements.Select(j => j.Result), Is.EqualTo(new[] { HitResult.Ok, HitResult.Ok }));
            Assert.That(simulated.Gauges.Last().States.Single().Health, Is.EqualTo(0.12).Within(1e-9));
            column.HandleNotePress(new BmsPressContext(time + 1, new BmsFrameContext(time + 1, 1)), algorithm);
            Assert.That(output.MainResults, Is.EqualTo(2));
        }
    }
}
