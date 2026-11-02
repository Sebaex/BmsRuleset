#nullable enable
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Threading;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;
using osu.Game.Rulesets.BmsRuleset.Gameplay.DifficultyTable;
using osu.Game.Rulesets.BmsRuleset.UI.Settings.Components;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.IO;

[TestFixture]
public class BmsTableCacheTest
{
    private const string data = """[{"level":1,"md5":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}]""";
    private const string valid = """{"name":"Valid","symbol":"P●","level_order":[1],"charts":[{"level":1,"md5":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}],"course":[{"name":"Old","md5":["aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"],"constraint":["grade_mirror"]}]}""";
    private static string directory()
    {
        var path = Path.Combine(Path.GetTempPath(), nameof(BmsTableCacheTest), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
    private static string cache(DifficultyTableStore store, string source)
        => (string)typeof(DifficultyTableStore).GetMethod("cacheFileForUrl", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(store, [source])!;
    private static DifficultyTableStore restore(string root, string source)
    {
        using var config = new BmsRulesetConfigManager(null, new BmsRuleset().RulesetInfo);
        config.SetValue(BmsRulesetSetting.DifficultyTableSources, source);
        var store = new DifficultyTableStore(config, root);
        store.LoadPersistedTables();
        return store;
    }

    [TestCase("<meta name='bmstable' content='header.json'>")]
    [TestCase("<meta content=\"header.json\" name=\"bmstable\">")]
    [TestCase("<META  NAME = \"bmstable\"  CONTENT = \"header.json\">")]
    [TestCase("<meta content=header.json name=bmstable>")]
    [TestCase("<meta name=\"bmstable-alt\" content=\"header.json\">")]
    [TestCase("<!-- <meta name=\"bmstable\" content=\"wrong.json\"> --><meta name='bmstable' content='header.json'>")]
    [TestCase("<script>var x='<meta name=\"bmstable\" content=\"wrong.json\">';</script><meta name='bmstable' content='header.json'>")]
    public void TestHtmlVariations(string html)
    {
        var method = typeof(DifficultyTableStore).GetMethod("resolveUrlFromHtml", BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.That(method.Invoke(null, [html, "https://fixture.test/table/index.html"]), Is.EqualTo("https://fixture.test/table/header.json"));
    }

    [Test]
    public async Task TestRedirectedHeaderRelativeDataAndRestart()
    {
        await using var server = new Server(path => path switch
        {
            "/page" => (302, "", "/site/index.html"),
            "/site/index.html" => (200, "<meta content='../header' name='bmstable'>", null),
            "/header" => (302, "", "/cdn/header.json"),
            "/cdn/header.json" => (200, """{"name":"Relative","symbol":"P●","level_order":[1],"data_url":"data.json"}""", null),
            "/cdn/data.json" => (200, data, null),
            _ => (404, "wrong URL", null),
        });
        var root = directory();
        var source = server.Url + "/page";
        var store = new DifficultyTableStore(null, root);
        var imported = await store.ImportAsync(source, new ProgressNotification());
        Assert.That(imported, Is.Not.Null);
        Assert.That(imported!.Table.SourcePath, Is.EqualTo(source));
        Assert.That(server.Requests, Does.Contain("/cdn/data.json"));
        Assert.That(Directory.GetFiles(root), Has.Length.EqualTo(1));
        var reloaded = restore(root, source).Tables.Single();
        Assert.That(reloaded.Name, Is.EqualTo("Relative"));
        Assert.That(reloaded.Entries.Single().Level, Is.EqualTo("1"));
        Assert.That(reloaded.Entries.Single().LevelIndex, Is.Zero);
        Assert.That(server.Requests.Count, Is.EqualTo(5), "Restoration must not request the network.");
    }

    [TestCase("http")]
    [TestCase("header")]
    [TestCase("html-data")]
    [TestCase("truncated")]
    [TestCase("data-http")]
    public async Task TestFailedUpdateKeepsCacheMemoryAndRestart(string failure)
    {
        var updating = false;
        var origin = "";
        await using var server = new Server(path =>
        {
            if (!updating) return (200, valid, null);
            if (failure == "http") return (500, "failed", null);
            if (failure == "header") return (200, "{", null);
            if (path == "/header") return (200, "{\"name\":\"New\",\"data_url\":\"" + origin + "/data\",\"course\":[{\"name\":\"New course\",\"md5\":[\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"]}]}", null);
            return failure == "html-data" ? (200, "<html>error</html>", null) : failure == "truncated" ? (200, "[", null) : (500, "data failed", null);
        });
        origin = server.Url;
        var source = origin + "/header";
        var root = directory();
        var store = new DifficultyTableStore(null, root);
        var first = (await store.ImportAsync(source, new ProgressNotification()))!.Table;
        var before = await File.ReadAllBytesAsync(cache(store, source));
        var changes = 0;
        store.TablesChanged += () => changes++;
        updating = true;
        if (failure is "http" or "data-http")
            Assert.CatchAsync<HttpRequestException>(() => store.ImportAsync(source, new ProgressNotification(), first));
        else Assert.That(await store.ImportAsync(source, new ProgressNotification(), first), Is.Null);
        Assert.That(await File.ReadAllBytesAsync(cache(store, source)), Is.EqualTo(before));
        Assert.That(store.Tables.Single(), Is.SameAs(first));
        Assert.That(changes, Is.Zero);
        Assert.That(Directory.GetFiles(root, "*.tmp"), Is.Empty);
        var restored = restore(root, source).Tables.Single();
        Assert.That(restored.Name, Is.EqualTo("Valid"));
        Assert.That(restored.Courses.Single().Name, Is.EqualTo("Old"));
        Assert.That(restored.Courses.Single().Constraints, Is.EqualTo(["grade_mirror"]));
    }

    [Test]
    public void TestLegacySplitCacheStillLoads()
    {
        var root = directory();
        const string source = "https://legacy.test/header.json";
        const string dataUrl = "https://legacy.test/data.json";
        var store = new DifficultyTableStore(null, root);
        File.WriteAllText(cache(store, source), "{\"name\":\"Legacy\",\"data_url\":\"" + dataUrl + "\"}");
        File.WriteAllText(cache(store, dataUrl), data);
        Assert.That(restore(root, source).Tables.Single().Entries.Single().Level, Is.EqualTo("1"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task TestHttpFailureChangesVisibleNotificationTitle(bool chartData)
    {
        var origin = "";
        await using var server = new Server(path => chartData && path == "/header"
            ? (200, "{\"name\":\"Header\",\"data_url\":\"" + origin + "/data\"}", null)
            : (403, "denied", null));
        origin = server.Url;
        using var config = new BmsRulesetConfigManager(null, new BmsRuleset().RulesetInfo);
        using var settings = new DifficultyTableSettings(config);
        typeof(DifficultyTableSettings).GetField("difficultyTableStore", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(settings, new DifficultyTableStore(null, directory()));
        await settings.ImportTableAsync(origin + "/header");
        ((Scheduler)typeof(Drawable).GetProperty("Scheduler", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(settings)!).Update();
        var notification = (ProgressNotification)typeof(DifficultyTableSettings).GetField("importNotification", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(settings)!;
        Assert.That(notification.State, Is.EqualTo(ProgressNotificationState.Cancelled));
        Assert.That(notification.Text, Is.EqualTo(notification.CompletionText));
        Assert.That(notification.Text.ToString(), Does.Contain("403"));
    }

    [Test]
    public async Task TestCacheReplacementFailureKeepsOldSnapshotAndMemory()
    {
        if (!OperatingSystem.IsWindows()) Assert.Ignore("Windows file-sharing semantics.");
        var updating = false;
        await using var server = new Server(_ => (200, updating ? valid.Replace("Valid", "New") : valid, null));
        var root = directory();
        var source = server.Url + "/header";
        var store = new DifficultyTableStore(null, root);
        var first = (await store.ImportAsync(source, new ProgressNotification()))!.Table;
        var before = File.ReadAllBytes(cache(store, source));
        updating = true;
        using (var lease = new FileStream(cache(store, source), FileMode.Open, FileAccess.Read, FileShare.None))
            Assert.ThrowsAsync(Is.InstanceOf<IOException>().Or.InstanceOf<UnauthorizedAccessException>(),
                () => store.ImportAsync(source, new ProgressNotification(), first));
        Assert.That(File.ReadAllBytes(cache(store, source)), Is.EqualTo(before));
        Assert.That(store.Tables.Single(), Is.SameAs(first));
        Assert.That(Directory.GetFiles(root, "*.tmp"), Is.Empty);
    }

    [Test]
    public async Task TestSuccessfulUpdateReplacesOneSnapshotAndRestores()
    {
        var updating = false;
        await using var server = new Server(_ => (200, updating ? valid.Replace("Valid", "New").Replace("Old", "New course") : valid, null));
        var root = directory();
        var source = server.Url + "/header";
        var store = new DifficultyTableStore(null, root);
        var first = (await store.ImportAsync(source, new ProgressNotification()))!.Table;
        var events = 0;
        store.TablesChanged += () => events++;
        updating = true;
        var updated = await store.ImportAsync(source, new ProgressNotification(), first);
        Assert.That(updated!.Table.Name, Is.EqualTo("New"));
        Assert.That(store.Tables.Single(), Is.SameAs(updated.Table));
        Assert.That(events, Is.EqualTo(1));
        Assert.That(Directory.GetFiles(root), Has.Length.EqualTo(1));
        Assert.That(restore(root, source).Tables.Single().Courses.Single().Name, Is.EqualTo("New course"));
    }

    [Test]
    public async Task TestNotificationCancellationInterruptsDownload()
    {
        await using var server = new Server(_ => (200, valid, null), "/slow");
        using var config = new BmsRulesetConfigManager(null, new BmsRuleset().RulesetInfo);
        using var settings = new DifficultyTableSettings(config);
        typeof(DifficultyTableSettings).GetField("difficultyTableStore", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(settings, new DifficultyTableStore(null, directory()));
        var work = settings.ImportTableAsync(server.Url + "/slow");
        await server.Held.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var progress = (ProgressNotification)typeof(DifficultyTableSettings).GetField("importNotification", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(settings)!;
        ((CancellationTokenSource)typeof(ProgressNotification).GetField("cancellationTokenSource", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(progress)!).Cancel();
        await work.WaitAsync(TimeSpan.FromSeconds(5));
        ((Scheduler)typeof(Drawable).GetProperty("Scheduler", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(settings)!).Update();
        Assert.That(progress.State, Is.EqualTo(ProgressNotificationState.Cancelled));
        Assert.That(progress.Text, Is.EqualTo(progress.CompletionText));
    }

    private sealed class Server : IAsyncDisposable
    {
        private readonly TcpListener listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource stop = new();
        private readonly Task loop;
        internal readonly ConcurrentQueue<string> Requests = new();
        internal readonly TaskCompletionSource Held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal string Url { get; }
        internal Server(Func<string, (int Status, string Body, string? Location)> handler, string? heldPath = null)
        {
            listener.Start();
            Url = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port;
            loop = Task.Run(async () =>
            {
                try
                {
                    while (!stop.IsCancellationRequested)
                    {
                        using var client = await listener.AcceptTcpClientAsync(stop.Token);
                        using var stream = client.GetStream();
                        using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);
                        var first = await reader.ReadLineAsync(stop.Token);
                        if (first == null) continue;
                        while (!string.IsNullOrEmpty(await reader.ReadLineAsync(stop.Token))) { }
                        var path = first.Split(' ')[1];
                        Requests.Enqueue(path);
                        if (path == heldPath) { Held.TrySetResult(); await Task.Delay(Timeout.Infinite, stop.Token); }
                        var response = handler(path);
                        var bytes = Encoding.UTF8.GetBytes(response.Body);
                        var redirect = response.Location == null ? "" : "Location: " + response.Location + "\r\n";
                        var header = Encoding.ASCII.GetBytes($"HTTP/1.1 {response.Status} Fixture\r\n{redirect}Content-Type: application/json; charset=utf-8\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n");
                        await stream.WriteAsync(header, stop.Token);
                        await stream.WriteAsync(bytes, stop.Token);
                    }
                }
                catch (OperationCanceledException) { }
                catch (SocketException) when (stop.IsCancellationRequested) { }
            });
        }
        public async ValueTask DisposeAsync() { stop.Cancel(); listener.Stop(); await loop; stop.Dispose(); }
    }
}
