#nullable enable
using System;
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
public class BmsTableImportLifecycleTest
{
    private const string valid = """{"name":"Valid","charts":[{"md5":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","level":"1"}]}""";
    private static string directory()
    {
        var path = Path.Combine(Path.GetTempPath(), nameof(BmsTableImportLifecycleTest), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    [TestCase(403)]
    [TestCase(404)]
    [TestCase(500)]
    public void TestDownloadHttpFailure(int status)
    {
        using var client = new HttpClient(new FakeHandler((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status))));
        Assert.ThrowsAsync<HttpRequestException>(() => BmsTableDownload.GetStringAsync(client, "https://fixture.test/", TimeSpan.FromSeconds(5), default));
    }

    [Test]
    public void TestTimeoutIsDistinctFromCallerCancellation()
    {
        using var client = new HttpClient(new FakeHandler(async (_, token) => { await Task.Delay(Timeout.Infinite, token); return new HttpResponseMessage(); }));
        Assert.ThrowsAsync<TimeoutException>(() => BmsTableDownload.GetStringAsync(client, "https://fixture.test/", TimeSpan.FromMilliseconds(30), default));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));
        Assert.CatchAsync<OperationCanceledException>(() => BmsTableDownload.GetStringAsync(client, "https://fixture.test/", TimeSpan.FromSeconds(5), cancellation.Token));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public async Task TestCancellationInterruptsEachHttpPhase(int stage)
    {
        var origin = "";
        await using var server = new Server(path => path switch
        {
            "/page" => (200, "<meta name=\"bmstable\" content=\"/header.json\">"),
            "/header.json" => (200, "{\"name\":\"Header\",\"data_url\":\"" + origin + "/data.json\"}"),
            _ => (200, "[{\"md5\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\",\"level\":\"1\"}]"),
        }, stage == 1 ? "/page" : stage == 2 ? "/header.json" : "/data.json");
        origin = server.Url;
        var store = new DifficultyTableStore(null, directory());
        using var cancellation = new CancellationTokenSource();
        var work = store.ImportAsync(origin + "/page", new ProgressNotification(), cancellationToken: cancellation.Token);
        await server.Held.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        Assert.CatchAsync<OperationCanceledException>(async () => await work);
        Assert.That(store.Tables, Is.Empty);
    }

    [TestCase(403, false)]
    [TestCase(404, false)]
    [TestCase(500, false)]
    [TestCase(200, false)]
    [TestCase(403, true)]
    public async Task TestUiHttpErrorTerminatesNotification(int status, bool update)
    {
        await using var server = new Server(_ => (status, status == 200 ? "{" : "denied"));
        using var config = new BmsRulesetConfigManager(null, new BmsRuleset().RulesetInfo);
        using var settings = new DifficultyTableSettings(config);
        var store = new DifficultyTableStore(null, directory());
        setStore(settings, store);
        var previous = new global::osu.Game.Rulesets.BmsRuleset.Gameplay.DifficultyTable.DifficultyTable { Name = "Previous", SourcePath = server.Url + "/header.json", Source = TableSource.RemoteUrl };
        if (update) store.RestoreTable(previous);
        await settings.ImportTableAsync(previous.SourcePath, update ? previous : null);
        pump(settings);
        Assert.That(notification(settings).State, Is.EqualTo(ProgressNotificationState.Cancelled));
        if (update) Assert.That(store.Tables.Single(), Is.SameAs(previous));
    }

    [Test]
    public async Task TestUiTimeoutTerminatesNotification()
    {
        await using var server = new Server(_ => (200, valid), "/slow");
        using var config = new BmsRulesetConfigManager(null, new BmsRuleset().RulesetInfo);
        using var settings = new DifficultyTableSettings(config);
        setStore(settings, new DifficultyTableStore(null, directory()) { DownloadTimeout = TimeSpan.FromMilliseconds(50) });
        await settings.ImportTableAsync(server.Url + "/slow");
        pump(settings);
        Assert.That(notification(settings).State, Is.EqualTo(ProgressNotificationState.Cancelled));
        Assert.That(notification(settings).CompletionText, Is.EqualTo(global::osu.Game.Rulesets.BmsRuleset.Localisation.BmsStrings.TableDownloadTimedOut));
    }

    [Test]
    public async Task TestSupersededImportCannotCompleteNewNotification()
    {
        await using var server = new Server(_ => (200, valid), "/slow");
        using var config = new BmsRulesetConfigManager(null, new BmsRuleset().RulesetInfo);
        using var settings = new DifficultyTableSettings(config);
        setStore(settings, new DifficultyTableStore(null, directory()));
        var first = settings.ImportTableAsync(server.Url + "/slow");
        await server.Held.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var oldNotification = notification(settings);
        var file = Path.Combine(directory(), "valid.json");
        await File.WriteAllTextAsync(file, valid);
        var second = settings.ImportTableAsync(file);
        var latest = notification(settings);
        Assert.That(oldNotification.State, Is.EqualTo(ProgressNotificationState.Cancelled));
        await Task.WhenAll(first, second);
        pump(settings);
        Assert.That(latest.State, Is.EqualTo(ProgressNotificationState.Completed));
        Assert.That(oldNotification.State, Is.EqualTo(ProgressNotificationState.Cancelled));
    }

    [Test]
    public async Task TestDisposalCancelsHttpAndTerminatesNotification()
    {
        await using var server = new Server(_ => (200, valid), "/slow");
        using var config = new BmsRulesetConfigManager(null, new BmsRuleset().RulesetInfo);
        var settings = new DifficultyTableSettings(config);
        setStore(settings, new DifficultyTableStore(null, directory()));
        var work = settings.ImportTableAsync(server.Url + "/slow");
        await server.Held.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var progress = notification(settings);
        settings.Dispose();
        await work.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(progress.State, Is.EqualTo(ProgressNotificationState.Cancelled));
    }

    [Test]
    public void TestPreCancelledImportDoesNotWriteCache()
    {
        var root = directory();
        var store = new DifficultyTableStore(null, root);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.CatchAsync<OperationCanceledException>(() => store.ImportAsync("http://127.0.0.1/", new ProgressNotification(), cancellationToken: cancellation.Token));
        Assert.That(Directory.GetFiles(root), Is.Empty);
    }

    private static void setStore(DifficultyTableSettings settings, DifficultyTableStore store)
        => typeof(DifficultyTableSettings).GetField("difficultyTableStore", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(settings, store);
    private static ProgressNotification notification(DifficultyTableSettings settings)
        => (ProgressNotification)typeof(DifficultyTableSettings).GetField("importNotification", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(settings)!;
    private static void pump(DifficultyTableSettings settings)
        => ((Scheduler)typeof(Drawable).GetProperty("Scheduler", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(settings)!).Update();

    private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => handler(request, cancellationToken);
    }

    private sealed class Server : IAsyncDisposable
    {
        private readonly TcpListener listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource stop = new();
        private readonly Task loop;
        internal readonly TaskCompletionSource Held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal string Url { get; }
        internal Server(Func<string, (int Status, string Body)> handler, string? heldPath = null)
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
                        if (path == heldPath) { Held.TrySetResult(); await Task.Delay(Timeout.Infinite, stop.Token); }
                        var response = handler(path);
                        var body = Encoding.UTF8.GetBytes(response.Body);
                        var header = Encoding.ASCII.GetBytes($"HTTP/1.1 {response.Status} Fixture\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
                        await stream.WriteAsync(header, stop.Token);
                        await stream.WriteAsync(body, stop.Token);
                    }
                }
                catch (OperationCanceledException) { }
                catch (SocketException) when (stop.IsCancellationRequested) { }
            });
        }
        public async ValueTask DisposeAsync() { stop.Cancel(); listener.Stop(); await loop; stop.Dispose(); }
    }
}
