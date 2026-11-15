using System.Reflection;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays.SkinEditor;
using osu.Game.Rulesets.BmsRuleset.UI.HudComponents;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.SkinTest;

[TestFixture]
public class BmsTextHudEditModeTest
{
    [Test]
    public void TestEditModeShowsPlaceholderAndFullyVisible()
    {
        var hud = new BmsTextHud();
        applyEditModeVisibility(hud, true);

        Assert.Multiple(() =>
        {
            Assert.That(hud.Alpha, Is.EqualTo(1f));
            Assert.That(getMainText(hud).Text.ToString(), Is.EqualTo("Sample Text Event"));
        });
    }

    [Test]
    public void TestNonEditModeShowsGameStartText()
    {
        var hud = new BmsTextHud();
        applyEditModeVisibility(hud, false);

        Assert.That(getMainText(hud).Text.ToString(), Is.EqualTo("Game Start"));
    }

    [Test]
    public void TestDisposedHudStopsObservingSkinEditor()
    {
        using var editor = new SkinEditorOverlay(new ScalingContainer());
        using var textHud = new BmsTextHud();
        using var stageHud = new BmsStageHud();
        completeLoadWithEditor(textHud, editor);
        completeLoadWithEditor(stageHud, editor);

        editor.State.Value = Visibility.Visible;
        Assert.That(getMainText(textHud).Text.ToString(), Is.EqualTo("Sample Text Event"));
        Assert.That(stageHud.Alpha, Is.EqualTo(1));

        textHud.Dispose();
        editor.State.Value = Visibility.Hidden;
        Assert.That(stageHud.Alpha, Is.EqualTo(0));
        Assert.That(getMainText(textHud).Text.ToString(), Is.EqualTo("Sample Text Event"));

        stageHud.Dispose();
        editor.State.Value = Visibility.Visible;
        Assert.That(stageHud.Alpha, Is.EqualTo(0));
    }

    private static void completeLoadWithEditor(BmsHudComponent hud, SkinEditorOverlay editor)
    {
        typeof(BmsHudComponent).GetProperty("SkinEditor", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(hud, editor);
        hud.GetType().GetMethod("LoadComplete", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(hud, []);
    }

    private static void applyEditModeVisibility(BmsTextHud hud, bool isEditing)
    {
        var method = typeof(BmsTextHud).GetMethod("applyEditModeVisibility", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(method, Is.Not.Null);

        method!.Invoke(hud, [isEditing]);
    }

    private static SpriteText getMainText(BmsTextHud hud)
    {
        var field = typeof(BmsTextHud).GetField("mainText", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null);

        return (SpriteText)field!.GetValue(hud)!;
    }
}
