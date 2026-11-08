using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring.Gauge;
using osu.Game.Rulesets.BmsRuleset.Localisation;
using osu.Game.Rulesets.BmsRuleset.UI.Icons;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Gameplay.Mods.Gauge;

public enum BmsGasLowerLimit
{
    
    Assist,
    Easy,
    Normal,
}

public class BmsModGaugeAutoShift : Mod, IApplicableToHealthProcessor, IApplicableToScorePopulation
{

    public override string Name => "Gauge Auto Shift";

    public override string Acronym => "GAS";

    public override IconUsage? Icon => BmsIcons.GaugeAutoShift;

    public override LocalisableString Description => BmsStrings.ModGaugeAutoShift;

    public override ModType Type => ModType.Automation;

    [SettingSource(typeof(BmsStrings), nameof(BmsStrings.CourseInitialHealth), SettingControlType = typeof(BmsCourseGaugeSetting<double?>))]
    public Bindable<double?> ClassHealth { get; } = new();

    [SettingSource(typeof(BmsStrings), nameof(BmsStrings.CourseInitialHealth), SettingControlType = typeof(BmsCourseGaugeSetting<double?>))]
    public Bindable<double?> ExClassHealth { get; } = new();

    [SettingSource(typeof(BmsStrings), nameof(BmsStrings.CourseInitialHealth), SettingControlType = typeof(BmsCourseGaugeSetting<double?>))]
    public Bindable<double?> ExHardClassHealth { get; } = new();

    [SettingSource(typeof(BmsStrings), nameof(BmsStrings.CourseGaugeProfile), SettingControlType = typeof(BmsCourseGaugeSetting<BmsGaugeProfileFamily?>))]
    public Bindable<BmsGaugeProfileFamily?> GaugeProfileFamily { get; } = new();

    [SettingSource(typeof(BmsStrings), nameof(BmsStrings.GasLowerLimit), nameof(BmsStrings.GasLowerLimitDescription))]
    public Bindable<BmsGasLowerLimit> GasMode { get; } = new(BmsGasLowerLimit.Easy);

    public override IEnumerable<(LocalisableString setting, LocalisableString value)> SettingDescription => [];


    /// <summary>
    ///     The regular gameplay chain used by Auto Gauge, hardest first.
    /// </summary>
    internal static readonly BmsGaugeType[] AUTO_GAUGE_CHAIN =
    [
        BmsGaugeType.Hazard,
        BmsGaugeType.ExHard,
        BmsGaugeType.Hard,
        BmsGaugeType.Normal
    ];

    internal static readonly BmsGaugeType[] COURSE_AUTO_GAUGE_CHAIN =
    [
        BmsGaugeType.ExHardClass,
        BmsGaugeType.ExClass,
        BmsGaugeType.Class,
    ];

    private BmsGaugeType[]? FINAL_AUTO_GAUGE_CHAIN;

    private BmsHealthProcessor? resolvedHealthProcessor;

    public void ApplyToHealthProcessor(HealthProcessor healthProcessor)
    {
        if (healthProcessor is BmsHealthProcessor bmsHp)
        {
            bmsHp.IsGaugeAutoShift = true;
            bmsHp.EnableClearThresholdGaugeShift();
            resolvedHealthProcessor = bmsHp;
            switch (GasMode.Value)
            {
                case BmsGasLowerLimit.Assist:
                    FINAL_AUTO_GAUGE_CHAIN = [..AUTO_GAUGE_CHAIN, BmsGaugeType.Normal, BmsGaugeType.Easy, BmsGaugeType.AssistEasy];
                    break;
                case BmsGasLowerLimit.Easy:
                    FINAL_AUTO_GAUGE_CHAIN = [..AUTO_GAUGE_CHAIN, BmsGaugeType.Normal, BmsGaugeType.Easy];
                    break;
                case BmsGasLowerLimit.Normal:
                    FINAL_AUTO_GAUGE_CHAIN = AUTO_GAUGE_CHAIN;
                    break;
                // So gaugeTypes doesn't cry if something magical happens to GasMode.Value.
                default:
                    FINAL_AUTO_GAUGE_CHAIN = [..AUTO_GAUGE_CHAIN, BmsGaugeType.Normal, BmsGaugeType.Easy];
                    break;
            }
            var initialStates = new[]
            {
                (Type: BmsGaugeType.Class, Health: ClassHealth.Value),
                (Type: BmsGaugeType.ExClass, Health: ExClassHealth.Value),
                (Type: BmsGaugeType.ExHardClass, Health: ExHardClassHealth.Value),
            }.Where(state => state.Health.HasValue)
                .Select(state => new BmsGaugeStateSnapshot(state.Type, state.Health.GetValueOrDefault(), false)).ToArray();
            if (initialStates.Length > 0)
            {
                var family = GaugeProfileFamily.Value;
                bmsHp.ConfigureGaugeContext(true, family);
                bmsHp.SetGaugeTypes(initialStates.Select(state => state.GaugeType), replaceExisting: true,
                    profileFamilyOverride: family, initialStates: initialStates);
                return;
            }

            // Course sessions encode their surviving seeds on GAS. Attribution mods must not
            // manufacture a course context that the same score cannot reconstruct in replay.
            bmsHp.ConfigureGaugeContext(false, GaugeProfileFamily.Value);
            bmsHp.SetGaugeTypes(
                FINAL_AUTO_GAUGE_CHAIN,
                replaceExisting: true,
                profileFamilyOverride: bmsHp.ConfiguredProfileFamily);
        }
    }

    public void ApplyToScore(ScoreInfo score)
    {
        if (resolvedHealthProcessor is null)
            return;

        score.Mods = [.. score.Mods.Where(m => m is not BmsModGauge)];

        // Normal is represented by AG alone, so an earlier live-population attribution must be removed.
        var mod = BmsModGauge.CreateForType(resolvedHealthProcessor.WorstGaugeType);
        if (mod is null)
            return;

        score.Mods = [.. score.Mods, mod];
    }
}
