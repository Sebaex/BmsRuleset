#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;
using osu.Game.Rulesets.BmsRuleset.Gameplay.DifficultyTable;
using osu.Game.Rulesets.BmsRuleset.Localisation;
using osu.Game.Rulesets.BmsRuleset.UI.Settings.Components;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.IO;

[TestFixture, NonParallelizable]
public partial class BmsTablePublishedOutcomeTest
{
    private const BindingFlags privateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [TestCase(false)]
    [TestCase(true)]
    public void TestLoadedNotificationDoesNotCancelDeterminedFailure(bool disposeBeforePublication)
    {
        using var host = new TestRunHeadlessGameHost("PublishedOutcome-" + Guid.NewGuid());
        Exception? failure = null;
        host.Run(new PublishedGame(async game =>
        {
            try
            {
                var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                await using var server = new BmsTableImportOutcomeTest.Server(async _ =>
                {
                    held.TrySetResult();
                    await release.Task;
                    return (200, "{");
                });
                var root = Path.Combine(Path.GetTempPath(), nameof(BmsTablePublishedOutcomeTest), Guid.NewGuid().ToString("N"));
                using var config = new BmsRulesetConfigManager(null, new BmsRuleset().RulesetInfo);
                using var settings = new DifficultyTableSettings(config) { Alpha = 0 };
                typeof(DifficultyTableSettings).GetField("difficultyTableStore", privateInstance)!.SetValue(settings, new DifficultyTableStore(null, root));
                typeof(DifficultyTableSettings).GetProperty("notifications", privateInstance)!.SetValue(settings, game.Overlay);
                // Leave Settings detached: capture its final callback while the global overlay keeps updating.
                var source = server.Url + "/header";
                var work = settings.ImportTableAsync(source);
                var notification = (ProgressNotification)typeof(DifficultyTableSettings).GetField("importNotification", privateInstance)!.GetValue(settings)!;
                var operationToken = ((CancellationTokenSource)typeof(DifficultyTableSettings).GetField("importCancellation", privateInstance)!.GetValue(settings)!).Token;
                try
                {
                    await held.Task.WaitAsync(TimeSpan.FromSeconds(5));
                    await until(() => notification.IsLoaded && game.Overlay.AllNotifications.Any(n => ReferenceEquals(n, notification)));
                    Assert.That(notification.State, Is.EqualTo(ProgressNotificationState.Active));
                    release.TrySetResult();
                    await work.WaitAsync(TimeSpan.FromSeconds(5));
                    Assert.That(operationToken.IsCancellationRequested, Is.False);
                    var callbacks = BmsTableImportSchedulingTest.TakePendingCallbacks(settings);
                    if (disposeBeforePublication) settings.Dispose();
                    host.UpdateThread.Scheduler.Add(() =>
                    {
                        foreach (var callback in callbacks) callback.RunTask();
                    });
                    await fence(host);
                    await until(() => notification.CancellationToken.IsCancellationRequested);
                    Assert.That(operationToken.IsCancellationRequested, Is.False, "Terminal presentation must not feed back into the completed operation.");
                    Assert.That(notification.Text, Is.EqualTo(BmsStrings.FailedToParseTable(source)));
                    Assert.That(notification.CompletionText, Is.EqualTo(BmsStrings.FailedToParseTable(source)));
                    Assert.That(notification.State, Is.EqualTo(ProgressNotificationState.Cancelled));
                    Assert.That(config.Get<string>(BmsRulesetSetting.DifficultyTableHistory), Is.Empty);
                }
                finally { release.TrySetResult(); }
            }
            catch (Exception e) { failure = e; }
        }));
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static async Task fence(GameHost host)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.UpdateThread.Scheduler.Add(() => done.SetResult());
        await done.Task.WaitAsync(TimeSpan.FromSeconds(1));
    }

    private static async Task until(Func<bool> condition)
    {
        var timer = Stopwatch.StartNew();
        while (!condition() && timer.Elapsed < TimeSpan.FromSeconds(1)) await Task.Delay(10);
        Assert.That(condition(), Is.True, "The observed loaded notification did not reach its expected state within one second.");
    }

    private partial class PublishedGame(Func<PublishedGame, Task> work) : OsuGameBase
    {
        internal NotificationOverlay Overlay { get; } = new();
        protected override void LoadComplete()
        {
            base.LoadComplete();
            Add(Overlay);
            Overlay.Show();
            Scheduler.Add(async () =>
            {
                try { await until(() => Overlay.IsLoaded); await work(this); }
                finally { Exit(); }
            });
        }
    }
}
