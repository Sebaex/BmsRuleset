using System;
using System.Collections.Generic;
using System.IO;
using osu.Game.Rulesets.BmsRuleset.IO.ResourceStore;
using osu.Game.Rulesets.BmsRuleset.Media.Audio.Samples;

namespace osu.Game.Rulesets.BmsRuleset.Media.Audio.Preview;

internal static class BmsPreviewAudioResolver
{
    internal static IReadOnlyList<string> GetExistingDedicatedPreviewCandidates(string basePath, string? previewFile)
    {
        basePath = Path.GetFullPath(string.IsNullOrEmpty(basePath) ? "." : basePath);
        // Optional discovery must leave the event-based preview fallback available.
        if (!Directory.Exists(basePath))
            return [];

        using var fileStore = new BmsFileResourceStore(basePath);
        var candidates = new List<string>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in getDedicatedPreviewCandidates(basePath, previewFile))
        {
            foreach (var resolvedPath in resolveCandidatePaths(fileStore, candidate))
            {
                var relativePath = Path.GetRelativePath(basePath, resolvedPath);

                if (seenPaths.Add(relativePath))
                    candidates.Add(relativePath);
            }
        }

        return candidates;
    }

    private static IEnumerable<string> getDedicatedPreviewCandidates(string basePath, string? previewFile)
    {
        if (!string.IsNullOrWhiteSpace(previewFile))
            yield return previewFile;

        var fallbackCandidates = new List<string>();

        foreach (var file in Directory.EnumerateFiles(basePath, "*", SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileName(file);

            if (!fileName.Contains("preview", StringComparison.OrdinalIgnoreCase))
                continue;

            var extension = Path.GetExtension(file).TrimStart('.');

            foreach (var supportedExtension in BmsAudioResourceStore.Extensions)
            {
                if (!extension.Equals(supportedExtension, StringComparison.OrdinalIgnoreCase))
                    continue;

                var relativePath = Path.GetRelativePath(basePath, file);

                if (fileName.Equals($"preview.{supportedExtension}", StringComparison.OrdinalIgnoreCase))
                    yield return relativePath;
                else
                    fallbackCandidates.Add(relativePath);

                break;
            }
        }

        foreach (var candidate in fallbackCandidates)
            yield return candidate;
    }

    private static IEnumerable<string> resolveCandidatePaths(BmsFileResourceStore fileStore, string candidate)
    {
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var lookup in new BmsSampleInfo(candidate).LookupNames)
        {
            if (fileStore.TryResolve(lookup, out var resolvedPath) && seenPaths.Add(resolvedPath))
                yield return resolvedPath;

            var stem = Path.ChangeExtension(lookup, null);

            foreach (var extension in BmsAudioResourceStore.Extensions)
            {
                if (fileStore.TryResolve($"{stem}.{extension}", out resolvedPath) && seenPaths.Add(resolvedPath))
                    yield return resolvedPath;
            }
        }
    }
}
