using NUnit.Framework;
using System.Reflection;
﻿using System;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps.Objects;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring.Gauge;
using osu.Game.Rulesets.BmsRuleset.Localisation;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.BmsRuleset.UI.HudComponents;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Scoring;
using osu.Game.Skinning;
using osu.Game.Tests.Visual;
using osuTK.Graphics;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Components;

[TestFixture]
public partial class TestSceneBmsHealthBar : OsuTestScene
{
    private BmsHealthProcessor healthProcessor = null!;
    private BmsBeatmap beatmap = null!;

    [SetUpSteps]
    public void SetUpSteps()
    {
        AddStep("create display", () =>
        {
            healthProcessor = new BmsHealthProcessor();
            healthProcessor.ApplyBeatmap(beatmap = new BmsBeatmap
            {
                LayoutVariant = BmsLayoutVariant.Bme7K,
                TotalColumns = 8,
                HitObjects =
                {
                    new BmsHitObject { StartTime = 1000, Column = 1 },
                    new BmsLandmine { StartTime = 2000, Column = 2, LandmineDamagePercent = 25 },
                },
            });

            Child = new DependencyProvidingContainer
            {
                RelativeSizeAxes = Axes.Both,
                CachedDependencies =
                [
                    (typeof(BmsHealthProcessor), healthProcessor),
                    (typeof(HealthProcessor), healthProcessor),
                ],
                Child = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    AutoSizeAxes = Axes.Both,
                    Child = new BmsHealthDisplay(),
                },
            };
        });
    }

    [Test]
    public void TestAssistEasyGaugeGrooveDisplay()
    {
        AddStep("set Assist Easy gauge", () => healthProcessor.SetGaugeType(BmsGaugeType.AssistEasy));
        AddAssert("display is GrooveDynamic", () =>
            healthProcessor.DisplayProfile.Value.ColourMode == BmsGaugeColourMode.GrooveDynamic);
        AddAssert("has clear line at 60%", () =>
        {
            var dp = healthProcessor.DisplayProfile.Value;
            return dp.ShowClearLine && dp.ClearThreshold == 0.6;
        });
        AddStep("set health above clear", () => healthProcessor.Health.Value = 0.65);
    }

    [TestCase(BmsGaugeType.AssistEasy)]
    [TestCase(BmsGaugeType.Easy)]
    [TestCase(BmsGaugeType.Normal)]
    public void TestClearLineAlignsWithStartOfRedZone(BmsGaugeType gaugeType)
    {
        BmsHealthDisplay display = null!;

        AddStep("fill gauge", () =>
        {
            healthProcessor.SetGaugeType(gaugeType);
            healthProcessor.Health.Value = 1;
            display = this.ChildrenOfType<BmsHealthDisplay>().Single();
        });
        AddUntilStep("show red clear zone", () => display.ChildrenOfType<Box>().Single(box => box.Name == "Above clear fill").Alpha == 1);
        AddAssert("clear threshold aligns with red zone edge", () =>
            Math.Abs(
                display.ChildrenOfType<Box>().Single(box => box.Name == "Above clear fill").ScreenSpaceDrawQuad.AABBFloat.Bottom
                - display.ChildrenOfType<Box>().Single(box => box.Name == "Clear threshold line").ScreenSpaceDrawQuad.AABBFloat.Bottom),
            () => Is.LessThan(0.01f));
    }

    [Test]
    public void TestClearZone()
    {
        AddStep("set health to 80%", () => healthProcessor.Health.Value = 0.80);
    }

    [Test]
    public void TestDrainAndRecover()
    {
        AddStep("set health to 100%", () => healthProcessor.Health.Value = 1.0);
        AddStep("drain to 10%", () => healthProcessor.Health.Value = 0.10);
        AddStep("recover to 85%", () => healthProcessor.Health.Value = 0.85);
    }

    [Test]
    public void TestEasyGaugeGrooveDisplay()
    {
        AddStep("set Easy gauge", () => healthProcessor.SetGaugeType(BmsGaugeType.Easy));
        AddAssert("display is GrooveDynamic", () =>
            healthProcessor.DisplayProfile.Value.ColourMode == BmsGaugeColourMode.GrooveDynamic);
        AddAssert("has clear line at 80%", () =>
        {
            var dp = healthProcessor.DisplayProfile.Value;
            return dp.ShowClearLine && dp.ClearThreshold == 0.8;
        });
    }

    [Test]
    public void TestEmpty()
    {
        AddStep("set health to 0%", () => healthProcessor.Health.Value = 0.0);
    }

    [Test]
    public void TestEmptyPoorDrain()
    {
        AddStep("set health to 80%", () => healthProcessor.Health.Value = 0.80);
        AddStep("register empty poor", () => healthProcessor.RegisterEmptyPoor());
        AddUntilStep("health display drained", () => healthProcessor.Health.Value, () => Is.EqualTo(0.78).Within(0.001));
    }

    [Test]
    public void TestExHardGaugeDisplay()
    {
        AddStep("set EX Hard gauge", () => healthProcessor.SetGaugeType(BmsGaugeType.ExHard));
        AddAssert("display is Fixed gold", () =>
        {
            var dp = healthProcessor.DisplayProfile.Value;
            return dp.ColourMode == BmsGaugeColourMode.Fixed && dp.FillColour == new Color4(255, 215, 0, 255);
        });
        AddAssert("no clear line", () => !healthProcessor.DisplayProfile.Value.ShowClearLine);
        AddStep("set health to 100%", () => healthProcessor.Health.Value = 1.0);
        assertActualFill(new Color4(255, 215, 0, 255));
    }

    [Test]
    public void TestFullHealth()
    {
        AddStep("set health to 100%", () => healthProcessor.Health.Value = 1.0);
    }

    [Test]
    public void TestHealthValueDisplayCanBeAddedAndTracksHealth()
    {
        BmsHealthValueDisplay display = null!;

        AddStep("create editable health value", () =>
        {
            healthProcessor.Health.Value = 0.65;
            Child = new DependencyProvidingContainer
            {
                RelativeSizeAxes = Axes.Both,
                CachedDependencies =
                [
                    (typeof(BmsHealthProcessor), healthProcessor),
                    (typeof(HealthProcessor), healthProcessor),
                ],
                Child = display = new BmsHealthValueDisplay
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                },
            };
        });
        AddAssert("show current health percentage", () =>
            display.ChildrenOfType<OsuSpriteText>().Single().Text.ToString(),
            () => Is.EqualTo(BmsStrings.HealthPercentage(0.65).ToString()));
        AddAssert("available in skin editor toolbox", () =>
            SerialisedDrawableInfo.GetAllAvailableDrawables(new BmsRuleset().RulesetInfo).Contains(typeof(BmsHealthValueDisplay)));
        AddStep("change current health", () => healthProcessor.Health.Value = 0.25);
        AddUntilStep("update displayed health percentage", () =>
            display.ChildrenOfType<OsuSpriteText>().Single().Text.ToString() == BmsStrings.HealthPercentage(0.25).ToString());
        AddStep("set health to a fractional percentage", () => healthProcessor.Health.Value = 0.6579);
        AddUntilStep("truncate percentage to one decimal", () =>
            display.ChildrenOfType<OsuSpriteText>().Single().Text.ToString() == BmsStrings.HealthPercentage(0.657).ToString());
    }

    [Test]
    public void TestHardGaugeDisplay()
    {
        AddStep("set Hard gauge", () => healthProcessor.SetGaugeType(BmsGaugeType.Hard));
        AddAssert("display is Fixed red", () =>
        {
            var dp = healthProcessor.DisplayProfile.Value;
            return dp.ColourMode == BmsGaugeColourMode.Fixed && dp.FillColour == new Color4(220, 55, 50, 255);
        });
        AddAssert("no clear line", () => !healthProcessor.DisplayProfile.Value.ShowClearLine);
        AddStep("set health to 100%", () => healthProcessor.Health.Value = 1.0);
    }

    [Test]
    public void TestHazardGaugeDisplay()
    {
        AddStep("set Hazard gauge", () => healthProcessor.SetGaugeType(BmsGaugeType.Hazard));
        AddAssert("display is Fixed gray", () =>
        {
            var dp = healthProcessor.DisplayProfile.Value;
            return dp.ColourMode == BmsGaugeColourMode.Fixed && dp.FillColour == new Color4(198, 198, 198, 255);
        });
        AddAssert("no clear line", () => !healthProcessor.DisplayProfile.Value.ShowClearLine);
        AddStep("set health to 100%", () => healthProcessor.Health.Value = 1.0);
    }

    [Test]
    public void TestHazardGaugeHealthLevels()
    {
        AddStep("set Hazard gauge", () => healthProcessor.SetGaugeType(BmsGaugeType.Hazard));
        AddStep("set health to 50%", () => healthProcessor.Health.Value = 0.50);
        AddAssert("display still Fixed gray", () =>
        {
            var dp = healthProcessor.DisplayProfile.Value;
            return dp.ColourMode == BmsGaugeColourMode.Fixed && dp.FillColour == new Color4(198, 198, 198, 255);
        });
        assertActualFill(new Color4(198, 198, 198, 255));
        AddStep("drop health to 5%", () => healthProcessor.Health.Value = 0.05);
        AddAssert("display colour unchanged (Fixed)", () =>
            healthProcessor.DisplayProfile.Value.ColourMode == BmsGaugeColourMode.Fixed
            && healthProcessor.DisplayProfile.Value.FillColour == new Color4(198, 198, 198, 255));
        assertActualFill(new Color4(198, 198, 198, 255));
    }

    private void assertActualFill(Color4 expected) => AddUntilStep("actual HUD fill matches fixed palette", () =>
    {
        var display = this.ChildrenOfType<BmsHealthDisplay>().Single();
        var fill = (Box)typeof(BmsHealthDisplay).GetField("normalFill", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(display)!;
        return fill.Colour.Equals(ColourInfo.SingleColour(expected));
    });

    [Test]
    public void TestInitialState()
    {
        AddAssert("health is 20%", () => healthProcessor.Health.Value, () => Is.EqualTo(0.2).Within(0.001));
    }

    [Test]
    public void TestLandmineDrain()
    {
        AddStep("set health to 80%", () => healthProcessor.Health.Value = 0.80);
        AddStep("detonate landmine", () => healthProcessor.ApplyResult(new JudgementResult(beatmap.HitObjects[1], beatmap.HitObjects[1].CreateJudgement())
        {
            Type = HitResult.Meh,
        }));
        AddUntilStep("health display drained", () => healthProcessor.Health.Value, () => Is.EqualTo(0.55).Within(0.001));
    }

    [Test]
    public void TestRedZone()
    {
        AddStep("set health to 10%", () => healthProcessor.Health.Value = 0.10);
    }

    [Test]
    public void TestSwitchFromHardToNormalRestoresGroove()
    {
        AddStep("set Hard gauge", () => healthProcessor.SetGaugeType(BmsGaugeType.Hard));
        AddAssert("display is Fixed", () =>
            healthProcessor.DisplayProfile.Value.ColourMode == BmsGaugeColourMode.Fixed);
        AddStep("switch back to Normal", () => healthProcessor.SetGaugeType(BmsGaugeType.Normal));
        AddAssert("display is GrooveDynamic", () =>
            healthProcessor.DisplayProfile.Value.ColourMode == BmsGaugeColourMode.GrooveDynamic);
        AddAssert("clear line restored at 80%", () =>
        {
            var dp = healthProcessor.DisplayProfile.Value;
            return dp.ShowClearLine && dp.ClearThreshold == 0.8;
        });
    }

    [Test]
    public void TestYellowZone()
    {
        AddStep("set health to 50%", () => healthProcessor.Health.Value = 0.50);
    }
}
