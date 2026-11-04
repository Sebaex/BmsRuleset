using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Mods.Gauge;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Mods;

[TestFixture]
public class BmsGasOrderParityTest
{
    [TestCase(BmsGasLowerLimit.Normal, false)]
    [TestCase(BmsGasLowerLimit.Normal, true)]
    [TestCase(BmsGasLowerLimit.Easy, false)]
    [TestCase(BmsGasLowerLimit.Easy, true)]
    [TestCase(BmsGasLowerLimit.Assist, false)]
    [TestCase(BmsGasLowerLimit.Assist, true)]
    public void TestGaugeAttributionDoesNotChangeGasChain(BmsGasLowerLimit limit, bool gasFirst)
    {
        foreach (var ordinary in new BmsModGauge[] { new BmsModEasyGauge(), new BmsModHardGauge() })
        {
            using var expected = new BmsHealthProcessor();
            var reference = new BmsModGaugeAutoShift();
            reference.GasMode.Value = limit;
            reference.ApplyToHealthProcessor(expected);
            using var actual = new BmsHealthProcessor();
            var gas = new BmsModGaugeAutoShift();
            gas.GasMode.Value = limit;
            IApplicableToHealthProcessor[] mods = gasFirst ? [gas, ordinary] : [ordinary, gas];
            foreach (var mod in mods)
                mod.ApplyToHealthProcessor(actual);
            Assert.That(actual.CurrentGaugeStates.Select(s => s.GaugeType), Is.EqualTo(expected.CurrentGaugeStates.Select(s => s.GaugeType)), $"{limit}/{gasFirst}/{ordinary.Acronym}");
            var beatmap = new osu.Game.Rulesets.BmsRuleset.Beatmaps.BmsBeatmap { LayoutVariant = osu.Game.Rulesets.BmsRuleset.BmsParser.BmsLayoutVariant.Bme7K, TotalColumns = 8, Total = 300 };
            var note = new osu.Game.Rulesets.BmsRuleset.Beatmaps.Objects.BmsNote { Beatmap = beatmap, StartTime = 1000, Column = 1 };
            beatmap.HitObjects.Add(note);
            actual.ApplyBeatmap(beatmap);
            actual.ApplyResult(new osu.Game.Rulesets.Judgements.JudgementResult(note, note.CreateJudgement()) { Type = osu.Game.Rulesets.Scoring.HitResult.Meh });
            var source = new osu.Game.Scoring.ScoreInfo { Mods = gasFirst ? [gas, ordinary] : [ordinary, gas] };
            var replay = new osu.Game.Replays.Replay { HasReceivedAllFrames = true };
            replay.Frames.Add(new osu.Game.Rulesets.BmsRuleset.Gameplay.Replays.BmsReplayFrame(0));
            replay.Frames.Add(new osu.Game.Rulesets.BmsRuleset.Gameplay.Replays.BmsReplayFrame(2000));
            var simulated = osu.Game.Rulesets.BmsRuleset.Gameplay.Replays.BmsReplaySimulation.Run(beatmap, replay, source);
            Assert.That(simulated.Gauges.Last().States, Is.EqualTo(actual.CurrentGaugeStates));
            actual.HasPassedAtEnd();
            gas.ApplyToScore(source);
            var attributed = osu.Game.Rulesets.BmsRuleset.Gameplay.Replays.BmsReplaySimulation.Run(beatmap, replay, source);
            Assert.That(attributed.Gauges.Last().States, Is.EqualTo(actual.CurrentGaugeStates));
        }
    }
}
