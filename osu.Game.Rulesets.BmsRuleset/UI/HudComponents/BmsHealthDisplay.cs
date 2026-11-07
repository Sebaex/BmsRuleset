using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Utils;
using osu.Game.Rulesets.BmsRuleset.Localisation;
using osu.Game.Rulesets.Scoring;
using osuTK;
using osuTK.Graphics;
using osu.Game.Configuration;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring.Gauge;
using osu.Game.Rulesets.BmsRuleset.UI.Gameplay;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.BmsRuleset.UI.HudComponents;

/// <summary>
///     BMS-style groove gauge display that replaces osu!'s native health bar UI.
/// </summary>
public sealed partial class BmsHealthDisplay : BmsHudComponent
{
    [SettingSource(typeof(BmsStrings), nameof(BmsStrings.AssistEasyGaugeFillColour), nameof(BmsStrings.AssistEasyGaugeFillColourDescription))]
    public BindableColour4 AssistEasyFillColour { get; } = new(new Color4(200, 0, 255, 255));

    [SettingSource(typeof(BmsStrings), nameof(BmsStrings.EasyGaugeFillColour), nameof(BmsStrings.EasyGaugeFillColourDescription))]
    public BindableColour4 EasyFillColour { get; } = new(new Color4(45, 225, 80, 255));

    [SettingSource(typeof(BmsStrings), nameof(BmsStrings.NormalGaugeFillColour), nameof(BmsStrings.NormalGaugeFillColourDescription))]
    public BindableColour4 NormalFillColour { get; } = new(new Color4(80, 180, 255, 255));

    [SettingSource(typeof(BmsStrings), nameof(BmsStrings.HardGaugeFillColour), nameof(BmsStrings.HardGaugeFillColourDescription))]
    public BindableColour4 HardFillColour { get; } = new(new Color4(220, 55, 50, 255));

    [SettingSource(typeof(BmsStrings), nameof(BmsStrings.ExHardGaugeFillColour), nameof(BmsStrings.ExHardGaugeFillColourDescription))]
    public BindableColour4 ExHardFillColour { get; } = new(new Color4(255, 215, 0, 255));

    [SettingSource(typeof(BmsStrings), nameof(BmsStrings.HazardGaugeFillColour), nameof(BmsStrings.HazardGaugeFillColourDescription))]
    public BindableColour4 HazardFillColour { get; } = new(new Color4(198, 198, 198, 255));

    private const float clear_threshold = 0.8f;
    private static readonly Color4 above_clear_colour = new(255, 45, 40, 255);

    private readonly Box normalFill;
    private readonly Box aboveClearFill;
    private readonly Box clearLine;
    private readonly Box gaugeSegment;

    private BindableNumber<double>? health;
    private double displayedHealth;
    private BmsHealthProcessor? bmsHealthProcessor;
    private IBmsGameplayEvents? gameplayEvents;

    private float lastFillHeight = float.NaN;
    private Color4 lastFillColour;
    private bool hasFillColour;
    private float lastGaugeSegmentHeight = float.NaN;
    private float lastGaugeSegmentAlpha = float.NaN;
    private float lastClearLineY = float.NaN;
    private float lastClearLineAlpha = float.NaN;

    private readonly Bindable<BmsGaugeDisplayProfile> displayProfile =
        new(BmsGaugeProfileFactory.Create(BmsGaugeType.Normal).Display);

    [Resolved]
    private HealthProcessor healthProcessor { get; set; } = null!;

    [BackgroundDependencyLoader(true)]
    private void load(DrawableRuleset? drawableRuleset)
    {
        gameplayEvents = (drawableRuleset as BmsDrawableRuleset)?.GameplayEvents;
        if (gameplayEvents != null) gameplayEvents.ReplaySeeked += synchroniseReplayState;
    }

    protected override void Dispose(bool isDisposing)
    {
        if (gameplayEvents != null) gameplayEvents.ReplaySeeked -= synchroniseReplayState;
        base.Dispose(isDisposing);
    }

    private void synchroniseReplayState()
    {
        displayedHealth = healthProcessor.Health.Value;
        updateDisplay();
    }

    public BmsHealthDisplay()
    {
        Size = new Vector2(30, 500);

        InternalChildren =
        [
            new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.5f, 1f),
                Children =
                [
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(8, 10, 14, 255),
                    },
                    gaugeSegment = new GaugeSegment
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        Colour = new Color4(16, 42, 52, 255),
                    },
                    normalFill = new Box
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        RelativeSizeAxes = Axes.Both,
                        Width = 1,
                    },
                    aboveClearFill = new Box
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        RelativeSizeAxes = Axes.Both,
                        RelativePositionAxes = Axes.Y,
                        Width = 1,
                        Colour = above_clear_colour,
                    },
                    clearLine = new Box
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.CentreLeft,
                        RelativeSizeAxes = Axes.X,
                        RelativePositionAxes = Axes.Y,
                        Y = -clear_threshold,
                        Height = 2,
                        Colour = new Color4(255, 240, 120, 255),
                    },
                ],
            },
        ];
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();

        health = healthProcessor.Health.GetBoundCopy();

        if (healthProcessor is BmsHealthProcessor bmsHp)
        {
            bmsHealthProcessor = bmsHp;
            displayProfile.BindTo(bmsHp.DisplayProfile);
        }

        displayedHealth = health.Value;
        updateDisplay();
    }

    protected override void Update()
    {
        base.Update();

        if (health == null)
            return;

        displayedHealth = Interpolation.DampContinuously(displayedHealth, health.Value, 35, Time.Elapsed);
        updateDisplay();
    }

    private void updateDisplay()
    {
        var clampedHealth = double.IsFinite(displayedHealth) ? Math.Clamp(displayedHealth, 0, 1) : 0;
        var profile = displayProfile.Value;
        var clearThreshold = profile.ClearThreshold;
        var clearThresholdValue = clearThreshold ?? clear_threshold;
        var hasAboveClearZone = profile.ColourMode == BmsGaugeColourMode.GrooveDynamic
                                && clearThreshold.HasValue
                                && clampedHealth > clearThresholdValue;
        var baseFillHeight = (float)(hasAboveClearZone ? clearThresholdValue : clampedHealth);
        var fillColour = resolveFixedColour(profile);

        if (baseFillHeight != lastFillHeight)
        {
            lastFillHeight = baseFillHeight;
            normalFill.Height = baseFillHeight;
        }

        if (!hasFillColour || fillColour != lastFillColour)
        {
            hasFillColour = true;
            lastFillColour = fillColour;
            normalFill.Colour = fillColour;
        }

        var aboveClearHeight = hasAboveClearZone ? (float)(clampedHealth - clearThresholdValue) : 0;
        aboveClearFill.Height = aboveClearHeight;
        aboveClearFill.Y = hasAboveClearZone ? -(float)clearThresholdValue : 0;
        aboveClearFill.Alpha = hasAboveClearZone ? 1 : 0;

        if (profile.ClearThreshold is double threshold)
        {
            var segmentHeight = (float)threshold;
            if (segmentHeight != lastGaugeSegmentHeight)
            {
                lastGaugeSegmentHeight = segmentHeight;
                gaugeSegment.Height = segmentHeight;
            }

            if (lastGaugeSegmentAlpha != 1)
            {
                lastGaugeSegmentAlpha = 1;
                gaugeSegment.Alpha = 1;
            }
        }
        else
        {
            if (lastGaugeSegmentAlpha != 0)
            {
                lastGaugeSegmentAlpha = 0;
                gaugeSegment.Alpha = 0;
            }
        }

        if (profile.ShowClearLine && profile.ClearThreshold is double)
        {
            var clearLineY = -(float)clearThreshold;
            if (clearLineY != lastClearLineY)
            {
                lastClearLineY = clearLineY;
                clearLine.Y = clearLineY;
            }

            var clearLineAlpha = clampedHealth >= clearThreshold ? 0.85f : 1;
            if (clearLineAlpha != lastClearLineAlpha)
            {
                lastClearLineAlpha = clearLineAlpha;
                clearLine.Alpha = clearLineAlpha;
            }
        }
        else
        {
            if (lastClearLineAlpha != 0)
            {
                lastClearLineAlpha = 0;
                clearLine.Alpha = 0;
            }
        }
    }

    private Color4 resolveFixedColour(BmsGaugeDisplayProfile profile)
    {
        if (bmsHealthProcessor == null)
            return profile.FillColour;

        return bmsHealthProcessor.GaugeType switch
        {
            BmsGaugeType.AssistEasy => AssistEasyFillColour.Value,
            BmsGaugeType.Easy => EasyFillColour.Value,
            BmsGaugeType.Normal => NormalFillColour.Value,
            BmsGaugeType.Hard or BmsGaugeType.Class => HardFillColour.Value,
            BmsGaugeType.ExHard or BmsGaugeType.ExClass => ExHardFillColour.Value,
            BmsGaugeType.Hazard or BmsGaugeType.ExHardClass => HazardFillColour.Value,
            _ => profile.FillColour,
        };
    }

    private sealed partial class GaugeSegment : Box
    {
        public GaugeSegment()
        {
            RelativeSizeAxes = Axes.Both;
            Width = 1;
        }
    }
}
