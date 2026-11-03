using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Mods;
using osu.Game.Rulesets.Mods;
using osu.Game.Utils;

namespace osu.Game.Rulesets.BmsRuleset.UI.SongSelect;

internal sealed class BmsScratchPositionPreference(BmsScratchPosition position)
{
    private BmsScratchPosition position = position;

    internal void PreferenceChanged(BmsScratchPosition newPosition) => position = newPosition;

    internal IReadOnlyList<Mod> ApplyToCourse(IReadOnlyList<Mod> mods, IEnumerable<BeatmapInfo> stages)
    {
        // A course shares one mod set; its random preview must not decide the side for later 5K/7K stages.
        var hasSinglePlay = stages.Any(stage => BmsRuleset.TryGetLayoutVariantForBeatmap(stage)
            is BmsLayoutVariant.Bms5K or BmsLayoutVariant.Bme7K or BmsLayoutVariant.Bms5K2P or BmsLayoutVariant.Bme7K2P);
        return Apply(mods, hasSinglePlay ? BmsLayoutVariant.Bms5K : null);
    }

    internal IReadOnlyList<Mod> Apply(IReadOnlyList<Mod> mods, BmsLayoutVariant? layout)
    {
        var secondPlayer = mods.OfType<BmsModSecondPlayer>().ToArray();
        var otherMods = withoutSecondPlayer(mods);
        if (position != BmsScratchPosition.SecondPlayer
            || layout is not (BmsLayoutVariant.Bms5K or BmsLayoutVariant.Bme7K or BmsLayoutVariant.Bms5K2P or BmsLayoutVariant.Bme7K2P))
            return otherMods;

        var selectedSecondPlayer = secondPlayer.FirstOrDefault() ?? new BmsModSecondPlayer();
        // Unrelated conflicts still belong to the mod selector; they must not override the saved side.
        if (ModUtils.FlattenMods(otherMods).Any(mod => !ModUtils.CheckCompatibleSet([selectedSecondPlayer, mod], out _)))
            return otherMods;

        return secondPlayer.Length == 1 ? mods : [.. otherMods, selectedSecondPlayer];
    }

    private static IReadOnlyList<Mod> withoutSecondPlayer(IReadOnlyList<Mod> mods)
        => mods.Any(mod => mod is BmsModSecondPlayer) ? mods.Where(mod => mod is not BmsModSecondPlayer).ToArray() : mods;
}
