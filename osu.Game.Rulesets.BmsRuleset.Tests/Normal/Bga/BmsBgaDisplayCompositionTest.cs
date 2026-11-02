#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering.Dummy;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Timing;
using osu.Framework.Testing;
using osu.Framework.Graphics;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Media;
using osu.Game.Rulesets.BmsRuleset.UI.Gameplay;
using osu.Game.Rulesets.BmsRuleset.UI.HudComponents;
using osu.Game.Rulesets.UI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using Vector2 = osuTK.Vector2;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Bga;

[TestFixture]
public partial class BmsBgaDisplayCompositionTest
{
    [TestCase(BmsBgaLayer.Base, 255)]
    [TestCase(BmsBgaLayer.Poor, 255)]
    [TestCase(BmsBgaLayer.Layer1, 0)]
    [TestCase(BmsBgaLayer.Layer2, 0)]
    public void ActualDisplaySelectsTheCorrectAlphaForSharedBitmap(BmsBgaLayer layer, int expectedAlpha)
    {
        using var scene = new Fixture();
        var drawable = scene.Create(new BmsBgaEvent(0, 0, 1, layer, 0))!;
        var sprite = drawable as Sprite ?? drawable.ChildrenOfType<Sprite>().Single();
        Assert.That(sprite.Texture, Is.Not.Null);
        var store = layer is BmsBgaLayer.Layer1 or BmsBgaLayer.Layer2 ? scene.Overlays : scene.Ordinary;
        Assert.That(store.Pixels[0].A, Is.EqualTo(expectedAlpha));
        Assert.That(store.Pixels[1], Is.EqualTo(new Rgba32(0, 255, 0)));
    }

    [TestCase(BmsBgaLayer.Layer1)]
    [TestCase(BmsBgaLayer.Layer2)]
    public void CroppedOverlayKeepsSourceAndDestinationGeometry(BmsBgaLayer layer)
    {
        using var scene = new Fixture();
        scene.SetTimeline(scene.Timeline with
        {
            BgaDefinitions = new Dictionary<ushort, BmsBgaDefinition> { [2] = new(1, 1, 0, 1, 1, 5, 7) },
        });
        var sprite = (Sprite)scene.Create(new BmsBgaEvent(0, 0, 2, layer, 0))!;
        Assert.That(scene.Overlays.Pixels[0].A, Is.Zero);
        Assert.That(sprite.Texture!.DisplaySize, Is.EqualTo(Vector2.One));
        Assert.That(sprite.Position, Is.EqualTo(new Vector2(5, 7)));
        Assert.That(sprite.Size, Is.EqualTo(Vector2.One));
    }

    [TestCase(4)]
    [TestCase(70)]
    public void ImageSequenceSurvivesForwardAndBackwardSeekWithIndependentLayerOpacity(int count)
    {
        using var scene = new Fixture();
        scene.DefineImage(2, "blue.png", new Rgba32(0, 0, 255));
        scene.DefineImage(3, "red.png", new Rgba32(255, 0, 0));
        scene.DefineImage(4, "yellow.png", new Rgba32(255, 255, 0));
        var events = Enumerable.Range(0, count).Select(index => new BmsBgaEvent(index * 10, index, (ushort)(index % 4 + 1), BmsBgaLayer.Layer1, index)).ToArray();
        var opacity = new BmsBgaOpacityEvent(0, 0, BmsBgaLayer.Layer1, .5f, 0);
        scene.SetTimeline(scene.Timeline with { Events = events, OpacityEvents = [opacity] });
        scene.Advance(0);
        Assert.That(scene.Layer(BmsBgaLayer.Layer1).ChildrenOfType<Sprite>().Count(), Is.EqualTo(1));
        scene.Advance((count - 1) * 10);
        var final = scene.Layer(BmsBgaLayer.Layer1).Children.Single();
        Assert.That(final.ChildrenOfType<Sprite>().Count(), Is.EqualTo(1));
        string[] paths = ["shared.png", "blue.png", "red.png", "yellow.png"];
        var expectedPath = paths[(count - 1) % 4];
        Assert.That(final.ChildrenOfType<Sprite>().Single().Texture, Is.SameAs(scene.Overlays.Get(expectedPath)));
        scene.Advance((count - 1) * 10);
        Assert.That(scene.Layer(BmsBgaLayer.Layer1).Children.Single(), Is.SameAs(final));
        Assert.That(scene.Layer(BmsBgaLayer.Layer1).Alpha, Is.EqualTo(.5f));
        scene.Advance(0);
        Assert.That(scene.Layer(BmsBgaLayer.Layer1).ChildrenOfType<Sprite>().Single().Texture, Is.SameAs(scene.Overlays.Get("shared.png")));
        Assert.That(scene.Overlays.Pixels[0].A, Is.Zero);
        scene.Advance(-1);
        Assert.That(scene.Layer(BmsBgaLayer.Layer1).Children, Is.Empty);
        scene.Advance(0);
        Assert.That(scene.Layer(BmsBgaLayer.Layer1).ChildrenOfType<Sprite>().Count(), Is.EqualTo(1));
    }

    [Test]
    public void MissingEventClearsOnlyItsLayerAndLaterImagesCanReappear()
    {
        using var scene = new Fixture();
        scene.SetTimeline(scene.Timeline with { Events = [new(0, 0, 1, BmsBgaLayer.Base, 0), new(0, 0, 1, BmsBgaLayer.Layer1, 1),
            new(10, 1, 9, BmsBgaLayer.Layer1, 2), new(20, 2, 1, BmsBgaLayer.Layer1, 3)] });
        scene.Advance(0);
        Assert.That(scene.Layer(BmsBgaLayer.Layer1).Children, Has.Count.EqualTo(1));
        scene.Advance(10);
        Assert.That(scene.Layer(BmsBgaLayer.Base).Children, Has.Count.EqualTo(1));
        Assert.That(scene.Layer(BmsBgaLayer.Layer1).Children, Is.Empty);
        scene.Advance(20);
        Assert.That(scene.Layer(BmsBgaLayer.Layer1).Children, Has.Count.EqualTo(1));
    }

    [TestCase(BmsBgaLayer.Base, false, 1)]
    [TestCase(BmsBgaLayer.Base, true, 0)]
    [TestCase(BmsBgaLayer.Poor, false, 1)]
    [TestCase(BmsBgaLayer.Layer1, false, 0)]
    [TestCase(BmsBgaLayer.Layer2, false, 0)]
    public void SmallImagesShareTheReferenceCanvasWithoutStretchingTheFrame(BmsBgaLayer layer, bool hasAlpha, int backgrounds)
    {
        using var texture = new DummyRenderer().CreateTexture(128, 64);
        using var canvas = (Container)typeof(BmsBgaDisplay).GetMethod("createImageCanvas", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [texture, layer, hasAlpha])!;
        var sprite = canvas.ChildrenOfType<Sprite>().Single(drawable => drawable.GetType() == typeof(Sprite));
        Assert.That(canvas.FillMode, Is.EqualTo(FillMode.Fit));
        Assert.That(canvas.FillAspectRatio, Is.EqualTo(1));
        Assert.That(sprite.Position, Is.EqualTo(new Vector2(.25f, 0)));
        Assert.That(sprite.Size, Is.EqualTo(new Vector2(.5f, .25f)));
        Assert.That(canvas.Children.Count - 1, Is.EqualTo(backgrounds));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EncodedAlphaDeterminesPaddingWithoutChangingTheOriginalPixels(bool hasAlpha)
    {
        using var scene = new Fixture();
        scene.DefineImage(2, "alpha.png", new Rgba32(0, 255, 0), hasAlpha);
        Assert.That(scene.SourceHasAlpha("alpha.png"), Is.EqualTo(hasAlpha));
        var canvas = (Container)scene.Create(new BmsBgaEvent(0, 0, 2, BmsBgaLayer.Base, 0))!;
        Assert.That(canvas.Children.Count, Is.EqualTo(hasAlpha ? 1 : 2));
        Assert.That(scene.Ordinary.Pixels[0].A, Is.EqualTo(255));
    }

    private sealed class Fixture : IDisposable
    {
        private const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly string directory = Path.Combine(Path.GetTempPath(), $"bga-composition-{Guid.NewGuid():N}");
        private readonly BmsBgaDisplay display = new() { RenderOutsideHudVisibility = false };
        private readonly TestRuleset ruleset = new();
        private readonly BmsBgaDisplay.BmsBgaResourceStore resources;
        internal readonly RecordingStore Ordinary;
        internal readonly RecordingStore Overlays;
        internal BmsBgaTimeline Timeline = new(new Dictionary<ushort, string> { [1] = "shared.bmp" }, new Dictionary<ushort, BmsBgaDefinition>(), [], [], BmsPoorBgaMode.Replace);

        internal Fixture()
        {
            Directory.CreateDirectory(directory);
            using (var image = Image.LoadPixelData<Rgba32>([new Rgba32(0, 0, 0), new Rgba32(0, 255, 0)], 2, 1))
                image.SaveAsPng(Path.Combine(directory, "shared.png"));
            resources = new BmsBgaDisplay.BmsBgaResourceStore(directory, null);
            Ordinary = new RecordingStore(new TextureLoaderStore(resources));
            Overlays = new RecordingStore(new BmsBgaLayerTextureLoaderStore(resources));
            Set("resources", resources);
            Set("textures", Ordinary);
            Set("layerTextures", Overlays);
            typeof(BmsBgaDisplay).GetProperty("drawableRuleset", flags)!.SetValue(display, ruleset);
            SetTimeline(Timeline);
        }

        internal void SetTimeline(BmsBgaTimeline timeline)
        {
            Timeline = timeline;
            Set("bga", timeline);
            Set("events", timeline.Events.ToArray());
            Set("opacityEvents", timeline.OpacityEvents.ToArray());
        }

        internal Drawable? Create(BmsBgaEvent evt) => (Drawable?)typeof(BmsBgaDisplay).GetMethod("createDrawable", flags)!.Invoke(display, [evt]);

        internal bool SourceHasAlpha(string name) => resources.ImageHasAlpha(name);

        internal void DefineImage(ushort key, string name, Rgba32 colour, bool hasAlpha = true)
        {
            using var image = Image.LoadPixelData<Rgba32>([new Rgba32(0, 0, 0), colour], 2, 1);
            image.SaveAsPng(Path.Combine(directory, name), new PngEncoder { ColorType = hasAlpha ? PngColorType.RgbWithAlpha : PngColorType.Rgb });
            var definitions = new Dictionary<ushort, string>(Timeline.BitmapDefinitions) { [key] = name };
            SetTimeline(Timeline with { BitmapDefinitions = definitions });
        }
        internal Container Layer(BmsBgaLayer layer) => ((Dictionary<BmsBgaLayer, Container>)typeof(BmsBgaDisplay).GetField("layerHosts", flags)!.GetValue(display)!)[layer];
        private void Set(string field, object value) => typeof(BmsBgaDisplay).GetField(field, flags)!.SetValue(display, value);

        internal void Advance(double time)
        {
            var clock = ruleset.TestClock;
            ((ManualClock)typeof(FrameStabilityContainer).GetField("manualClock", flags)!.GetValue(clock)!).CurrentTime = time;
            ((FramedClock)typeof(FrameStabilityContainer).GetField("framedClock", flags)!.GetValue(clock)!).ProcessFrame();
            typeof(BmsBgaDisplay).GetMethod("Update", flags)!.Invoke(display, null);
        }

        public void Dispose()
        {
            display.Dispose();
            ruleset.TestClock.Dispose();
            ruleset.Dispose();
            Directory.Delete(directory, true);
        }
    }

    private sealed class RecordingStore : TextureStore
    {
        private readonly IResourceStore<TextureUpload> loader;
        internal Rgba32[] Pixels = [];

        internal RecordingStore(IResourceStore<TextureUpload> loader)
            : base(new DummyRenderer(), loader, false, scaleAdjust: 1) => this.loader = loader;

        public override Texture Get(string name, WrapMode wrapModeS, WrapMode wrapModeT)
        {
            using var upload = loader.Get(name);
            if (upload != null) Pixels = upload.Data.ToArray();
            return base.Get(name, wrapModeS, wrapModeT);
        }
    }

    private sealed partial class TestRuleset() : BmsDrawableRuleset(new BmsRuleset(), new BmsBeatmap())
    {
        internal FrameStabilityContainer TestClock { get; } = new();
        public override IFrameStableClock FrameStableClock => TestClock;
    }
}
