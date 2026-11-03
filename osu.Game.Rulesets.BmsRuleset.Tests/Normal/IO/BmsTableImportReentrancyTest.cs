#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Threading;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;
using osu.Game.Rulesets.BmsRuleset.Gameplay.DifficultyTable;
using osu.Game.Rulesets.BmsRuleset.Localisation;
using osu.Game.Rulesets.BmsRuleset.UI.Settings.Components;
using Table = osu.Game.Rulesets.BmsRuleset.Gameplay.DifficultyTable.DifficultyTable;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.IO;

[TestFixture]
public class BmsTableImportReentrancyTest
{
    private const string hash_a = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string hash_b = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string hash_c = "cccccccccccccccccccccccccccccccc";
    private const string remote_json = """{"name":"B","charts":[{"md5":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb","level":2}]}""";
    private const BindingFlags privateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [TestCase("add")]
    [TestCase("remove")]
    [TestCase("replace")]
    [TestCase("add-throw")]
    public async Task TestConfigurationObserverPreservesReentrantMutation(string mutation)
    {
        var root = directory();
        var cacheRoot = Path.Combine(root, "cache");
        var aPath = await localFile(root, "A", hash_a);
        var cPath = await localFile(root, "C", hash_c);
        using var config = configuration();
        var store = new DifficultyTableStore(config, cacheRoot);
        var a = (await store.ImportWithOutcomeAsync(aPath, new ProgressNotification())).Table!;
        var c = new Table { Name = "C", Source = TableSource.LocalFile, SourcePath = cPath, Entries = [new TableEntry { Md5Hash = hash_c, Level = "1" }] };
        var requests = 0;
        await using var server = new BmsTableImportOutcomeTest.Server(_ =>
        {
            Interlocked.Increment(ref requests);
            return Task.FromResult((200, remote_json));
        });
        var source = server.Url + "/header";
        var sources = config.GetBindable<string>(BmsRulesetSetting.DifficultyTableSources);
        var publications = new List<string>();
        string[]? callbackTables = null;
        var callbackIndexedB = false;
        var callbackCachedB = false;
        var entered = false;
        sources.BindValueChanged(e =>
        {
            publications.Add(e.NewValue);
            if (entered) return;
            entered = true;
            // Capture observations instead of throwing assertions into production's observer catch.
            callbackTables = store.Tables.Select(t => t.Name).ToArray();
            callbackIndexedB = store.GetMarkers(hash_b).Count == 1;
            callbackCachedB = File.Exists(cache(cacheRoot, source));
            switch (mutation)
            {
                case "add":
                case "add-throw":
                    store.AddTable(c);
                    break;
                case "remove":
                    store.RemoveTable(a);
                    break;
                case "replace":
                    store.ReplaceTable(a, c);
                    break;
            }
            if (mutation == "add-throw") throw new InvalidOperationException("Fixture observer failed after its legitimate mutation.");
        });
        var outcome = await store.ImportWithOutcomeAsync(source, new ProgressNotification());
        GC.KeepAlive(sources);
        Assert.That(entered, Is.True);
        Assert.That(callbackTables, Is.EqualTo(new[] { "A", "B" }));
        Assert.That(callbackIndexedB, Is.True);
        Assert.That(callbackCachedB, Is.True);
        Assert.That(outcome.Status, Is.EqualTo(TableImportStatus.Success));
        Assert.That(outcome.Error, Is.Null);
        var expectedNames = mutation switch { "remove" => new[] { "B" }, "replace" => ["C", "B"], _ => ["A", "B", "C"] };
        var expectedSources = mutation switch { "remove" => new[] { source }, "replace" => [cPath, source], _ => [aPath, source, cPath] };
        assertState(store, config, expectedNames, expectedSources);
        Assert.That(publications[0], Is.EqualTo(aPath + ";" + source));
        Assert.That(config.Get<string>(BmsRulesetSetting.DifficultyTableSources), Is.EqualTo(publications[^1]));
        Assert.That(BmsTableJsonParser.Parse(await File.ReadAllTextAsync(cache(cacheRoot, source)))!.Header!.Name, Is.EqualTo("B"));
        Assert.That(Directory.GetFiles(cacheRoot, "*.tmp"), Is.Empty);
        var restored = new DifficultyTableStore(config, cacheRoot);
        restored.LoadPersistedTables();
        assertState(restored, config, expectedNames, expectedSources);
        Assert.That(requests, Is.EqualTo(1), "Restoring all three outcomes must use files/cache without another HTTP request.");
    }

    [Test]
    public async Task TestRenameFailurePublishesNoUnconfirmedSources()
    {
        if (!OperatingSystem.IsWindows()) Assert.Ignore("Windows file-sharing semantics.");
        var root = directory();
        var cacheRoot = Path.Combine(root, "cache");
        var aPath = await localFile(root, "A", hash_a);
        using var config = configuration();
        var store = new DifficultyTableStore(config, cacheRoot);
        var a = (await store.ImportWithOutcomeAsync(aPath, new ProgressNotification())).Table!;
        await using var server = new BmsTableImportOutcomeTest.Server(_ => Task.FromResult((200, remote_json)));
        var source = server.Url + "/header";
        var target = cache(cacheRoot, source);
        var previous = remote_json.Replace("\"B\"", "\"Previous snapshot\"");
        await File.WriteAllTextAsync(target, previous);
        var sources = config.GetBindable<string>(BmsRulesetSetting.DifficultyTableSources);
        var publications = new List<string>();
        sources.BindValueChanged(e => publications.Add(e.NewValue));
        TableImportOutcome outcome;
        using (var lease = new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.None))
            outcome = await store.ImportWithOutcomeAsync(source, new ProgressNotification());
        GC.KeepAlive(sources);
        Assert.That(outcome.Status, Is.EqualTo(TableImportStatus.CacheFailed));
        Assert.That(outcome.Error, Is.InstanceOf<IOException>().Or.InstanceOf<UnauthorizedAccessException>());
        Assert.That(publications, Is.Empty);
        Assert.That(store.Tables.Single(), Is.SameAs(a));
        assertState(store, config, ["A"], [aPath]);
        Assert.That(await File.ReadAllTextAsync(target), Is.EqualTo(previous));
        Assert.That(Directory.GetFiles(cacheRoot, "*.tmp"), Is.Empty);
        var restored = new DifficultyTableStore(config, cacheRoot);
        restored.LoadPersistedTables();
        assertState(restored, config, ["A"], [aPath]);
    }

    [Test]
    public async Task TestCancellationFromConfigurationObserverIsAfterCommit()
    {
        var root = directory();
        using var config = configuration();
        using var cancellation = new CancellationTokenSource();
        var store = new DifficultyTableStore(config, root);
        var sources = config.GetBindable<string>(BmsRulesetSetting.DifficultyTableSources);
        var sawConfirmedTable = false;
        sources.BindValueChanged(_ =>
        {
            sawConfirmedTable = store.Tables.Count == 1 && store.GetMarkers(hash_b).Count == 1;
            cancellation.Cancel();
        });
        await using var server = new BmsTableImportOutcomeTest.Server(_ => Task.FromResult((200, remote_json)));
        var outcome = await store.ImportWithOutcomeAsync(server.Url, new ProgressNotification(), cancellationToken: cancellation.Token);
        GC.KeepAlive(sources);
        Assert.That(cancellation.IsCancellationRequested, Is.True);
        Assert.That(sawConfirmedTable, Is.True);
        Assert.That(outcome.Status, Is.EqualTo(TableImportStatus.Success));
        Assert.That(outcome.Error, Is.Null);
        assertState(store, config, ["B"], [server.Url]);
        var restored = new DifficultyTableStore(config, root);
        restored.LoadPersistedTables();
        assertState(restored, config, ["B"], [server.Url]);
    }

    [TestCase("cancel")]
    [TestCase("unexpected")]
    [TestCase("io")]
    public async Task TestPostCommitObserverCannotPublishNotAppliedMessage(string observer)
    {
        var root = directory();
        using var config = configuration();
        var store = new DifficultyTableStore(config, root);
        using var settings = new DifficultyTableSettings(config);
        typeof(DifficultyTableSettings).GetField("difficultyTableStore", privateInstance)!.SetValue(settings, store);
        var sources = config.GetBindable<string>(BmsRulesetSetting.DifficultyTableSources);
        var observed = false;
        var sawConfirmedCache = false;
        var changes = 0;
        store.TablesChanged += () => changes++;
        await using var server = new BmsTableImportOutcomeTest.Server(_ => Task.FromResult((200, remote_json)));
        sources.BindValueChanged(_ =>
        {
            observed = true;
            sawConfirmedCache = File.Exists(cache(root, server.Url)) && store.Tables.Count == 1 && store.GetMarkers(hash_b).Count == 1;
            if (observer == "cancel")
                ((CancellationTokenSource)typeof(DifficultyTableSettings).GetField("importCancellation", privateInstance)!.GetValue(settings)!).Cancel();
            else if (observer == "io") throw new IOException("Fixture publication observer I/O failure after commit.");
            else throw new InvalidOperationException("Fixture publication observer failed after commit.");
        });
        await settings.ImportTableAsync(server.Url);
        GC.KeepAlive(sources);
        ((Scheduler)typeof(Drawable).GetProperty("Scheduler", privateInstance)!.GetValue(settings)!).Update();
        var notification = (ProgressNotification)typeof(DifficultyTableSettings).GetField("importNotification", privateInstance)!.GetValue(settings)!;
        Assert.That(observed, Is.True);
        Assert.That(sawConfirmedCache, Is.True);
        Assert.That(changes, Is.EqualTo(1), "A failed source observer must not suppress the remaining table notifications.");
        Assert.That(notification.State, Is.EqualTo(ProgressNotificationState.Completed));
        Assert.That(notification.Text, Is.EqualTo(BmsStrings.LoadedTable("B", 1)));
        Assert.That(notification.CompletionText, Is.EqualTo(BmsStrings.LoadedTable("B", 1)));
        assertState(store, config, ["B"], [server.Url]);
        var restored = new DifficultyTableStore(config, root);
        restored.LoadPersistedTables();
        assertState(restored, config, ["B"], [server.Url]);
    }

    [Test]
    public void TestPublicReplaceTablePreservesVoidContract()
    {
        var method = typeof(DifficultyTableStore).GetMethod(nameof(DifficultyTableStore.ReplaceTable), [typeof(Table), typeof(Table), typeof(ProgressNotification)]);
        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(void)));
        Assert.That(method.IsPublic, Is.True);
        var store = new DifficultyTableStore(null, directory());
        var a = new Table { Name = "A", Entries = [new TableEntry { Md5Hash = hash_a }] };
        var c = new Table { Name = "C", Entries = [new TableEntry { Md5Hash = hash_c }] };
        Assert.That(store.TryReplaceTable(a, c), Is.False);
        Assert.DoesNotThrow(() => store.ReplaceTable(a, c));
        Assert.That(store.Tables, Is.Empty);
        store.AddTable(a);
        Assert.That(store.TryReplaceTable(a, c), Is.True);
        Assert.That(store.Tables.Single(), Is.SameAs(c));
        Assert.That(store.GetMarkers(hash_a), Is.Empty);
        Assert.That(store.GetMarkers(hash_c).Single().table, Is.SameAs(c));
    }

    private static void assertState(DifficultyTableStore store, BmsRulesetConfigManager config, string[] names, string[] sources)
    {
        Assert.That(store.Tables.Select(t => t.Name), Is.EqualTo(names));
        Assert.That(config.Get<string>(BmsRulesetSetting.DifficultyTableSources), Is.EqualTo(string.Join(";", sources)));
        foreach (var (name, hash) in new[] { ("A", hash_a), ("B", hash_b), ("C", hash_c) })
        {
            var markers = store.GetMarkers(hash);
            if (names.Contains(name))
            {
                Assert.That(markers, Has.Count.EqualTo(1));
                Assert.That(markers.Single().table, Is.SameAs(store.Tables.Single(t => t.Name == name)));
            }
            else Assert.That(markers, Is.Empty, "Removed tables must not leave ghost index entries.");
        }
    }

    private static string cache(string root, string source) => Path.Combine(root, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant() + ".json");
    private static BmsRulesetConfigManager configuration() => new(null, new BmsRuleset().RulesetInfo);
    private static async Task<string> localFile(string root, string name, string hash)
    {
        var path = Path.Combine(root, name + ".json");
        await File.WriteAllTextAsync(path, "{\"name\":\"" + name + "\",\"charts\":[{\"md5\":\"" + hash + "\",\"level\":1}]}");
        return path;
    }
    private static string directory()
    {
        var root = Path.Combine(Path.GetTempPath(), nameof(BmsTableImportReentrancyTest), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
