using osu.Framework.Localisation;
using osu.Game.Rulesets.BmsRuleset.Localisation;

namespace osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;

public enum BmsScratchPosition
{
    [LocalisableDescription(typeof(BmsStrings), nameof(BmsStrings.ScratchPositionFirstPlayer))]
    FirstPlayer,

    [LocalisableDescription(typeof(BmsStrings), nameof(BmsStrings.ScratchPositionSecondPlayer))]
    SecondPlayer,
}
