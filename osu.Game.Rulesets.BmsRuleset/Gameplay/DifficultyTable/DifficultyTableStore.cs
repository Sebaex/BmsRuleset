using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using osu.Game.Database;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;
using osu.Game.Rulesets.BmsRuleset.Localisation;

namespace osu.Game.Rulesets.BmsRuleset.Gameplay.DifficultyTable;

/// <summary>
/// Result of an import operation.
/// </summary>
public record ImportResult(DifficultyTable Table);

public partial class DifficultyTableStore
{
    public IReadOnlyList<DifficultyTable> Tables => tables;

    internal event Action? TablesChanged;

    internal CollectionSyncManager? CollectionSyncManager { get; }

    /// <summary>
    /// Optional; set after construction to enable automatic marker refresh when tables change.
    /// </summary>
    public DifficultyNameUpdater? DifficultyNameUpdater { get; set; }

    private static readonly HttpClient http_client = new() { Timeout = Timeout.InfiniteTimeSpan };

    internal TimeSpan DownloadTimeout { get; set; } = BmsTableDownload.Timeout;

    private readonly List<DifficultyTable> tables = [];

    private readonly BmsRulesetConfigManager? config;
    private readonly string cacheDirectory;
    private readonly object indexLock = new();

    private readonly Dictionary<string, List<(DifficultyTable table, TableEntry entry)>> md5Index
        = new(StringComparer.OrdinalIgnoreCase);

    public DifficultyTableStore(
        BmsRulesetConfigManager? config, string cacheDirectory,
        CollectionSyncManager? syncManager = null,
        RealmAccess? realm = null)
    {
        this.config = config;
        this.cacheDirectory = cacheDirectory;
        CollectionSyncManager = syncManager;
        Directory.CreateDirectory(cacheDirectory);
        RefreshDiffNameEvent += notification => Task.Factory.StartNew(() =>
        {
            DifficultyNameUpdater?.RefreshAllMarkers(notification);
        }, TaskCreationOptions.LongRunning);

        TableListRebuildEvent += tb => CollectionSyncManager?.SyncInTransaction(realm, tb);
    }

    /// <summary>
    /// Reload all persisted table sources into memory index only.
    /// Does NOT trigger TableLoaded / TableRemoved.
    /// Remote URLs read from local cache if available; no HTTP unless cache is missing.
    /// </summary>
    public void LoadPersistedTables()
    {
        tables.Clear();
        lock (indexLock)
            md5Index.Clear();

        var sources = config?.Get<string>(BmsRulesetSetting.DifficultyTableSources) ?? string.Empty;
        if (string.IsNullOrEmpty(sources))
        {
            TablesChanged?.Invoke();
            return;
        }

        foreach (var source in sources.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.IsNullOrEmpty(source)) continue;

            DifficultyTable? table = null;

            if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || source.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // Restore from cache if present; no HTTP at startup
                var cachePath = cacheFileForUrl(source);
                if (File.Exists(cachePath))
                    table = loadFromCache(cachePath, source);
            }
            else if (File.Exists(source))
            {
                table = loadFromCache(source, source);
            }

            if (table != null)
                RestoreTable(table);
        }

        TablesChanged?.Invoke();
    }

    /// <summary>
    /// User-initiated import: download/read, parse, index, update markers + collections.
    /// </summary>
    public async Task<ImportResult?> ImportAsync(string source, ProgressNotification notification, DifficultyTable? tableToReplace = null, CancellationToken cancellationToken = default, Action<Action>? scheduleNotification = null)
    {
        void notify(Action action)
        {
            if (scheduleNotification != null)
                scheduleNotification(action);
            else
                action();
        }
        async Task<(string Content, Uri Uri)> download(string url, bool chartData = false)
        {
            try { return await BmsTableDownload.GetAsync(http_client, url, DownloadTimeout, cancellationToken).ConfigureAwait(false); }
            catch (HttpRequestException e) { throw new BmsTableDownloadException(chartData, e); }
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(source)) return null;

        notify(() => notification.Progress = 0f);

        // ── Stage 1: Read + Parse ──
        string json;
        string effectiveSource;
        string? dataJson = null;

        var isUrl = source.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || source.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        if (isUrl)
        {
            effectiveSource = source;
            var firstResponse = await download(source).ConfigureAwait(false);
            var body = firstResponse.Content;
            effectiveSource = firstResponse.Uri.AbsoluteUri;

            var trimmed = body.TrimStart();
            if (trimmed.Length > 0 && (trimmed[0] == '{' || trimmed[0] == '['))
            {
                json = body;
            }
            else
            {
                // HTML — look for bmstable meta tag
                var jsonUrl = resolveUrlFromHtml(body, effectiveSource);
                if (jsonUrl == null)
                {
                    notify(() => notification.CompletionText = BmsStrings.MissingBmstableMeta(source));
                    notify(() => notification.State = ProgressNotificationState.Cancelled);
                    return null;
                }

                var headerResponse = await download(jsonUrl).ConfigureAwait(false);
                json = headerResponse.Content;
                effectiveSource = headerResponse.Uri.AbsoluteUri;
            }


        }
        else
        {
            if (!File.Exists(source))
            {
                notify(() => notification.CompletionText = BmsStrings.FileDoesNotExist);
                notify(() => notification.State = ProgressNotificationState.Cancelled);
                return null;
            }

            effectiveSource = source;
            json = await File.ReadAllTextAsync(source, cancellationToken).ConfigureAwait(false);
        }

        notify(() => notification.Progress = 0.5f);

        cancellationToken.ThrowIfCancellationRequested();
        var parsed = BmsTableJsonParser.Parse(json);
        if (parsed == null)
        {
            notify(() => notification.CompletionText = BmsStrings.FailedToParseTable(source));
            notify(() => notification.State = ProgressNotificationState.Cancelled);
            return null;
        }

        // If header has a separate data_url but no embedded charts, fetch data.
        var charts = parsed.Charts;
        var headerDataUrl = parsed.Header?.DataUrl;
        if (charts == null && headerDataUrl != null && isUrl)
        {
            if (!Uri.TryCreate(new Uri(effectiveSource), headerDataUrl, out var dataUri)
                || (dataUri.Scheme != Uri.UriSchemeHttp && dataUri.Scheme != Uri.UriSchemeHttps))
                throw new InvalidDataException("Unsupported difficulty table data URL.");
            dataJson = (await download(dataUri.AbsoluteUri, true).ConfigureAwait(false)).Content;
            var dataParsed = BmsTableJsonParser.Parse(dataJson);
            charts = dataParsed?.Charts;
            if (charts == null)
            {
                notify(() => notification.CompletionText = BmsStrings.FailedToParseTable(source));
                notify(() => notification.State = ProgressNotificationState.Cancelled);
                return null;
            }
        }

        // ── Stage 2: Merge + Index ──
        var tableSource = isUrl ? TableSource.RemoteUrl : TableSource.LocalFile;
        var table = BmsTableJsonParser.Merge(source, tableSource, parsed.Header, charts);

        if (table == null)
        {
            notify(() => notification.CompletionText = BmsStrings.NoValidTableEntries(source));
            notify(() => notification.State = ProgressNotificationState.Cancelled);
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (isUrl)
        {
            // A single combined snapshot makes header and data one atomic cache generation.
            var snapshot = json;
            if (dataJson != null)
            {
                var headerNode = JsonNode.Parse(json.TrimStart('\uFEFF'), documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip })!.AsObject();
                var dataNode = JsonNode.Parse(dataJson.TrimStart('\uFEFF'), documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip })!;
                headerNode["data_url"] = new Uri(new Uri(effectiveSource), headerDataUrl!).AbsoluteUri;
                headerNode["charts"] = dataNode is JsonArray ? dataNode : dataNode["charts"]!.DeepClone();
                snapshot = headerNode.ToJsonString();
            }
            await writeCacheAsync(cacheFileForUrl(source), snapshot, cancellationToken).ConfigureAwait(false);
        }
        // The cache rename is the commit boundary; cancellation afterwards must
        // not leave the memory index on the previous cache generation.
        // Marker refresh runs in the background and must not update a finished
        // or superseded import notification.
        if (tableToReplace == null)
            AddTable(table);
        else
            ReplaceTable(tableToReplace, table);
        notify(() => notification.Progress = 1);
        return new ImportResult(table);
    }

    public void NotifyToRebuildTableList(DifficultyTable? tableRemoved)
    {
        TableListRebuildEvent?.Invoke(tableRemoved);
    }

    public void NotifyToRefreshAllDiffNames(ProgressNotification? notification = null)
    {
        RefreshDiffNameEvent?.Invoke(notification);
    }

    #region Persistence

    private void persistTableList()
    {
        if (config == null) return;

        var sources = string.Join(";", tables.Select(t => t.SourcePath));
        config.SetValue(BmsRulesetSetting.DifficultyTableSources, sources);
    }

    #endregion

    /// <summary>
    /// Fires when a table is added or removed, so the table list in settings can rebuild.
    /// </summary>
    public event Action<DifficultyTable?>? TableListRebuildEvent;

    public event Action<ProgressNotification?>? RefreshDiffNameEvent;


    #region Public CRUD

    /// <summary>
    /// Add a newly-imported table. Fires events for UI updates and triggers marker refresh.
    /// </summary>
    public void AddTable(DifficultyTable table, ProgressNotification? notification = null)
    {
        tables.Add(table);
        addToIndex(table);
        TablesChanged?.Invoke();
        NotifyToRebuildTableList(null);
        NotifyToRefreshAllDiffNames(notification);
        persistTableList();
    }

    /// <summary>
    /// Restore a table at startup. UI, marker, and collection events are not fired because those values are already persisted.
    /// </summary>
    public void RestoreTable(DifficultyTable table)
    {
        // var entries = new List<TableEntry>();
        // DifficultyTable[] dts =
        // [
        //     new() { Name = "A", Symbol = "#", LevelOrder = ["★1", "★2", "★3"], Source = TableSource.RemoteUrl, SourcePath = "http://x/a", Entries = entries },
        //     new() { Name = "Normal Table", Symbol = "★", LevelOrder = ["★1", "★2", "★3"], Source = TableSource.RemoteUrl, SourcePath = "http://x/n", Entries = entries },
        //     new() { Name = "Turbow Difficulty Table (2024 Edition)", Symbol = "TT", LevelOrder = ["★1", "★2", "★3"], Source = TableSource.RemoteUrl, SourcePath = "http://x/t", Entries = entries },
        //     new() { Name = "超絶技巧的難易度表: The Ultimate BMS Difficulty Reference Collection", Symbol = "ULT", LevelOrder = ["★1", "★2", "★3"], Source = TableSource.RemoteUrl, SourcePath = "http://x/u", Entries = entries },
        //     new() { Name = "ThisIsAVeryLongTableNameWithoutAnySpacesThatShouldWrapAtTheContainerEdge", Symbol = "LNG", LevelOrder = ["★1", "★2", "★3"], Source = TableSource.LocalFile, SourcePath = "C:\\t\\l.json", Entries = entries },
        //     new() { Name = "Short last line — this name wraps around but ends early", Symbol = "END", LevelOrder = ["★1", "★2", "★3"], Source = TableSource.LocalFile, SourcePath = "C:\\t\\e.json", Entries = entries },
        //     new() { Name = "This table name wraps perfectly to fill nearly the entire available container width", Symbol = "FULL", LevelOrder = ["★1", "★2", "★3"], Source = TableSource.LocalFile, SourcePath = "C:\\t\\f.json", Entries = entries },
        // ];
        // foreach (var dt in dts)
        //     tables.Add(dt);
        tables.Add(table);
        addToIndex(table);
        TablesChanged?.Invoke();
    }

    /// <summary>
    /// Remove a table and trigger cleanup events, including marker refresh.
    /// </summary>
    public void RemoveTable(DifficultyTable table, ProgressNotification? notification = null)
    {
        tables.Remove(table);
        removeFromIndex(table);
        TablesChanged?.Invoke();
        NotifyToRebuildTableList(table);
        NotifyToRefreshAllDiffNames(notification);
        persistTableList();
    }

    /// <summary>
    /// Replace an existing table with updated data. Removes the old entries from the index,
    /// inserts the new table at the same position, and fires all events including marker refresh.
    /// Handles the case where newTable was already added via ImportAsync -> AddTable.
    /// </summary>
    public void ReplaceTable(DifficultyTable oldTable, DifficultyTable newTable, ProgressNotification? notification = null)
    {
        var oldIndex = tables.IndexOf(oldTable);
        if (oldIndex < 0) return;

        tables.RemoveAt(oldIndex);
        removeFromIndex(oldTable);

        // newTable may already be in the list (added by ImportAsync -> AddTable).
        var newIndex = tables.IndexOf(newTable);
        if (newIndex >= 0)
        {
            // Move it to the old position.
            tables.RemoveAt(newIndex);
            var insertIndex = newIndex < oldIndex ? oldIndex - 1 : oldIndex;
            if (insertIndex >= tables.Count)
                tables.Add(newTable);
            else
                tables.Insert(insertIndex, newTable);
        }
        else
        {
            if (oldIndex >= tables.Count)
                tables.Add(newTable);
            else
                tables.Insert(oldIndex, newTable);
            addToIndex(newTable);
        }

        TablesChanged?.Invoke();
        NotifyToRebuildTableList(null);
        NotifyToRefreshAllDiffNames(notification);
        persistTableList();
    }

    public List<(DifficultyTable table, TableEntry entry)> GetMarkers(string md5Hash)
    {
        lock (indexLock)
            return md5Index.TryGetValue(md5Hash, out var markers) ? [..markers] : [];
    }

    internal Dictionary<string, string> GetMarkerNames()
    {
        lock (indexLock)
            return md5Index.ToDictionary(pair => pair.Key, pair => DifficultyNameUpdater.FormatMarkers(pair.Value), StringComparer.OrdinalIgnoreCase);
    }

    #endregion

    #region Index management

    private void addToIndex(DifficultyTable table)
    {
        lock (indexLock)
            addToIndexLocked(table);
    }

    private void addToIndexLocked(DifficultyTable table)
    {
        foreach (var entry in table.Entries)
        {
            if (!md5Index.TryGetValue(entry.Md5Hash, out var list))
                md5Index[entry.Md5Hash] = list = [];
            list.Add((table, entry));
        }
    }

    private void removeFromIndex(DifficultyTable table)
    {
        lock (indexLock)
            removeFromIndexLocked(table);
    }

    private void removeFromIndexLocked(DifficultyTable table)
    {
        foreach (var entry in table.Entries)
        {
            if (md5Index.TryGetValue(entry.Md5Hash, out var list))
            {
                list.RemoveAll(t => t.table == table);
                if (list.Count == 0)
                    md5Index.Remove(entry.Md5Hash);
            }
        }
    }

    #endregion

    #region Cache

    private static async Task writeCacheAsync(string path, string json, CancellationToken token)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, json, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            File.Move(temporary, path, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
    private string cacheFileForUrl(string url)
    {
        var hash = Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(url))).ToLowerInvariant();
        return Path.Combine(cacheDirectory, $"{hash}.json");
    }

    private DifficultyTable? loadFromCache(string cachePath, string sourcePath)
    {
        try
        {
            var json = File.ReadAllText(cachePath);
            var parsed = BmsTableJsonParser.Parse(json);
            if (parsed == null) return null;

            // If the cached header has a data_url, try to load cached data separately.
            var charts = parsed.Charts;
            if (charts == null && parsed.Header?.DataUrl != null)
            {
                var dataUrl = parsed.Header.DataUrl.StartsWith("http", StringComparison.Ordinal)
                    ? parsed.Header.DataUrl
                    : new Uri(new Uri(sourcePath), parsed.Header.DataUrl).ToString();

                var dataCachePath = cacheFileForUrl(dataUrl);
                if (File.Exists(dataCachePath))
                {
                    var dataJson = File.ReadAllText(dataCachePath);
                    var dataParsed = BmsTableJsonParser.Parse(dataJson);
                    charts = dataParsed?.Charts;
                }
            }

            var source = sourcePath.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? TableSource.RemoteUrl
                : TableSource.LocalFile;

            return BmsTableJsonParser.Merge(sourcePath, source, parsed.Header, charts);
        }
        catch (Exception e)
        {
            BmsLogger.Error(e, $"Failed to read cached difficulty table {cachePath}: {e.Message}");
            return null;
        }
    }

    #endregion

    #region HTML parsing

    private static string? resolveUrlFromHtml(string html, string pageUrl)
    {
        var content = extractMetaContent(html, "bmstable")
                      ?? extractMetaContent(html, "bmstable-alt");

        if (string.IsNullOrWhiteSpace(content)) return null;

        return Uri.TryCreate(new Uri(pageUrl), content, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri.AbsoluteUri
            : null;
    }

    private static string? extractMetaContent(string html, string name)
    {
        // Examples in comments or raw script/style text are not document metadata.
        html = Regex.Replace(html, @"<!--.*?-->|<script\b[^>]*>.*?</script\s*>|<style\b[^>]*>.*?</style\s*>", "",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        foreach (Match tag in Regex.Matches(html, @"<meta\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)))
        {
            var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match attribute in Regex.Matches(tag.Value,
                         """([\w:-]+)\s*=\s*(?:"([^"]*)"|'([^']*)'|([^\s"'=<>`]+))""",
                         RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)))
                attributes.TryAdd(attribute.Groups[1].Value, WebUtility.HtmlDecode(attribute.Groups[2].Success ? attribute.Groups[2].Value : attribute.Groups[3].Success ? attribute.Groups[3].Value : attribute.Groups[4].Value));
            if (attributes.TryGetValue("name", out var tagName) && tagName.Equals(name, StringComparison.OrdinalIgnoreCase)
                && attributes.TryGetValue("content", out var content)) return content;
        }
        return null;
    }
    #endregion

}
