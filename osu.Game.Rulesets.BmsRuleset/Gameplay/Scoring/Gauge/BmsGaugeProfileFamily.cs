using System;
using osu.Game.Rulesets.BmsRuleset.BmsParser;

namespace osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring.Gauge;

public enum BmsGaugeProfileFamily
{
    FiveKeys,
    SevenKeys,
    Pms,
    Keyboard,
    Lr2,
}

public static class BmsGaugeProfileFamilyProvider
{
    public static BmsGaugeProfileFamily FromLayout(BmsLayoutVariant layout) => layout switch
    {
        BmsLayoutVariant.Bms5K or BmsLayoutVariant.Bms5K2P or BmsLayoutVariant.Bms5KDouble => BmsGaugeProfileFamily.Lr2,
        BmsLayoutVariant.Bme7K or BmsLayoutVariant.Bme7K2P or BmsLayoutVariant.Bme7KDouble => BmsGaugeProfileFamily.Lr2,
        BmsLayoutVariant.Pms9K or BmsLayoutVariant.Pms9K2P or BmsLayoutVariant.Pms9KDouble => BmsGaugeProfileFamily.Lr2,
        _ => throw new ArgumentOutOfRangeException(nameof(layout), layout, null),
    };
}
