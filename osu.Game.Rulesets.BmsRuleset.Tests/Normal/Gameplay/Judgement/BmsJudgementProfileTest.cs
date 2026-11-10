using System;
using NUnit.Framework;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Judgement;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Gameplay.Judgement;

[TestFixture]
public class BmsJudgementProfileTest
{
    // JudgeProperty.LR2 at d4995153; older per-layout expectations described other Java profiles.
    [TestCase(0, 8, 24, 40)]
    [TestCase(1, 15, 30, 60)]
    [TestCase(2, 18, 40, 100)]
    [TestCase(3, 21, 60, 120)]
    [TestCase(4, 18, 40, 100)]
    public void TestLr2HeadsAndTails(int rank, double perfect, double great, double good)
    {
        foreach (var layout in Enum.GetValues<BmsLayoutVariant>())
        {
            for (var column = 0; column < BmsLayout.GetTotalColumns(layout); column++)
            {
                foreach (var tail in new[] { false, true })
                {
                    var table = BmsJudgementProfileProvider.GetTable(layout, column, rank, tail);
                    foreach (var sign in new[] { -1, 1 })
                    {
                        Assert.That(table.ResultForOffset(sign * (tail ? good : perfect)), Is.EqualTo(HitResult.Perfect));
                        Assert.That(table.ResultForOffset(sign * great), Is.EqualTo(tail ? HitResult.Perfect : HitResult.Great));
                        Assert.That(table.ResultForOffset(sign * good), Is.EqualTo(tail ? HitResult.Perfect : HitResult.Good));
                        Assert.That(table.ResultForOffset(sign * 200), Is.EqualTo(HitResult.Ok));
                        Assert.That(table.ResultForOffset(sign * 200.001), Is.EqualTo(HitResult.None));
                    }
                    Assert.That(table.IsPastPassivePoorOffset(200), Is.False);
                    Assert.That(table.IsPastPassivePoorOffset(200.001), Is.True);
                    Assert.That(table.IsEmptyPoorOffset(-1000), Is.EqualTo(!tail));
                    Assert.That(table.IsEmptyPoorOffset(-1000.001), Is.False);
                    Assert.That(table.IsEmptyPoorOffset(201), Is.False);
                }
            }
        }
    }

    [TestCase(-28.56, HitResult.Perfect)]
    [TestCase(-28.561, HitResult.Great)]
    [TestCase(28.56, HitResult.Perfect)]
    [TestCase(28.561, HitResult.Great)]
    public void TestIntegerRankMicrosecondBoundary(double offset, HitResult expected)
    {
        // DEFEXRANK 182 converts to integer rank 136 before scaling the 21000-us LR2 row.
        var rate = BmsJudgementProfileProvider.RateForExRank(BmsLayoutVariant.Bme7K, 182);
        var table = BmsJudgementProfileProvider.GetTable(BmsLayoutVariant.Bme7K, 1, rate, false);
        Assert.That(table.ResultForOffset(offset), Is.EqualTo(expected));
    }
}