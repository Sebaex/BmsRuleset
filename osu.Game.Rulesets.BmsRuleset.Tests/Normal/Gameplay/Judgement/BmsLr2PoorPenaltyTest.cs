using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps.Objects;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Replays;
using osu.Game.Rulesets.BmsRuleset.IO.Input;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Gameplay.Judgement;

[TestFixture]
public class BmsLr2PoorPenaltyTest
{
    [TestCase(BmsLayoutVariant.Pms9K)]
    [TestCase(BmsLayoutVariant.Bme7K)]
    [TestCase(BmsLayoutVariant.Bms5K)]
    public void EarlyEmptyPoorDoesNotSuppressLaterConsumedPoor(BmsLayoutVariant layout)
    {
        // JudgeProperty.LR2 uses MissCondition.ALWAYS, unlike historical PMS MissCondition.ONE.
        var beatmap = new BmsBeatmap { LayoutVariant = layout, TotalColumns = BmsLayout.GetTotalColumns(layout), Rank = 2, Total = 300 };
        beatmap.HitObjects.Add(new BmsNote { Beatmap = beatmap, Column = 1, StartTime = 1000 });
        var replay = new osu.Game.Replays.Replay { HasReceivedAllFrames = true };
        replay.Frames.Add(new BmsReplayFrame(0));
        replay.Frames.Add(new BmsReplayFrame(250, layout == BmsLayoutVariant.Pms9K ? BmsAction.PmsKey2 : BmsAction.Key1));
        replay.Frames.Add(new BmsReplayFrame(251));
        replay.Frames.Add(new BmsReplayFrame(1300));
        var run = BmsReplaySimulation.Run(beatmap, replay, new ScoreInfo());
        Assert.That(run.Judgements.Count(j => j.Result == HitResult.Miss), Is.EqualTo(1));
        var poor = run.Judgements.Single(j => j.Result == HitResult.Meh);
        Assert.That(poor.SuppressPenalty, Is.False);
        Assert.That(poor.Source.IsScoring, Is.True);
        Assert.That(run.Gauges.Last().States.Single().Health, Is.EqualTo(0.12).Within(1e-9));
    }
}
