using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps.Objects;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Mods.Gauge;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Replays;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring.Gauge;
using osu.Game.Rulesets.BmsRuleset.IO.Input;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Mods;

[TestFixture]
public class BmsGasCourseParityTest
{
    [Test]
    public void FailedCourseLayersDoNotRecoverInGameplayOrReplay()
    {
        var beatmap = new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K, TotalColumns = 8, Rank = 2, Total = 300 };
        var notes = new[] { 1000d, 2000d }.Select(t => new BmsNote { Beatmap = beatmap, Column = 1, StartTime = t }).ToArray();
        beatmap.HitObjects.AddRange(notes);
        var gas = new BmsModGaugeAutoShift();
        gas.GaugeProfileFamily.Value = BmsGaugeProfileFamily.Lr2;
        gas.ClassHealth.Value = gas.ExClassHealth.Value = gas.ExHardClassHealth.Value = 0.03;
        using var health = new BmsHealthProcessor();
        health.ApplyBeatmap(beatmap);
        gas.ApplyToHealthProcessor(health);
        health.ApplyResult(new JudgementResult(notes[0], notes[0].CreateJudgement()) { Type = HitResult.Ok });
        Assert.That(health.CurrentGaugeStates.All(s => s.Failed && s.Health == 0), Is.True);
        health.ApplyResult(new JudgementResult(notes[1], notes[1].CreateJudgement()) { Type = HitResult.Perfect });
        Assert.That(health.HasPassedAtEnd(), Is.False);
        var replay = new osu.Game.Replays.Replay { HasReceivedAllFrames = true };
        replay.Frames.Add(new BmsReplayFrame(0));
        replay.Frames.Add(new BmsReplayFrame(850, BmsAction.Key1));
        replay.Frames.Add(new BmsReplayFrame(851));
        replay.Frames.Add(new BmsReplayFrame(2000, BmsAction.Key1));
        replay.Frames.Add(new BmsReplayFrame(2001));
        var source = new ScoreInfo { Mods = [gas] };
        gas.ApplyToScore(source);
        Assert.That(BmsReplaySimulation.Run(beatmap, replay, source).Gauges.Last().States, Is.EqualTo(health.CurrentGaugeStates));
    }

    [Test]
    public void CourseAndOrdinaryAttributionMatchReplay(
        [Values(BmsGaugeType.Easy, BmsGaugeType.Hard, BmsGaugeType.Class, BmsGaugeType.ExClass, BmsGaugeType.ExHardClass)] BmsGaugeType type,
        [Values] bool course, [Values] bool gasFirst)
    {
        var beatmap = new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K, TotalColumns = 8, Rank = 2, Total = 300 };
        var notes = new[] { 1000d, 2000d }.Select(t => new BmsNote { Beatmap = beatmap, Column = 1, StartTime = t }).ToArray();
        beatmap.HitObjects.AddRange(notes);
        var gas = new BmsModGaugeAutoShift();
        if (course)
        {
            // BmsCourseSession serialises these seeds onto GAS before gameplay and replay.
            gas.GaugeProfileFamily.Value = BmsGaugeProfileFamily.Lr2;
            gas.ClassHealth.Value = 0.4;
            gas.ExClassHealth.Value = 0.15;
            gas.ExHardClassHealth.Value = 0.1;
        }
        var ordinary = BmsModGauge.CreateForType(type)!;
        Mod[] mods = gasFirst ? [gas, ordinary] : [ordinary, gas];
        using var actual = new BmsHealthProcessor();
        actual.ApplyBeatmap(beatmap);
        foreach (var mod in mods.OfType<IApplicableToHealthProcessor>()) mod.ApplyToHealthProcessor(actual);
        using var expected = new BmsHealthProcessor();
        expected.ApplyBeatmap(beatmap);
        ((BmsModGaugeAutoShift)gas.DeepClone()).ApplyToHealthProcessor(expected);
        Assert.That(actual.CurrentGaugeStates, Is.EqualTo(expected.CurrentGaugeStates), "Initial chain and seeds");

        var replay = new osu.Game.Replays.Replay { HasReceivedAllFrames = true };
        replay.Frames.Add(new BmsReplayFrame(0));
        replay.Frames.Add(new BmsReplayFrame(850, BmsAction.Key1));
        replay.Frames.Add(new BmsReplayFrame(851));
        var source = new ScoreInfo { Mods = mods };
        actual.ApplyResult(new JudgementResult(notes[0], notes[0].CreateJudgement()) { Type = HitResult.Ok });
        var prefix = BmsReplaySimulation.Run(beatmap, replay, source);
        Assert.That(prefix.Gauges.Last().States, Is.EqualTo(actual.CurrentGaugeStates), "Damage transition");
        Assert.That(prefix.Gauges.Last().ActiveGaugeType, Is.EqualTo(actual.GaugeType));
        if (course)
        {
            var explicitStates = BmsReplaySimulation.Run(beatmap, replay, source,
                courseGaugeStates: expected.CurrentGaugeStates, courseGaugeProfileFamily: BmsGaugeProfileFamily.Lr2);
            Assert.That(explicitStates.Gauges.Last().States, Is.EqualTo(actual.CurrentGaugeStates));
        }
        actual.ApplyResult(new JudgementResult(notes[1], notes[1].CreateJudgement()) { Type = HitResult.Perfect });
        replay.Frames.Add(new BmsReplayFrame(2000, BmsAction.Key1));
        replay.Frames.Add(new BmsReplayFrame(2001));
        var full = BmsReplaySimulation.Run(beatmap, replay, source);
        Assert.That(full.Gauges.Last().States, Is.EqualTo(actual.CurrentGaugeStates), "Recovery transition");
        Assert.That(actual.HasPassedAtEnd(), Is.True);
        gas.ApplyToScore(source);
        Assert.That(source.Mods.OfType<BmsModGauge>().Single().GaugeType, Is.EqualTo(actual.WorstGaugeType));
        var attributed = BmsReplaySimulation.Run(beatmap, replay, source);
        Assert.That(attributed.Gauges.Last().States, Is.EqualTo(actual.CurrentGaugeStates));
        if (course)
        {
            Assert.That(actual.GaugeType, Is.EqualTo(BmsGaugeType.ExClass));
            Assert.That(actual.CurrentGaugeStates.Single(s => s.GaugeType == BmsGaugeType.ExHardClass).Failed, Is.True);
        }
    }
}
