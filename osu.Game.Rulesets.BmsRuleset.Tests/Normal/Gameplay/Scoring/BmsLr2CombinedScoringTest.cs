using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps.Objects;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Replays;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring;
using osu.Game.Rulesets.BmsRuleset.IO.Input;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Gameplay.Scoring;

[TestFixture]
public class BmsLr2CombinedScoringTest
{
    [TestCase(false)]
    [TestCase(true)]
    public void ExRankChangesMultiBadConsumptionAndFollowingEmptyPoor(bool customRank)
    {
        // At -70/-90ms, rank 2 yields GOOD; DEFEXRANK 60 yields BAD outside GOOD=56ms.
        var beatmap = new BmsBeatmap
        {
            LayoutVariant = BmsLayoutVariant.Bme7K, TotalColumns = 8, Rank = 2,
            ExRank = customRank ? 60 : null, Total = 300,
        };
        beatmap.HitObjects.AddRange(new[] { 1000d, 1020d }
            .Select(t => new BmsNote { Beatmap = beatmap, Column = 1, StartTime = t }));
        var replay = new osu.Game.Replays.Replay { HasReceivedAllFrames = true };
        replay.Frames.Add(new BmsReplayFrame(0));
        replay.Frames.Add(new BmsReplayFrame(930, BmsAction.Key1));
        replay.Frames.Add(new BmsReplayFrame(931));
        var prefix = BmsReplaySimulation.Run(beatmap, replay, new ScoreInfo());
        Assert.That(prefix.Judgements.Select(j => j.Result), Is.EqualTo(customRank ? new[] { HitResult.Ok, HitResult.Ok } : new[] { HitResult.Good }));
        Assert.That(prefix.Gauges.Last().States.Single().Health, Is.EqualTo(customRank ? 0.12 : 0.95).Within(1e-9));

        replay.Frames.Add(new BmsReplayFrame(1020, BmsAction.Key1));
        replay.Frames.Add(new BmsReplayFrame(1021));
        var full = BmsReplaySimulation.Run(beatmap, replay, new ScoreInfo());
        Assert.That(full.Judgements.Take(prefix.Judgements.Count).Select(j => (j.Source.StartTime, j.Result)),
            Is.EqualTo(prefix.Judgements.Select(j => (j.Source.StartTime, j.Result))));
        Assert.That(full.Judgements.Count(j => j.Source.IsScoring), Is.EqualTo(2));
        Assert.That(full.Judgements.Count(j => j.Result == HitResult.Miss), Is.EqualTo(customRank ? 1 : 0));
        Assert.That(BmsExScore.CreateProgression(full.Judgements).Last(), Is.EqualTo(customRank ? 0 : 2));
        Assert.That(full.Gauges.Last().States.Single().Health, Is.EqualTo(customRank ? 0.1 : 1).Within(1e-9));
    }
}
