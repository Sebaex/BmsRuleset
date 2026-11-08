using System.Linq;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.IO.Serialization;
using osu.Game.Online.API;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps.Objects;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Course;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Mods.Gauge;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring.Gauge;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Mods;

[TestFixture]
public class BmsModGaugeAutoShiftTest
{

    /// <summary>
    ///     LR2's N=1 damage multiplier is ten: one BAD kills Hazard and ExHard,
    ///     leaving Hard at 40%. Attribution must follow that surviving tier.
    /// </summary>
    private static (BmsModGaugeAutoShift autoGauge, BmsHealthProcessor hp) createAutoGaugeResolvedToHard()
    {
        var hp = new BmsHealthProcessor();
        var autoGauge = new BmsModGaugeAutoShift();
        autoGauge.ApplyToHealthProcessor(hp);

        var beatmap = new BmsBeatmap
        {
            LayoutVariant = BmsLayoutVariant.Bme7K,
            TotalColumns = 8,
            HitObjects = { new BmsHitObject { StartTime = 1000, Column = 1 } },
        };
        hp.ApplyBeatmap(beatmap);

        hp.ApplyResult(new JudgementResult(beatmap.HitObjects[0], beatmap.HitObjects[0].CreateJudgement())
            { Type = HitResult.Ok });
        hp.HasPassedAtEnd();
        Assert.That(hp.WorstGaugeType, Is.EqualTo(BmsGaugeType.Hard));
        Assert.That(hp.Health.Value, Is.EqualTo(1 - .06 * 10).Within(1e-7));

        return (autoGauge, hp);
    }

    [Test]
    public void TestAutoGaugeAllowsResolvedGaugeAttribution()
    {
        var autoGauge = new BmsModGaugeAutoShift();

        Assert.That(autoGauge.IncompatibleMods, Does.Not.Contain(typeof(BmsModGauge)));
        Assert.That(autoGauge.IncompatibleMods, Does.Not.Contain(typeof(BmsModHardGauge)));
    }

    [Test]
    public void TestAutoGaugeAppendsResolvedGaugeModOnApplyToScore()
    {
        var (autoGauge, hp) = createAutoGaugeResolvedToHard();
        Assert.That(hp.WorstGaugeType, Is.EqualTo(BmsGaugeType.Hard));

        var score = new ScoreInfo { Mods = [autoGauge] };

        autoGauge.ApplyToScore(score);

        Assert.That(score.Mods, Has.One.TypeOf<BmsModHardGauge>());
        Assert.That(score.Mods, Contains.Item(autoGauge));
    }

    [Test]
    public void TestAutoGaugeAttributionDispatchedFromPopulateScore()
    {
        var (autoGauge, _) = createAutoGaugeResolvedToHard();

        var scoreProcessor = new BmsScoreProcessor();
        scoreProcessor.Mods.Value = new Mod[] { autoGauge };
        var score = new ScoreInfo { Mods = [autoGauge] };

        scoreProcessor.PopulateScore(score);

        Assert.That(score.Mods, Has.One.TypeOf<BmsModHardGauge>());
    }

    [Test]
    public void TestAutoGaugeAttributionIsIdempotent()
    {
        var (autoGauge, _) = createAutoGaugeResolvedToHard();
        var score = new ScoreInfo { Mods = [autoGauge] };

        autoGauge.ApplyToScore(score);
        autoGauge.ApplyToScore(score);

        Assert.That(score.Mods.Count(m => m is BmsModHardGauge), Is.EqualTo(1));
    }

    [Test]
    public void TestAutoGaugeAttributionReplacesEarlierResolvedGauge()
    {
        var hp = new BmsHealthProcessor();
        var autoGauge = new BmsModGaugeAutoShift();
        autoGauge.ApplyToHealthProcessor(hp);

        var beatmap = new BmsBeatmap
        {
            LayoutVariant = BmsLayoutVariant.Bme7K,
            TotalColumns = 8,
            HitObjects = { new BmsHitObject { StartTime = 1000, Column = 1 } },
        };
        hp.ApplyBeatmap(beatmap);

        var score = new ScoreInfo { Mods = [autoGauge] };

        autoGauge.ApplyToScore(score);
        Assert.That(score.Mods, Has.One.TypeOf<BmsModHazardGauge>());

        hp.ApplyResult(new JudgementResult(beatmap.HitObjects[0], beatmap.HitObjects[0].CreateJudgement())
            { Type = HitResult.Ok });
        hp.HasPassedAtEnd();

        autoGauge.ApplyToScore(score);

        Assert.That(score.Mods, Has.None.TypeOf<BmsModHazardGauge>());
        Assert.That(score.Mods, Has.One.TypeOf<BmsModHardGauge>());
        Assert.That(score.Mods, Contains.Item(autoGauge));
    }

    [Test]
    public void TestAutoGaugeIsAutomationType()
    {
        var mod = new BmsModGaugeAutoShift();

        Assert.That(mod.Type, Is.EqualTo(ModType.Automation));
    }

    [Test]
    public void TestAutoGaugeUsesConfiguredCourseGaugeContext()
    {
        var hp = new BmsHealthProcessor();
        hp.ApplyBeatmap(new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K });
        var stage = new BmsResolvedCourseStage(new BmsCourseStage("stage", "1", BeatmapHash: "course-seed"), new BeatmapInfo { Hash = "course-seed" });
        var session = new BmsCourseSession(
            new BmsCourseDefinition("course", "table", "course", [stage.Definition], ["gauge_5k"]),
            [stage], [new BmsModGaugeAutoShift()], BmsGaugeType.ExHardClass);
        session.BeginCurrentStage();
        session.ConfigureHealthProcessor(hp);
        var saved = new APIMod(session.Mods.OfType<BmsModGaugeAutoShift>().Single().DeepClone()).Serialize().Deserialize<APIMod>();
        ((BmsModGaugeAutoShift)saved.ToMod(new BmsRuleset())).ApplyToHealthProcessor(hp);

        Assert.Multiple(() =>
        {
            Assert.That(hp.CurrentGaugeStates.Select(state => state.GaugeType), Is.EqualTo(
            [
                BmsGaugeType.ExHardClass,
                BmsGaugeType.ExClass,
                BmsGaugeType.Class,
            ]));
            Assert.That(hp.GaugeProfile.PerfectGain, Is.EqualTo(0.0001).Within(0.000001));
            Assert.That(hp.IsCourseGaugeMode, Is.True);
            Assert.That(hp.ConfiguredProfileFamily, Is.EqualTo(BmsGaugeProfileFamily.FiveKeys));
            Assert.That(hp.CurrentGaugeStates.Select(s => s.Health), Is.All.EqualTo(1));
        });
    }

    [Test]
    public void TestCourseGaugeUsesConfiguredProfileFamily()
    {
        var hp = new BmsHealthProcessor();
        hp.ApplyBeatmap(new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K });
        hp.ConfigureGaugeContext(true, BmsGaugeProfileFamily.FiveKeys);

        new BmsModClassGauge().ApplyToHealthProcessor(hp);

        Assert.That(hp.GaugeProfile.PerfectGain, Is.EqualTo(0.0001).Within(0.000001));
    }

    [Test]
    public void TestCourseStartingStatesSurviveCloneAndModSerialisation()
    {
        var mod = new BmsModGaugeAutoShift
        {
            ClassHealth = { Value = 0.81 },
            ExClassHealth = { Value = 0.42 },
            GaugeProfileFamily = { Value = BmsGaugeProfileFamily.Lr2 },
        };
        var clone = mod.DeepClone();
        var saved = new APIMod(clone).Serialize().Deserialize<APIMod>();
        var restored = (BmsModGaugeAutoShift)saved.ToMod(new BmsRuleset());
        using var health = new BmsHealthProcessor();
        health.ApplyBeatmap(new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K });
        restored.ApplyToHealthProcessor(health);
        restored.ApplyToScore(new ScoreInfo { Mods = [restored] });
        Assert.Multiple(() =>
        {
            Assert.That(health.IsCourseGaugeMode, Is.True);
            Assert.That(health.ConfiguredProfileFamily, Is.EqualTo(BmsGaugeProfileFamily.Lr2));
            Assert.That(health.CurrentGaugeStates, Is.EqualTo(new[]
            {
                new BmsGaugeStateSnapshot(BmsGaugeType.ExClass, 0.42, false),
                new BmsGaugeStateSnapshot(BmsGaugeType.Class, 0.81, false),
            }));
            Assert.That(restored.ExHardClassHealth.Value, Is.Null);
            Assert.That(restored.GasMode.Value, Is.EqualTo(BmsGasLowerLimit.Easy));
        });
        var controls = restored.CreateSettingsControls().ToArray();
        try
        {
            Assert.That(controls, Has.Length.EqualTo(5));
            var carriedHealth = controls.OfType<BmsCourseGaugeSetting<double?>>().ToArray();
            Assert.That(carriedHealth, Has.Length.EqualTo(3));
            assertHiddenCarry(restored.ClassHealth);
            assertHiddenCarry(restored.ExClassHealth);
            assertHiddenCarry(restored.ExHardClassHealth);
            var family = controls.OfType<BmsCourseGaugeSetting<BmsGaugeProfileFamily?>>().Single();
            Assert.That(family.SettingSourceObject, Is.SameAs(restored));
            Assert.That(family.Current.Value, Is.EqualTo(BmsGaugeProfileFamily.Lr2));
            Assert.That(family.CanBeShown.Value, Is.False);
            Assert.That(family.IsPresent, Is.False);
            var limit = controls.OfType<SettingsDropdown<BmsGasLowerLimit>>().Single();
            Assert.That(limit.CanBeShown.Value, Is.True);
            Assert.That(limit.IsPresent, Is.True);
            Assert.That(limit.Current.Value, Is.EqualTo(BmsGasLowerLimit.Easy));
            restored.GasMode.Value = BmsGasLowerLimit.Normal;
            Assert.That(limit.Current.Value, Is.EqualTo(BmsGasLowerLimit.Normal));
            restored.GasMode.Value = BmsGasLowerLimit.Easy;

            void assertHiddenCarry(Bindable<double?> source)
            {
                var original = source.Value;
                try
                {
                    source.Value = .314159;
                    var control = carriedHealth.Single(c => c.Current.Value == .314159);
                    Assert.That(control.SettingSourceObject, Is.SameAs(restored));
                    Assert.That(control.CanBeShown.Value, Is.False);
                    Assert.That(control.IsPresent, Is.False);
                }
                finally { source.Value = original; }
            }
        }
        finally { foreach (var control in controls) control.Dispose(); }
    }

    [TestCase(BmsGasLowerLimit.Normal)]
    [TestCase(BmsGasLowerLimit.Easy)]
    [TestCase(BmsGasLowerLimit.Assist)]
    public void TestGasLowerLimitSurvivesCloneAndSerialisation(BmsGasLowerLimit limit)
    {
        // Retain a real Course seed, as in the original serialization fixture, to isolate
        // lower-limit preservation from the framework's unrelated empty-settings JSON path.
        var mod = new BmsModGaugeAutoShift { GasMode = { Value = limit }, ClassHealth = { Value = .81 } };
        var clone = (BmsModGaugeAutoShift)mod.DeepClone();
        var api = new APIMod(clone);
        var restored = (BmsModGaugeAutoShift)api.Serialize().Deserialize<APIMod>().ToMod(new BmsRuleset());
        TestContext.WriteLine($"GAS_SERIALISATION requested={limit} default={mod.GasMode.Default} clone={clone.GasMode.Value} settings=[{string.Join(",", api.Settings.Keys)}] restored={restored.GasMode.Value}");
        Assert.Multiple(() =>
        {
            Assert.That(clone.GasMode.Value, Is.EqualTo(limit), "Clone preserves the selected lower limit.");
            Assert.That(restored.GasMode.Value, Is.EqualTo(limit), "Serialisation preserves the selected lower limit.");
        });
    }

    [TestCase(BmsGaugeType.Class)]
    [TestCase(BmsGaugeType.ExClass)]
    [TestCase(BmsGaugeType.ExHardClass)]
    public void TestSingleCourseStartingHealthSurvivesModSerialisation(BmsGaugeType type)
    {
        var mod = BmsModGauge.CreateForType(type)!;
        mod.InitialHealth.Value = 0.42;
        mod.GaugeProfileFamily.Value = BmsGaugeProfileFamily.FiveKeys;
        var saved = new APIMod(mod.DeepClone()).Serialize().Deserialize<APIMod>();
        var restored = (BmsModGauge)saved.ToMod(new BmsRuleset());
        using var health = new BmsHealthProcessor();
        health.ApplyBeatmap(new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K });
        restored.ApplyToHealthProcessor(health);
        Assert.Multiple(() =>
        {
            Assert.That(health.IsCourseGaugeMode, Is.True);
            Assert.That(health.ConfiguredProfileFamily, Is.EqualTo(BmsGaugeProfileFamily.FiveKeys));
            Assert.That(health.CurrentGaugeStates.Single(), Is.EqualTo(new BmsGaugeStateSnapshot(type, 0.42, false)));
        });
        foreach (var control in restored.CreateSettingsControls())
        {
            Assert.That(control.IsPresent, Is.False);
            control.Dispose();
        }
    }
}
