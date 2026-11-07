using System;
using osu.Framework.Graphics.Colour;
using osu.Game.Scoring;
using osu.Game.Rulesets.BmsRuleset.UI.SongSelect.Lamp;
using osuTK.Graphics;

namespace osu.Game.Rulesets.BmsRuleset.UI.Result;

internal static class BmsResultColours
{
    internal static readonly Color4 ACCENT = new(50, 217, 241, 255);
    internal static readonly Color4 FAST = new(90, 175, 255, 255);
    internal static readonly Color4 SLOW = new(255, 130, 92, 255);
    internal static readonly ColourInfo PROGRESS = ColourInfo.GradientHorizontal(new Color4(64, 174, 255, 255), ACCENT);

    internal static Color4 ClearTypeColor(ScoreInfo? score) => BmsLampCalculator.Calculate(score) switch
    {
        BmsLamp.NoPlay => new Color4(255, 255, 255, 255),
        BmsLamp.Failed => new Color4(233, 47, 10, 255),
        BmsLamp.AssistClear => new Color4(165, 0, 206, 255),
        BmsLamp.EasyClear => new Color4(0, 215, 15, 255),
        BmsLamp.Clear => new Color4(90, 175, 255, 255),
        BmsLamp.HardClear => new Color4(255, 130, 92, 255),
        BmsLamp.ExHardClear => new Color4(255, 253, 67, 255),
        BmsLamp.FullCombo => new Color4(120, 255, 247, 255),
        BmsLamp.Perfect => new Color4(221, 200, 225, 255),
        BmsLamp.Max => new Color4(255, 255, 255, 255),
        _ => throw new ArgumentOutOfRangeException(),
    };
}
