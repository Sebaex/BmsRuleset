using System;
using System.Globalization;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring.Gauge;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring;
using osu.Game.Rulesets.BmsRuleset.Tests.Normal.Gameplay.Judgement;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Gameplay.Gauge;

[TestFixture]
public class BmsExecutedJavaGaugeTest
{
    [Test]
    public void OriginalExScoreMatchesAggregatedEarlyAndLateCounts()
    {
        var rows = BmsExecutedJavaJudgementTest.ReadOracle("scores");
        Assert.That(rows, Has.Length.EqualTo(6));
        foreach (var row in rows)
        {
            int number(int index) => int.Parse(row[index], CultureInfo.InvariantCulture);
            var statistics = new Dictionary<HitResult, int>
            {
                [HitResult.Perfect] = number(0) + number(1),
                [HitResult.Great] = number(2) + number(3),
                [HitResult.Good] = 12, [HitResult.Ok] = 7, [HitResult.Meh] = 3, [HitResult.Miss] = 9,
            };
            Assert.That(BmsExScore.Calculate(statistics), Is.EqualTo(number(4)));
        }
    }

    [Test]
    public void OriginalGaugeSequencesMatchHealthDeathAndClear()
    {
        HitResult[] results = [HitResult.Perfect, HitResult.Great, HitResult.Good, HitResult.Ok, HitResult.Meh, HitResult.Miss];
        BmsGaugeCalculator? calculator = null;
        var health = 0d;
        var cases = 0;
        foreach (var row in BmsExecutedJavaJudgementTest.ReadOracle("gauges"))
        {
            double number(int index) => double.Parse(row[index], CultureInfo.InvariantCulture);
            var step = int.Parse(row[6], CultureInfo.InvariantCulture);
            if (step == 0)
            {
                calculator = new BmsGaugeCalculator(BmsGaugeProfileFactory.Create((BmsGaugeType)int.Parse(row[1], CultureInfo.InvariantCulture), BmsGaugeProfileFamily.Lr2),
                    number(3), int.Parse(row[2], CultureInfo.InvariantCulture), BmsGaugeProfileFamily.Lr2);
                health = calculator.Profile.ClampHealth(number(5) / 100);
                Assert.That(calculator.Total, Is.EqualTo(number(4)), row[0]);
                cases++;
            }
            else
                health = calculator!.ApplyDelta(health, calculator.GetDeltaFor(results[int.Parse(row[7], CultureInfo.InvariantCulture)], health));

            // Only numeric health permits float-rounding error: death and qualification are exact assertions.
            Assert.That(health, Is.EqualTo(number(8) / 100).Within(2e-7), row[0] + ":" + step);
            Assert.That(health == 0, Is.EqualTo(number(8) == 0), row[0] + " death:" + step);
            Assert.That(health > 0 && health >= calculator!.Profile.ClearThreshold, Is.EqualTo(bool.Parse(row[9])), row[0] + " clear:" + step);
        }
        Assert.That(cases, Is.EqualTo(23));
    }
}
