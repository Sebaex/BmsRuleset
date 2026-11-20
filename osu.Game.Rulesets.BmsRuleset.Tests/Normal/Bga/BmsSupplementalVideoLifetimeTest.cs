#nullable enable

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Dummy;
using osu.Framework.Graphics.Textures;
using osu.Framework.Timing;
using osu.Game.Rulesets.BmsRuleset.Media.FFmpeg;
using osu.Game.Rulesets.BmsRuleset.Media.Video.Supplemental;
using osu.Game.Rulesets.BmsRuleset.Tests.Performance;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Bga;

[TestFixture]
[NonParallelizable]
public class BmsSupplementalVideoLifetimeTest
{
    private const BindingFlags instance_flags = BindingFlags.Instance | BindingFlags.NonPublic;

    [Test]
    public void TestQueuedUploadKeepsPixelsUntilRendererConsumesIt()
    {
        var pool = new RecordingPixelPool();
        using var probe = interceptTextureUploads();
        using var completionProbe = interceptUploadCompletion();
        var source = new BmsSupplementalVideoFrameSource([]);
        using var drawable = createDrawable(source);
        enqueue(source, new BmsSupplementalVideoFrame(pool, pool.Rent(1), 1, 1, 0));

        update(drawable);
        Assert.That(pendingUpload, Is.Not.Null);
        Assert.That(pool.Returns, Is.Zero, "The draw thread has not consumed the queued upload.");

        drawable.Dispose();
        Assert.That(pool.Returns, Is.Zero, "Disposing the producer must leave queued pixels owned by the renderer.");
        Assert.That(pendingUpload!.Data[0].R, Is.EqualTo(42));
        pendingUpload!.Dispose();
        pendingUpload.Dispose();
        pendingUpload = null;
        Assert.That(pool.Returns, Is.EqualTo(1));
    }

    [Test]
    public void TestDelayedRendererCoalescesFramesAndSubmitsFinalFrameWhenReady()
    {
        var pool = new RecordingPixelPool();
        using var probe = interceptTextureUploads();
        using var completionProbe = interceptUploadCompletion();
        var source = new BmsSupplementalVideoFrameSource([]);
        using var drawable = createDrawable(source);
        enqueue(source, new BmsSupplementalVideoFrame(pool, pool.Rent(1), 1, 1, 0));
        update(drawable);
        var first = pendingUpload;

        for (var i = 0; i < 100; i++)
        {
            enqueue(source, new BmsSupplementalVideoFrame(pool, pool.Rent(1), 1, 1, 0));
            update(drawable);
            Assert.That(pendingUpload, Is.SameAs(first));
        }

        Assert.That(pool.Returns, Is.EqualTo(99));
        Assert.That(first!.Data[0].R, Is.EqualTo(42));
        first.Dispose();
        pendingUpload = null;

        // No further decoded frame arrives: the latest waiting frame must survive EOF.
        update(drawable);
        Assert.That(pendingUpload, Is.Not.Null.And.Not.SameAs(first));
        pendingUpload!.Dispose();
        pendingUpload = null;
        Assert.That(pool.Returns, Is.EqualTo(101));
    }

    [Test]
    public void TestDisposalReturnsWaitingFrameWithoutReclaimingQueuedUpload()
    {
        var pool = new RecordingPixelPool();
        using var probe = interceptTextureUploads();
        using var completionProbe = interceptUploadCompletion();
        var source = new BmsSupplementalVideoFrameSource([]);
        using var drawable = createDrawable(source);
        enqueue(source, new BmsSupplementalVideoFrame(pool, pool.Rent(1), 1, 1, 0));
        update(drawable);
        enqueue(source, new BmsSupplementalVideoFrame(pool, pool.Rent(1), 1, 1, 0));
        update(drawable);
        Assert.That(pool.Returns, Is.Zero);

        drawable.Dispose();
        Assert.That(pool.Returns, Is.EqualTo(1));
        Assert.That(pendingUpload!.Data[0].R, Is.EqualTo(42));
        pendingUpload.Dispose();
        pendingUpload = null;
        Assert.That(pool.Returns, Is.EqualTo(2));
    }

    [Test]
    public void TestRejectedUploadReturnsPixelsExactlyOnce()
    {
        var pool = new RecordingPixelPool();
        using var probe = interceptTextureUploads();
        rejectUpload = true;
        var source = new BmsSupplementalVideoFrameSource([]);
        using var drawable = createDrawable(source);
        enqueue(source, new BmsSupplementalVideoFrame(pool, pool.Rent(1), 1, 1, 0));

        Assert.That(() => update(drawable), Throws.TypeOf<TargetInvocationException>()
                                                       .With.InnerException.TypeOf<ObjectDisposedException>());
        Assert.That(pendingUpload, Is.Null);
        Assert.That(pool.Returns, Is.EqualTo(1));
        drawable.Dispose();
        Assert.That(pool.Returns, Is.EqualTo(1));
    }

    [Test]
    public void TestWorkerCompletesAtEndOfStreamAndLeavesQueuedFrameAvailable()
    {
        if (!BmsSupplementalFFmpegFuncs.TryCreate(out _, out var error))
            Assert.Ignore(error ?? "Supplemental FFmpeg backend unavailable.");

        var bytes = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "bga_fixtures", "mpeg1.mpg"));
        using var source = new BmsSupplementalVideoFrameSource(bytes);
        source.SetTargetTime(60);
        source.Start();

        Assert.That(SpinWait.SpinUntil(() => BmsVideoTestAccess.IsWorkerCompleted(source), TimeSpan.FromSeconds(3)), Is.True,
                    "A fully drained in-memory decoder must not keep polling until the drawable is disposed.");
        Assert.That(source.TryTakeLatestFrame(out var frame), Is.True);
        frame!.Dispose();
        source.Dispose();
        Assert.That(BmsVideoTestAccess.GetQueuedFrameCount(source), Is.Zero);
    }

    private static BmsSupplementalVideoDrawable createDrawable(BmsSupplementalVideoFrameSource source)
    {
        var renderer = new DummyRenderer();
        var texture = renderer.CreateTexture(1, 1);
        var drawable = new BmsSupplementalVideoDrawable(source, new ManualClock(), 0);
        typeof(BmsSupplementalVideoDrawable).GetField("texture", instance_flags)!.SetValue(drawable, texture);
        typeof(BmsSupplementalVideoDrawable).GetField("renderer", instance_flags)!.SetValue(drawable, renderer);
        return drawable;
    }

    private static void enqueue(BmsSupplementalVideoFrameSource source, BmsSupplementalVideoFrame frame)
        => ((ConcurrentQueue<BmsSupplementalVideoFrame>)typeof(BmsSupplementalVideoFrameSource)
            .GetField("queuedFrames", instance_flags)!.GetValue(source)!).Enqueue(frame);

    private static void update(BmsSupplementalVideoDrawable drawable)
        => typeof(BmsSupplementalVideoDrawable).GetMethod("Update", instance_flags)!.Invoke(drawable, null);

    private sealed class RecordingPixelPool : ArrayPool<Rgba32>
    {
        private readonly HashSet<Rgba32[]> leased = [];
        public int Returns { get; private set; }

        public override Rgba32[] Rent(int minimumLength)
        {
            Rgba32[] pixels = [new(42, 0, 0, 255)];
            leased.Add(pixels);
            return pixels;
        }

        public override void Return(Rgba32[] array, bool clearArray = false)
        {
            Assert.That(leased.Remove(array), Is.True, "A pixel buffer must be returned exactly once.");
            Returns++;
            array[0] = new Rgba32(255, 0, 0, 255);
        }
    }

    private static ITextureUpload? pendingUpload;
    private static bool rejectUpload;

    [SetUp]
    public void SetUp() => rejectUpload = false;

    [TearDown]
    public void TearDown()
    {
        pendingUpload?.Dispose();
        pendingUpload = null;
    }

    private static ScopedMethodProbe interceptTextureUploads()
    {
        // Delay the backend's consumption while exercising the real drawable and Texture.
        // The fixture is non-parallel and the scoped probe removes its patch after each test.
        var native = typeof(Texture).Assembly.GetType("osu.Framework.Graphics.Rendering.Dummy.DummyNativeTexture")!;
        return new ScopedMethodProbe(native.GetMethod("SetData")!,
            prefix: typeof(BmsSupplementalVideoLifetimeTest).GetMethod(nameof(captureUpload), BindingFlags.Static | BindingFlags.NonPublic)!);
    }

    private static bool captureUpload(ITextureUpload upload)
    {
        if (rejectUpload)
            throw new ObjectDisposedException("test texture");
        pendingUpload = upload;
        return false;
    }

    private static ScopedMethodProbe interceptUploadCompletion()
    {
        var native = typeof(Texture).Assembly.GetType("osu.Framework.Graphics.Rendering.Dummy.DummyNativeTexture")!;
        return new ScopedMethodProbe(native.GetProperty("UploadComplete")!.GetMethod!,
            prefix: typeof(BmsSupplementalVideoLifetimeTest).GetMethod(nameof(uploadComplete), BindingFlags.Static | BindingFlags.NonPublic)!);
    }

    private static bool uploadComplete(ref bool __result)
    {
        __result = pendingUpload == null;
        return false;
    }
}
