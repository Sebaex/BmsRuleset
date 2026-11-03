using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;
using osu.Game.Rulesets.BmsRuleset.Localisation;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Components;

public partial class TestSceneBmsSettings
{
    [Test]
    public void ScratchPreferenceDropdownUpdatesThePersistentSetting()
    {
        BmsRulesetConfigManager config = null!;
        FormEnumDropdown<BmsScratchPosition> dropdown = null!;
        AddStep("find scratch position setting", () =>
        {
            config = (BmsRulesetConfigManager)RulesetConfigs.GetConfigFor(new BmsRuleset());
            config.SetValue(BmsRulesetSetting.DefaultScratchPosition, BmsScratchPosition.FirstPlayer);
            dropdown = settings.ChildrenOfType<FormEnumDropdown<BmsScratchPosition>>().Single();
        });
        AddAssert("caption and hint use BMS localisation", () => dropdown.Caption.Equals(BmsStrings.DefaultScratchPosition)
            && dropdown.HintText.Equals(BmsStrings.DefaultScratchPositionHint));
        AddAssert("dropdown initially uses 1P", () => dropdown.Current.Value == BmsScratchPosition.FirstPlayer);
        AddStep("choose 2P in settings", () => dropdown.Current.Value = BmsScratchPosition.SecondPlayer);
        AddAssert("setting receives dropdown selection", () => config.Get<BmsScratchPosition>(BmsRulesetSetting.DefaultScratchPosition) == BmsScratchPosition.SecondPlayer);
        AddStep("change configuration to 1P", () => config.SetValue(BmsRulesetSetting.DefaultScratchPosition, BmsScratchPosition.FirstPlayer));
        AddAssert("dropdown receives configuration change", () => dropdown.Current.Value == BmsScratchPosition.FirstPlayer);
    }
}
