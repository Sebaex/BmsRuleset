using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps.Objects;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Judgement;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Gameplay.Judgement;

[TestFixture]
public class BmsLr2ComplementaryJudgementTest
{
    [TestCase(false)]
    [TestCase(true)]
    public void RejectedFirstLongHeadDoesNotHideLaterCandidate(bool secondLong)
    {
        foreach (var algorithm in System.Enum.GetValues<BmsJudgementAlgorithm>())
        {
            BmsJudgementCandidate[] candidates = [new(1000, 2000, 1, 0.75, true), new(1050, 2050, 1, 0.75, secondLong)];
            var selection = BmsJudgementSelector.SelectPress(BmsLayoutVariant.Bme7K, 1, candidates, 1155, algorithm);
            Assert.That(selection.Candidate?.StartTime, Is.EqualTo(secondLong ? (double?)null : 1050));
            Assert.That(selection.Result, Is.EqualTo(secondLong ? HitResult.None : HitResult.Ok));
        }
    }

    [TestCase(BmsJudgementAlgorithm.Combo, false)]
    [TestCase(BmsJudgementAlgorithm.Duration, false)]
    [TestCase(BmsJudgementAlgorithm.Lowest, true)]
    [TestCase(BmsJudgementAlgorithm.Score, false)]
    public void RejectedLaterLongHeadInvalidatesEligibleEarlierCandidate(BmsJudgementAlgorithm algorithm, bool keepEarlier)
    {
        // JudgeManager.java:429-452: compare first, then judge=6 clears tnote.
        var beatmap = new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K, Rank = 2, LockedLongNoteMode = BmsLongNoteMode.ChargeNote };
        var first = new BmsNote { Beatmap = beatmap, Column = 1, StartTime = 1000 };
        var second = new BmsLongNote { Beatmap = beatmap, Column = 1, StartTime = 1050, Duration = 1000 };
        var output = new BmsRecordingJudgementOutput();
        var column = new BmsJudgementEngine([first, second], beatmap.LayoutVariant, 1, output).GetColumn(1);
        column.HandleNotePress(new BmsPressContext(1155, new BmsFrameContext(1155, 1)), algorithm);
        Assert.That(output.GetMainResult(first).HasResult, Is.EqualTo(keepEarlier));
        Assert.That(column.GetLongNoteState(second).HeadJudged, Is.False);
        Assert.That(column.GetNoteCandidates().Count(), Is.EqualTo(keepEarlier ? 1 : 2));
        column.HandleNotePress(new BmsPressContext(1160, new BmsFrameContext(1160, 1)), algorithm);
        Assert.That(output.MainResults, Is.EqualTo(keepEarlier ? 1 : 0));
        Assert.That(column.GetLongNoteState(second).HeadJudged, Is.False);
    }

    [TestCase(1150, HitResult.Good)]
    [TestCase(1150.001, HitResult.None)]
    public void LaterLongHeadGoodBoundary(double time, HitResult expected)
    {
        BmsJudgementCandidate[] candidates = [new(1000, 1000, 1, 0.75, false), new(1050, 2050, 1, 0.75, true)];
        var selection = BmsJudgementSelector.SelectPress(BmsLayoutVariant.Bme7K, 1, candidates, time);
        Assert.That(selection.Result, Is.EqualTo(expected));
        Assert.That(selection.Candidate?.StartTime, Is.EqualTo(expected == HitResult.None ? (double?)null : 1050));
    }
}
