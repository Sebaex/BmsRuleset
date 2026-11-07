using osu.Framework.Graphics;
using osu.Game.Rulesets.BmsRuleset.Localisation;
using osu.Game.Rulesets.BmsRuleset.UI.Result;
using osu.Game.Rulesets.BmsRuleset.UI.Result.Statistic;
using osu.Game.Scoring;
using osuTK;

internal partial class BmsResultClearDisplay : BmsResultFittedText
{
    internal BmsResultClearDisplay(ScoreInfo? score)
        : base(
            BmsStrings.ResultClearType(score),
            40,
            Anchor.Centre,
            BmsResultColours.ClearTypeColor(score),
            useFullGlyphHeight: false)
    {
        Anchor = Anchor.Centre;
        Origin = Anchor.Centre;
    }
}