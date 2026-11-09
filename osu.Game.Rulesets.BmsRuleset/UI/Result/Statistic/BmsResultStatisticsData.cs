using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osu.Game.Beatmaps;
using osu.Game.Replays;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Replays;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring.Events;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring.Gauge;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.UI.Result.Statistic;

internal sealed record BmsResultStatisticsData(
    IReadOnlyList<BmsGaugeHistoryGraph.GaugeSeries> Gauges,
    IReadOnlyList<float> StageBoundaries,
    BmsTimelineStatistic.TimelineData Timeline,
    BmsHitScatterStatistic.HitScatterStatistics Scatter,
    BmsHitOffsetStatistic.HitOffsetStatistics Offset)
{
    internal IReadOnlyList<HitEvent> HitEvents { get; init; } = [];

    internal double GaugeDuration { get; init; }

    internal IReadOnlyList<BmsGaugeStateSnapshot> EndingGaugeStates { get; init; } = [];

    internal double? FinalHealth { get; init; }

    internal BmsGaugeType? FinalGaugeType { get; init; }

    internal static BmsResultStatisticsData Create(ScoreInfo score, IBeatmap beatmap, IReadOnlyList<BmsGaugeHistoryEvent>? gauges = null,
                                                   bool truncateAtEnd = false)
        => create(score, beatmap, score.HitEvents, score.HitEvents, gauges ?? [], truncateAtEnd);

    internal static BmsResultStatisticsData? Load(ScoreInfo score, IBeatmap beatmap, Replay replay, CancellationToken cancellationToken,
                                                  IReadOnlyList<BmsGaugeStateSnapshot>? courseGaugeStates = null, BmsGaugeProfileFamily? courseGaugeProfileFamily = null,
                                                  bool truncateAtEnd = false)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (beatmap is not BmsBeatmap bmsBeatmap || !replay.HasReceivedAllFrames || replay.Frames.Count == 0)
            return score.HitEvents.Count > 0 || truncateAtEnd
                ? Create(score, beatmap, truncateAtEnd: truncateAtEnd)
                : null;

        var simulated = BmsReplaySimulation.Run(bmsBeatmap, replay, score, cancellationToken, courseGaugeStates, courseGaugeProfileFamily);
        cancellationToken.ThrowIfCancellationRequested();
        return create(score, beatmap,
            BmsJudgementEventProjection.CreateTimingHitEvents(simulated.Judgements),
            BmsJudgementEventProjection.CreateScoringHitEvents(simulated.Judgements), simulated.Gauges, truncateAtEnd);
    }

    private static BmsResultStatisticsData create(ScoreInfo score, IBeatmap beatmap, IReadOnlyList<HitEvent> hitEvents,
                                                  IReadOnlyList<HitEvent> scoringEvents, IReadOnlyList<BmsGaugeHistoryEvent> gauges, bool truncateAtEnd)
    {
        // The processor expands snapshots on read; reuse them within this statistics request.
        gauges = gauges as BmsGaugeHistoryEvent[] ?? gauges.ToArray();
        var timeline = BmsTimelineStatistic.CreateData(beatmap, hitEvents, scoringEvents);
        var gaugeSeries = BmsGaugeHistoryGraph.CreateSeries(score, beatmap, scoringEvents, gauges, truncateAtEnd);
        return new BmsResultStatisticsData(
            gaugeSeries,
            [],
            timeline,
            BmsHitScatterStatistic.CreateStatistics(beatmap, hitEvents),
            BmsHitOffsetStatistic.CreateStatistics(beatmap, hitEvents))
        {
            HitEvents = hitEvents,
            GaugeDuration = Math.Max(timeline.Duration, gauges.Select(entry => entry.Time).DefaultIfEmpty(0).Max()),
            EndingGaugeStates = gauges.LastOrDefault()?.States ?? [],
            FinalHealth = gaugeSeries.FirstOrDefault(gauge => gauge.IsFinalUsedGauge)?.Points.LastOrDefault().Health,
        };
    }

    internal static BmsResultStatisticsData CreateCourse(IReadOnlyList<(IBeatmap Beatmap, BmsResultStatisticsData Data)> stages, double? finalHealth = null,
                                                         BmsGaugeType? finalGaugeType = null, IReadOnlyList<bool>? fillZeroStages = null)
    {
        var durations = stages.Select(stage => stage.Data.GaugeDuration).ToArray();
        var played = stages.Where(stage => stage.Data.HitEvents.Count > 0).Select(stage => (stage.Beatmap, stage.Data.HitEvents)).ToArray();
        return new BmsResultStatisticsData(
            BmsGaugeHistoryGraph.CreateCourseSeries(stages.Select(stage => stage.Data.Gauges).ToArray(), durations, fillZeroStages),
            BmsGaugeHistoryGraph.CreateCourseStageBoundaries(durations),
            BmsTimelineStatistic.CreateCourseData(stages.Select(stage => stage.Data.Timeline).ToArray()),
            BmsHitScatterStatistic.CreateCourseStatistics(played),
            BmsHitOffsetStatistic.CreateCourseStatistics(played))
        {
            HitEvents = stages.SelectMany(stage => stage.Data.HitEvents).ToArray(),
            GaugeDuration = durations.Sum(),
            FinalHealth = finalHealth,
            FinalGaugeType = finalGaugeType,
        };
    }

    internal static BmsResultStatisticsData Empty() => Create(new ScoreInfo(), new BmsBeatmap());
}
