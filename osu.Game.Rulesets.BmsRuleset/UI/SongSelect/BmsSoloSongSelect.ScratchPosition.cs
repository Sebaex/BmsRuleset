using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.BmsRuleset.UI.SongSelect;

public partial class BmsSoloSongSelect
{
    private BmsScratchPositionPreference? scratchPositionPreference;
    private Bindable<BmsScratchPosition>? scratchPositionSetting;
    private bool scratchPositionActive;
    private bool applyingScratchPosition;
    private bool scratchPositionWaitingForRestoration;

    private void loadScratchPosition(IRulesetConfigCache configs)
    {
        if (configs.GetConfigFor(new BmsRuleset()) is not BmsRulesetConfigManager config)
            return;

        scratchPositionSetting = config.GetBindable<BmsScratchPosition>(BmsRulesetSetting.DefaultScratchPosition);
        scratchPositionPreference ??= new BmsScratchPositionPreference(scratchPositionSetting.Value);
        scratchPositionSetting.BindValueChanged(scratchPositionChanged);
        Beatmap.BindValueChanged(scratchBeatmapChanged);
        Mods.BindValueChanged(scratchModsChanged);
    }

    public override void OnEntering(ScreenTransitionEvent e)
    {
        base.OnEntering(e);
        resumeScratchPositionPreference();
    }

    public override void OnSuspending(ScreenTransitionEvent e)
    {
        scratchPositionActive = false;
        base.OnSuspending(e);
    }

    protected override void Dispose(bool isDisposing)
    {
        scratchPositionSetting?.UnbindAll();
        if (Beatmap != null)
            Beatmap.ValueChanged -= scratchBeatmapChanged;
        if (Mods != null)
            Mods.ValueChanged -= scratchModsChanged;
        base.Dispose(isDisposing);
    }

    protected override void RestoreInitialMods(IReadOnlyList<Mod> mods)
    {
        applyingScratchPosition = true;
        try
        {
            base.RestoreInitialMods(mods);
        }
        finally
        {
            applyingScratchPosition = false;
        }

        // Course restoration arrives after initial carousel presentation and owns the incoming mod list.
        scratchPositionWaitingForRestoration = false;
        applyScratchPositionPreference();
    }

    private void resumeScratchPositionPreference()
    {
        scratchPositionActive = true;
        applyScratchPositionPreference();
    }

    internal IReadOnlyList<Mod> ApplyScratchPositionToCourse(IReadOnlyList<Mod> mods, IEnumerable<BeatmapInfo> stages)
        => scratchPositionPreference?.ApplyToCourse(mods, stages) ?? mods;

    private void scratchPositionChanged(ValueChangedEvent<BmsScratchPosition> change)
    {
        scratchPositionPreference?.PreferenceChanged(change.NewValue);
        applyScratchPositionPreference();
    }

    private void scratchBeatmapChanged(ValueChangedEvent<WorkingBeatmap> _) => applyScratchPositionPreference();

    private void scratchModsChanged(ValueChangedEvent<IReadOnlyList<Mod>> change)
    {
        if (!canApplyScratchPosition() || applyingScratchPosition)
            return;

        // Presets and score selections can carry an old 2P; only the saved setting owns its state.
        applyScratchPositionPreference();
    }

    private bool canApplyScratchPosition() => scratchPositionActive && !scratchPositionWaitingForRestoration
        && this.IsCurrentScreen()
        && Ruleset.Value.ShortName == Constant.SHORT_NAME && !Mods.Disabled;

    private void applyScratchPositionPreference()
    {
        if (!canApplyScratchPosition() || scratchPositionPreference == null)
            return;

        var mods = scratchPositionPreference.Apply(Mods.Value, scratchLayout());
        if (ReferenceEquals(mods, Mods.Value))
            return;

        applyingScratchPosition = true;
        try
        {
            Mods.Value = mods;
        }
        finally
        {
            applyingScratchPosition = false;
        }
    }

    private BmsLayoutVariant? scratchLayout()
        => Beatmap.IsDefault ? null : BmsRuleset.TryGetLayoutVariantForBeatmap(Beatmap.Value.BeatmapInfo);
}
