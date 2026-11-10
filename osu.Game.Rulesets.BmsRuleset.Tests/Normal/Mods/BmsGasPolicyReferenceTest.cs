using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Mods.Gauge;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring.Gauge;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Mods;

[TestFixture]
public class BmsGasPolicyReferenceTest
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void BestClearSelectionMatchesJavaWithinConfiguredChain(int scenario)
    {
        foreach (var lower in Enum.GetValues<BmsGasLowerLimit>())
        {
            using var health = new BmsHealthProcessor();
            health.ApplyBeatmap(new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K });
            var gas = new BmsModGaugeAutoShift();
            gas.GasMode.Value = lower;
            gas.ApplyToHealthProcessor(health);
            var states = health.CurrentGaugeStates.Select(s =>
            {
                var hp = s.GaugeType switch
                {
                    BmsGaugeType.Hard when scenario == 0 => 0.4,
                    BmsGaugeType.Normal when scenario == 3 => 0.8,
                    BmsGaugeType.Easy when scenario == 2 => 0.81,
                    BmsGaugeType.AssistEasy when scenario == 4 => 0.65,
                    <= BmsGaugeType.Normal => 0.2,
                    _ => 0,
                };
                return s with { Health = hp, Failed = hp == 0 };
            }).ToArray();
            health.RestoreGaugeStates(states);

            // BMSPlayer.java:784-795: ascend through the eligible range, keeping the last qualified type.
            var expected = states.OrderBy(s => s.GaugeType).First().GaugeType;
            var passed = false;
            foreach (var state in states.OrderBy(s => s.GaugeType))
            {
                var border = state.GaugeType switch
                {
                    BmsGaugeType.AssistEasy => 0.6,
                    BmsGaugeType.Easy or BmsGaugeType.Normal => 0.8,
                    _ => 0,
                };
                if (state.Health > 0 && state.Health >= border)
                {
                    expected = state.GaugeType;
                    passed = true;
                }
            }
            Assert.That(health.GaugeType, Is.EqualTo(expected), $"{lower}/{scenario}");
            Assert.That(health.HasPassedAtEnd(), Is.EqualTo(passed));
            Assert.That(health.WorstGaugeType, Is.EqualTo(expected));
        }
    }
}
