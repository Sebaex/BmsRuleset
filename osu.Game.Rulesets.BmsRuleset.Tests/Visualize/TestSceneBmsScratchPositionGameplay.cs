#nullable enable

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps.Objects;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Mods;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Replays;
using osu.Game.Rulesets.BmsRuleset.UI.Gameplay;
using osu.Game.Rulesets.BmsRuleset.UI.SongSelect;
using osu.Game.Rulesets.Mods;
using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Visualize;

[HeadlessTest]
public partial class TestSceneBmsScratchPositionGameplay : BmsPlayerTestScene
{
    private BmsRulesetConfigManager config => (BmsRulesetConfigManager)RulesetConfigs.GetConfigFor(new BmsRuleset())!;

    private BmsLayoutVariant layout;
    private bool useReplay;
    private Mod[] replayMods = [];

    protected override TestPlayer CreatePlayer(Ruleset ruleset)
        => CreateBmsPlayer(useReplay ? _ => [new BmsReplayFrame(0), new BmsReplayFrame(60000)] : null, replayMods: replayMods);

    protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
    {
        var beatmap = new BmsBeatmap
        {
            LayoutVariant = layout,
            TotalColumns = BmsLayout.GetTotalColumns(layout),
            HitObjects =
            [
                new BmsNote { StartTime = 60000, Column = 0 },
                new BmsNote { StartTime = 60100, Column = 1 },
            ],
        };
        BmsTestBeatmaps.SetupBeatmapInfo(beatmap, ruleset);
        return beatmap;
    }

    [TestCase(BmsLayoutVariant.Bms5K, false)]
    [TestCase(BmsLayoutVariant.Bme7K, false)]
    [TestCase(BmsLayoutVariant.Bms5K, true)]
    [TestCase(BmsLayoutVariant.Bme7K, true)]
    public void ScratchSettingControlsLayoutAndIsRecordedInScore(BmsLayoutVariant selectedLayout, bool secondPlayer)
    {
        AddStep("load preference-selected 2P gameplay", () =>
        {
            layout = selectedLayout;
            useReplay = false;
            replayMods = [];
            config.SetValue(BmsRulesetSetting.DefaultScratchPosition, secondPlayer ? BmsScratchPosition.SecondPlayer : BmsScratchPosition.FirstPlayer);
            var preference = new BmsScratchPositionPreference(config.Get<BmsScratchPosition>(BmsRulesetSetting.DefaultScratchPosition));
            var selectedMods = preference.Apply([new BmsModSecondPlayer()], layout);
            Assert.That(selectedMods.OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(secondPlayer ? 1 : 0));
            LoadPlayer(selectedMods.ToArray());
        });
        waitForPlayer();
        AddStep("check actual gameplay and score mods", () =>
        {
            var expected = secondPlayer ? BmsLayout.SecondPlayerVariant(layout) : layout;
            Assert.That(((BmsBeatmap)Player.GameplayState.Beatmap).LayoutVariant, Is.EqualTo(expected));
            Assert.That(Playfield.LayoutVariant, Is.EqualTo(expected));
            Assert.That(Player.GameplayState.Mods.OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(secondPlayer ? 1 : 0));
            Assert.That(Player.Score.ScoreInfo.Mods.OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(secondPlayer ? 1 : 0));
            Assert.That(((BmsDrawableRuleset)Player.DrawableRuleset).Variant, Is.EqualTo((int)expected));
            Assert.That(Player.GameplayState.Beatmap.HitObjects.Cast<BmsHitObject>().First().Column, Is.Zero);
            Assert.That(Playfield.Stage.Columns[0].IsScratch, Is.True);
        });
        AddAssert("logical scratch zero is on the configured side", () => scratchIsOnSide(secondPlayer));
        AddStep("change setting during gameplay", () => config.SetValue(BmsRulesetSetting.DefaultScratchPosition,
            secondPlayer ? BmsScratchPosition.FirstPlayer : BmsScratchPosition.SecondPlayer));
        AddAssert("running layout retains its starting side", () => scratchIsOnSide(secondPlayer));
        AddAssert("running input variant retains its starting layout", () => ((BmsDrawableRuleset)Player.DrawableRuleset).Variant == (int)(secondPlayer ? BmsLayout.SecondPlayerVariant(layout) : layout));
        AddStep("restore scratch default", () => config.SetValue(BmsRulesetSetting.DefaultScratchPosition, BmsScratchPosition.FirstPlayer));
    }

    [TestCase(BmsLayoutVariant.Bms5K, false)]
    [TestCase(BmsLayoutVariant.Bme7K, false)]
    [TestCase(BmsLayoutVariant.Bms5K, true)]
    [TestCase(BmsLayoutVariant.Bme7K, true)]
    public void ReplayKeepsRecordedScratchSideDespiteOppositePreference(BmsLayoutVariant selectedLayout, bool recordedSecondPlayer)
    {
        AddStep("load replay with opposite scratch preference", () =>
        {
            layout = selectedLayout;
            useReplay = true;
            replayMods = recordedSecondPlayer ? [new BmsModSecondPlayer()] : [];
            config.SetValue(BmsRulesetSetting.DefaultScratchPosition,
                recordedSecondPlayer ? BmsScratchPosition.FirstPlayer : BmsScratchPosition.SecondPlayer);
            LoadPlayer(replayMods);
        });
        waitForPlayer();
        AddStep("check replay retains its real mods", () =>
        {
            Assert.That(Player.DrawableRuleset.HasReplayLoaded.Value, Is.True);
            Assert.That(Player.DrawableRuleset.ReplayScore.ScoreInfo.Mods.Any(mod => mod is BmsModSecondPlayer), Is.EqualTo(recordedSecondPlayer));
            Assert.That(Player.GameplayState.Mods.Any(mod => mod is BmsModSecondPlayer), Is.EqualTo(recordedSecondPlayer));
            Assert.That(Player.Score.ScoreInfo.Mods.Any(mod => mod is BmsModSecondPlayer), Is.EqualTo(recordedSecondPlayer));
            Assert.That(Playfield.LayoutVariant, Is.EqualTo(recordedSecondPlayer ? BmsLayout.SecondPlayerVariant(layout) : layout));
        });
        AddAssert("recorded scratch side matches visible columns", () => scratchIsOnSide(recordedSecondPlayer));
        AddStep("restore scratch default", () => config.SetValue(BmsRulesetSetting.DefaultScratchPosition, BmsScratchPosition.FirstPlayer));
    }

    [TestCase(BmsLayoutVariant.Bms5K, false, false, false)]
    [TestCase(BmsLayoutVariant.Bme7K, false, false, false)]
    [TestCase(BmsLayoutVariant.Bms5K, true, false, false)]
    [TestCase(BmsLayoutVariant.Bme7K, true, false, false)]
    [TestCase(BmsLayoutVariant.Bms5K, true, true, false)]
    [TestCase(BmsLayoutVariant.Bme7K, true, true, false)]
    [TestCase(BmsLayoutVariant.Bms5K, true, false, true)]
    [TestCase(BmsLayoutVariant.Bme7K, true, false, true)]
    [TestCase(BmsLayoutVariant.Bms5K, false, false, true)]
    [TestCase(BmsLayoutVariant.Bme7K, false, false, true)]
    public void HideScratchKeepsLayoutInputsAndRecordedMods(BmsLayoutVariant selectedLayout, bool secondPlayer, bool mirror, bool replay)
    {
        AddStep("load configured hidden scratch gameplay", () =>
        {
            layout = selectedLayout;
            useReplay = replay;
            var preference = new BmsScratchPositionPreference(secondPlayer ? BmsScratchPosition.SecondPlayer : BmsScratchPosition.FirstPlayer);
            Mod[] selected = mirror ? [new BmsModHideScratch(), new BmsModMirror()] : [new BmsModHideScratch()];
            replayMods = preference.Apply(selected, layout).ToArray();
            config.SetValue(BmsRulesetSetting.DefaultScratchPosition,
                (replay ? !secondPlayer : secondPlayer) ? BmsScratchPosition.SecondPlayer : BmsScratchPosition.FirstPlayer);
            LoadPlayer(replayMods);
        });
        waitForPlayer();
        AddStep("verify gameplay layout and removed scratch notes", () =>
        {
            var expected = secondPlayer ? BmsLayout.SecondPlayerVariant(layout) : layout;
            Assert.That(Playfield.LayoutVariant, Is.EqualTo(expected));
            Assert.That(((BmsDrawableRuleset)Player.DrawableRuleset).Variant, Is.EqualTo((int)expected));
            Assert.That(Playfield.Stage.Columns[0].Hidden, Is.True);
            Assert.That(((Drawable)Playfield.Stage.Columns[0]).DrawWidth, Is.Zero);
            var note = (BmsHitObject)Player.GameplayState.Beatmap.HitObjects.Single();
            Assert.That(note.Column, Is.EqualTo(mirror ? BmsLayout.GetTotalColumns(layout) - 1 : 1));
            Assert.That(Player.GameplayState.Mods.OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(secondPlayer ? 1 : 0));
            Assert.That(Player.Score.ScoreInfo.Mods.OfType<BmsModHideScratch>().Count(), Is.EqualTo(1));
            if (replay)
                Assert.That(Player.DrawableRuleset.ReplayScore.ScoreInfo.Mods.OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(secondPlayer ? 1 : 0));
        });
        AddAssert("visible keys retain their left-to-right geometry", () =>
        {
            var positions = Playfield.Stage.Columns.Skip(1).Cast<Drawable>().Select(column => column.ScreenSpaceDrawQuad.Centre.X).ToArray();
            return positions.Zip(positions.Skip(1)).All(pair => pair.First < pair.Second);
        });
        AddAssert("hidden scratch retains the layout's visual child order", () =>
        {
            var scratch = (Drawable)Playfield.Stage.Columns[0];
            var flow = (FillFlowContainer)scratch.Parent!;
            return ReferenceEquals(secondPlayer ? flow.Children.Last() : flow.Children.First(), scratch);
        });
        AddStep("restore first player setting", () => config.SetValue(BmsRulesetSetting.DefaultScratchPosition, BmsScratchPosition.FirstPlayer));
    }

    private void waitForPlayer()
    {
        AddUntilStep("player loaded", () => Player.IsLoaded && Player.LoadedBeatmapSuccessfully && Player.Alpha == 1);
        AddUntilStep("column geometry available", () => Playfield.Stage.Columns.Where(column => !column.Hidden).Cast<Drawable>().All(column => column.DrawWidth > 0));
    }

    private bool scratchIsOnSide(bool right)
    {
        var scratchX = ((Drawable)Playfield.Stage.Columns[0]).ScreenSpaceDrawQuad.Centre.X;
        return Playfield.Stage.Columns.Skip(1).Cast<Drawable>().All(column => right
            ? scratchX > column.ScreenSpaceDrawQuad.Centre.X
            : scratchX < column.ScreenSpaceDrawQuad.Centre.X);
    }
}
