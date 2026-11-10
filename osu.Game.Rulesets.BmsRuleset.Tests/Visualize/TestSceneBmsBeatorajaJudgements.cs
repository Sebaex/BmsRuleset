using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps.Objects;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Replays;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring;
using osu.Game.Rulesets.BmsRuleset.IO.Input;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.Scoring;
using osu.Game.Tests.Visual;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Visualize;

[TestFixture]
public partial class TestSceneBmsBeatorajaJudgements : BmsPlayerTestScene
{
    private BmsLayoutVariant layout;

    protected override TestPlayer CreatePlayer(Ruleset ruleset) => CreateBmsPlayer(createReplay);

    protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
    {
        var beatmap = new BmsBeatmap
        {
            LayoutVariant = layout,
            TotalColumns = layout == BmsLayoutVariant.Pms9K ? 9 : 8,
            Rank = 2,
            Total = 30,
            HitObjects =
            [
                new BmsNote { StartTime = 3000, Column = 1 },
                new BmsNote { StartTime = 6000, Column = 1 },
                new BmsNote { StartTime = 10000, Column = 1 },
            ],
        };
        BmsTestBeatmaps.SetupBeatmapInfo(beatmap, ruleset);
        return beatmap;
    }

    private IList<ReplayFrame> createReplay(BmsBeatmap beatmap) =>
    [
        new BmsReplayFrame(0),
        new BmsReplayFrame(2850, layout == BmsLayoutVariant.Pms9K ? BmsAction.PmsKey2 : BmsAction.Key1),
        new BmsReplayFrame(2851),
        new BmsReplayFrame(3000, layout == BmsLayoutVariant.Pms9K ? BmsAction.PmsKey2 : BmsAction.Key1),
        new BmsReplayFrame(3001),
        new BmsReplayFrame(5600, layout == BmsLayoutVariant.Pms9K ? BmsAction.PmsKey2 : BmsAction.Key1),
        new BmsReplayFrame(5601),
        new BmsReplayFrame(5850, layout == BmsLayoutVariant.Pms9K ? BmsAction.PmsKey2 : BmsAction.Key1),
        new BmsReplayFrame(5851),
        new BmsReplayFrame(11000),
    ];

    [TestCase(BmsLayoutVariant.Pms9K, 2, 0, 2)]
    [TestCase(BmsLayoutVariant.Bme7K, 2, 0, 2)]
    public void TestPressRulesAndRewind(BmsLayoutVariant variant, int bad, int perfect, int emptyPoor)
    {
        AddStep("load replay", () =>
        {
            layout = variant;
            LoadPlayer();
        });
        AddUntilStep("player loaded", () => Player.IsLoaded && Player.Alpha == 1);
        checkpoint(2900, "after BAD", () => assertState(.16, 0, 0, 1));
        checkpoint(3100, "after rehit", () => assertState(.14, 0, 0, 1));
        checkpoint(5700, "after empty POOR", () => assertState(.12, 0, 0, 1));
        checkpoint(6300, "after passive judgement", finalState);
        checkpoint(5700, "rewind after empty POOR", () => assertState(.12, 0, 0, 1));
        checkpoint(6300, "replay suppressed miss", finalState);
        checkpoint(3100, "rewind before next empty POOR", () => assertState(.14, 0, 0, 1));
        checkpoint(6300, "replay combo-breaking empty POOR", finalState);
        checkpoint(2900, "rewind after BAD before rehit", () => assertState(.16, 0, 0, 1));
        checkpoint(6300, "replay from non-consuming BAD", finalState);

        void finalState()
        {
            matches();
            assertState(.08, 0, 0, 2);
            var processor = (BmsScoreProcessor)Player.ScoreProcessor;
            Assert.That(processor.Accuracy.Value, Is.Zero);
            Assert.That(processor.TotalScoreWithoutMods.Value, Is.Zero);
            var saved = new ScoreInfo();
            processor.PopulateScore(saved);
            Assert.That(saved.Accuracy, Is.Zero);
            Assert.That(saved.MaxCombo, Is.Zero);
            Assert.That(saved.Statistics.GetValueOrDefault(HitResult.Ok), Is.EqualTo(bad));
            Assert.That(saved.Statistics.GetValueOrDefault(HitResult.Miss), Is.EqualTo(emptyPoor));
            Assert.That(saved.Statistics.GetValueOrDefault(HitResult.Meh), Is.Zero);
            var expectedSequence = new[] { HitResult.Ok, HitResult.Miss, HitResult.Miss, HitResult.Ok };
            Assert.That(processor.JudgementEvents.Where(e => !e.SuppressPenalty).Select(e => e.Result), Is.EqualTo(expectedSequence));
            var suppressed = processor.JudgementEvents.Where(e => e.SuppressPenalty).ToArray();
            Assert.That(suppressed, Is.Empty, "LR2 ALWAYS keeps both consumed BAD results, with no later suppressed POOR.");
            var consumed = processor.JudgementEvents.Where(e => e.Source.IsScoring).ToArray();
            Assert.That(consumed.Select(e => e.Source.StartTime), Is.EqualTo(new[] { 3000d, 6000d }));
            Assert.That(consumed.Select(e => e.Source.Column), Is.EqualTo(new[] { 1, 1 }));
            Assert.That(consumed.Select(e => e.Result), Is.EqualTo(new[] { HitResult.Ok, HitResult.Ok }));
            Assert.That(BmsExScore.CreateProgression(processor.JudgementEvents), Is.EqualTo(new[] { 0, 0, 0 }));
            Assert.That(saved.HitEvents.Select(e => e.Result), Is.EqualTo(expectedSequence));
        }

        bool matches()
        {
            var events = ((BmsScoreProcessor)Player.ScoreProcessor).JudgementEvents;
            var details = string.Join(", ", events.Select(e => $"{e.Source.StartTime}:{e.Result}"));
            Assert.That(events.Count(e => e.Result == HitResult.Ok), Is.EqualTo(bad), details);
            Assert.That(events.Count(e => e.Result == HitResult.Perfect), Is.EqualTo(perfect), details);
            Assert.That(events.Count(e => e.Result == HitResult.Miss), Is.EqualTo(emptyPoor), details);
            Assert.That(events.Where(e => !e.SuppressPenalty).All(e => e.Result != HitResult.Meh), Is.True, details);
            return true;
        }
    }
    private void checkpoint(double time, string description, Action assertion)
    {
        AddStep(description, () =>
        {
            Player.GameplayClockContainer.Stop();
            Player.GameplayClockContainer.Seek(time);
        });
        AddUntilStep("simulation reached " + description, () => Math.Abs(Player.DrawableRuleset.FrameStableClock.CurrentTime - time) < 0.001);
        AddStep("assert " + description, assertion);
    }

    // Pinned LR2: initial20%, consuming BAD-4%, early empty POOR-2% in both layouts.
    private void assertState(double health, int combo, int highestCombo, int consumed)
    {
        Assert.That(Player.HealthProcessor.Health.Value, Is.EqualTo(health).Within(1e-8), "health");
        Assert.That(Player.ScoreProcessor.Combo.Value, Is.EqualTo(combo), "combo");
        Assert.That(Player.ScoreProcessor.HighestCombo.Value, Is.EqualTo(highestCombo), "highest combo");
        Assert.That(((BmsScoreProcessor)Player.ScoreProcessor).ScoringJudgementEventCount, Is.EqualTo(consumed), "consumed notes");
    }

}
