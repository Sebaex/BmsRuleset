using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Platform;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.BmsRuleset.Database;
using osu.Game.Rulesets.BmsRuleset.Gameplay.DifficultyTable;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;
using osu.Game.Rulesets.BmsRuleset.Localisation;
using osuTK;
using DifficultyNameUpdater = osu.Game.Rulesets.BmsRuleset.Gameplay.DifficultyTable.DifficultyNameUpdater;
using DT = osu.Game.Rulesets.BmsRuleset.Gameplay.DifficultyTable.DifficultyTable;

namespace osu.Game.Rulesets.BmsRuleset.UI.Settings.Components;

internal partial class DifficultyTableSettings : FillFlowContainer
{
    private static readonly ImportOption[] preset_tables =
    [
        new("Satellite (sl)", "http://zris.work/bmstable/satellite/header.json"),
        new("Stella (st)", "http://zris.work/bmstable/stella/header.json"),
        new("発狂BMS難易度表 (★)", "http://zris.work/bmstable/insane/insane_header.json"),
        new("通常難易度表 (☆)", "http://zris.work/bmstable/normal/normal_header.json"),
        new("NEW GENERATION 発狂 (▼)", "http://zris.work/bmstable/insane2/insane_header.json"),
        new("第三期Overjoy (★★)", "http://zris.work/bmstable/overjoy/header.json"),
        new("Scramble (SB)", "http://zris.work/bmstable/scramble/header.json"),
        new("Luminous (ln)", "http://zris.work/bmstable/luminous/header.json"),
        new("BMS図書館 (T)", "http://zris.work/bmstable/turbow/header.json"),
    ];

    private const int max_history = 20;

    private readonly BmsRulesetConfigManager configManager;

    private DifficultyTableStore? difficultyTableStore;
    private CollectionSyncManager? collectionSyncManager;
    private DifficultyTableAutocomplete? autocomplete;
    private CancellationTokenSource? importCancellation;
    private readonly object importLock = new();
    private ProgressNotification? importNotification;
    // Queued UI callbacks may outlive cancellation and a replacement operation.
    private int importGeneration;

    [Resolved(CanBeNull = true)]
    private RealmAccess? realm { get; set; }

    [Resolved(CanBeNull = true)]
    private INotificationOverlay? notifications { get; set; }

    [Resolved(CanBeNull = true)]
    private GameHost? host { get; set; }

    public DifficultyTableSettings(BmsRulesetConfigManager configManager)
    {
        this.configManager = configManager;
        Direction = FillDirection.Vertical;
        RelativeSizeAxes = Axes.X;
        AutoSizeAxes = Axes.Y;
        Spacing = new Vector2(0, SettingsSection.ITEM_SPACING_V2);
    }

    [BackgroundDependencyLoader]
    private void load()
    {
        initialiseStore();

        autocomplete = new DifficultyTableAutocomplete
        {
            OnImport = importFromPathOrUrl,
            OnHistoryDelete = item => deleteFromHistory(item.Url),
            Padding = SettingsPanel.CONTENT_PADDING,
        };
        autocomplete.SetItems(buildPresetItems(), buildHistoryItems());

        Children =
        [
            new OsuSpriteText
            {
                Text = BmsStrings.BmsDifficultyTables,
                Font = OsuFont.GetFont(size: 18),
                Margin = new MarginPadding { Vertical = SettingsSubsection.VERTICAL_PADDING },
                Padding = SettingsPanel.CONTENT_PADDING,
            },
            new DifficultyTableListContainer(difficultyTableStore!, collectionSyncManager, deleteTable, updateTable)
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Padding = SettingsPanel.CONTENT_PADDING,
            },
            new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Padding = SettingsPanel.CONTENT_PADDING,
                Child = new SettingsNote
                {
                    RelativeSizeAxes = Axes.X,
                    Current =
                    {
                        Value = BmsBeatmapNotificationPatcher.IsInstalled
                            ? null
                            : new SettingsNote.Data(BmsStrings.DifficultyTableWarning, SettingsNote.Type.Warning),
                    },
                },
            },
            autocomplete,
        ];
    }

    protected override void Dispose(bool isDisposing)
    {
        ++importGeneration;
        cancelImport();

        base.Dispose(isDisposing);
    }

    private void initialiseStore()
    {
        if (BmsRulesetRuntime.DifficultyTableStore == null && host != null)
        {
            var cacheDir = Path.Combine(host.Storage.GetFullPath(string.Empty), "difficulty-tables");
            collectionSyncManager = new CollectionSyncManager(configManager);
            var store = new DifficultyTableStore(configManager, cacheDir, collectionSyncManager, realm);
            BmsRulesetRuntime.DifficultyTableStore = store;

            if (realm != null)
                store.DifficultyNameUpdater = new DifficultyNameUpdater(realm, store);

            store.LoadPersistedTables();
        }

        difficultyTableStore = BmsRulesetRuntime.DifficultyTableStore;
        collectionSyncManager = difficultyTableStore?.CollectionSyncManager;
    }

    private static string friendlyName(string url)
    {
        try
        {
            var name = Path.GetFileNameWithoutExtension(new Uri(url).LocalPath);
            return !string.IsNullOrEmpty(name) ? $"{name} ({url})" : url;
        }
        catch
        {
            return url;
        }
    }

    private static ImportOption parseHistoryEntry(string entry)
    {
        var parts = entry.Split('|', 3);

        if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[0]) && !string.IsNullOrEmpty(parts[1]))
        {
            var symbol = parts.Length >= 3 ? parts[2] : null;
            var display = !string.IsNullOrEmpty(symbol) ? $"{parts[0]} ({symbol})" : parts[0];
            return new ImportOption(display, parts[1]);
        }

        return new ImportOption(friendlyName(entry), entry);
    }

    private bool isPreset(string url) => preset_tables.Any(p => p.Url == url);

    private void addToHistory(string url, string name, string symbol)
    {
        if (isPreset(url)) return;

        var bindable = configManager.GetBindable<string>(BmsRulesetSetting.DifficultyTableHistory);
        var entries = bindable.Value
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var existingIndex = entries.FindIndex(e =>
        {
            var parts = e.Split('|', 3);
            return parts.Length > 1 && parts[1] == url;
        });

        if (existingIndex >= 0)
            entries.RemoveAt(existingIndex);

        entries.Insert(0, $"{name}|{url}|{symbol}");

        if (entries.Count > max_history)
            entries = entries.Take(max_history).ToList();

        bindable.Value = string.Join(";", entries);
    }

    private void deleteFromHistory(string url)
    {
        var bindable = configManager.GetBindable<string>(BmsRulesetSetting.DifficultyTableHistory);
        var entries = bindable.Value
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(e =>
            {
                var parts = e.Split('|', 3);
                var entryUrl = parts.Length > 1 ? parts[1] : parts[0];
                return entryUrl != url;
            })
            .ToList();

        bindable.Value = string.Join(";", entries);
        autocomplete?.SuppressAutoHide();
        autocomplete?.SetItems(buildPresetItems(), buildHistoryItems());
        Schedule(() => autocomplete?.RefreshFilter());
    }

    private static List<ImportOption> buildPresetItems() => preset_tables.ToList();

    private List<ImportOption> buildHistoryItems()
    {
        var history = configManager.Get<string>(BmsRulesetSetting.DifficultyTableHistory);
        return history
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(parseHistoryEntry)
            .ToList();
    }

    private async void importFromPathOrUrl(string pathOrUrl) => await ImportTableAsync(pathOrUrl);

    private void scheduleImportUpdate(Action action)
    {
        // Notifications outlive the Settings subtree, whose scheduler stops updating when hidden.
        if (host != null)
            host.UpdateThread.Scheduler.Add(action);
        else
            Schedule(action);
    }

    private void cancelImport()
    {
        lock (importLock)
        {
            importCancellation?.Cancel();
            importCancellation = null;
        }
        if (importNotification?.State == ProgressNotificationState.Active)
        {
            importNotification.CompletionText = BmsStrings.ImportWasCancelled;
            importNotification.Text = BmsStrings.ImportWasCancelled;
            importNotification.State = ProgressNotificationState.Cancelled;
        }
        importNotification = null;
    }

    internal async Task ImportTableAsync(string source, DT? tableToReplace = null)
    {
        if (IsDisposed || difficultyTableStore == null || string.IsNullOrWhiteSpace(source)) return;
        cancelImport();
        var generation = ++importGeneration;
        var operation = new CancellationTokenSource();
        var token = operation.Token;
        lock (importLock) importCancellation = operation;
        var notification = importNotification = new ProgressNotification
        {
            Text = tableToReplace == null ? BmsStrings.ImportingDifficultyTable : BmsStrings.UpdatingTable(tableToReplace.Name),
            State = ProgressNotificationState.Active,
        };
        var importScheduler = host?.UpdateThread.Scheduler ?? Scheduler;
        notifications?.Post(notification);
        using var notificationCancellation = notification.CancellationToken.Register(() =>
        {
            lock (importLock)
            {
                if (ReferenceEquals(importCancellation, operation)) operation.Cancel();
            }
        });
        var result = new TableImportOutcome(TableImportStatus.UnexpectedError);
        try
        {
            await Task.Delay(300, token).ConfigureAwait(false);
            result = await difficultyTableStore.ImportWithOutcomeAsync(source, notification, tableToReplace, token, action => scheduleImportUpdate(() =>
            {
                if (!IsDisposed && generation == importGeneration && !token.IsCancellationRequested) action();
            })).ConfigureAwait(false);
            // The store decides whether cancellation preceded confirmation. A cancelled
            // token after confirmation cannot turn a committed table into a failed import.
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            result = new(TableImportStatus.Cancelled);
        }
        catch (Exception e)
        {
            BmsLogger.Error(e, $"Failed to import or update difficulty table from: {source}");
        }
        finally
        {
            lock (importLock)
            {
                if (ReferenceEquals(importCancellation, operation)) importCancellation = null;
                operation.Dispose();
            }
            importScheduler.Add(() =>
            {
                var isCurrent = generation == importGeneration && ReferenceEquals(importNotification, notification);
                if (!IsDisposed && !isCurrent) return;
                // Disposal must not suppress the captured notification's error, while a late
                // successful callback must preserve cancellation by disposal or replacement.
                var success = result.Succeeded;
                var cancelledAfterSuccess = success && notification.State == ProgressNotificationState.Cancelled;
                var finalText = cancelledAfterSuccess ? BmsStrings.ImportWasCancelled : result.GetMessage(source, tableToReplace != null);
                notification.CompletionText = finalText;
                notification.Text = finalText;
                if (success && !cancelledAfterSuccess) notification.Progress = 1;
                notification.State = success && !cancelledAfterSuccess ? ProgressNotificationState.Completed : ProgressNotificationState.Cancelled;
                if (IsDisposed || !isCurrent) return;
                if (result.Status == TableImportStatus.Success && !cancelledAfterSuccess && result.Table != null && tableToReplace == null)
                {
                    addToHistory(source, result.Table.Name, result.Table.Symbol);
                    autocomplete?.SetItems(buildPresetItems(), buildHistoryItems());
                }
            });
        }
    }
    private async void deleteTable(DT table)
    {
        try
        {
            ProgressNotification? notification = null;

            Schedule(() =>
            {
                notification = new ProgressNotification
                {
                    Text = BmsStrings.RemovingTable(table.Name),
                    Progress = 0,
                    State = ProgressNotificationState.Active,
                };
                notifications?.Post(notification);
            });

            await Task.Run(() => difficultyTableStore?.RemoveTable(table, notification)).ConfigureAwait(false);

            Schedule(() =>
            {
                if (notification == null) return;

                notification.CompletionText = BmsStrings.RemovedTable(table.Name);
                notification.Progress = 1;
                notification.State = ProgressNotificationState.Completed;
            });
        }
        catch (Exception e)
        {
            BmsLogger.Error(e, $"Failed to delete difficulty table: {table.SourcePath}");
        }
    }

    private async void updateTable(DT table)
    {
        if (table.Source == TableSource.RemoteUrl && table.SourcePath != null)
            await ImportTableAsync(table.SourcePath, table);
    }
}
