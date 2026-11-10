using System;
using NUnit.Framework;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Judgement.State;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring.Gauge;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Gameplay.Judgement;

[TestFixture]
public class BmsHcnFrameReferenceTest
{
    [TestCase(1000d, 9)]
    [TestCase(16666.7d, 9)]
    [TestCase(100000d, 9)]
    [TestCase(200000d, 9)]
    [TestCase(201000d, 9)]
    [TestCase(500000d, 4)]
    [TestCase(1000000d, 2)]
    public void FramePartitionsExposeJavaSingleTickLimit(double stepMicroseconds, int expectedJavaTicks)
    {
        var tracker = new BmsHellChargeBodyTracker();
        tracker.Reset(0);
        long previous = 0;
        long javaAccumulator = 0;
        var javaTicks = 0;
        var actualTicks = 0;
        for (var index = 1; previous < 2000000; index++)
        {
            var now = Math.Min(2000000, (long)Math.Round(index * stepMicroseconds));
            var elapsed = now - previous;
            // JudgeManager.java:337-343: one strict comparison per call, not a while loop.
            javaAccumulator += elapsed;
            if (javaAccumulator > 200000)
            {
                javaTicks++;
                javaAccumulator -= 200000;
            }
            tracker.Update(elapsed / 1000d, true, (_, scale) =>
            {
                Assert.That(scale, Is.EqualTo(0.5));
                actualTicks++;
            }, now / 1000d);
            if (stepMicroseconds <= 200000)
                Assert.That(actualTicks, Is.EqualTo(javaTicks), $"Frame ending at {now}us");
            previous = now;
        }
        Assert.That(javaTicks, Is.EqualTo(expectedJavaTicks));
        Assert.That(actualTicks, Is.EqualTo(9));
        // This deliberately documents non-equivalence for long calls, rather than hiding it.
        if (stepMicroseconds >= 500000)
            Assert.That(actualTicks, Is.GreaterThan(javaTicks));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void SignedTransitionsMatchJavaAtBoundedFrameEndpoints(bool startsHolding)
    {
        var tracker = new BmsHellChargeBodyTracker();
        tracker.Reset(0);
        var calculator = new BmsGaugeCalculator(BmsGaugeProfileFactory.Create(BmsGaugeType.Normal, BmsGaugeProfileFamily.Lr2), 10, 1000);
        var actualHealth = 0.2;
        var javaHealth = 0.2f;
        long accumulator = 0;
        var expectedPositive = 0;
        var expectedNegative = 0;
        var positive = 0;
        var negative = 0;
        for (var time = 1; time <= 2000; time++)
        {
            var holding = (time <= 1000) == startsHolding;
            accumulator += holding ? 1000 : -1000;
            if (holding && accumulator > 200000)
            {
                expectedPositive++;
                accumulator -= 200000;
                javaHealth = Math.Clamp(javaHealth + 0.00005f, 0.02f, 1f);
            }
            else if (!holding && accumulator < -200000)
            {
                expectedNegative++;
                accumulator += 200000;
                javaHealth = Math.Clamp(javaHealth - 0.02f, 0.02f, 1f);
            }
            tracker.Update(1, holding, (held, scale) =>
            {
                if (held) positive++; else negative++;
                actualHealth = calculator.ApplyDelta(actualHealth, calculator.GetDeltaFor(held ? HitResult.Great : HitResult.Ok, actualHealth) * scale);
            }, time);
            Assert.That(positive, Is.EqualTo(expectedPositive));
            Assert.That(negative, Is.EqualTo(expectedNegative));
            // Only gauge accumulation uses tolerance; strict tick boundaries above remain exact.
            Assert.That(actualHealth, Is.EqualTo(javaHealth).Within(2e-7));
        }
    }
}
