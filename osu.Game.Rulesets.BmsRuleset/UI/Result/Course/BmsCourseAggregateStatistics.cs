using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Course;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Judgement;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Mods;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Mods.Gauge;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring.Gauge;
using osu.Game.Rulesets.BmsRuleset.UI.Result.Statistic;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.UI.Result.Course;

internal partial class BmsCourseAggregateStatistics : BmsStatisticsPanel
{
    protected override bool StartHidden => false;

    private readonly BmsCourseSession session;

    internal BmsCourseAggregateStatistics(BmsCourseSession session, ScoreInfo aggregate)
        : base(false)
    {
        this.session = session;
        Score.Value = aggregate;
    }

    protected override Task<BmsResultStatisticsData?> LoadStatisticsAsync(ScoreInfo score, CancellationToken cancellationToken)
        => loadStatisticsAsync(score, null, cancellationToken);

    internal Task<BmsResultStatisticsData?> LoadStageStatisticsAsync(ScoreInfo score, CancellationToken cancellationToken)
        => loadStatisticsAsync(score, session.Stages.TakeWhile(stage => stage.Score?.ID != score.ID).Count(), cancellationToken);

    private async Task<BmsResultStatisticsData?> loadStatisticsAsync(ScoreInfo score, int? stageIndex, CancellationToken cancellationToken)
    {
        var stages = session.Stages
            .Take(stageIndex.HasValue ? stageIndex.Value + 1 : session.Stages.Count)
            .Select(stage => (stage.Score, stage.Status, WorkingBeatmap: Beatmaps.GetWorkingBeatmap(stage.Stage.Beatmap, true)))
            .ToArray();

        var gaugeTypes = session.Mods.Any(mod => mod is BmsModGaugeAutoShift)
            ? BmsModGaugeAutoShift.COURSE_AUTO_GAUGE_CHAIN
            : [session.GaugeType];
        var gaugeStates = gaugeTypes.Select(type => new BmsGaugeStateSnapshot(type, 1, false)).ToArray();
        var profileFamily = BmsCourseSession.ResolveCourseGaugeProfileFamily(session.Course.Constraints);
        var data = new (IBeatmap Beatmap, BmsResultStatisticsData Data)[stages.Length];
        for (var i = 0; i < stages.Length; i++)
        {
            var stage = stages[i];
            var replay = stage.Score != null ? await LoadReplayAsync(stage.Score, cancellationToken).ConfigureAwait(false) : null;
            var stageScore = replay?.ScoreInfo ?? stage.Score ?? score;
            var truncateAtEnd = stage.Status == BmsCourseStageStatus.Aborted;
            BmsResultStatisticsData? statistics = null;
            data[i] = await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var mods = stageScore.Mods;
                using var windows = BmsJudgementProfileProvider.UseWindowMods(mods.OfType<IApplicableToJudgementWindow>().ToArray());
                var beatmap = stage.WorkingBeatmap.GetPlayableBeatmap(stageScore.Ruleset, mods, cancellationToken);
                // Legacy scores lack starting-health settings, so reconstruct each surviving tier from the preceding replay.
                statistics = replay != null
                    ? BmsResultStatisticsData.Load(stageScore, beatmap, replay.Replay, cancellationToken, gaugeStates, profileFamily, truncateAtEnd: truncateAtEnd)
                    : null;
                var stageData = statistics
                                ?? (stage.Score != null
                                    ? BmsResultStatisticsData.Create(stageScore, beatmap, truncateAtEnd: truncateAtEnd)
                                    : BmsResultStatisticsData.Create(
                                        new ScoreInfo
                                        {
                                            Mods = stageScore.Mods.OfType<BmsModGauge>().Any()
                                                ? stageScore.Mods
                                                : [.. stageScore.Mods, BmsCourseSession.CreateGaugeMod(session.GaugeType)],
                                        },
                                        beatmap,
                                        truncateAtEnd: truncateAtEnd));
                return (beatmap, stageData);
            }, cancellationToken).ConfigureAwait(false);

            if (statistics?.EndingGaugeStates.Count > 0)
                gaugeStates = statistics.EndingGaugeStates.Where(state => !state.Failed).ToArray();
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (stageIndex.HasValue)
        {
            var stageData = data[^1].Data;
            var stageScore = stages[^1].Score ?? score;
            var endingGaugeType = stageScore.Mods.OfType<BmsModGauge>().FirstOrDefault()?.GaugeType
                                  ?? stageData.EndingGaugeStates.FirstOrDefault(state => !state.Failed)?.GaugeType
                                  ?? stageData.EndingGaugeStates.LastOrDefault()?.GaugeType;

            return stageData with { FinalGaugeType = endingGaugeType };
        }

        double? finalHealth = session.CanContinue
            ? null
            : session.CurrentStage.Status == BmsCourseStageStatus.Aborted
                ? 0
                : session.CurrentHealth;
        BmsGaugeType? finalGaugeType = session.Status == BmsCourseStatus.Passed ? session.FinalGaugeType : null;

        return await Task.Run(
            () => BmsResultStatisticsData.CreateCourse(
                data,
                finalHealth,
                finalGaugeType,
                stages.Select(stage => session.Status == BmsCourseStatus.Aborted
                                       && session.CurrentStage.Status == BmsCourseStageStatus.Passed
                                       && stage.Status == BmsCourseStageStatus.NotPlayed).ToArray()),
            cancellationToken).ConfigureAwait(false);
    }
}
