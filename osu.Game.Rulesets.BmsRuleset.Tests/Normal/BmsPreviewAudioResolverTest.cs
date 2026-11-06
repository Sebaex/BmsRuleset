#nullable enable
using System;
using System.IO;
using NUnit.Framework;
using osu.Game.Rulesets.BmsRuleset.Media.Audio.Preview;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal;

[TestFixture]
public class BmsPreviewAudioResolverTest
{
    [TestCase(null)]
    [TestCase("dedicated.wav")]
    public void MissingDirectoryAllowsPreviewFallback(string? previewFile)
    {
        var missing = Path.Combine(Path.GetTempPath(), "bms-missing-preview-" + Guid.NewGuid().ToString("N"));
        Assert.That(Directory.Exists(missing), Is.False);
        Assert.That(BmsPreviewAudioResolver.GetExistingDedicatedPreviewCandidates(missing, previewFile), Is.Empty);
    }

    [Test]
    public void EmptySourceUsesTheResourceStoresCurrentDirectoryContract()
        => Assert.That(BmsPreviewAudioResolver.GetExistingDedicatedPreviewCandidates("", null),
            Is.EqualTo(BmsPreviewAudioResolver.GetExistingDedicatedPreviewCandidates(Directory.GetCurrentDirectory(), null)));

    [Test]
    public void ExistingDirectoryKeepsExplicitThenNamedThenFallbackPreviews()
    {
        var directory = Directory.CreateTempSubdirectory("bms-preview-resolver-");
        try
        {
            foreach (var name in new[] { "selected.ogg", "preview.wav", "alternate_preview.ogg" })
                File.WriteAllBytes(Path.Combine(directory.FullName, name), []);
            Assert.That(BmsPreviewAudioResolver.GetExistingDedicatedPreviewCandidates(directory.FullName, "selected.ogg"),
                Is.EqualTo(new[] { "selected.ogg", "preview.wav", "alternate_preview.ogg" }));
        }
        finally
        {
            directory.Delete(true);
        }
    }
}
