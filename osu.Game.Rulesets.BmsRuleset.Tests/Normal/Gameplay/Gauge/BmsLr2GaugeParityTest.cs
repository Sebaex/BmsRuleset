using NUnit.Framework;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring.Gauge;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Gameplay.Gauge;

[TestFixture]
public class BmsLr2GaugeParityTest
{
    [TestCase(0.2999, 0.6)]
    [TestCase(0.30, 0.6)]
    [TestCase(0.31, 0.6)]
    [TestCase(0.3199, 0.6)]
    [TestCase(0.32, 1)]
    [TestCase(0.3201, 1)]
    public void TestLr2Guts(double health, double multiplier)
    {
        foreach (var type in new[] { BmsGaugeType.Hard, BmsGaugeType.Class, BmsGaugeType.ExClass })
        {
            var profile = BmsGaugeProfileFactory.Create(type, BmsGaugeProfileFamily.Lr2);
            var calculator = new BmsGaugeCalculator(profile, 300, 1000, BmsGaugeProfileFamily.Lr2);
            var delta = calculator.GetDeltaFor(HitResult.Ok, health);
            Assert.That(delta, Is.EqualTo(profile.BadDelta * multiplier).Within(1e-7));
            Assert.That(calculator.ApplyDelta(health, delta), Is.EqualTo(health + delta).Within(1e-7));
        }
        var ordinary = BmsGaugeProfileFactory.Create(BmsGaugeType.Hard, BmsGaugeProfileFamily.SevenKeys);
        Assert.That(new BmsGaugeCalculator(ordinary, 300, 1000).GetDeltaFor(HitResult.Ok, 0.31), Is.EqualTo(-0.035).Within(1e-7));
    }

    [Test]
    public void TestDeathShiftsChainAndCannotRecover()
    {
        using var processor = new BmsHealthProcessor();
        processor.SetGaugeTypes([BmsGaugeType.Hard, BmsGaugeType.Normal], profileFamilyOverride: BmsGaugeProfileFamily.Lr2,
            initialStates: [new(BmsGaugeType.Hard, 0.0199, false), new(BmsGaugeType.Normal, 0.8, false)]);
        Assert.That(processor.CurrentGaugeStates[0].Failed, Is.True);
        Assert.That(processor.CurrentGaugeStates[0].Health, Is.Zero);
        processor.RegisterEmptyPoor();
        Assert.That(processor.CurrentGaugeStates[0].Health, Is.Zero);
        Assert.That(processor.HasPassedAtEnd(), Is.False);
        processor.RestoreGaugeStates([new(BmsGaugeType.Hard, 0.015, false), new(BmsGaugeType.Normal, 0.8, false)]);
        Assert.That(processor.CurrentGaugeStates[0].Failed, Is.True);
        Assert.That(processor.HasPassedAtEnd(), Is.True);
    }

    [TestCase(0.0201, 0.0201)]
    [TestCase(0.02, 0.02)]
    [TestCase(0.0199, 0)]
    [TestCase(0.015, 0)]
    [TestCase(0, 0)]
    public void TestSurvivalDeath(double health, double expected)
    {
        foreach (var type in new[] { BmsGaugeType.Hard, BmsGaugeType.ExHard, BmsGaugeType.Class, BmsGaugeType.ExClass, BmsGaugeType.ExHardClass })
        {
            var profile = BmsGaugeProfileFactory.Create(type, BmsGaugeProfileFamily.Lr2);
            var calculator = new BmsGaugeCalculator(profile, 300, 1000, BmsGaugeProfileFamily.Lr2);
            var after = calculator.ApplyDelta(health == 0 ? 0 : health + 0.01, -0.01);
            Assert.That(after, Is.EqualTo(expected).Within(1e-12));
            Assert.That(calculator.ApplyDelta(after, 0.001), Is.EqualTo(expected == 0 ? 0 : expected + 0.001).Within(1e-12));
        }
    }

    [TestCase(100, 10)]
    [TestCase(112, 5)]
    [TestCase(125, 5)]
    [TestCase(190, 1.6666667)]
    [TestCase(200, 1.4285714)]
    [TestCase(207.999, 1.4285714)]
    [TestCase(208, 1.25)]
    [TestCase(239.999, 1.1111111)]
    [TestCase(240, 1)]
    [TestCase(300, 1)]
    public void TestTotalDamage(double total, double factor)
    {
        var profile = BmsGaugeProfileFactory.Create(BmsGaugeType.Hard, BmsGaugeProfileFamily.Lr2);
        var calculator = new BmsGaugeCalculator(profile, total, 1000, BmsGaugeProfileFamily.Lr2);
        Assert.That(calculator.GetDeltaFor(HitResult.Ok, 1), Is.EqualTo(-0.06 * factor).Within(0.000001));
        var shortChart = new BmsGaugeCalculator(profile, total, 500, BmsGaugeProfileFamily.Lr2);
        Assert.That(shortChart.GetDeltaFor(HitResult.Ok, 1), Is.EqualTo(-0.06 * System.Math.Max(2, factor)).Within(0.000001));
    }

    // GrooveGauge.MODIFY_DAMAGE, Endless Dream d4995153: float-valued piecewise factors.
    [TestCase(1, 10)]
    [TestCase(20, 10)]
    [TestCase(21, 9.8)]
    [TestCase(29, 8.2)]
    [TestCase(30, 7)]
    [TestCase(59, 5.0666667)]
    [TestCase(60, 5)]
    [TestCase(100, 4.3846154)]
    [TestCase(124, 4.0153846)]
    [TestCase(125, 4)]
    [TestCase(249, 3.008)]
    [TestCase(250, 3)]
    [TestCase(499, 2.004)]
    [TestCase(500, 2)]
    [TestCase(999, 1.002)]
    [TestCase(1000, 1)]
    [TestCase(1001, 1)]
    public void TestNoteCountDamage(int notes, double factor)
    {
        foreach (var type in new[] { BmsGaugeType.Hard, BmsGaugeType.ExHard })
        {
            var profile = BmsGaugeProfileFactory.Create(type, BmsGaugeProfileFamily.Lr2);
            var calculator = new BmsGaugeCalculator(profile, 300, notes, BmsGaugeProfileFamily.Lr2);
            Assert.Multiple(() =>
            {
                Assert.That(calculator.GetDeltaFor(HitResult.Ok, 1), Is.EqualTo(profile.BadDelta * factor).Within(0.000001));
                Assert.That(calculator.GetDeltaFor(HitResult.Meh, 1), Is.EqualTo(profile.PoorDelta * factor).Within(0.000001));
                Assert.That(calculator.GetDeltaFor(HitResult.Miss, 1), Is.EqualTo(profile.EmptyPoorDelta * factor).Within(0.000001));
                Assert.That(calculator.GetDeltaFor(HitResult.Perfect, 1), Is.EqualTo(profile.PerfectGain));
            });
        }
    }
}
