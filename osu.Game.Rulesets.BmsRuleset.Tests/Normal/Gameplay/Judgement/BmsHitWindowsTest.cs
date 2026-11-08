using NUnit.Framework;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Judgement;
using System.Globalization;
using System.Linq;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Judgement.Framework;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Gameplay.Judgement;

[TestFixture]
public class BmsHitWindowsTest
{
    [Test]
    public void TestHitWindowDefaultRankIsNormalSevenKeysBeatoraja()
    {
        var windows = new BmsHitWindows();
        windows.SetDifficulty(5);

        Assert.That(windows.WindowFor(HitResult.Perfect), Is.EqualTo(18).Within(0.001));
        Assert.That(windows.WindowFor(HitResult.Great), Is.EqualTo(40).Within(0.001));
        Assert.That(windows.WindowFor(HitResult.Good), Is.EqualTo(100).Within(0.001));
        Assert.That(windows.WindowFor(HitResult.Ok), Is.EqualTo(200).Within(0.001));
        Assert.That(windows.WindowFor(HitResult.Meh), Is.EqualTo(200).Within(0.001));
    }

    [Test]
    [TestCase(0, 8, 24, 40, 200)]
    [TestCase(1, 15, 30, 60, 200)]
    [TestCase(2, 18, 40, 100, 200)]
    [TestCase(3, 21, 60, 120, 200)]
    [TestCase(4, 18, 40, 100, 200)]
    public void TestHitWindowRank(int rank, double expectedPerfect, double expectedGreat, double expectedGood, double expectedBad)
    {
        var windows = new BmsHitWindows(rank);
        windows.SetDifficulty(5);

        Assert.That(windows.WindowFor(HitResult.Perfect), Is.EqualTo(expectedPerfect).Within(0.001));
        Assert.That(windows.WindowFor(HitResult.Great), Is.EqualTo(expectedGreat).Within(0.001));
        Assert.That(windows.WindowFor(HitResult.Good), Is.EqualTo(expectedGood).Within(0.001));
        Assert.That(windows.WindowFor(HitResult.Ok), Is.EqualTo(expectedBad).Within(0.001));
        Assert.That(windows.WindowFor(HitResult.Meh), Is.EqualTo(expectedBad).Within(0.001));
    }

    [Test]
    public void TestHitWindowCanUseExRankPercentage()
    {
        var windows = new BmsHitWindows(judgementRate: 1.5);
        windows.SetDifficulty(5);

        Assert.That(windows.WindowFor(HitResult.Perfect), Is.EqualTo(31.5).Within(0.001));
        Assert.That(windows.WindowFor(HitResult.Great), Is.EqualTo(90).Within(0.001));
        Assert.That(windows.WindowFor(HitResult.Good), Is.EqualTo(180).Within(0.001));
        Assert.That(windows.WindowFor(HitResult.Ok), Is.EqualTo(200).Within(0.001));
    }

    [Test]
    public void TestHitWindowsIncludeMiss()
    {
        var windows = new BmsHitWindows();

        Assert.That(windows.IsHitResultAllowed(HitResult.Miss), Is.True);
        Assert.That(windows.GetAllAvailableWindows(), Has.Some.Matches<(HitResult result, double length)>(window => window.result == HitResult.Miss));
    }

    [Test]
    public void TestHitWindowUsesLayoutAndColumnProfile()
    {
        var fiveKey = new BmsHitWindows(rank: 3, BmsLayoutVariant.Bms5K, column: 1);
        var scratch = new BmsHitWindows(rank: 3, column: 0);

        fiveKey.SetDifficulty(5);
        scratch.SetDifficulty(5);

        Assert.That(fiveKey.WindowFor(HitResult.Good), Is.EqualTo(120).Within(0.001));
        Assert.That(scratch.WindowFor(HitResult.Perfect), Is.EqualTo(21).Within(0.001));
        Assert.That(scratch.WindowFor(HitResult.Great), Is.EqualTo(60).Within(0.001));
        Assert.That(scratch.WindowFor(HitResult.Ok), Is.EqualTo(200).Within(0.001));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void TestInclusiveEdgesUseExecutedJavaWindows(int rank)
    {
        HitResult[] results = [HitResult.Perfect, HitResult.Great, HitResult.Good, HitResult.Ok];
        int[] signs = [-1, 1];
        var rows = BmsExecutedJavaJudgementTest.ReadOracle("windows")
            .Where(r => r[0] == "rank" && r[1] == rank.ToString(CultureInfo.InvariantCulture) && r[2] == "NOTE" && r[3] != "4").ToArray();
        Assert.That(rows, Has.Length.EqualTo(4));
        var table = BmsJudgementProfileProvider.GetTable(BmsLayoutVariant.Bme7K, 1, rank, false);
        for (var i = 0; i < rows.Length; i++)
        {
            var radius = long.Parse(rows[i][5], CultureInfo.InvariantCulture) / 1000d;
            foreach (var sign in signs)
            {
                Assert.That(table.ResultForOffset(sign * (radius - .001)), Is.EqualTo(results[i]));
                Assert.That(table.ResultForOffset(sign * radius), Is.EqualTo(results[i]));
                Assert.That(table.ResultForOffset(sign * (radius + .001)), Is.EqualTo(i == 3 ? HitResult.None : results[i + 1]));
            }
        }
    }
}
