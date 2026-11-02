#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Framework.Threading;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets.BmsRuleset;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;
using osu.Game.Rulesets.BmsRuleset.Gameplay.DifficultyTable;
using osu.Game.Rulesets.BmsRuleset.Localisation;
using osu.Game.Rulesets.BmsRuleset.UI.Settings.Components;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.IO;

[TestFixture]
public partial class BmsTableImportSchedulingTest
{
    [TestCase(true)]
    [TestCase(false)]
    public void TestNotificationFinishesWithoutUpdatingSettings(bool valid)
    {
        using var host = new TestRunHeadlessGameHost("ImportScheduling-" + Guid.NewGuid());
        Exception? failure = null;
        host.Run(new WorkGame(async () =>
        {
            try
            {
                var root = Path.Combine(Path.GetTempPath(), nameof(BmsTableImportSchedulingTest), Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(root);
                var path = Path.Combine(root, "table.json");
                await File.WriteAllTextAsync(path, valid
                    ? """{"name":"Detached settings","charts":[{"level":1,"md5":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}]}""" : "{");
                using var config = new BmsRulesetConfigManager(null, new BmsRuleset().RulesetInfo);
                using var settings = new DifficultyTableSettings(config) { Alpha = 0 };
                var store = new DifficultyTableStore(null, Path.Combine(root, "cache"));
                typeof(DifficultyTableSettings).GetField("difficultyTableStore", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(settings, store);
                typeof(DifficultyTableSettings).GetProperty("host", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(settings, host);
                // A global notification must finish even when its Settings owner is no longer updated.
                await settings.ImportTableAsync(path);
                var notification = (ProgressNotification)typeof(DifficultyTableSettings).GetField("importNotification", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(settings)!;
                // The store can publish Cancelled before Settings queues its final title.
                // A fence on the same FIFO queue observes that final callback as well.
                var publication = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                host.UpdateThread.Scheduler.Add(() => publication.SetResult());
                await publication.Task.WaitAsync(TimeSpan.FromSeconds(1));
                var expectedText = valid ? BmsStrings.LoadedTable("Detached settings", 1) : BmsStrings.FailedToLoadTable(path);
                Assert.That(notification.State, Is.EqualTo(valid ? ProgressNotificationState.Completed : ProgressNotificationState.Cancelled));
                Assert.That(notification.Text, Is.EqualTo(notification.CompletionText));
                Assert.That(notification.Text, Is.EqualTo(expectedText));
                Assert.That(notification.CompletionText, Is.EqualTo(expectedText));
                Assert.That(store.Tables.Count, Is.EqualTo(valid ? 1 : 0));
            }
            catch (Exception e) { failure = e; }
        }));
        if (failure != null) throw failure;
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task TestFinalErrorAfterOwnerDisposal(bool disposeBetweenCallbacks)
    {
        var root = Path.Combine(Path.GetTempPath(), nameof(BmsTableImportSchedulingTest), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "table.json");
        await File.WriteAllTextAsync(path, "{");
        using var config = new BmsRulesetConfigManager(null, new BmsRuleset().RulesetInfo);
        using var settings = new DifficultyTableSettings(config) { Alpha = 0 };
        typeof(DifficultyTableSettings).GetField("difficultyTableStore", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(settings, new DifficultyTableStore(null, Path.Combine(root, "cache")));
        await settings.ImportTableAsync(path);
        var notification = (ProgressNotification)typeof(DifficultyTableSettings).GetField("importNotification", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(settings)!;
        var sawEarlyTerminal = false;
        foreach (var callback in takePendingCallbacks(settings))
        {
            callback.RunTask();
            if (!sawEarlyTerminal && notification.State == ProgressNotificationState.Cancelled)
            {
                sawEarlyTerminal = true;
                Assert.That(notification.CompletionText, Is.EqualTo(BmsStrings.FailedToParseTable(path)));
                Assert.That(notification.Text, Is.EqualTo(BmsStrings.ImportingDifficultyTable));
                if (disposeBetweenCallbacks) settings.Dispose();
            }
        }
        Assert.That(sawEarlyTerminal, Is.True);
        Assert.That(notification.State, Is.EqualTo(ProgressNotificationState.Cancelled));
        Assert.That(notification.Text, Is.EqualTo(BmsStrings.FailedToLoadTable(path)));
        Assert.That(notification.CompletionText, Is.EqualTo(BmsStrings.FailedToLoadTable(path)));
        Assert.That(config.Get<string>(BmsRulesetSetting.DifficultyTableHistory), Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task TestPendingFinalCallbackCannotChangeReplacement(bool firstValid)
    {
        var root = Path.Combine(Path.GetTempPath(), nameof(BmsTableImportSchedulingTest), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var firstPath = Path.Combine(root, "first.json");
        var secondPath = Path.Combine(root, "second.json");
        await File.WriteAllTextAsync(firstPath, firstValid ? """{"name":"First","charts":[{"level":1,"md5":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}]}""" : "{");
        await File.WriteAllTextAsync(secondPath, """{"name":"Replacement","charts":[{"level":1,"md5":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"}]}""");
        using var config = new BmsRulesetConfigManager(null, new BmsRuleset().RulesetInfo);
        using var settings = new DifficultyTableSettings(config) { Alpha = 0 };
        typeof(DifficultyTableSettings).GetField("difficultyTableStore", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(settings, new DifficultyTableStore(null, Path.Combine(root, "cache")));
        await settings.ImportTableAsync(firstPath);
        var notificationField = typeof(DifficultyTableSettings).GetField("importNotification", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var first = (ProgressNotification)notificationField.GetValue(settings)!;
        var oldCallbacks = takePendingCallbacks(settings);
        await settings.ImportTableAsync(secondPath);
        var second = (ProgressNotification)notificationField.GetValue(settings)!;
        foreach (var callback in oldCallbacks) callback.RunTask();
        Assert.That(notificationField.GetValue(settings), Is.SameAs(second));
        Assert.That(second.State, Is.EqualTo(ProgressNotificationState.Active));
        Assert.That(second.Text, Is.EqualTo(BmsStrings.ImportingDifficultyTable));
        Assert.That(first.State, Is.EqualTo(ProgressNotificationState.Cancelled));
        Assert.That(first.Text, Is.EqualTo(BmsStrings.ImportWasCancelled));
        Assert.That(config.Get<string>(BmsRulesetSetting.DifficultyTableHistory), Is.Empty);
        foreach (var callback in takePendingCallbacks(settings)) callback.RunTask();
        Assert.That(second.State, Is.EqualTo(ProgressNotificationState.Completed));
        Assert.That(second.Text, Is.EqualTo(BmsStrings.LoadedTable("Replacement", 1)));
        Assert.That(second.CompletionText, Is.EqualTo(BmsStrings.LoadedTable("Replacement", 1)));
        Assert.That(config.Get<string>(BmsRulesetSetting.DifficultyTableHistory), Does.Contain(secondPath).And.Not.Contain(firstPath));
    }

    [Test]
    public async Task TestSuccessfulCallbackPreservesDisposalCancellation()
    {
        var root = Path.Combine(Path.GetTempPath(), nameof(BmsTableImportSchedulingTest), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "table.json");
        await File.WriteAllTextAsync(path, """{"name":"Finished","charts":[{"level":1,"md5":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}]}""");
        using var config = new BmsRulesetConfigManager(null, new BmsRuleset().RulesetInfo);
        using var settings = new DifficultyTableSettings(config) { Alpha = 0 };
        typeof(DifficultyTableSettings).GetField("difficultyTableStore", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(settings, new DifficultyTableStore(null, Path.Combine(root, "cache")));
        await settings.ImportTableAsync(path);
        var notification = (ProgressNotification)typeof(DifficultyTableSettings).GetField("importNotification", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(settings)!;
        var callbacks = takePendingCallbacks(settings);
        settings.Dispose();
        foreach (var callback in callbacks) callback.RunTask();
        Assert.That(notification.State, Is.EqualTo(ProgressNotificationState.Cancelled));
        Assert.That(notification.Text, Is.EqualTo(BmsStrings.ImportWasCancelled));
        Assert.That(notification.CompletionText, Is.EqualTo(BmsStrings.ImportWasCancelled));
        Assert.That(config.Get<string>(BmsRulesetSetting.DifficultyTableHistory), Is.Empty);
    }

    [Test]
    public async Task TestDisposalBeforeCompletionStillQueuesFinalCallback()
    {
        var root = Path.Combine(Path.GetTempPath(), nameof(BmsTableImportSchedulingTest), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "table.json");
        await File.WriteAllTextAsync(path, "{");
        using var config = new BmsRulesetConfigManager(null, new BmsRuleset().RulesetInfo);
        using var settings = new DifficultyTableSettings(config) { Alpha = 0 };
        typeof(DifficultyTableSettings).GetField("difficultyTableStore", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(settings, new DifficultyTableStore(null, Path.Combine(root, "cache")));
        var work = settings.ImportTableAsync(path);
        var notification = (ProgressNotification)typeof(DifficultyTableSettings).GetField("importNotification", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(settings)!;
        settings.Dispose();
        await work.WaitAsync(TimeSpan.FromSeconds(1));
        var callbacks = takePendingCallbacks(settings);
        Assert.That(callbacks, Has.Length.EqualTo(1), "Disposal before finally must not suppress the final publication.");
        foreach (var callback in callbacks) callback.RunTask();
        Assert.That(notification.State, Is.EqualTo(ProgressNotificationState.Cancelled));
        Assert.That(notification.Text, Is.EqualTo(BmsStrings.ImportWasCancelled));
        Assert.That(notification.CompletionText, Is.EqualTo(BmsStrings.ImportWasCancelled));
        Assert.That(config.Get<string>(BmsRulesetSetting.DifficultyTableHistory), Is.Empty);
    }

    private static ScheduledDelegate[] takePendingCallbacks(DifficultyTableSettings settings)
    {
        // Capture only after the backend finishes, so no worker can mutate this detached queue.
        // Replaying the original callbacks lets the test stop between store and Settings publication.
        var scheduler = (Scheduler)typeof(Drawable).GetProperty("Scheduler", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(settings)!;
        var queue = (Queue<ScheduledDelegate>)typeof(Scheduler).GetField("runQueue", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(scheduler)!;
        var callbacks = queue.ToArray();
        queue.Clear();
        return callbacks;
    }

    private partial class WorkGame(Func<Task> work) : osu.Framework.Game
    {
        protected override void LoadComplete()
        {
            base.LoadComplete();
            Scheduler.Add(async () =>
            {
                try { await work(); }
                finally { Exit(); }
            });
        }
    }
}
