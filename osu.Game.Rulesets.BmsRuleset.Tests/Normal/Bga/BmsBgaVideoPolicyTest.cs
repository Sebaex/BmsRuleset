using System.IO;
using System.Reflection;
using NUnit.Framework;
using osu.Framework.Graphics.Video;
using osu.Framework.Graphics.Animations;
using osu.Framework.Timing;
using osu.Game.Rulesets.BmsRuleset.Media.Video;
using osu.Game.Rulesets.BmsRuleset.UI.HudComponents;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Bga;

[TestFixture]
public class BmsBgaVideoPolicyTest
{
    [TestCase("clip.m1v")]
    [TestCase("clip.M2V")]
    [TestCase("clip.asf")]
    [TestCase("clip.m4v")]
    [TestCase("clip.mp4")]
    public void SupportedVideoPathsReachTheVideoFactory(string path)
    {
        var classify = typeof(BmsBgaDisplay).GetMethod("isVideo", BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.That(classify.Invoke(null, [path]), Is.True);
    }

    [Test]
    public void FrameworkPlaybackDoesNotLoopAndRestartsForEachEvent()
    {
        var provider = new FrameworkBmsBgaVideoProvider();
        var clock = new ManualFramedClock { CurrentTime = 1000 };
        using var firstStream = new MemoryStream();
        using var first = (Video)provider.Create(new BmsBgaVideoRequest("clip.mp4", firstStream, clock, 600));
        using var secondStream = new MemoryStream();
        using var second = (Video)provider.Create(new BmsBgaVideoRequest("clip.mp4", secondStream, clock, 1000));
        Assert.That(first.Loop, Is.False, "BGAProcessor.getBGAData starts normal videos with play(time, false)");
        // An unloaded video has duration zero, so its public playback position is clamped until decoding starts.
        var playbackClock = (ManualClock)typeof(AnimationClockComposite).GetField("manualClock", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(first)!;
        Assert.That(playbackClock.CurrentTime, Is.EqualTo(400));
        Assert.That(second.PlaybackPosition, Is.Zero);
        Assert.That(second, Is.Not.SameAs(first));
    }
}
