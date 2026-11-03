#nullable enable

using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps.Objects;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Replays;
using osu.Game.Rulesets.BmsRuleset.UI.Gameplay.Components;
using osu.Game.Rulesets.BmsRuleset.UI.Gameplay.Drawables;
using osu.Game.Skinning;
using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Visualize;

[HeadlessTest]
public partial class TestSceneBmsHitLightAnimationLifecycle : BmsPlayerTestScene
{
    private BmsTestSkins.SkinKind skinKind = BmsTestSkins.SkinKind.Legacy;

    protected override TestPlayer CreatePlayer(Ruleset ruleset)
        => CreateBmsPlayer(_ => [new BmsReplayFrame(0), new BmsReplayFrame(60000)], skinKind);

    protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
    {
        var beatmap = new BmsBeatmap
        {
            LayoutVariant = BmsLayoutVariant.Bme7K,
            TotalColumns = 8,
            HitObjects = [new BmsNote { StartTime = 60000, Column = 1 }],
        };
        BmsTestBeatmaps.SetupBeatmapInfo(beatmap, ruleset);
        return beatmap;
    }

    [TestCase(BmsHitLightStyle.Note)]
    [TestCase(BmsHitLightStyle.Hold)]
    public void AnimatedHitLightsAdvanceOnThreeUsesOfTheSamePooledDrawable(BmsHitLightStyle style)
    {
        BmsColumn column = null!;
        BmsHitExplosion pulse = null!;
        BmsHitExplosion firstPulse = null!;
        Drawable animation = null!;
        Drawable firstAnimation = null!;
        var firstFrames = new Texture[3];

        AddStep("choose animated note and hold lights", () => skinKind = BmsTestSkins.SkinKind.LegacyAnimatedHitLights);
        AddStep("load animated hit light player", LoadPlayer);
        AddUntilStep("player loaded", () => Player.IsLoaded && Player.LoadedBeatmapSuccessfully);
        seek(1000);
        AddStep("find column", () => column = (BmsColumn)Playfield.Stage.Columns[1]);

        for (var activation = 0; activation < 3; activation++)
        {
            var use = activation;
            var startTime = 1000 + use * 300;
            AddStep($"trigger activation {use + 1}", () => column.TriggerHitExplosion(style));
            AddUntilStep("animated pulse loaded", () =>
            {
                pulse = column.HitExplosionArea.AliveChildren.OfType<BmsHitExplosion>().SingleOrDefault()!;
                animation = pulse?.ChildrenOfType<Drawable>().SingleOrDefault(drawable => drawable is IFramedAnimation)!;
                return animation?.IsLoaded == true;
            });
            AddStep("verify pooled owner and animation identity", () =>
            {
                if (use == 0)
                {
                    firstPulse = pulse;
                    firstAnimation = animation;
                }
                Assert.That(pulse, Is.SameAs(firstPulse));
                Assert.That(animation, Is.SameAs(firstAnimation));
                Assert.That(((IFramedAnimation)animation).FrameCount, Is.EqualTo(3));
                Assert.That(((IFramedAnimation)animation).Loop, Is.False);
                Assert.That(pulse.LifetimeEnd - pulse.LifetimeStart, Is.EqualTo(200));
                Assert.That(((BmsHitExplosionLayer)column.HitExplosionArea).Pulses, Is.Empty);
            });

            seek(startTime + 20);
            AddStep("clock displays first frame", () => checkFrame(0, use, .25f));
            seek(startTime + 80);
            AddStep("clock advances to second frame", () => checkFrame(1, use, 1));
            seek(startTime + 140);
            AddStep("clock advances to third frame", () => checkFrame(2, use, .5f));
            seek(startTime + 190);
            AddStep("animation holds final frame until pulse ends", () => checkFrame(2, use, 1f / 12));
            seek(startTime + 200);
            AddUntilStep("same pulse returned to pool", () => !pulse.IsInUse);
            seek(startTime + 300);
        }

        seek(900);
        AddAssert("rewind keeps expired hit lights returned", () => !firstPulse.IsInUse
            && !column.HitExplosionArea.AliveChildren.OfType<BmsHitExplosion>().Any());
        AddStep("trigger animated light after rewind", () => column.TriggerHitExplosion(style));
        AddUntilStep("same owner reused after rewind", () => firstPulse.IsInUse);
        seek(920);
        AddStep("rewound activation starts from first frame", () => checkFrame(0, 3, .25f));
        seek(980);
        AddStep("rewound activation advances to second frame", () => checkFrame(1, 3, 1));
        seek(1040);
        AddStep("rewound activation advances to third frame", () => checkFrame(2, 3, .5f));

        void checkFrame(int index, int use, float alpha)
        {
            var framed = (IFramedAnimation)animation;
            Assert.That(pulse, Is.SameAs(firstPulse));
            Assert.That(animation, Is.SameAs(firstAnimation));
            Assert.That(column.HitExplosionArea.AliveChildren.OfType<BmsHitExplosion>().Single(), Is.SameAs(firstPulse));
            Assert.That(pulse.ChildrenOfType<Drawable>().Single(drawable => drawable is IFramedAnimation), Is.SameAs(firstAnimation));
            Assert.That(framed.IsPlaying, Is.True);
            Assert.That(framed.CurrentFrameIndex, Is.EqualTo(index));
            Assert.That(pulse.Alpha, Is.EqualTo(alpha).Within(.001));
            var texture = (animation as Sprite ?? animation.ChildrenOfType<Sprite>().Single()).Texture;
            Assert.That(texture, Is.Not.Null);
            if (use == 0)
            {
                if (index > 0)
                    Assert.That(texture, Is.Not.SameAs(firstFrames[index - 1]));
                firstFrames[index] = texture!;
            }
            else
                Assert.That(texture, Is.SameAs(firstFrames[index]));
        }
    }

    [TestCase(BmsHitLightStyle.Note)]
    [TestCase(BmsHitLightStyle.Hold)]
    public void AnimatedHitLightsReloadReuseAndDisposeWithTheirOwners(BmsHitLightStyle style)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var isDisposed = typeof(Drawable).GetProperty("IsDisposed", flags)!.GetMethod!.CreateDelegate<Func<Drawable, bool>>();
        var reload = typeof(SkinProvidingContainer).GetMethod("TriggerSourceChanged", flags)!;
        BmsHitExplosion pulse = null!;
        Drawable oldAnimation = null!;
        Drawable finalAnimation = null!;
        BmsColumn column = null!;
        AddStep("choose animated legacy lights", () => skinKind = BmsTestSkins.SkinKind.LegacyAnimatedHitLights);
        AddStep("load legacy hit light player", LoadPlayer);
        AddUntilStep("player loaded", () => Player.IsLoaded && Player.LoadedBeatmapSuccessfully);
        seek(1000);
        AddStep("trigger animated pulse", () =>
        {
            column = (BmsColumn)Playfield.Stage.Columns[1];
            column.TriggerHitExplosion(style);
        });
        AddUntilStep("animated pulse loaded", () =>
        {
            pulse = column.HitExplosionArea.AliveChildren.OfType<BmsHitExplosion>().SingleOrDefault()!;
            return pulse != null && animation().IsLoaded;
        });
        AddStep("check first pulse frames", checkFrames);
        AddStep("reload active hit light", () =>
        {
            oldAnimation = animation();
            reload.Invoke(((BmsTestSkins.SkinnedTestPlayer)Player).SkinSource, null);
        });
        AddUntilStep("active animation replaced", () => animation().IsLoaded && !ReferenceEquals(animation(), oldAnimation));
        AddStep("check active replacement", () => { Assert.That(isDisposed(oldAnimation), Is.True); checkFrames(); });
        seek(1300);
        AddUntilStep("pulse returned to pool", () => !pulse.IsInUse);
        AddStep("reload idle hit light", () =>
        {
            oldAnimation = animation();
            reload.Invoke(((BmsTestSkins.SkinnedTestPlayer)Player).SkinSource, null);
        });
        // Hit-light wrappers refresh pending skins when their pooled owner is used again.
        AddStep("reuse pulse", () => column.TriggerHitExplosion(style));
        AddUntilStep("same owner has refreshed animation", () => pulse.IsInUse && animation().IsLoaded && !ReferenceEquals(animation(), oldAnimation));
        AddStep("check reused frames", () => { Assert.That(isDisposed(oldAnimation), Is.True); checkFrames(); });
        AddStep("unload hit light player", () => { finalAnimation = animation(); Stack.Exit(); });
        AddUntilStep("pooled owner released", () => isDisposed(pulse));
        AddAssert("animation released", () => isDisposed(finalAnimation));

        Drawable animation() => pulse.ChildrenOfType<LegacyBmsHitExplosion>().Single().ChildrenOfType<Drawable>()
            .Single(drawable => drawable is IFramedAnimation);

        void checkFrames()
        {
            var drawable = animation();
            var framed = (IFramedAnimation)drawable;
            Assert.That(framed.FrameCount, Is.EqualTo(3));
            for (var index = 0; index < framed.FrameCount; index++)
            {
                framed.GotoAndStop(index);
                pulse.UpdateSubTree();
                Assert.That(framed.CurrentFrameIndex, Is.EqualTo(index));
                Assert.That((drawable as Sprite ?? drawable.ChildrenOfType<Sprite>().Single()).Texture, Is.Not.Null);
            }
        }
    }

    private void seek(double time)
    {
        AddStep($"seek {time}", () => SeekReplayInFrames(time));
        AddUntilStep("simulation reached target", () => Math.Abs(Player.DrawableRuleset.FrameStableClock.CurrentTime - time) < .001);
    }
}
