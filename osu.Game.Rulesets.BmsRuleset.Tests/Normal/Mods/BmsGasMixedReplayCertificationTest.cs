using System.Linq;
using NUnit.Framework;
using osu.Framework.Timing;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps.Objects;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Judgement;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Judgement.Framework;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Mods.Gauge;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Replays;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring.Gauge;
using osu.Game.Rulesets.BmsRuleset.IO.Input;
using osu.Game.Rulesets.BmsRuleset.Tests.Normal.Gameplay.Judgement;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Mods;

[TestFixture]
public class BmsGasMixedReplayCertificationTest
{
    [Test]
    public void BadRecoveryAndChargeEndpointsMatchReplayAtIntermediateStates()
    {
        var beatmap = new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K, TotalColumns = 8, Rank = 2, Total = 300, LockedLongNoteMode = BmsLongNoteMode.ChargeNote };
        BmsHitObject[] notes = [
            new BmsNote { Beatmap = beatmap, StartTime = 1000, Column = 1 },
            new BmsLongNote { Beatmap = beatmap, StartTime = 2000, Duration = 500, Column = 2 },
            new BmsNote { Beatmap = beatmap, StartTime = 3000, Column = 1 },
        ];
        beatmap.HitObjects.AddRange(notes);
        var gas = new BmsModGaugeAutoShift();
        gas.GasMode.Value = BmsGasLowerLimit.Normal;
        gas.GaugeProfileFamily.Value = BmsGaugeProfileFamily.Lr2;
        var source = new ScoreInfo { Mods = [gas] };
        var replay = new osu.Game.Replays.Replay { HasReceivedAllFrames = true };
        replay.Frames.Add(new BmsReplayFrame(0));
        replay.Frames.Add(new BmsReplayFrame(850, BmsAction.Key1));
        replay.Frames.Add(new BmsReplayFrame(851));
        replay.Frames.Add(new BmsReplayFrame(2000, BmsAction.Key2));
        replay.Frames.Add(new BmsReplayFrame(2500));
        replay.Frames.Add(new BmsReplayFrame(3000, BmsAction.Key1));
        replay.Frames.Add(new BmsReplayFrame(3001));
        replay.Frames.Add(new BmsReplayFrame(3201));
        var simulated = BmsReplaySimulation.Run(beatmap, replay, source);

        var manualClock = new ManualClock();
        var clock = new FramedClock(manualClock);
        using var score = new BmsScoreProcessor { Clock = clock };
        using var health = new BmsHealthProcessor { Clock = clock };
        score.ApplyBeatmap(beatmap);
        health.ApplyBeatmap(beatmap);
        gas.ApplyToHealthProcessor(health);
        var output = new BmsRecordingJudgementOutput();
        output.BeforeResultCheckpoint = (note, frame) =>
        {
            BmsJudgementResult result = note is BmsLongNote hold
                ? new BmsLongNoteJudgementResult(note, note.CreateJudgement(), output.GetEndpoints(hold))
                : new BmsJudgementResult(note, note.CreateJudgement());
            result.Type = output.GetMainResult(note).Type;
            result.SetTiming(frame.SimulationTime, frame.GameplayRate);
            score.ApplyResult(result);
            health.ApplyResult(result);
        };
        output.SyntheticEndpoint = (endpoint, after) =>
        {
            if (!after) health.ApplySyntheticLongNoteEndpoint(score.ApplySyntheticLongNoteEndpoint(endpoint));
        };
        var engine = new BmsJudgementEngine(
            new BmsJudgementColumnDefinition([notes[0], notes[2]], beatmap.LayoutVariant, 1, output),
            new BmsJudgementColumnDefinition([notes[1]], beatmap.LayoutVariant, 2, output));
        var damaged = 0d;
        for (var time = 0; time <= 3201; time++)
        {
            manualClock.CurrentTime = time;
            clock.ProcessFrame();
            if (time is 850 or 3000) engine.QueueInput(time, 1, true, false);
            if (time is 851 or 3001) engine.QueueInput(time, 1, false, false);
            if (time == 2000) engine.QueueInput(time, 2, true, false);
            if (time == 2500) engine.QueueInput(time, 2, false, false);
            engine.ProcessFrame(new BmsFrameContext(time, 1), time == 0 ? 0 : 1);
            if (time is not (850 or 2000 or 2500 or 3000 or 3201)) continue;
            var expected = simulated.Gauges.Last(g => g.Time <= time);
            Assert.That(health.CurrentGaugeStates, Is.EqualTo(expected.States), "health at " + time);
            Assert.That(health.GaugeType, Is.EqualTo(expected.ActiveGaugeType), "active at " + time);
            var replayPrefix = simulated.Judgements.Where(j => j.TimingObservations.Max(t => t.ActualTime) <= time).ToArray();
            Assert.That(score.JudgementEvents.Select(j => j.Result), Is.EqualTo(replayPrefix.Select(j => j.Result)), "events at " + time);
            Assert.That(BmsExScore.CreateProgression(score.JudgementEvents), Is.EqualTo(BmsExScore.CreateProgression(replayPrefix)), "EX at " + time);
            if (time == 850)
            {
                Assert.That(health.GaugeType, Is.EqualTo(BmsGaugeType.Hard));
                Assert.That(health.CurrentGaugeStates.Where(s => s.GaugeType is BmsGaugeType.Hazard or BmsGaugeType.ExHard).All(s => s.Failed), Is.True);
                damaged = health.Health.Value;
            }
        }
        Assert.That(health.Health.Value, Is.GreaterThan(damaged));
        Assert.That(score.JudgementEvents.Select(j => j.Result), Is.EqualTo(new[] { HitResult.Ok, HitResult.Perfect, HitResult.Perfect, HitResult.Perfect }));
        Assert.That(BmsExScore.CreateProgression(score.JudgementEvents).Last(), Is.EqualTo(6));
        Assert.That(health.HasPassedAtEnd(), Is.True);
        gas.ApplyToScore(source);
        Assert.That(source.Mods.OfType<BmsModGauge>().Single().GaugeType, Is.EqualTo(BmsGaugeType.Hard));
        Assert.That(BmsReplaySimulation.Run(beatmap, replay, source).Gauges.Last().States, Is.EqualTo(health.CurrentGaugeStates));
    }
}
