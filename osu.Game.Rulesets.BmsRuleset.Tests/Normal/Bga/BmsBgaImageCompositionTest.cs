#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Game.Rulesets.BmsRuleset.Media;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Bga;

[TestFixture]
public class BmsBgaImageCompositionTest
{
    [TestCase("png")]
    [TestCase("bmp")]
    public void BlackOverlayRevealsBaseWhileColouredFigureRemainsOpaque(string extension)
    {
        using var bytes = new ImageStore(encode(extension, [new Rgba32(0, 0, 0), new Rgba32(0, 255, 0)]));
        using var layers = new BmsBgaLayerTextureLoaderStore(bytes);
        using var upload = layers.Get("layer." + extension);
        Assert.That(upload, Is.Not.Null);
        Assert.That(upload.Data[0].A, Is.Zero, "beatoraja layer.frag keys pure black, including opaque PNG pixels");
        Assert.That(upload.Data[1], Is.EqualTo(new Rgba32(0, 255, 0)));
        Assert.That(composite(new Rgba32(255, 0, 255), upload.Data[0]), Is.EqualTo(new Rgba32(255, 0, 255)));
    }

    [Test]
    public void OnlyPureBlackIsKeyedAndIntrinsicAlphaIsPreserved()
    {
        Rgba32[] pixels = [new(0, 0, 0, 255), new(1, 0, 0, 255), new(0, 0, 1, 255), new(10, 20, 30, 128), new(255, 0, 0, 0)];
        using var bytes = new ImageStore(encode("png", pixels));
        using var layers = new BmsBgaLayerTextureLoaderStore(bytes);
        using var upload = layers.Get("layer.png");
        var actual = upload.Data.ToArray();
        Assert.That(actual[0].A, Is.Zero);
        Assert.That(actual.Skip(1), Is.EqualTo(pixels.Skip(1)));
    }

    [Test]
    public void BaseAndPoorKeepOpaqueBlackForTheSameResource()
    {
        using var bytes = new ImageStore(encode("png", [new Rgba32(0, 0, 0)]));
        using var ordinary = new TextureLoaderStore(bytes);
        using var layers = new BmsBgaLayerTextureLoaderStore(bytes);
        using var baseUpload = ordinary.Get("shared.png");
        using var overlayUpload = layers.Get("shared.png");
        Assert.That(baseUpload.Data[0].A, Is.EqualTo(255));
        Assert.That(overlayUpload.Data[0].A, Is.Zero);
    }

    [Test]
    public void TwoOverlaysAndOpacityCompositeWithoutBlackOcclusion()
    {
        using var bytes = new ImageStore(encode("png", [new Rgba32(0, 0, 0), new Rgba32(0, 255, 0)]));
        using var layers = new BmsBgaLayerTextureLoaderStore(bytes);
        using var first = layers.Get("layer1.png");
        using var second = layers.Get("layer2.png");
        var background = new Rgba32(255, 0, 255);
        var green = composite(background, first.Data[1]);
        Assert.That(composite(green, second.Data[0]), Is.EqualTo(green));
        Assert.That(composite(background, first.Data[0], .5f), Is.EqualTo(background));
        Assert.That(composite(background, first.Data[1], .5f), Is.EqualTo(new Rgba32(128, 128, 128)));
    }

    [Test]
    public async Task MissingImagesRemainMissingInBothLoadPaths()
    {
        using var bytes = new ImageStore(null);
        using var layers = new BmsBgaLayerTextureLoaderStore(bytes);
        Assert.That(layers.Get("missing.png"), Is.Null);
        Assert.That(await layers.GetAsync("missing.png"), Is.Null);
    }

    private static Rgba32 composite(Rgba32 background, Rgba32 foreground, float opacity = 1)
    {
        var alpha = foreground.A / 255f * opacity;
        return new Rgba32((byte)MathF.Round(foreground.R * alpha + background.R * (1 - alpha)),
            (byte)MathF.Round(foreground.G * alpha + background.G * (1 - alpha)),
            (byte)MathF.Round(foreground.B * alpha + background.B * (1 - alpha)), 255);
    }

    private static byte[] encode(string extension, Rgba32[] pixels)
    {
        using var image = Image.LoadPixelData<Rgba32>(pixels, pixels.Length, 1);
        using var stream = new MemoryStream();
        if (extension == "bmp") image.SaveAsBmp(stream);
        else image.SaveAsPng(stream);
        return stream.ToArray();
    }

    private sealed class ImageStore(byte[]? bytes) : IResourceStore<byte[]>
    {
        public byte[] Get(string name) => bytes!;
        public Task<byte[]> GetAsync(string name, CancellationToken cancellationToken = default) => Task.FromResult(bytes!);
        public Stream GetStream(string name) => bytes == null ? null! : new MemoryStream(bytes, false);
        public IEnumerable<string> GetAvailableResources() => [];
        public void Dispose() { }
    }
}
