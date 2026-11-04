using System;
using NUnit.Framework;
using osu.Game.Rulesets.BmsRuleset.Beatmaps.Objects;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Judgement;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Gameplay.Judgement;

[TestFixture]
public class BmsLr2JudgementParityTest
{
    [TestCase(1100.001)]
    [TestCase(1150)]
    [TestCase(1200)]
    public void TestLateLongHeadIsNotStarted(double time)
    {
        foreach (var mode in new[] { BmsLongNoteMode.LongNote, BmsLongNoteMode.ChargeNote, BmsLongNoteMode.HellChargeNote })
        {
            var context = new BmsLongNoteTestContext(mode);
            context.Column.HandleNotePress(new BmsPressContext(time, new BmsFrameContext(time, 1)));
            Assert.That(context.State.HeadJudged, Is.False);
            Assert.That(context.Output.MainResults, Is.Zero);
            context.Passive(1201);
            Assert.That(context.State.HeadResult, Is.EqualTo(HitResult.Meh));
            if (mode == BmsLongNoteMode.HellChargeNote)
                Assert.That(context.Output.SyntheticEndpoints[0].Result, Is.EqualTo(HitResult.Meh));
            else
                Assert.That(context.Output.GetMainResult(context.Note).Type, Is.EqualTo(HitResult.Meh));
        }
    }

    [TestCase(850, HitResult.Ok)]
    [TestCase(1100, HitResult.Good)]
    public void TestAdmissibleLongHead(double time, HitResult expected)
    {
        var context = new BmsLongNoteTestContext(BmsLongNoteMode.ChargeNote);
        context.Column.HandleNotePress(new BmsPressContext(time, new BmsFrameContext(time, 1)));
        Assert.That(context.State.HeadResult, Is.EqualTo(expected));
        Assert.That(context.State.IsHolding, Is.True);
    }

    [TestCase(-120)]
    [TestCase(-100)]
    [TestCase(-70)]
    [TestCase(-40)]
    [TestCase(-18)]
    [TestCase(0)]
    [TestCase(70)]
    [TestCase(100)]
    [TestCase(120)]
    public void TestLongEndpoints(double offset)
    {
        foreach (var mode in new[] { BmsLongNoteMode.LongNote, BmsLongNoteMode.ChargeNote, BmsLongNoteMode.HellChargeNote })
        {
            foreach (var layout in new[] { BmsLayoutVariant.Bme7K, BmsLayoutVariant.Pms9K })
            {
                foreach (var column in new[] { 0, 1 })
                {
                    var context = new BmsLongNoteTestContext(mode, column: column, layout: layout);
                    context.Press(1000, HitResult.Perfect);
                    if (mode == BmsLongNoteMode.LongNote && offset > 0)
                        context.Passive(1500.001);
                    if (column == 0 && layout == BmsLayoutVariant.Bme7K && mode != BmsLongNoteMode.LongNote)
                        context.Column.HandleNotePress(new BmsPressContext(1500 + offset, new BmsFrameContext(1500 + offset, 1), true));
                    else
                        context.Release(1500 + offset);
                    var expected = mode == BmsLongNoteMode.LongNote && offset > 0 || Math.Abs(offset) <= 100 ? HitResult.Perfect : HitResult.Ok;
                    Assert.That(context.State.TailResult, Is.EqualTo(expected), $"{mode}/{layout}/{column}/{offset}");
                    Assert.That(context.State.TailJudged, Is.True);
                    Assert.That(context.Output.SyntheticEndpoints.Count, Is.EqualTo(mode == BmsLongNoteMode.LongNote ? 0 : 1));
                }
            }
        }
    }

    [TestCase(1, 0, 0, 0)]
    [TestCase(25, 5.76, 17.28, 28.8)]
    [TestCase(50, 11.36, 26.88, 49.6)]
    [TestCase(60, 13.6, 28.8, 56)]
    [TestCase(75, 15.72, 32.4, 69.6)]
    [TestCase(100, 18, 40, 100)]
    [TestCase(125, 20.16, 54.4, 114.4)]
    [TestCase(200, 31.5, 90, 180)]
    [TestCase(400, 63, 180, 200)]
    public void TestDefExRank(double exRank, double perfect, double great, double good)
    {
        foreach (var layout in Enum.GetValues<BmsLayoutVariant>())
        {
            var rate = BmsJudgementProfileProvider.RateForExRank(layout, exRank);
            var table = BmsJudgementProfileProvider.GetTable(layout, 0, rate, false);
            Assert.That(table.FastWindowFor(HitResult.Perfect), Is.EqualTo(perfect).Within(1e-9));
            Assert.That(table.FastWindowFor(HitResult.Great), Is.EqualTo(great).Within(1e-9));
            Assert.That(table.FastWindowFor(HitResult.Good), Is.EqualTo(good).Within(1e-9));
            Assert.That(table.SlowWindowFor(HitResult.Good), Is.EqualTo(good).Within(1e-9));
        }
    }

    [TestCase(0, 8, 24, 40)]
    [TestCase(1, 15, 30, 60)]
    [TestCase(2, 18, 40, 100)]
    [TestCase(3, 21, 60, 120)]
    [TestCase(4, 18, 40, 100)]
    public void TestRankAcrossLayouts(int rank, double perfect, double great, double good)
    {
        foreach (var layout in Enum.GetValues<BmsLayoutVariant>())
        {
            for (var column = 0; column < BmsLayout.GetTotalColumns(layout); column++)
            {
                var table = BmsJudgementProfileProvider.GetTable(layout, column, rank, false);
                foreach (var sign in new[] { -1, 1 })
                {
                    Assert.That(table.ResultForOffset(sign * perfect), Is.EqualTo(HitResult.Perfect), $"{layout}/{column}/{rank}");
                    Assert.That(table.ResultForOffset(sign * (perfect + 0.001)), Is.EqualTo(HitResult.Great));
                    Assert.That(table.ResultForOffset(sign * great), Is.EqualTo(HitResult.Great));
                    Assert.That(table.ResultForOffset(sign * good), Is.EqualTo(HitResult.Good));
                    Assert.That(table.ResultForOffset(sign * (good + 0.001)), Is.EqualTo(HitResult.Ok));
                    Assert.That(table.ResultForOffset(sign * 200), Is.EqualTo(HitResult.Ok));
                    Assert.That(table.ResultForOffset(sign * 200.001), Is.EqualTo(HitResult.None));
                }
            }
        }
    }
}
