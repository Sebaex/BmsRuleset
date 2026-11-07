using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring;
using osu.Game.Rulesets.BmsRuleset.Localisation;
using osu.Game.Rulesets.BmsRuleset.UI.Result.Statistic;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.BmsRuleset.UI.HudComponents;

public sealed partial class BmsHealthValueDisplay : BmsHudComponent
{
    private readonly OsuSpriteText healthText;
    private BindableNumber<double>? health;

    [Resolved]
    private HealthProcessor healthProcessor { get; set; } = null!;

    public BmsHealthValueDisplay()
    {
        Size = new Vector2(120, 40);
        InternalChild = new BmsResultFittedContainer(healthText = new OsuSpriteText
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            Font = OsuFont.GetFont(size: 28, weight: FontWeight.Bold),
            UseFullGlyphHeight = false,
        });
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();

        health = healthProcessor.Health.GetBoundCopy();
        health.BindValueChanged(updateValue, true);
    }

    protected override void Dispose(bool isDisposing)
    {
        health?.UnbindAll();
        base.Dispose(isDisposing);
    }

    private void updateValue(ValueChangedEvent<double> value)
    {
        var healthValue = double.IsFinite(value.NewValue) ? Math.Clamp(value.NewValue, 0, 1) : 0;
        healthText.Text = BmsStrings.HealthPercentage(Math.Truncate(healthValue * 1000 + 1e-9) / 1000);
    }
}
