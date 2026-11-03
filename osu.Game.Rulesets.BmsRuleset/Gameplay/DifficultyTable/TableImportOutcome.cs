using System;
using osu.Framework.Localisation;
using osu.Game.Rulesets.BmsRuleset.Localisation;

namespace osu.Game.Rulesets.BmsRuleset.Gameplay.DifficultyTable;

internal enum TableImportStatus
{
    Success,
    AlreadyImported,
    MissingMetadata,
    MissingFile,
    InvalidJson,
    NoEntries,
    InvalidDataUrl,
    MetadataDownloadFailed,
    DataDownloadFailed,
    TimedOut,
    Cancelled,
    ReplacementMissing,
    CacheFailed,
    UnexpectedError,
}

internal sealed record TableImportOutcome(TableImportStatus Status, DifficultyTable? Table = null, Exception? Error = null, int? HttpStatus = null)
{
    internal bool Succeeded => Status is TableImportStatus.Success or TableImportStatus.AlreadyImported;

    internal LocalisableString GetMessage(string source, bool updating) => Status switch
    {
        TableImportStatus.Success => updating ? BmsStrings.UpdatedTable(Table!.Name, Table.Entries.Count) : BmsStrings.LoadedTable(Table!.Name, Table.Entries.Count),
        TableImportStatus.AlreadyImported => BmsStrings.DifficultyTableAlreadyImported(source),
        TableImportStatus.MissingMetadata => BmsStrings.MissingBmstableMeta(source),
        TableImportStatus.MissingFile => BmsStrings.FileDoesNotExist,
        TableImportStatus.InvalidJson => BmsStrings.FailedToParseTable(source),
        TableImportStatus.NoEntries => BmsStrings.NoValidTableEntries(source),
        TableImportStatus.InvalidDataUrl => BmsStrings.InvalidTableDataUrl(source),
        TableImportStatus.MetadataDownloadFailed => HttpStatus is { } metadataStatus ? BmsStrings.TableMetadataDownloadFailed(metadataStatus) : BmsStrings.TableMetadataRequestFailed,
        TableImportStatus.DataDownloadFailed => HttpStatus is { } dataStatus ? BmsStrings.TableDataDownloadFailed(dataStatus) : BmsStrings.TableDataRequestFailed,
        TableImportStatus.TimedOut => BmsStrings.TableDownloadTimedOut,
        TableImportStatus.Cancelled => BmsStrings.ImportWasCancelled,
        TableImportStatus.ReplacementMissing => BmsStrings.TableReplacementFailed,
        TableImportStatus.CacheFailed => BmsStrings.TableCacheFailed,
        _ => updating ? BmsStrings.FailedToUpdateTable(source) : BmsStrings.FailedToLoadTable(source),
    };
}
