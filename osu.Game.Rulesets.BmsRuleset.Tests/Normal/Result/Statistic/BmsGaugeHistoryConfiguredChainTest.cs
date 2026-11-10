using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps.Objects;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Mods.Gauge;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring.Gauge;
using osu.Game.Rulesets.BmsRuleset.UI.Result.Statistic;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Result.Statistic;

[TestFixture]
public class BmsGaugeHistoryConfiguredChainTest
{
    [Test]
    public void LegacyGraphIncludesTheConfiguredGameplayTiers([Values] BmsGasLowerLimit lower)
    {
        var beatmap = new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K, Total = 200 };
        var note = new BmsNote { Beatmap = beatmap, StartTime = 1000, Column = 1 };
        beatmap.HitObjects.Add(note);
        var gas = new BmsModGaugeAutoShift();
        gas.GasMode.Value = lower;
        using var health = new BmsHealthProcessor();
        health.ApplyBeatmap(beatmap);
        gas.ApplyToHealthProcessor(health);
        var expected = health.CurrentGaugeStates.Select(s => s.GaugeType == BmsGaugeType.AssistEasy ? "Assist Easy" : s.GaugeType.ToString());
        var source = new ScoreInfo
        {
            Mods = [gas],
            HitEvents = [new HitEvent(0, 1, HitResult.Perfect, note, null, null)],
        };
        Assert.That(BmsGaugeHistoryGraph.CreateSeries(source, beatmap).Select(s => s.Name), Is.EqualTo(expected));
    }
}
