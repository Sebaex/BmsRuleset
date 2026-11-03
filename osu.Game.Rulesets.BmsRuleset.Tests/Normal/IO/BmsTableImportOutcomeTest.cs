#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Framework.Threading;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;
using osu.Game.Rulesets.BmsRuleset.Gameplay.DifficultyTable;
using osu.Game.Rulesets.BmsRuleset.Localisation;
using osu.Game.Rulesets.BmsRuleset.UI.Settings.Components;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.IO;

[TestFixture]
public class BmsTableImportOutcomeTest
{
    private const string valid = """{"name":"Valid","charts":[{"md5":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","level":1}]}""";
    private const string data = """[{"md5":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","level":1}]""";
    private const BindingFlags privateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [TestCase("valid", "Success")]
    [TestCase("invalid-json", "InvalidJson")]
    [TestCase("wrong-types", "InvalidJson")]
    [TestCase("empty", "NoEntries")]
    [TestCase("unusable", "NoEntries")]
    [TestCase("course-only", "Success")]
    [TestCase("missing-file", "MissingFile")]
    [TestCase("missing-meta", "MissingMetadata")]
    [TestCase("invalid-url", "InvalidDataUrl")]
    [TestCase("empty-url", "InvalidDataUrl")]
    [TestCase("split", "Success")]
    public async Task TestOutcomeAndSettingsPreserveSpecificMessage(string scenario, string expectedStatus)
    {
        var root = directory();
        var body = scenario switch
        {
            "invalid-json" => "{",
            "wrong-types" => """{"name":17,"charts":[]}""",
            "empty" => """{"name":"Valid","charts":[]}""",
            "unusable" => """{"name":"Valid","charts":[{"md5":"bad"}]}""",
            "course-only" => """{"name":"Valid","course":[{"name":"Course","md5":["aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"]}]}""",
            "missing-meta" => "<html>No metadata</html>",
            "invalid-url" => """{"name":"Valid","data_url":"file:///not-a-table"}""",
            "empty-url" => """{"name":"Valid","data_url":""}""",
            "split" => """{"name":"Valid","data_url":"/data"}""",
            _ => valid,
        };
        await using var server = new Server(path => Task.FromResult((200, path == "/data" ? data : body)));
        var remote = scenario is "missing-meta" or "invalid-url" or "empty-url" or "split";
        var source = remote ? server.Url + "/header" : Path.Combine(root, "table.json");
        if (!remote && scenario != "missing-file") await File.WriteAllTextAsync(source, body);
        var store = new DifficultyTableStore(null, Path.Combine(root, "core"));
        var progress = new ProgressNotification { State = ProgressNotificationState.Active };
        var outcome = await store.ImportWithOutcomeAsync(source, progress);
        Assert.That(outcome.Status.ToString(), Is.EqualTo(expectedStatus));
        Assert.That(progress.State, Is.EqualTo(ProgressNotificationState.Active), "Only the owner publishes the terminal outcome.");
        var expectedMessage = scenario switch
        {
            "invalid-json" or "wrong-types" => BmsStrings.FailedToParseTable(source),
            "empty" or "unusable" => BmsStrings.NoValidTableEntries(source),
            "missing-file" => BmsStrings.FileDoesNotExist,
            "missing-meta" => BmsStrings.MissingBmstableMeta(source),
            "invalid-url" or "empty-url" => BmsStrings.InvalidTableDataUrl(source),
            _ => BmsStrings.LoadedTable("Valid", scenario == "course-only" ? 0 : 1),
        };
        using var config = configuration();
        using var settings = settingsFor(config, new DifficultyTableStore(null, Path.Combine(root, "ui")));
        await settings.ImportTableAsync(source);
        assertNotification(settings, expectedMessage, outcome.Succeeded);
    }

    [TestCase(403, false)]
    [TestCase(404, false)]
    [TestCase(500, false)]
    [TestCase(403, true)]
    [TestCase(404, true)]
    [TestCase(500, true)]
    public async Task TestHttpOutcomeAndVisibleMessage(int status, bool chartData)
    {
        await using var server = new Server(path => Task.FromResult(chartData && path == "/header"
            ? (200, """{"name":"Valid","data_url":"/data"}""") : (status, "denied")));
        var source = server.Url + "/header";
        var outcome = await new DifficultyTableStore(null, directory()).ImportWithOutcomeAsync(source, new ProgressNotification());
        Assert.That(outcome.Status, Is.EqualTo(chartData ? TableImportStatus.DataDownloadFailed : TableImportStatus.MetadataDownloadFailed));
        Assert.That(outcome.HttpStatus, Is.EqualTo(status));
        using var config = configuration();
        using var settings = settingsFor(config, new DifficultyTableStore(null, directory()));
        await settings.ImportTableAsync(source);
        assertNotification(settings, chartData ? BmsStrings.TableDataDownloadFailed(status) : BmsStrings.TableMetadataDownloadFailed(status), false);
    }

    [Test]
    public async Task TestDuplicateIsIdentifiedWithoutAnotherRegistration()
    {
        var file = Path.Combine(directory(), "table.json");
        await File.WriteAllTextAsync(file, valid);
        var store = new DifficultyTableStore(null, directory());
        using var config = configuration();
        using var settings = settingsFor(config, store);
        await settings.ImportTableAsync(file);
        assertNotification(settings, BmsStrings.LoadedTable("Valid", 1), true);
        await settings.ImportTableAsync(file);
        assertNotification(settings, BmsStrings.DifficultyTableAlreadyImported(file), true);
        Assert.That(store.Tables, Has.Count.EqualTo(1));
        Assert.That(store.GetMarkers("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task TestRemovedReplacementDoesNotPublishPreparedCache()
    {
        var root = directory();
        using var config = configuration();
        var updating = false;
        var held = signal();
        var release = signal();
        await using var server = new Server(async _ =>
        {
            if (updating) { held.TrySetResult(); await release.Task; }
            return (200, updating ? valid.Replace("Valid", "New") : valid);
        });
        var source = server.Url + "/header";
        var store = new DifficultyTableStore(config, root);
        var original = (await store.ImportWithOutcomeAsync(source, new ProgressNotification())).Table!;
        var before = File.ReadAllBytes(cache(root, source));
        updating = true;
        var work = store.ImportWithOutcomeAsync(source, new ProgressNotification(), original);
        try
        {
            await held.Task.WaitAsync(TimeSpan.FromSeconds(5));
            store.RemoveTable(original);
        }
        finally { release.TrySetResult(); }
        var outcome = await work.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(outcome.Status, Is.EqualTo(TableImportStatus.ReplacementMissing));
        Assert.That(File.ReadAllBytes(cache(root, source)), Is.EqualTo(before));
        Assert.That(store.Tables, Is.Empty);
        Assert.That(store.GetMarkers("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"), Is.Empty);
        Assert.That(config.Get<string>(BmsRulesetSetting.DifficultyTableSources), Is.Empty);
        Assert.That(Directory.GetFiles(root, "*.tmp"), Is.Empty);
        Assert.That(store.TryReplaceTable(original, original), Is.False);
        using var settings = settingsFor(config, store);
        await settings.ImportTableAsync(source, original);
        assertNotification(settings, BmsStrings.TableReplacementFailed, false);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task TestConcurrentConfirmationHasOneWinner(bool replacing)
    {
        var root = directory();
        var arrived = signal();
        var release = signal();
        var requests = 0;
        var concurrent = false;
        await using var server = new Server(async _ =>
        {
            var number = Interlocked.Increment(ref requests);
            if (concurrent)
            {
                if (number == 2) arrived.TrySetResult();
                await release.Task;
            }
            return (200, valid.Replace("Valid", "Generation" + number));
        });
        var source = server.Url + "/header";
        using var config = configuration();
        var store = new DifficultyTableStore(config, root);
        var original = replacing ? (await store.ImportWithOutcomeAsync(source, new ProgressNotification())).Table : null;
        requests = 0;
        concurrent = true;
        var first = store.ImportWithOutcomeAsync(source, new ProgressNotification(), original);
        var second = store.ImportWithOutcomeAsync(source, new ProgressNotification(), original);
        try { await arrived.Task.WaitAsync(TimeSpan.FromSeconds(5)); }
        finally { release.TrySetResult(); }
        var outcomes = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(outcomes.Select(o => o.Status), Is.EquivalentTo(new[] { TableImportStatus.Success, replacing ? TableImportStatus.ReplacementMissing : TableImportStatus.AlreadyImported }));
        var winner = outcomes.Single(o => o.Status == TableImportStatus.Success).Table!;
        Assert.That(store.Tables.Single(), Is.SameAs(winner));
        Assert.That(store.GetMarkers("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa").Single().table, Is.SameAs(winner));
        Assert.That(config.Get<string>(BmsRulesetSetting.DifficultyTableSources), Is.EqualTo(source));
        var restarted = new DifficultyTableStore(config, root);
        restarted.LoadPersistedTables();
        Assert.That(restarted.Tables.Single().Name, Is.EqualTo(winner.Name));
        Assert.That(Directory.GetFiles(root, "*.tmp"), Is.Empty);
    }

    [Test]
    public async Task TestCancellationBeforeCommitPublishesNoSourcesOrCache()
    {
        var root = directory();
        using var config = configuration();
        using var cancellation = new CancellationTokenSource();
        var sources = config.GetBindable<string>(BmsRulesetSetting.DifficultyTableSources);
        var publications = new List<string>();
        sources.BindValueChanged(e => publications.Add(e.NewValue));
        await using var server = new Server(_ => Task.FromResult((200, valid)));
        var store = new DifficultyTableStore(config, root);
        var progress = new ProgressNotification();
        // The existing progress callback supplies a deterministic pre-commit cancellation.
        var outcome = await store.ImportWithOutcomeAsync(server.Url, progress, cancellationToken: cancellation.Token, scheduleNotification: action =>
        {
            action();
            if (progress.Progress == 0.5f) cancellation.Cancel();
        });
        GC.KeepAlive(sources);
        Assert.That(outcome.Status, Is.EqualTo(TableImportStatus.Cancelled));
        Assert.That(store.Tables, Is.Empty);
        Assert.That(config.Get<string>(BmsRulesetSetting.DifficultyTableSources), Is.Empty);
        Assert.That(publications, Is.Empty);
        Assert.That(Directory.GetFiles(root), Is.Empty);
        Assert.That(store.GetMarkers("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"), Is.Empty);
    }

    [Test]
    public async Task TestCancellationAfterConfirmationDoesNotRelabelSuccess()
    {
        await using var server = new Server(_ => Task.FromResult((200, valid)));
        var root = directory();
        using var config = configuration();
        var store = new DifficultyTableStore(config, root);
        using var settings = settingsFor(config, store);
        var cancelled = false;
        store.TablesChanged += () =>
        {
            var operation = (CancellationTokenSource)typeof(DifficultyTableSettings).GetField("importCancellation", privateInstance)!.GetValue(settings)!;
            operation.Cancel();
            cancelled = true;
        };
        await settings.ImportTableAsync(server.Url);
        Assert.That(cancelled, Is.True, "Cancellation must occur after the real memory and cache commit.");
        assertNotification(settings, BmsStrings.LoadedTable("Valid", 1), true);
        Assert.That(store.Tables, Has.Count.EqualTo(1));
        var restarted = new DifficultyTableStore(config, root);
        restarted.LoadPersistedTables();
        Assert.That(restarted.Tables.Single().Name, Is.EqualTo("Valid"));
    }

    [Test]
    public async Task TestCachePublicationFailureHasSpecificMessageAndKeepsPreviousGeneration()
    {
        if (!OperatingSystem.IsWindows()) Assert.Ignore("Windows file-sharing semantics.");
        var root = directory();
        var updating = false;
        await using var server = new Server(_ => Task.FromResult((200, updating ? valid.Replace("Valid", "New") : valid)));
        using var config = configuration();
        var store = new DifficultyTableStore(config, root);
        var original = (await store.ImportWithOutcomeAsync(server.Url, new ProgressNotification())).Table!;
        var before = File.ReadAllBytes(cache(root, server.Url));
        updating = true;
        using var settings = settingsFor(config, store);
        using (var lease = new FileStream(cache(root, server.Url), FileMode.Open, FileAccess.Read, FileShare.None))
            await settings.ImportTableAsync(server.Url, original);
        assertNotification(settings, BmsStrings.TableCacheFailed, false);
        Assert.That(File.ReadAllBytes(cache(root, server.Url)), Is.EqualTo(before));
        Assert.That(store.Tables.Single(), Is.SameAs(original));
        Assert.That(store.GetMarkers("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa").Single().table, Is.SameAs(original));
        Assert.That(config.Get<string>(BmsRulesetSetting.DifficultyTableSources), Is.EqualTo(server.Url));
        Assert.That(Directory.GetFiles(root, "*.tmp"), Is.Empty);
    }

    [Test]
    public async Task TestUnexpectedErrorIsNotCancellation()
    {
        var outcome = await new DifficultyTableStore(null, directory()).ImportWithOutcomeAsync("fixture", new ProgressNotification(),
            scheduleNotification: _ => throw new InvalidOperationException("fixture observer"));
        Assert.That(outcome.Status, Is.EqualTo(TableImportStatus.UnexpectedError));
        Assert.That(outcome.Error, Is.TypeOf<InvalidOperationException>());
        Assert.That(outcome.GetMessage("fixture", false), Is.EqualTo(BmsStrings.FailedToLoadTable("fixture")));
    }

    [Test]
    public async Task TestCachePreparationFailureDoesNotApplyImport()
    {
        var root = directory();
        using var config = configuration();
        var store = new DifficultyTableStore(config, root);
        Directory.Delete(root);
        await File.WriteAllTextAsync(root, "fixture blocks cache directory");
        await using var server = new Server(_ => Task.FromResult((200, valid)));
        using var settings = settingsFor(config, store);
        await settings.ImportTableAsync(server.Url);
        assertNotification(settings, BmsStrings.TableCacheFailed, false);
        Assert.That(store.Tables, Is.Empty);
        Assert.That(store.GetMarkers("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"), Is.Empty);
        Assert.That(config.Get<string>(BmsRulesetSetting.DifficultyTableSources), Is.Empty);
        Assert.That(await File.ReadAllTextAsync(root), Is.EqualTo("fixture blocks cache directory"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task TestLegacyCallerNotificationTerminates(bool validJson)
    {
        var path = Path.Combine(directory(), "table.json");
        await File.WriteAllTextAsync(path, validJson ? valid : "{");
        var progress = new ProgressNotification { State = ProgressNotificationState.Active };
        var result = await new DifficultyTableStore(null, directory()).ImportAsync(path, progress);
        Assert.That(result != null, Is.EqualTo(validJson));
        var expected = validJson ? BmsStrings.LoadedTable("Valid", 1) : BmsStrings.FailedToParseTable(path);
        Assert.That(progress.Text, Is.EqualTo(expected));
        Assert.That(progress.CompletionText, Is.EqualTo(expected));
        Assert.That(progress.State, Is.EqualTo(validJson ? ProgressNotificationState.Completed : ProgressNotificationState.Cancelled));
    }

    [Test]
    public async Task TestLegacyInvalidDataUrlStillThrowsAndTerminatesNotification()
    {
        await using var server = new Server(_ => Task.FromResult((200, """{"name":"Valid","data_url":"file:///invalid"}""")));
        var progress = new ProgressNotification { State = ProgressNotificationState.Active };
        var store = new DifficultyTableStore(null, directory());
        Assert.ThrowsAsync<InvalidDataException>(() => store.ImportAsync(server.Url, progress));
        Assert.That(progress.Text, Is.EqualTo(BmsStrings.InvalidTableDataUrl(server.Url)));
        Assert.That(progress.CompletionText, Is.EqualTo(BmsStrings.InvalidTableDataUrl(server.Url)));
        Assert.That(progress.State, Is.EqualTo(ProgressNotificationState.Cancelled));
        Assert.That(store.Tables, Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task TestSettingsSameSourceReplacementAcrossConfirmation(bool alreadyCommitted)
    {
        var root = directory();
        var held = signal();
        var release = signal();
        var requests = 0;
        await using var server = new Server(async _ =>
        {
            var number = Interlocked.Increment(ref requests);
            if (!alreadyCommitted && number == 1) { held.TrySetResult(); await release.Task; }
            return (200, valid.Replace("Valid", "Generation" + number));
        }, allowClientAbort: !alreadyCommitted);
        using var config = configuration();
        var store = new DifficultyTableStore(config, root);
        using var settings = settingsFor(config, store);
        Task? second = null;
        ProgressNotification? latest = null;
        void startReplacement()
        {
            second = settings.ImportTableAsync(server.Url, alreadyCommitted ? store.Tables.Single() : null);
            latest = (ProgressNotification)typeof(DifficultyTableSettings).GetField("importNotification", privateInstance)!.GetValue(settings)!;
        }
        if (alreadyCommitted)
        {
            store.TablesChanged += () =>
            {
                if (second == null) startReplacement();
            };
        }
        var first = settings.ImportTableAsync(server.Url);
        try
        {
            if (!alreadyCommitted)
            {
                await held.Task.WaitAsync(TimeSpan.FromSeconds(5));
                startReplacement();
            }
            await first.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(second, Is.Not.Null);
            await second!.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally { release.TrySetResult(); }
        assertNotification(settings, alreadyCommitted ? BmsStrings.UpdatedTable("Generation2", 1) : BmsStrings.LoadedTable("Generation2", 1), true);
        Assert.That(latest!.State, Is.EqualTo(ProgressNotificationState.Completed));
        Assert.That(requests, Is.EqualTo(2));
        Assert.That(store.Tables.Single().Name, Is.EqualTo("Generation2"));
        Assert.That(store.GetMarkers("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa").Single().table, Is.SameAs(store.Tables.Single()));
        Assert.That(config.Get<string>(BmsRulesetSetting.DifficultyTableSources), Is.EqualTo(server.Url));
        var restarted = new DifficultyTableStore(config, root);
        restarted.LoadPersistedTables();
        Assert.That(restarted.Tables.Single().Name, Is.EqualTo("Generation2"));
    }

    private static TaskCompletionSource signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static BmsRulesetConfigManager configuration() => new(null, new BmsRuleset().RulesetInfo);
    private static string directory()
    {
        var root = Path.Combine(Path.GetTempPath(), nameof(BmsTableImportOutcomeTest), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
    private static string cache(string root, string source) => Path.Combine(root, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant() + ".json");
    private static DifficultyTableSettings settingsFor(BmsRulesetConfigManager config, DifficultyTableStore store)
    {
        var settings = new DifficultyTableSettings(config);
        typeof(DifficultyTableSettings).GetField("difficultyTableStore", privateInstance)!.SetValue(settings, store);
        return settings;
    }
    private static void assertNotification(DifficultyTableSettings settings, LocalisableString expected, bool success)
    {
        ((Scheduler)typeof(Drawable).GetProperty("Scheduler", privateInstance)!.GetValue(settings)!).Update();
        var notification = (ProgressNotification)typeof(DifficultyTableSettings).GetField("importNotification", privateInstance)!.GetValue(settings)!;
        Assert.That(notification.Text, Is.EqualTo(expected));
        Assert.That(notification.CompletionText, Is.EqualTo(expected));
        Assert.That(notification.State, Is.EqualTo(success ? ProgressNotificationState.Completed : ProgressNotificationState.Cancelled));
    }

    internal sealed class Server : IAsyncDisposable
    {
        private readonly TcpListener listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource stop = new();
        private readonly List<Task> connections = [];
        private readonly Task loop;
        internal string Url { get; }
        internal Server(Func<string, Task<(int Status, string Body)>> handler, bool allowClientAbort = false)
        {
            listener.Start();
            Url = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port;
            loop = Task.Run(async () =>
            {
                try
                {
                    while (!stop.IsCancellationRequested)
                    {
                        var client = await listener.AcceptTcpClientAsync(stop.Token);
                        connections.Add(serve(client, handler, allowClientAbort));
                    }
                }
                catch (OperationCanceledException) { }
                catch (SocketException) when (stop.IsCancellationRequested) { }
            });
        }
        private async Task serve(TcpClient client, Func<string, Task<(int Status, string Body)>> handler, bool allowClientAbort)
        {
            using (client)
            {
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);
                var request = await reader.ReadLineAsync(stop.Token);
                if (request == null) return;
                while (!string.IsNullOrEmpty(await reader.ReadLineAsync(stop.Token))) { }
                var response = await handler(request.Split(' ')[1]).WaitAsync(stop.Token);
                var bytes = Encoding.UTF8.GetBytes(response.Body);
                var header = Encoding.ASCII.GetBytes($"HTTP/1.1 {response.Status} Fixture\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n");
                try
                {
                    await stream.WriteAsync(header, stop.Token);
                    await stream.WriteAsync(bytes, stop.Token);
                }
                catch (IOException e) when (allowClientAbort && e.InnerException is SocketException { SocketErrorCode: SocketError.ConnectionAborted or SocketError.ConnectionReset or SocketError.Shutdown })
                {
                    // This fixture deliberately cancels the first HTTP client before releasing its response.
                    TestContext.Out.WriteLine("Expected disconnected fixture client: " + e.InnerException.Message);
                }
            }
        }
        public async ValueTask DisposeAsync()
        {
            stop.Cancel();
            listener.Stop();
            await loop;
            try { await Task.WhenAll(connections); }
            catch (OperationCanceledException) { }
            stop.Dispose();
        }
    }
}
