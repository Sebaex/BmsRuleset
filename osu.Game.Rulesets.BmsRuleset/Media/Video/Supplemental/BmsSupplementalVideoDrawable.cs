using System;
using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Timing;

namespace osu.Game.Rulesets.BmsRuleset.Media.Video.Supplemental;

internal sealed partial class BmsSupplementalVideoDrawable : CompositeDrawable
{
    private readonly Stream? stream;
    private readonly IClock playbackClock;
    private readonly double eventStartTime;
    private readonly Sprite sprite;
    private IRenderer? renderer;
    private Texture? texture;
    private BmsSupplementalVideoFrameSource? frameSource;
    private BmsSupplementalVideoFrame? pendingFrame;

    private BmsSupplementalVideoDrawable(Stream? stream, BmsSupplementalVideoFrameSource? frameSource, IClock playbackClock, double eventStartTime)
    {
        this.stream = stream;
        this.frameSource = frameSource;
        this.playbackClock = playbackClock;
        this.eventStartTime = eventStartTime;

        Anchor = Anchor.Centre;
        Origin = Anchor.Centre;
        RelativeSizeAxes = Axes.Both;
        FillMode = FillMode.Stretch;

        InternalChild = sprite = new Sprite
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            RelativeSizeAxes = Axes.Both,
            FillMode = FillMode.Fit,
        };
    }

    // Lazy path: the stream is copied and the frame source is started in load(). Used when the video
    // is created mid-gameplay (poor BGA, or a hit on a file the loading screen didn't warm).
    public BmsSupplementalVideoDrawable(Stream stream, IClock playbackClock, double eventStartTime)
        : this(stream, null, playbackClock, eventStartTime)
    {
    }

    // Pre-warmed path: the frame source was already started during the loading screen, so load()
    // skips the stream copy + FFmpeg init and only takes the renderer for texture uploads — no
    // game-thread hitch at the BGA event's fire time.
    public BmsSupplementalVideoDrawable(BmsSupplementalVideoFrameSource frameSource, IClock playbackClock, double eventStartTime)
        : this(null, frameSource, playbackClock, eventStartTime)
    {
    }

    [BackgroundDependencyLoader]
    private void load(IRenderer renderer)
    {
        this.renderer = renderer;

        // Pre-warmed by the loading screen: the frame source is already decoding, so we only need
        // the renderer for texture uploads. The lazy path below is the fallback for videos created
        // mid-gameplay (poor BGA, a hit on a file the loading screen didn't warm) where there was no
        // chance to warm — it pays the stream copy + FFmpeg init on the game thread, but only then.
        if (frameSource != null)
        {
            BmsLogger.Log("[BGA] Supplemental FFmpeg video path selected (pre-warmed)");
            return;
        }

        var sourceStream = stream;
        if (sourceStream == null)
            return;

        using var memory = new MemoryStream();
        if (sourceStream.CanSeek)
            sourceStream.Position = 0;

        sourceStream.CopyTo(memory);
        frameSource = new BmsSupplementalVideoFrameSource(memory.ToArray());
        frameSource.Start();

        BmsLogger.Log("[BGA] Supplemental FFmpeg video path selected");
    }

    protected override void Update()
    {
        base.Update();

        if (frameSource == null || renderer == null)
            return;

        var playbackTime = Math.Max(0, (playbackClock.CurrentTime - eventStartTime) / 1000.0);
        frameSource.SetTargetTime(playbackTime);

        if (frameSource.TryTakeLatestFrame(out var frame))
        {
            pendingFrame?.Dispose();
            pendingFrame = frame;
        }

        // Keep only the newest decoded frame while the draw thread is behind. It must
        // survive EOF so the final image can still be submitted when drawing resumes.
        if (pendingFrame == null || texture?.UploadComplete == false)
            return;

        var currentFrame = pendingFrame;
        pendingFrame = null;

        using (currentFrame)
        {
            if (texture == null || texture.Width != currentFrame.Width || texture.Height != currentFrame.Height)
            {
                texture = renderer.CreateTexture(currentFrame.Width, currentFrame.Height);
                sprite.Texture = texture;
            }

            var upload = currentFrame.CreateUpload();
            try
            {
                // SetData queues consumption on the draw thread, which owns the pixels until then.
                texture.SetData(upload);
            }
            catch
            {
                upload.Dispose();
                throw;
            }
        }
    }

    protected override void Dispose(bool isDisposing)
    {
        frameSource?.Dispose();
        pendingFrame?.Dispose();
        pendingFrame = null;
        stream?.Dispose();
        base.Dispose(isDisposing);
    }
}
