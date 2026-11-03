#nullable enable

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Screens;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Overlays.Mods;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Mods;
using osu.Game.Rulesets.BmsRuleset.UI.SongSelect;
using osu.Game.Rulesets.BmsRuleset.UI.SongSelect.Course;
using osu.Game.Utils;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Visualize;

public partial class TestSceneBmsSongSelectLampHack
{
    private BmsRulesetConfigManager? scratchConfig;
    private static readonly int[] scratch_columns = [6, 8, 9, 12, 16, 18];

    [SetUpSteps]
    public void ResetScratchPositionPreferenceSteps()
    {
        AddStep("reset scratch preference on update thread", () =>
        {
            scratchConfig = (BmsRulesetConfigManager)RulesetConfigs.GetConfigFor(new BmsRuleset())!;
            scratchConfig.SetValue(BmsRulesetSetting.DefaultScratchPosition, BmsScratchPosition.FirstPlayer);
        });
    }

    [TestCase(6)]
    [TestCase(8)]
    public void TestScratchPositionUsesInternalModOutsideManualSelection(int columns)
    {
        prepareScratchPosition(BmsScratchPosition.SecondPlayer, columns);
        loadSongSelect();
        AddUntilStep("real second player mod selected", hasSecondPlayer);
        AddAssert("mod overlay has the same second player selection", () => songSelect.ModSelectOverlay.SelectedMods.Value.Single() is BmsModSecondPlayer);
        AddAssert("global selection carries the real mod", () => SelectedMods.Value.Single() is BmsModSecondPlayer);
        AddAssert("second player has no selectable card", () => songSelect.ModSelectOverlay.AllAvailableMods
            .Where(state => state.Mod is BmsModSecondPlayer).All(state => !state.ValidForSelection.Value && !state.Visible));
    }

    [Test]
    public void TestScratchSettingSurvivesDeselectAllChartResumeAndCourseReplacement()
    {
        prepareScratchPosition(BmsScratchPosition.SecondPlayer);
        loadSongSelect();
        AddUntilStep("default second player selected", hasSecondPlayer);
        showScratchModOverlay();
        AddStep("select mirror alongside the internal layout", () => songSelect.Mods.Value = [.. songSelect.Mods.Value, new BmsModMirror()]);
        AddStep("deselect all selectable mods", () => songSelect.ModSelectOverlay.DeselectAll());
        AddUntilStep("deselect all preserves only the setting", () => songSelect.Mods.Value.Count == 1 && hasSecondPlayer());
        AddStep("close mod overlay", () => songSelect.ModSelectOverlay.Hide());
        AddStep("apply an incoming selection without layout metadata", () => songSelect.Mods.Value = []);
        AddAssert("incoming mods do not override the setting", hasSecondPlayer);
        selectScratchColumns(8);
        AddWaitStep("allow selection callbacks", 3);
        AddAssert("chart change preserves setting", hasSecondPlayer);
        AddStep("suspend song select", () => songSelect.Push(new ResumeTestScreen()));
        AddUntilStep("child screen loaded", () => Stack.CurrentScreen is ResumeTestScreen { IsLoaded: true });
        AddStep("resume song select", () => Stack.Exit());
        AddUntilStep("song select resumed", () => Stack.CurrentScreen == songSelect && !songSelect.IsFiltering);
        AddAssert("resume preserves setting", hasSecondPlayer);

        replaceScratchCourseMode(true);
        AddAssert("course preserves setting", hasSecondPlayer);
        replaceScratchCourseMode(false);
        AddAssert("return from course preserves setting", hasSecondPlayer);

        AddStep("leave song select", () => songSelect.Exit());
        AddUntilStep("old selection exited", () => Stack.CurrentScreen != songSelect);
        loadSongSelect();
        AddUntilStep("new song select entry reapplies default", hasSecondPlayer);
    }

    [Test]
    public void TestFirstPlayerSettingRemovesResidualSecondPlayer()
    {
        prepareScratchPosition(BmsScratchPosition.FirstPlayer);
        AddStep("restore an older second player selection", () => SelectedMods.Value = [new BmsModSecondPlayer(), new BmsModMirror()]);
        loadSongSelect();
        AddAssert("first player setting clears only the old layout", () => songSelect.Mods.Value.Single() is BmsModMirror);
        AddStep("apply an older mod selection from the overlay", () => songSelect.ModSelectOverlay.SelectedMods.Value = [new BmsModSecondPlayer(), new BmsModMirror()]);
        AddAssert("first player also owns incoming selections", () => songSelect.Mods.Value.Single() is BmsModMirror);
        AddAssert("overlay agrees with first player", () => songSelect.ModSelectOverlay.SelectedMods.Value.Single() is BmsModMirror);
    }

    [Test]
    public void TestScratchThirdPartyConflict([Values] bool existingSecondPlayer)
    {
        prepareScratchPosition(BmsScratchPosition.SecondPlayer);
        loadSongSelect();
        AddUntilStep("second player selected", hasSecondPlayer);
        AddStep("restore a transient mirror and random conflict", () =>
        {
            var random = new BmsModLaneRandom();
            random.Seed.Value = 431;
            random.IncludeScratch.Value = true;
            songSelect.Mods.Value = existingSecondPlayer
                ? [new BmsModSecondPlayer(), new BmsModMirror(), random]
                : [new BmsModMirror(), random];
        });
        AddAssert("unrelated conflict retains side and both mods", () => hasSecondPlayer()
            && songSelect.Mods.Value.Count == 3
            && songSelect.Mods.Value.OfType<BmsModMirror>().Count() == 1
            && songSelect.Mods.Value.OfType<BmsModLaneRandom>().Count() == 1);
        AddAssert("selection remains invalid until its own conflict is resolved", () => !ModUtils.CheckCompatibleSet(songSelect.Mods.Value, out _));
        AddAssert("random settings survive restoration", () => songSelect.Mods.Value.OfType<BmsModLaneRandom>().Single() is var random
            && random.Seed.Value == 431 && random.IncludeScratch.Value);
        AddStep("resolve only the mirror conflict", () => songSelect.Mods.Value = songSelect.Mods.Value.Where(mod => mod is not BmsModMirror).ToArray());
        AddAssert("random and second player are compatible", () => hasSecondPlayer()
            && songSelect.Mods.Value.Count == 2 && ModUtils.CheckCompatibleSet(songSelect.Mods.Value, out _));
        AddStep("suspend selection", () => songSelect.Push(new ResumeTestScreen()));
        AddUntilStep("suspended child loaded", () => Stack.CurrentScreen is ResumeTestScreen { IsLoaded: true });
        AddStep("change side while suspended", () => scratchConfig!.SetValue(BmsRulesetSetting.DefaultScratchPosition, BmsScratchPosition.FirstPlayer));
        AddAssert("suspended selection keeps recorded state", hasSecondPlayer);
        AddStep("return to selection", () => Stack.Exit());
        AddUntilStep("first player applied on resume", () => Stack.CurrentScreen == songSelect && !hasSecondPlayer());
        AddAssert("random settings survive side change", () => songSelect.Mods.Value.Single() is BmsModLaneRandom random
            && random.Seed.Value == 431 && random.IncludeScratch.Value);
        AddStep("choose second player again", () => scratchConfig!.SetValue(BmsRulesetSetting.DefaultScratchPosition, BmsScratchPosition.SecondPlayer));
        AddAssert("second player restored once", () => songSelect.Mods.Value.OfType<BmsModSecondPlayer>().Count() == 1);
        showScratchModOverlay();
        AddStep("deselect manual mods", () => songSelect.ModSelectOverlay.DeselectAll());
        AddUntilStep("only the configured side remains", () => songSelect.Mods.Value.SingleOrDefault() is BmsModSecondPlayer);
    }

    [Test]
    public void TestScratchSettingChangesWaitForSongSelectToResume()
    {
        prepareScratchPosition(BmsScratchPosition.FirstPlayer);
        loadSongSelect();
        AddAssert("first player default leaves mods empty", () => !hasSecondPlayer());
        AddStep("suspend selection", () => songSelect.Push(new ResumeTestScreen()));
        AddUntilStep("suspended child loaded", () => Stack.CurrentScreen is ResumeTestScreen { IsLoaded: true });
        AddStep("change preference while suspended", () => scratchConfig!.SetValue(BmsRulesetSetting.DefaultScratchPosition, BmsScratchPosition.SecondPlayer));
        AddAssert("suspended selection remains untouched", () => !hasSecondPlayer());
        AddStep("return to selection", () => Stack.Exit());
        AddUntilStep("preference applied on resume", () => Stack.CurrentScreen == songSelect && hasSecondPlayer());
        AddStep("choose first player while selecting", () => scratchConfig!.SetValue(BmsRulesetSetting.DefaultScratchPosition, BmsScratchPosition.FirstPlayer));
        AddAssert("explicit first player removes second player", () => !hasSecondPlayer());
        AddStep("choose second player while selecting", () => scratchConfig!.SetValue(BmsRulesetSetting.DefaultScratchPosition, BmsScratchPosition.SecondPlayer));
        AddAssert("explicit second player applies immediately", hasSecondPlayer);
    }

    [TestCase(9)]
    [TestCase(12)]
    [TestCase(16)]
    [TestCase(18)]
    public void TestScratchPreferenceTracksLayoutCompatibility(int columns)
    {
        prepareScratchPosition(BmsScratchPosition.SecondPlayer);
        loadSongSelect();
        AddUntilStep("second player on single play", hasSecondPlayer);
        selectScratchColumns(columns);
        AddAssert("incompatible layout has no second player", () => !hasSecondPlayer());
        AddAssert("overlay clears incompatible second player", () => !songSelect.ModSelectOverlay.SelectedMods.Value.OfType<BmsModSecondPlayer>().Any());
        selectScratchColumns(6);
        AddAssert("return to single play restores preference", hasSecondPlayer);
    }

    [Test]
    public void TestScratchPreferencePreservesHideScratch()
    {
        prepareScratchPosition(BmsScratchPosition.SecondPlayer);
        AddStep("select hide scratch before entering", () => SelectedMods.Value = [new BmsModHideScratch()]);
        loadSongSelect();
        AddAssert("hide scratch preserves second player", () => hasSecondPlayer() && songSelect.Mods.Value.OfType<BmsModHideScratch>().Count() == 1);
        selectScratchColumns(8);
        AddAssert("chart change preserves hide scratch and side", () => hasSecondPlayer() && songSelect.Mods.Value.OfType<BmsModHideScratch>().Count() == 1);
        AddStep("reselect first then second player preference", () =>
        {
            scratchConfig!.SetValue(BmsRulesetSetting.DefaultScratchPosition, BmsScratchPosition.FirstPlayer);
            scratchConfig.SetValue(BmsRulesetSetting.DefaultScratchPosition, BmsScratchPosition.SecondPlayer);
        });
        AddAssert("explicit preference does not remove hide scratch or side", () => hasSecondPlayer() && songSelect.Mods.Value.OfType<BmsModHideScratch>().Count() == 1);
        showScratchModOverlay();
        AddStep("remove hide scratch in the mod overlay", () => songSelect.ModSelectOverlay.DeselectAll());
        AddUntilStep("saved second player preference returns", hasSecondPlayer);
        AddUntilStep("hide scratch was removed", () => !songSelect.Mods.Value.OfType<BmsModHideScratch>().Any());
        AddAssert("preference was retained", () => scratchConfig!.Get<BmsScratchPosition>(BmsRulesetSetting.DefaultScratchPosition) == BmsScratchPosition.SecondPlayer);
    }

    [Test]
    public void TestScratchSettingChangedInCourseControlsTheNextSelectionAndRestoration()
    {
        prepareScratchPosition(BmsScratchPosition.SecondPlayer);
        loadSongSelect();
        AddUntilStep("second player selected", hasSecondPlayer);
        AddStep("select second player and mirror", () => songSelect.Mods.Value = [new BmsModSecondPlayer(), new BmsModMirror()]);
        replaceScratchCourseMode(true);
        AddStep("choose first player in course settings", () => scratchConfig!.SetValue(BmsRulesetSetting.DefaultScratchPosition, BmsScratchPosition.FirstPlayer));
        AddAssert("setting applies before starting a new course", () => !hasSecondPlayer());
        AddAssert("other course mods remain selected", () => songSelect.Mods.Value.OfType<BmsModMirror>().Any());
        replaceScratchCourseMode(false);
        AddAssert("first player applies after course restoration", () => !hasSecondPlayer());
        AddAssert("other selected mods restored", () => songSelect.Mods.Value.Single() is BmsModMirror);
    }

    [Test]
    public void TestScratchPreferenceFollowsConvertedManiaSelection()
    {
        BeatmapInfo converted = null!;
        prepareScratchPosition(BmsScratchPosition.SecondPlayer);
        AddStep("import convertible mania metadata", () =>
        {
            var mania = rulesets.AvailableRulesets.FirstOrDefault(info => info.OnlineID == 3)
                        ?? new ManiaMetadataRuleset().RulesetInfo;
            Realm.Write(realm =>
            {
                if (realm.Find<RulesetInfo>("mania") == null)
                    realm.Add(mania.Clone());
            });
            var set = createBeatmapSet(mania);
            foreach (var info in set.Beatmaps)
                info.Difficulty.CircleSize = 7;
            var imported = beatmaps.Import(set);
            Assert.That(imported, Is.Not.Null);
            converted = imported!.Value.Detach().Beatmaps.First();
        });
        loadSongSelect();
        AddUntilStep("native second player selected", hasSecondPlayer);
        AddStep("select converted mania", () => songSelect.LoadBeatmapSelection(converted));
        AddUntilStep("converted selection applied", () => songSelect.Beatmap.Value.BeatmapInfo.ID == converted.ID);
        AddAssert("converted selection keeps second player", hasSecondPlayer);
        AddAssert("overlay keeps recorded layout marker", () => songSelect.ModSelectOverlay.SelectedMods.Value.OfType<BmsModSecondPlayer>().Count() == 1);
        AddAssert("mania metadata remains unchanged", () => songSelect.Beatmap.Value.BeatmapInfo.Ruleset.OnlineID == 3
            && songSelect.Beatmap.Value.BeatmapInfo.Difficulty.CircleSize == 7);
        showScratchModOverlay();
        AddStep("activate hide scratch on converted selection", () => songSelect.ModSelectOverlay.ChildrenOfType<ModPanel>().Single(panel => panel.Mod is BmsModHideScratch).Active.Value = true);
        AddUntilStep("converted selection keeps side with hide scratch", () => hasSecondPlayer() && songSelect.Mods.Value.OfType<BmsModHideScratch>().Any());
        AddStep("close converted mod overlay", () => songSelect.ModSelectOverlay.Hide());
        selectScratchColumns(8);
        AddAssert("native return preserves second player", hasSecondPlayer);
        AddStep("select converted mania again", () => songSelect.LoadBeatmapSelection(converted));
        AddUntilStep("converted selection restored", () => songSelect.Beatmap.Value.BeatmapInfo.ID == converted.ID);
        AddAssert("converted return preserves second player", hasSecondPlayer);
        AddStep("choose first player", () => scratchConfig!.SetValue(BmsRulesetSetting.DefaultScratchPosition, BmsScratchPosition.FirstPlayer));
        AddAssert("converted first player clears only layout marker", () => !hasSecondPlayer());
        AddStep("choose second player", () => scratchConfig!.SetValue(BmsRulesetSetting.DefaultScratchPosition, BmsScratchPosition.SecondPlayer));
        AddAssert("converted second player returns", hasSecondPlayer);
    }

    private void prepareScratchPosition(BmsScratchPosition preference, int selectedColumns = 6)
    {
        AddStep("configure scratch preference and compatible chart metadata", () =>
        {
            scratchConfig = (BmsRulesetConfigManager)RulesetConfigs.GetConfigFor(new BmsRuleset())!;
            scratchConfig.SetValue(BmsRulesetSetting.DefaultScratchPosition, preference);
            var set = createBeatmapSet(Ruleset.Value);
            for (var index = 0; index < set.Beatmaps.Count; index++)
                set.Beatmaps[index].Difficulty.CircleSize = scratch_columns[index % scratch_columns.Length];
            var imported = beatmaps.Import(set);
            Assert.That(imported, Is.Not.Null);
            lampBeatmapSet = imported!.Value.Detach();
            Beatmap.Value = beatmaps.GetWorkingBeatmap(lampBeatmapSet.Beatmaps.First(beatmap => beatmap.Difficulty.CircleSize == selectedColumns), true);
        });
        AddUntilStep("scratch charts imported", () => beatmaps.GetAllUsableBeatmapSets().Any(set => set.ID == lampBeatmapSet.ID));
        AddStep("load parent for song select replacements", () => Stack.Push(new ResumeTestScreen()));
        AddUntilStep("song select parent loaded", () => Stack.CurrentScreen is ResumeTestScreen { IsLoaded: true });
    }

    // This host has no official ruleset assembly; selection needs registered, instantiable source metadata.
    private sealed class ManiaMetadataRuleset : BmsRuleset
    {
        public override string ShortName => "mania";

        public ManiaMetadataRuleset()
        {
            RulesetInfo.OnlineID = 3;
        }
    }

    private void selectScratchColumns(int columns)
    {
        BeatmapInfo selected = null!;
        AddStep($"select chart with {columns} columns", () =>
        {
            selected = lampBeatmapSet.Beatmaps.First(beatmap => beatmap.Difficulty.CircleSize == columns);
            songSelect.LoadBeatmapSelection(selected);
        });
        AddUntilStep("chart selection applied", () => songSelect.Beatmap.Value.BeatmapInfo.ID == selected.ID);
    }

    private void replaceScratchCourseMode(bool courseMode)
    {
        BmsSongSelect previous = null!;
        AddStep(courseMode ? "enter course selection" : "return to normal selection", () =>
        {
            previous = songSelect;
            ((BmsSoloSongSelect)songSelect).ReplaceCourseMode(courseMode);
        });
        AddUntilStep("replacement song select loaded", () => Stack.CurrentScreen is BmsSoloSongSelect replacement
            && !ReferenceEquals(replacement, previous) && replacement.IsLoaded);
        AddStep("follow replacement selection", () => songSelect = (BmsSongSelect)Stack.CurrentScreen);
        AddUntilStep("replacement mode settled", () => courseMode
            ? songSelect.ChildrenOfType<BmsCourseSongSelectController>().Single().IsCourseMode
            : songSelect.CarouselItemsPresented && !songSelect.IsFiltering);
    }

    private bool hasSecondPlayer() => songSelect.Mods.Value.OfType<BmsModSecondPlayer>().Any();

    private void showScratchModOverlay()
    {
        AddStep("open mod overlay", () => songSelect.ModSelectOverlay.Show());
        AddUntilStep("selectable mod panels loaded", () => songSelect.ModSelectOverlay.State.Value == Visibility.Visible
            && songSelect.ModSelectOverlay.ChildrenOfType<ModPanel>().Any(panel => panel.IsLoaded));
        AddAssert("no second player card exists", () => !songSelect.ModSelectOverlay.ChildrenOfType<ModPanel>().Any(panel => panel.Mod is BmsModSecondPlayer));
    }
}
