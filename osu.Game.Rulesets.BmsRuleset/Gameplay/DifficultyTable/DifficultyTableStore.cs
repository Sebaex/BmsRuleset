using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.ExceptionServices;
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
    public IReadOnlyList<DifficultyTable> Tables
    {
        get { lock (tableLock) return tables.ToArray(); }
    }

    internal event Action? TablesChanged;

    internal CollectionSyncManager? CollectionSyncManager { get; }

    /// <summary>
    /// Optional; set after construction to enable automatic marker refresh when tables change.
    /// </summary>
    public DifficultyNameUpdater? DifficultyNameUpdater { get; set; }

    private static readonly HttpClient http_client = new() { Timeout = Timeout.InfiniteTimeSpan };

    internal TimeSpan DownloadTimeout { get; set; } = BmsTableDownload.Timeout;

    private readonly List<DifficultyTable> tables = [];
    private readonly object tableLock = new();

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
        var outcome = await ImportWithOutcomeAsync(source, notification, tableToReplace, cancellationToken, scheduleNotification).ConfigureAwait(false);
        void publish()
        {
            var message = outcome.GetMessage(source, tableToReplace != null);
            notification.CompletionText = message;
            notification.Text = message;
            if (outcome.Succeeded) notification.Progress = 1;
            notification.State = outcome.Succeeded ? ProgressNotificationState.Completed : ProgressNotificationState.Cancelled;
        }
        // Legacy callers still receive null for validation errors and the original exceptions.
        // Settings owns its terminal publication and consumes the explicit outcome instead.
        if (scheduleNotification != null) scheduleNotification(publish);
        else publish();
        if (outcome.Error != null) ExceptionDispatchInfo.Capture(outcome.Error).Throw();
        return outcome.Succeeded ? new ImportResult(outcome.Table!) : null;
    }

    internal async Task<TableImportOutcome> ImportWithOutcomeAsync(string source, ProgressNotification notification, DifficultyTable? tableToReplace = null, CancellationToken cancellationToken = default, Action<Action>? scheduleNotification = null)
    {
        void progress(float value)
        {
            if (scheduleNotification != null) scheduleNotification(() => notification.Progress = value);
            else notification.Progress = value;
        }
        async Task<(string Content, Uri Uri)> download(string url, bool chartData = false)
        {
            try { return await BmsTableDownload.GetAsync(http_client, url, DownloadTimeout, cancellationToken).ConfigureAwait(false); }
            catch (HttpRequestException e) { throw new BmsTableDownloadException(chartData, e); }
        }
        string? temporary = null;
        var writingCache = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (tableLock)
            {
                var rejected = checkDestination(source, tableToReplace);
                if (rejected != null) return rejected;
            }
            progress(0);
            string json;
            string effectiveSource;
            string? dataJson = null;
            var isUrl = source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || source.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
            if (isUrl)
            {
                var response = await download(source).ConfigureAwait(false);
                effectiveSource = response.Uri.AbsoluteUri;
                var trimmed = response.Content.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
                if (trimmed.Length > 0 && trimmed[0] is '{' or '[')
                    json = response.Content;
                else
                {
                    var jsonUrl = resolveUrlFromHtml(response.Content, effectiveSource);
                    if (jsonUrl == null) return new(TableImportStatus.MissingMetadata);
                    var header = await download(jsonUrl).ConfigureAwait(false);
                    json = header.Content;
                    effectiveSource = header.Uri.AbsoluteUri;
                }
            }
            else
            {
                if (!File.Exists(source)) return new(TableImportStatus.MissingFile);
                effectiveSource = source;
                json = await File.ReadAllTextAsync(source, cancellationToken).ConfigureAwait(false);
            }
            progress(0.5f);
            cancellationToken.ThrowIfCancellationRequested();
            var parsed = BmsTableJsonParser.Parse(json);
            if (parsed == null) return new(TableImportStatus.InvalidJson);
            var charts = parsed.Charts;
            var headerDataUrl = parsed.Header?.DataUrl;
            if (charts == null && headerDataUrl != null && isUrl)
            {
                if (string.IsNullOrWhiteSpace(headerDataUrl)
                    || !Uri.TryCreate(new Uri(effectiveSource), headerDataUrl, out var dataUri)
                    || (dataUri.Scheme != Uri.UriSchemeHttp && dataUri.Scheme != Uri.UriSchemeHttps))
                    return new(TableImportStatus.InvalidDataUrl, Error: new InvalidDataException("Unsupported difficulty table data URL."));
                dataJson = (await download(dataUri.AbsoluteUri, true).ConfigureAwait(false)).Content;
                charts = BmsTableJsonParser.Parse(dataJson)?.Charts;
                if (charts == null) return new(TableImportStatus.InvalidJson);
            }
            var table = BmsTableJsonParser.Merge(source, isUrl ? TableSource.RemoteUrl : TableSource.LocalFile, parsed.Header, charts);
            if (table == null || (table.Entries.Count == 0 && table.Courses.Count == 0)) return new(TableImportStatus.NoEntries);
            cancellationToken.ThrowIfCancellationRequested();
            if (isUrl)
            {
                var snapshot = json;
                if (dataJson != null)
                {
                    var headerNode = JsonNode.Parse(json.TrimStart('\uFEFF'), documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip })!.AsObject();
                    var dataNode = JsonNode.Parse(dataJson.TrimStart('\uFEFF'), documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip })!;
                    headerNode["data_url"] = new Uri(new Uri(effectiveSource), headerDataUrl!).AbsoluteUri;
                    headerNode["charts"] = dataNode is JsonArray ? dataNode : dataNode["charts"]!.DeepClone();
                    snapshot = headerNode.ToJsonString();
                }
                writingCache = true;
                temporary = await prepareCacheAsync(cacheFileForUrl(source), snapshot, cancellationToken).ConfigureAwait(false);
            }
            lock (tableLock)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rejected = checkDestination(source, tableToReplace);
                if (rejected != null) return rejected;
                var nextTables = tables.ToList();
                if (tableToReplace == null) nextTables.Add(table);
                else nextTables[nextTables.IndexOf(tableToReplace)] = table;
                tables.EnsureCapacity(nextTables.Count);
                cancellationToken.ThrowIfCancellationRequested();
                // From this point cancellation cannot roll back a published generation.
                // No observer runs between destination validation and memory confirmation.
                if (temporary != null) File.Move(temporary, cacheFileForUrl(source), true);
                lock (indexLock)
                {
                    if (tableToReplace != null) removeFromIndex(tableToReplace);
                    tables.Clear();
                    tables.AddRange(nextTables);
                    addToIndex(table);
                }
                // Config observers run synchronously and may re-enter CRUD. They must
                // observe committed state, with no older table snapshot applied afterwards.
                try { persistTableList(); }
                catch (Exception e)
                {
                    BmsLogger.Error(e, "Difficulty table committed, but source configuration publication failed.");
                }
            }
            // Observers run after confirmation. A refresh failure must not relabel an
            // already committed import as a failed or cancelled cache generation.
            notifyCommittedImport();
            return new(TableImportStatus.Success, table);
        }
        catch (OperationCanceledException e) when (cancellationToken.IsCancellationRequested)
        {
            return new(TableImportStatus.Cancelled, Error: e);
        }
        catch (BmsTableDownloadException e)
        {
            BmsLogger.Error(e, "Difficulty table HTTP download failed.");
            return new(e.ChartData ? TableImportStatus.DataDownloadFailed : TableImportStatus.MetadataDownloadFailed, Error: e, HttpStatus: e.StatusCode is { } status ? (int)status : null);
        }
        catch (TimeoutException e)
        {
            BmsLogger.Error(e, "Difficulty table download timed out.");
            return new(TableImportStatus.TimedOut, Error: e);
        }
        catch (Exception e)
        {
            BmsLogger.Error(e, $"Failed to import or update difficulty table from: {source}");
            return new(writingCache && e is (IOException or UnauthorizedAccessException) ? TableImportStatus.CacheFailed : TableImportStatus.UnexpectedError, Error: e);
        }
        finally
        {
            deleteTemporaryCache(temporary);
        }
    }

    private TableImportOutcome? checkDestination(string source, DifficultyTable? tableToReplace)
    {
        if (tableToReplace != null && !tables.Contains(tableToReplace)) return new(TableImportStatus.ReplacementMissing);
        var existing = tables.FirstOrDefault(t => t != tableToReplace && t.SourcePath == source);
        return existing != null ? new(TableImportStatus.AlreadyImported, existing) : null;
    }

    private void notifyCommittedImport()
    {
        Action[] observers = [() => TablesChanged?.Invoke(), () => NotifyToRebuildTableList(null), () => NotifyToRefreshAllDiffNames(null)];
        foreach (var action in observers)
        {
            try { action(); }
            catch (Exception e) { BmsLogger.Error(e, "Difficulty table committed, but a table observer failed."); }
        }
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
        lock (tableLock)
        {
            tables.Add(table);
            addToIndex(table);
            persistTableList();
        }
        TablesChanged?.Invoke();
        NotifyToRebuildTableList(null);
        NotifyToRefreshAllDiffNames(notification);
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
        lock (tableLock)
        {
            tables.Add(table);
            addToIndex(table);
        }
        TablesChanged?.Invoke();
    }

    /// <summary>
    /// Remove a table and trigger cleanup events, including marker refresh.
    /// </summary>
    public void RemoveTable(DifficultyTable table, ProgressNotification? notification = null)
    {
        lock (tableLock)
        {
            if (!tables.Remove(table)) return;
            removeFromIndex(table);
            persistTableList();
        }
        TablesChanged?.Invoke();
        NotifyToRebuildTableList(table);
        NotifyToRefreshAllDiffNames(notification);
    }

    /// <summary>
    /// Replace a table only while its original identity is still present.
    /// </summary>
    public void ReplaceTable(DifficultyTable oldTable, DifficultyTable newTable, ProgressNotification? notification = null)
        => TryReplaceTable(oldTable, newTable, notification);

    internal bool TryReplaceTable(DifficultyTable oldTable, DifficultyTable newTable, ProgressNotification? notification = null)
    {
        lock (tableLock)
        {
            var oldIndex = tables.IndexOf(oldTable);
            if (oldIndex < 0) return false;
            if (ReferenceEquals(oldTable, newTable)) return true;
            tables.RemoveAt(oldIndex);
            removeFromIndex(oldTable);
            var newIndex = tables.IndexOf(newTable);
            if (newIndex >= 0)
            {
                tables.RemoveAt(newIndex);
                if (newIndex < oldIndex) oldIndex--;
            }
            else addToIndex(newTable);
            tables.Insert(oldIndex, newTable);
            persistTableList();
        }
        TablesChanged?.Invoke();
        NotifyToRebuildTableList(null);
        NotifyToRefreshAllDiffNames(notification);
        return true;
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

    private static async Task<string> prepareCacheAsync(string path, string json, CancellationToken token)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, json, token).ConfigureAwait(false);
            return temporary;
        }
        catch
        {
            deleteTemporaryCache(temporary);
            throw;
        }
    }

    private static void deleteTemporaryCache(string? temporary)
    {
        try { if (temporary != null && File.Exists(temporary)) File.Delete(temporary); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            BmsLogger.Error(e, "Could not remove a temporary difficulty table cache file.");
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
