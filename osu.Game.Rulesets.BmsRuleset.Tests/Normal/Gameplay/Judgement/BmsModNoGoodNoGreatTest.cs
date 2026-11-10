using NUnit.Framework;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Judgement;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Mods;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Gameplay.Judgement;

[TestFixture]
public class BmsModNoGoodNoGreatTest
{
    [Test]
    public void TestNoGoodDegradesGoodToBad()
    {
        // Executed Java LR2 rank 2: PG=18, GR=40, GD=100, BAD=200ms, both signs inclusive.
        var mod = new BmsModNoGood();
        var table = mod.ApplyToJudgementWindow(
            BmsJudgementProfileProvider.GetTable(BmsLayoutVariant.Bme7K, column: 1, judgementRate: 0.75, tail: false));

        Assert.Multiple(() =>
        {
            Assert.That(table.ResultForOffset(10), Is.EqualTo(HitResult.Perfect));
            Assert.That(table.ResultForOffset(-10), Is.EqualTo(HitResult.Perfect));
            Assert.That(table.ResultForOffset(20), Is.EqualTo(HitResult.Great));
            Assert.That(table.ResultForOffset(-20), Is.EqualTo(HitResult.Great));
            Assert.That(table.ResultForOffset(60), Is.EqualTo(HitResult.Ok));
            Assert.That(table.ResultForOffset(-60), Is.EqualTo(HitResult.Ok));
            Assert.That(table.ResultForOffset(210), Is.EqualTo(HitResult.None));
            Assert.That(table.ResultForOffset(-165), Is.EqualTo(HitResult.Ok));
            Assert.That(table.ResultForOffset(200), Is.EqualTo(HitResult.Ok));
            Assert.That(table.ResultForOffset(-200), Is.EqualTo(HitResult.Ok));
            Assert.That(table.ResultForOffset(200.001), Is.EqualTo(HitResult.None));
            Assert.That(table.ResultForOffset(-200.001), Is.EqualTo(HitResult.None));
        });
    }

    [Test]
    public void TestNoGreatDegradesGreatAndGoodToBad()
    {
        var mod = new BmsModNoGreat();
        var table = mod.ApplyToJudgementWindow(
            BmsJudgementProfileProvider.GetTable(BmsLayoutVariant.Bme7K, column: 1, judgementRate: 0.75, tail: false));

        Assert.Multiple(() =>
        {
            Assert.That(table.ResultForOffset(10), Is.EqualTo(HitResult.Perfect));
            Assert.That(table.ResultForOffset(-10), Is.EqualTo(HitResult.Perfect));
            Assert.That(table.ResultForOffset(20), Is.EqualTo(HitResult.Ok));
            Assert.That(table.ResultForOffset(-20), Is.EqualTo(HitResult.Ok));
            Assert.That(table.ResultForOffset(60), Is.EqualTo(HitResult.Ok));
            Assert.That(table.ResultForOffset(-60), Is.EqualTo(HitResult.Ok));
            Assert.That(table.ResultForOffset(210), Is.EqualTo(HitResult.None));
            Assert.That(table.ResultForOffset(-165), Is.EqualTo(HitResult.Ok));
            Assert.That(table.ResultForOffset(200), Is.EqualTo(HitResult.Ok));
            Assert.That(table.ResultForOffset(-200), Is.EqualTo(HitResult.Ok));
            Assert.That(table.ResultForOffset(200.001), Is.EqualTo(HitResult.None));
            Assert.That(table.ResultForOffset(-200.001), Is.EqualTo(HitResult.None));
        });
    }

    [Test]
    public void TestConstraintDoesNotAffectBadAndMissRows()
    {
        var mod = new BmsModNoGreat();
        var table = mod.ApplyToJudgementWindow(
            BmsJudgementProfileProvider.GetTable(BmsLayoutVariant.Bme7K, column: 1, judgementRate: 0.75, tail: false));

        Assert.Multiple(() =>
        {
            Assert.That(table.IsPastPassivePoorOffset(200), Is.False);
            Assert.That(table.IsPastPassivePoorOffset(200.001), Is.True);
            Assert.That(table.IsPastPassivePoorOffset(210), Is.True);
            Assert.That(table.IsPastPassivePoorOffset(210.001), Is.True);
            Assert.That(table.IsEmptyPoorOffset(-221), Is.True);
            Assert.That(table.IsEmptyPoorOffset(-501), Is.True);
            Assert.That(table.IsEmptyPoorOffset(-1000), Is.True);
            Assert.That(table.IsEmptyPoorOffset(-1000.001), Is.False);
        });
    }
}
