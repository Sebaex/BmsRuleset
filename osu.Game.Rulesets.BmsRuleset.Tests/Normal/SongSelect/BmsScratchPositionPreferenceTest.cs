using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Online.API;
using osu.Game.Overlays.Mods;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Mods;
using osu.Game.Rulesets.BmsRuleset.UI.SongSelect;
using osu.Game.Rulesets.Mods;
using osu.Game.Scoring;
using osu.Game.Utils;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.SongSelect;

[TestFixture]
public class BmsScratchPositionPreferenceTest
{
    [TestCase(BmsLayoutVariant.Bms5K)]
    [TestCase(BmsLayoutVariant.Bme7K)]
    [TestCase(BmsLayoutVariant.Bms5K2P)]
    [TestCase(BmsLayoutVariant.Bme7K2P)]
    public void SecondPlayerPreferenceAddsTheRealModOnce(BmsLayoutVariant layout)
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.SecondPlayer);
        IReadOnlyList<Mod> mods = [new BmsModMirror()];
        var applied = preference.Apply(mods, layout);

        Assert.That(applied.OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(1));
        Assert.That(applied[0], Is.SameAs(mods[0]));
        Assert.That(preference.Apply(applied, layout), Is.SameAs(applied));
        Assert.That(ModUtils.CheckCompatibleSet(applied, out _), Is.True);
    }

    [TestCase(BmsLayoutVariant.Pms9K)]
    [TestCase(BmsLayoutVariant.Pms9K2P)]
    [TestCase(BmsLayoutVariant.Bms5KDouble)]
    [TestCase(BmsLayoutVariant.Bme7KDouble)]
    [TestCase(BmsLayoutVariant.Pms9KDouble)]
    public void IncompatibleLayoutsRemoveOnlySecondPlayer(BmsLayoutVariant layout)
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.SecondPlayer);
        IReadOnlyList<Mod> mods = [new BmsModMirror()];
        Assert.That(preference.Apply(mods, layout), Is.SameAs(mods));

        var selected = preference.Apply(mods, BmsLayoutVariant.Bme7K);
        var cleaned = preference.Apply(selected, layout);
        Assert.That(cleaned, Is.EqualTo(mods));
        Assert.That(preference.Apply(cleaned, BmsLayoutVariant.Bms5K).OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(1));
    }

    [Test]
    public void UnknownChartRemovesResidualSecondPlayerAndWaitsForACompatibleSelection()
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.SecondPlayer);
        IReadOnlyList<Mod> mods = [new BmsModMirror()];
        Assert.That(preference.Apply(mods, null), Is.SameAs(mods));
        IReadOnlyList<Mod> residual = [new BmsModSecondPlayer(), mods[0]];
        var cleaned = preference.Apply(residual, null);
        Assert.That(cleaned, Is.EqualTo(mods));
        Assert.That(preference.Apply(cleaned, BmsLayoutVariant.Bme7K).OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(1));
    }

    [Test]
    public void RemovingSecondPlayerFromIncomingModsDoesNotOverrideTheSetting()
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.SecondPlayer);
        IReadOnlyList<Mod> mods = [new BmsModMirror()];
        var selected = preference.Apply(mods, BmsLayoutVariant.Bme7K);
        var withoutSecondPlayer = selected.Where(mod => mod is not BmsModSecondPlayer).ToArray();
        Assert.That(preference.Apply(withoutSecondPlayer, BmsLayoutVariant.Bme7K).OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(1));
        Assert.That(preference.Apply(withoutSecondPlayer, BmsLayoutVariant.Bms5K).OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(1));
    }

    [TestCase(BmsLayoutVariant.Bms5K)]
    [TestCase(BmsLayoutVariant.Bme7K)]
    [TestCase(BmsLayoutVariant.Pms9K)]
    [TestCase(BmsLayoutVariant.Bms5KDouble)]
    [TestCase(null)]
    public void FirstPlayerPreferenceRemovesResidualSecondPlayer(BmsLayoutVariant? layout)
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.FirstPlayer);
        IReadOnlyList<Mod> mods = [new BmsModSecondPlayer(), new BmsModMirror()];
        var applied = preference.Apply(mods, layout);
        Assert.That(applied, Has.Count.EqualTo(1));
        Assert.That(applied[0], Is.SameAs(mods[1]));
        Assert.That(preference.Apply(applied, layout), Is.SameAs(applied));
        Assert.That(preference.Apply(applied, BmsLayoutVariant.Bms5K), Is.SameAs(applied));
    }

    [Test]
    public void FirstPlayerPreferencePreservesOtherModsWithoutAllocating()
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.FirstPlayer);
        IReadOnlyList<Mod> mods = [new BmsModMirror()];
        Assert.That(preference.Apply(mods, BmsLayoutVariant.Bme7K), Is.SameAs(mods));
    }

    [Test]
    public void PreferenceChangesAreTheOnlySourceOfScratchPosition()
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.SecondPlayer);
        var selected = preference.Apply([], BmsLayoutVariant.Bme7K);
        preference.PreferenceChanged(BmsScratchPosition.FirstPlayer);
        Assert.That(preference.Apply(selected, BmsLayoutVariant.Bme7K), Is.Empty);
        preference.PreferenceChanged(BmsScratchPosition.SecondPlayer);
        Assert.That(preference.Apply([], BmsLayoutVariant.Bme7K).OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(1));
    }

    [TestCase(BmsLayoutVariant.Bms5K, false)]
    [TestCase(BmsLayoutVariant.Bms5K, true)]
    [TestCase(BmsLayoutVariant.Bme7K, false)]
    [TestCase(BmsLayoutVariant.Bme7K, true)]
    public void HideScratchKeepsConfiguredSecondPlayer(BmsLayoutVariant layout, bool reverse)
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.SecondPlayer);
        Mod[] mods = reverse ? [new BmsModSecondPlayer(), new BmsModHideScratch()] : [new BmsModHideScratch(), new BmsModSecondPlayer()];
        Assert.That(ModUtils.CheckCompatibleSet(mods, out _), Is.True);
        Assert.That(preference.Apply(mods, layout), Is.SameAs(mods));
        Assert.That(mods.OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(1));
    }

    [Test]
    public void HideScratchAndOtherManualModsArePreserved()
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.SecondPlayer);
        IReadOnlyList<Mod> mods = [new BmsModHideScratch(), new BmsModMirror()];
        var applied = preference.Apply(mods, BmsLayoutVariant.Bme7K);
        Assert.That(applied.OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(1));
        Assert.That(applied.Take(2), Is.EqualTo(mods));
        Assert.That(preference.Apply(applied, BmsLayoutVariant.Bme7K), Is.SameAs(applied));
        Assert.That(ModUtils.CheckCompatibleSet(mods, out _), Is.True);
    }

    [Test]
    public void HideScratchRetainsSecondPlayerBeforeAndAfterItIsRemoved()
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.SecondPlayer);
        IReadOnlyList<Mod> mods = [new BmsModSecondPlayer(), new BmsModHideScratch(), new BmsModMirror()];
        var applied = preference.Apply(mods, BmsLayoutVariant.Bme7K);
        Assert.That(applied, Is.SameAs(mods));
        Assert.That(ModUtils.CheckCompatibleSet(applied, out _), Is.True);
        Assert.That(preference.Apply(applied, BmsLayoutVariant.Bme7K), Is.SameAs(applied));

        var withoutHideScratch = applied.Where(mod => mod is not BmsModHideScratch).ToArray();
        var restored = preference.Apply(withoutHideScratch, BmsLayoutVariant.Bme7K);
        Assert.That(restored.OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(1));
        Assert.That(restored.Single(mod => mod is BmsModMirror), Is.SameAs(mods[2]));
        Assert.That(ModUtils.CheckCompatibleSet(restored, out _), Is.True);
    }

    [Test]
    public void DuplicateResidualSecondPlayerModsAreNormalised()
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.SecondPlayer);
        IReadOnlyList<Mod> mods = [new BmsModSecondPlayer(), new BmsModMirror(), new BmsModSecondPlayer()];
        var applied = preference.Apply(mods, BmsLayoutVariant.Bme7K);
        Assert.That(applied.OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(1));
        Assert.That(applied.Single(mod => mod is not BmsModSecondPlayer), Is.SameAs(mods[1]));
        Assert.That(preference.Apply(applied, BmsLayoutVariant.Bme7K), Is.SameAs(applied));
    }

    [Test]
    public void ThirdPartyConflictKeepsSecondPlayer(
        [Values(BmsLayoutVariant.Bms5K, BmsLayoutVariant.Bme7K)] BmsLayoutVariant layout,
        [Values] bool existingSecondPlayer, [Values] bool reverse)
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.SecondPlayer);
        var mirror = new BmsModMirror();
        var random = new BmsModLaneRandom();
        random.Seed.Value = 431;
        random.IncludeScratch.Value = true;
        random.LaneOrder.Value = "1,0,2,3,4,5";
        var secondPlayer = new BmsModSecondPlayer();
        Mod[] otherMods = reverse ? [random, mirror] : [mirror, random];
        Mod[] incoming = existingSecondPlayer ? [secondPlayer, .. otherMods] : otherMods;
        var applied = preference.Apply(incoming, layout);

        Assert.That(applied.OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(1));
        Assert.That(applied.Where(mod => mod is not BmsModSecondPlayer), Is.EqualTo(otherMods));
        foreach (var mod in otherMods)
            Assert.That(applied.Any(candidate => ReferenceEquals(candidate, mod)), Is.True);
        if (existingSecondPlayer)
        {
            Assert.That(applied, Is.SameAs(incoming));
            Assert.That(applied.OfType<BmsModSecondPlayer>().Single(), Is.SameAs(secondPlayer));
        }

        Assert.That(preference.Apply(applied, layout), Is.SameAs(applied));
        Assert.That(ModUtils.CheckCompatibleSet(applied, out _), Is.False, "The unrelated Mirror/Random conflict remains invalid");
        Assert.That(random.Seed.Value, Is.EqualTo(431));
        Assert.That(random.IncludeScratch.Value, Is.True);
        Assert.That(random.LaneOrder.Value, Is.EqualTo("1,0,2,3,4,5"));
    }

    [Test]
    public void ThirdPartyDuplicateDoesNotRemoveSecondPlayer([Values] bool sameInstance)
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.SecondPlayer);
        var mirror = new BmsModMirror();
        Mod[] incoming = [mirror, sameInstance ? mirror : new BmsModMirror()];
        var applied = preference.Apply(incoming, BmsLayoutVariant.Bme7K);

        Assert.That(applied.OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(1));
        Assert.That(applied.Take(2), Is.EqualTo(incoming));
        Assert.That(applied[0], Is.SameAs(incoming[0]));
        Assert.That(applied[1], Is.SameAs(incoming[1]));
        Assert.That(ModUtils.CheckCompatibleSet(applied, out _), Is.False);
        Assert.That(preference.Apply(applied, BmsLayoutVariant.Bme7K), Is.SameAs(applied));
    }

    [Test]
    public void ThirdPartyConflictInsideMultiModKeepsSecondPlayer([Values] bool existingSecondPlayer)
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.SecondPlayer);
        var group = new MultiMod(new BmsModMirror(), new MultiMod(new BmsModLaneRandom()));
        Mod[] incoming = existingSecondPlayer ? [new BmsModSecondPlayer(), group] : [group];
        var applied = preference.Apply(incoming, BmsLayoutVariant.Bme7K);

        Assert.That(applied.OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(1));
        Assert.That(applied.Single(mod => mod is MultiMod), Is.SameAs(group));
        Assert.That(ModUtils.CheckCompatibleSet(applied, out _), Is.False);
        Assert.That(preference.Apply(applied, BmsLayoutVariant.Bme7K), Is.SameAs(applied));
    }

    [Test]
    public void RealSecondPlayerConflictRemovesOnlySecondPlayer([Values] bool declaredBySecondPlayer, [Values] bool reverse)
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.SecondPlayer);
        BmsModSecondPlayer secondPlayer = declaredBySecondPlayer ? new SecondPlayerRejectingMirror() : new BmsModSecondPlayer();
        Mod other = declaredBySecondPlayer ? new BmsModMirror() : new MirrorRejectingSecondPlayer();
        Mod[] incoming = reverse ? [other, secondPlayer] : [secondPlayer, other];

        Assert.That(ModUtils.CheckCompatibleSet(incoming, out _), Is.False);
        var applied = preference.Apply(incoming, BmsLayoutVariant.Bme7K);
        Assert.That(applied, Has.Count.EqualTo(1));
        Assert.That(applied[0], Is.SameAs(other));
    }

    [Test]
    public void CompatibleRandomPreservesSettingsAndReferences([Values] bool existingSecondPlayer)
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.SecondPlayer);
        var random = new BmsModLaneRandom();
        random.Seed.Value = 431;
        random.IncludeScratch.Value = true;
        Mod[] incoming = existingSecondPlayer ? [new BmsModSecondPlayer(), random] : [random];
        var applied = preference.Apply(incoming, BmsLayoutVariant.Bme7K);

        Assert.That(applied.OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(1));
        Assert.That(applied.OfType<BmsModLaneRandom>().Single(), Is.SameAs(random));
        Assert.That(random.Seed.Value, Is.EqualTo(431));
        Assert.That(random.IncludeScratch.Value, Is.True);
        Assert.That(ModUtils.CheckCompatibleSet(applied, out _), Is.True);
        Assert.That(preference.Apply(applied, BmsLayoutVariant.Bme7K), Is.SameAs(applied));
        if (existingSecondPlayer)
            Assert.That(applied, Is.SameAs(incoming));
    }

    [Test]
    public void ThirdPartyConflictNotificationsSettle([Values] bool existingSecondPlayer)
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.SecondPlayer);
        var selected = new Bindable<IReadOnlyList<Mod>>([]);
        var updates = 0;
        selected.BindValueChanged(change =>
        {
            Assert.That(++updates, Is.LessThanOrEqualTo(2), "Reconciliation must settle without a notification loop");
            var applied = preference.Apply(change.NewValue, BmsLayoutVariant.Bme7K);
            if (!ReferenceEquals(applied, change.NewValue))
                selected.Value = applied;
        });
        selected.Value = existingSecondPlayer
            ? [new BmsModSecondPlayer(), new BmsModMirror(), new BmsModLaneRandom()]
            : [new BmsModMirror(), new BmsModLaneRandom()];

        Assert.That(selected.Value, Has.Count.EqualTo(3));
        Assert.That(selected.Value.OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(1));
        Assert.That(updates, Is.EqualTo(existingSecondPlayer ? 1 : 2));
        Assert.That(preference.Apply(selected.Value, BmsLayoutVariant.Bme7K), Is.SameAs(selected.Value));
    }

    [Test]
    public void ThirdPartyConflictInMixedCourseKeepsSecondPlayer()
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.SecondPlayer);
        BeatmapInfo[] stages = [createCourseStage(BmsLayout.PMS_COLUMNS), createCourseStage(BmsLayout.BME7_KEY_COLUMNS)];
        Mod[] incoming = [new BmsModSecondPlayer(), new BmsModMirror(), new BmsModLaneRandom()];

        Assert.That(preference.ApplyToCourse(incoming, stages), Is.SameAs(incoming));
        Assert.That(ModUtils.CheckCompatibleSet(incoming, out _), Is.False);
    }

    [Test]
    public void ThirdPartyConflictPresetKeepsSecondPlayer()
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.SecondPlayer);
        var selected = new Bindable<IReadOnlyList<Mod>>(preference.Apply([], BmsLayoutVariant.Bme7K));
        selected.BindValueChanged(change =>
        {
            var applied = preference.Apply(change.NewValue, BmsLayoutVariant.Bme7K);
            if (!ReferenceEquals(applied, change.NewValue))
                selected.Value = applied;
        });
        using var panel = createPresetPanel(selected, [new BmsModMirror(), new BmsModLaneRandom()]);

        panel.Toggle();
        Assert.That(selected.Value.OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(1));
        Assert.That(selected.Value.OfType<BmsModMirror>().Count(), Is.EqualTo(1));
        Assert.That(selected.Value.OfType<BmsModLaneRandom>().Count(), Is.EqualTo(1));
        Assert.That(ModUtils.CheckCompatibleSet(selected.Value, out _), Is.False);
        // This headless panel has not run LoadComplete, which normally refreshes its active state.
        typeof(ModPresetPanel).GetMethod("updateActiveState", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(panel, null);
        Assert.That(panel.Active.Value, Is.True);
        panel.Toggle();
        Assert.That(selected.Value.Single(), Is.TypeOf<BmsModSecondPlayer>());
    }

    private sealed class MirrorRejectingSecondPlayer : BmsModMirror
    {
        public override System.Type[] IncompatibleMods => [typeof(BmsModSecondPlayer)];
    }

    private sealed class SecondPlayerRejectingMirror : BmsModSecondPlayer
    {
        public override System.Type[] IncompatibleMods => [.. base.IncompatibleMods, typeof(BmsModMirror)];
    }

    [Test]
    public void SecondPlayerIsUserPlayableAfterHistoricalDeserialisation()
    {
        var stored = JsonConvert.DeserializeObject<APIMod>("""{"acronym":"2P"}""")!;
        var restored = stored.ToMod(new BmsRuleset());

        Assert.Multiple(() =>
        {
            Assert.That(restored.UserPlayable, Is.True);
            Assert.That(restored.Type, Is.EqualTo(ModType.System));
            Assert.That(new APIMod(restored).Acronym, Is.EqualTo("2P"));
            Assert.That(ModUtils.IsValidModForMatch(restored, true, osu.Game.Online.Rooms.MatchType.HeadToHead, false), Is.False);
            Assert.That(new BmsModAutoplay().UserPlayable, Is.False);
            Assert.That(new BmsModPaused().UserPlayable, Is.False);
            Assert.That(new BmsModBranchReplay().UserPlayable, Is.False);
        });
    }

    [Test]
    public void SecondPlayerRemainsRecognisedAsASystemModOutsideConversionSelection()
    {
        var ruleset = new BmsRuleset();
        Assert.That(ruleset.GetModsFor(ModType.Conversion).OfType<BmsModSecondPlayer>(), Is.Empty);
        Assert.That(ruleset.GetModsFor(ModType.System).OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(1));
        var restored = ruleset.CreateModFromAcronym("2P");
        Assert.That(restored, Is.TypeOf<BmsModSecondPlayer>());
        Assert.That(restored!.Type, Is.EqualTo(ModType.System));
        Assert.That(ModUtils.CheckModsBelongToRuleset(ruleset, [restored]), Is.True);
    }

    [Test]
    public void HistoricalSecondPlayerAcronymStillRestoresItsLayout()
    {
        var stored = JsonConvert.DeserializeObject<APIMod>("""{"acronym":"2P"}""")!;
        var restored = stored.ToMod(new BmsRuleset());
        var beatmap = new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K, TotalColumns = BmsLayout.BME7_KEY_COLUMNS };

        Assert.That(restored, Is.TypeOf<BmsModSecondPlayer>());
        Assert.That(restored.Type, Is.EqualTo(ModType.System));
        ((IApplicableAfterBeatmapConversion)restored).ApplyToBeatmap(beatmap);
        Assert.That(beatmap.LayoutVariant, Is.EqualTo(BmsLayoutVariant.Bme7K2P));
    }

    [TestCase(BmsScratchPosition.FirstPlayer)]
    [TestCase(BmsScratchPosition.SecondPlayer)]
    public void HistoricalPresetNeverIntroducesDuplicateSecondPlayerAndRespectsSettings(BmsScratchPosition position)
    {
        var preference = new BmsScratchPositionPreference(position);
        var selected = new Bindable<IReadOnlyList<Mod>>(preference.Apply([], BmsLayoutVariant.Bme7K));
        var maximumIncomingSecondPlayerCount = 0;
        selected.BindValueChanged(change =>
        {
            maximumIncomingSecondPlayerCount = System.Math.Max(maximumIncomingSecondPlayerCount, change.NewValue.OfType<BmsModSecondPlayer>().Count());
            var applied = preference.Apply(change.NewValue, BmsLayoutVariant.Bme7K);
            if (!ReferenceEquals(applied, change.NewValue))
                selected.Value = applied;
        });
        using var panel = createPresetPanel(selected, [new BmsModSecondPlayer(), new BmsModMirror()]);

        panel.Toggle();
        Assert.That(maximumIncomingSecondPlayerCount, Is.EqualTo(1), "The preset panel must drop the previous internal 2P before publishing the preset");
        Assert.That(selected.Value.OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(position == BmsScratchPosition.SecondPlayer ? 1 : 0));
        Assert.That(selected.Value.OfType<BmsModMirror>().Count(), Is.EqualTo(1));
        Assert.That(ModUtils.CheckCompatibleSet(selected.Value, out _), Is.True);

        panel.Toggle();
        Assert.That(maximumIncomingSecondPlayerCount, Is.EqualTo(1));
        Assert.That(selected.Value.OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(position == BmsScratchPosition.SecondPlayer ? 1 : 0));
    }

    [Test]
    public void HideScratchPresetKeepsInternalSecondPlayerBeforeAndAfterPublishing()
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.SecondPlayer);
        var selected = new Bindable<IReadOnlyList<Mod>>(preference.Apply([], BmsLayoutVariant.Bme7K));
        var incompatibleIncomingSelections = 0;
        selected.BindValueChanged(change =>
        {
            if (!ModUtils.CheckCompatibleSet(change.NewValue, out _))
                incompatibleIncomingSelections++;
            var applied = preference.Apply(change.NewValue, BmsLayoutVariant.Bme7K);
            if (!ReferenceEquals(applied, change.NewValue))
                selected.Value = applied;
        });
        using var hideScratchPreset = createPresetPanel(selected, [new BmsModHideScratch(), new BmsModMirror()]);

        hideScratchPreset.Toggle();
        Assert.That(incompatibleIncomingSelections, Is.Zero, "Preset application must already respect incompatibility declared by the internal System mod");
        Assert.That(selected.Value.OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(1));
        Assert.That(selected.Value.OfType<BmsModHideScratch>().Count(), Is.EqualTo(1));
        Assert.That(selected.Value.OfType<BmsModMirror>().Count(), Is.EqualTo(1));

        using var normalPreset = createPresetPanel(selected, [new BmsModMirror()]);
        normalPreset.Toggle();
        Assert.That(incompatibleIncomingSelections, Is.Zero);
        Assert.That(selected.Value.OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(1));
        Assert.That(selected.Value.OfType<BmsModHideScratch>(), Is.Empty);
        Assert.That(selected.Value.OfType<BmsModMirror>().Count(), Is.EqualTo(1));
        Assert.That(ModUtils.CheckCompatibleSet(selected.Value, out _), Is.True);
    }

    [TestCase(BmsLayoutVariant.Bms5K, BmsLayoutVariant.Bms5K2P)]
    [TestCase(BmsLayoutVariant.Bme7K, BmsLayoutVariant.Bme7K2P)]
    public void ScoreModsRoundTripAndReplayTheSecondPlayerLayout(BmsLayoutVariant layout, BmsLayoutVariant expected)
    {
        var ruleset = new BmsRuleset();
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.SecondPlayer);
        var score = new ScoreInfo { Ruleset = ruleset.RulesetInfo, Mods = preference.Apply([], layout).ToArray() };
        var json = JsonConvert.SerializeObject(score.Mods.Select(mod => new APIMod(mod)).ToArray());
        var restored = JsonConvert.DeserializeObject<APIMod[]>(json)!.Select(mod => mod.ToMod(ruleset)).ToArray();
        var beatmap = new BmsBeatmap { LayoutVariant = layout, TotalColumns = BmsLayout.GetTotalColumns(layout) };

        Assert.That(restored.Single(), Is.TypeOf<BmsModSecondPlayer>());
        Assert.That(restored.Single().Type, Is.EqualTo(ModType.System));
        ((IApplicableAfterBeatmapConversion)restored.Single()).ApplyToBeatmap(beatmap);
        Assert.That(beatmap.LayoutVariant, Is.EqualTo(expected));
        Assert.That(BmsLayout.Is2P(beatmap.LayoutVariant), Is.True);
    }

    [TestCase(BmsLayout.PMS_COLUMNS, BmsLayout.BMS5_KEY_COLUMNS)]
    [TestCase(BmsLayout.BMS5_KEY_COLUMNS, BmsLayout.PMS_COLUMNS)]
    [TestCase(BmsLayout.DOUBLE_PLAY_COLUMNS, BmsLayout.BME7_KEY_COLUMNS)]
    [TestCase(BmsLayout.BME7_KEY_COLUMNS, BmsLayout.DOUBLE_PLAY_COLUMNS)]
    public void MixedCourseUsesSinglePlayPreferenceRegardlessOfPreviewOrder(int firstColumns, int secondColumns)
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.SecondPlayer);
        var stages = new[] { createCourseStage(firstColumns), createCourseStage(secondColumns) };
        IReadOnlyList<Mod> mods = [new BmsModMirror()];
        var applied = preference.ApplyToCourse(mods, stages);

        Assert.That(applied.OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(1));
        Assert.That(applied[0], Is.SameAs(mods[0]));
        Assert.That(preference.ApplyToCourse(applied, stages), Is.SameAs(applied));
        Assert.That(ModUtils.CheckCompatibleSet(applied, out _), Is.True);
    }

    [Test]
    public void CourseContainingOnlyPmsAndDoublePlayRemovesResidualSecondPlayer()
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.SecondPlayer);
        var stages = new[]
        {
            createCourseStage(BmsLayout.PMS_COLUMNS),
            createCourseStage(BmsLayout.BMS5_DOUBLE_PLAY_COLUMNS),
            createCourseStage(BmsLayout.DOUBLE_PLAY_COLUMNS),
            createCourseStage(BmsLayout.PMS_DOUBLE_PLAY_COLUMNS),
        };
        IReadOnlyList<Mod> mods = [new BmsModSecondPlayer(), new BmsModMirror()];
        var applied = preference.ApplyToCourse(mods, stages);

        Assert.That(applied, Has.Count.EqualTo(1));
        Assert.That(applied[0], Is.SameAs(mods[1]));
        Assert.That(preference.ApplyToCourse(applied, stages), Is.SameAs(applied));
    }

    [Test]
    public void FirstPlayerSettingRemovesResidualSecondPlayerFromMixedCourse()
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.FirstPlayer);
        var stages = new[] { createCourseStage(BmsLayout.PMS_COLUMNS), createCourseStage(BmsLayout.BMS5_KEY_COLUMNS) };
        IReadOnlyList<Mod> mods = [new BmsModSecondPlayer(), new BmsModMirror()];
        var applied = preference.ApplyToCourse(mods, stages);

        Assert.That(applied, Has.Count.EqualTo(1));
        Assert.That(applied[0], Is.SameAs(mods[1]));
    }

    [Test]
    public void MixedCoursePreservesHideScratchAndOtherMods()
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.SecondPlayer);
        var stages = new[] { createCourseStage(BmsLayout.DOUBLE_PLAY_COLUMNS), createCourseStage(BmsLayout.BME7_KEY_COLUMNS) };
        IReadOnlyList<Mod> mods = [new BmsModSecondPlayer(), new BmsModHideScratch(), new BmsModMirror()];
        var applied = preference.ApplyToCourse(mods, stages);

        Assert.That(applied, Is.SameAs(mods));
        Assert.That(ModUtils.CheckCompatibleSet(applied, out _), Is.True);
    }

    [Test]
    public void HideScratchStillExcludesAutoScratchAndFirstPlayerDoesNotAddSecondPlayer()
    {
        Assert.That(ModUtils.CheckCompatibleSet([new BmsModHideScratch(), new BmsModAutoScratch()], out _), Is.False);
        Assert.That(ModUtils.CheckCompatibleSet([new BmsModAutoScratch(), new BmsModSecondPlayer()], out _), Is.True);
        Assert.That(ModUtils.CheckCompatibleSet([new BmsModSecondPlayer(), new BmsModSecondPlayer()], out _), Is.False);
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.FirstPlayer);
        IReadOnlyList<Mod> mods = [new BmsModHideScratch()];
        Assert.That(preference.Apply(mods, BmsLayoutVariant.Bme7K), Is.SameAs(mods));
    }

    [Test]
    public void PositionChangedBeforeCourseStartDeterminesThePreparedCourseMods()
    {
        var preference = new BmsScratchPositionPreference(BmsScratchPosition.SecondPlayer);
        var stages = new[] { createCourseStage(BmsLayout.PMS_COLUMNS), createCourseStage(BmsLayout.BME7_KEY_COLUMNS) };
        var selected = preference.ApplyToCourse([], stages);

        preference.PreferenceChanged(BmsScratchPosition.FirstPlayer);
        var firstPlayer = preference.ApplyToCourse(selected, stages);
        Assert.That(firstPlayer, Is.Empty);

        preference.PreferenceChanged(BmsScratchPosition.SecondPlayer);
        Assert.That(preference.ApplyToCourse(firstPlayer, stages).OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(1));
    }

    [TestCase(BmsLayoutVariant.Pms9K)]
    [TestCase(BmsLayoutVariant.Bms5KDouble)]
    [TestCase(BmsLayoutVariant.Bme7KDouble)]
    [TestCase(BmsLayoutVariant.Pms9KDouble)]
    public void InternalSecondPlayerPreservesIncompatibleCourseStageLayoutAndColumnCount(BmsLayoutVariant layout)
    {
        var columns = BmsLayout.GetTotalColumns(layout);
        var beatmap = new BmsBeatmap { LayoutVariant = layout, TotalColumns = columns };

        new BmsModSecondPlayer().ApplyToBeatmap(beatmap);

        Assert.That(beatmap.LayoutVariant, Is.EqualTo(layout));
        Assert.That(beatmap.TotalColumns, Is.EqualTo(columns));
    }

    [TestCase(BmsScratchPosition.FirstPlayer, false)]
    [TestCase(BmsScratchPosition.FirstPlayer, true)]
    [TestCase(BmsScratchPosition.SecondPlayer, false)]
    [TestCase(BmsScratchPosition.SecondPlayer, true)]
    public void ConvertedManiaStageUsesScratchPreference(BmsScratchPosition position, bool reverse)
    {
        var preference = new BmsScratchPositionPreference(position);
        var converted = new BeatmapInfo(new RulesetInfo { OnlineID = 3, ShortName = "mania" },
            new BeatmapDifficulty { CircleSize = 7 });
        BeatmapInfo[] stages = [createCourseStage(BmsLayout.PMS_COLUMNS), converted];
        if (reverse)
            System.Array.Reverse(stages);
        IReadOnlyList<Mod> mods = [new BmsModMirror()];
        var applied = preference.ApplyToCourse(mods, stages);

        Assert.That(applied.OfType<BmsModSecondPlayer>().Count(), Is.EqualTo(position == BmsScratchPosition.SecondPlayer ? 1 : 0));
        Assert.That(applied[0], Is.SameAs(mods[0]));
        Assert.That(preference.ApplyToCourse(applied, stages), Is.SameAs(applied));
        Assert.That(converted.Difficulty.CircleSize, Is.EqualTo(7));
        Assert.That(converted.Ruleset.ShortName, Is.EqualTo("mania"));
    }

    private static BeatmapInfo createCourseStage(int columns)
        => new(new BmsRuleset().RulesetInfo, new BeatmapDifficulty { CircleSize = columns });

    private static ModPresetPanel createPresetPanel(Bindable<IReadOnlyList<Mod>> selected, Mod[] mods)
    {
        var preset = new ModPreset
        {
            Name = "Scratch position regression",
            Ruleset = new BmsRuleset().RulesetInfo,
            Mods = mods,
        };
        var panel = new ModPresetPanel(preset.ToLiveUnmanaged());
        typeof(ModPresetPanel).GetProperty("selectedMods", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(panel, selected);
        return panel;
    }
}
