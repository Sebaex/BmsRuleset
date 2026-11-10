using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Mods;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Gameplay.Judgement;

public static class BmsJudgementProfileProvider
{
    private static readonly double[] rank_rates = [0.25, 0.50, 0.75, 1.00, 0.75];
    private static readonly ConcurrentDictionary<ProfileKey, BmsJudgementProfile> profiles = new();

    // Most chart updates reuse one layout/rate. Keep the shared profile identity while avoiding
    // concurrent-dictionary lookups on every presentation read. The active mod list is stable for
    // a play, so cache its four transformed tables as well instead of reapplying each mod per read.
    [ThreadStatic]
    private static (ProfileKey Key, BmsJudgementProfile? Profile) lastProfile;

    [ThreadStatic]
    private static WindowTableCache lastWindowTables;

    private static IReadOnlyList<IApplicableToJudgementWindow> activeWindowMods = [];

    [ThreadStatic]
    private static IReadOnlyList<IApplicableToJudgementWindow>? simulationWindowMods;

    internal static IDisposable UseWindowMods(IReadOnlyList<IApplicableToJudgementWindow> mods)
    {
        var previous = simulationWindowMods;
        simulationWindowMods = mods;
        return new WindowModScope(previous);
    }

    private sealed class WindowModScope(IReadOnlyList<IApplicableToJudgementWindow>? previous) : IDisposable
    {
        public void Dispose() => simulationWindowMods = previous;
    }

    internal static void SetActiveWindowMods(IReadOnlyList<IApplicableToJudgementWindow> mods)
        => activeWindowMods = mods;

    internal static void ClearActiveWindowMods(IReadOnlyList<IApplicableToJudgementWindow> mods)
    {
        if (ReferenceEquals(activeWindowMods, mods))
            activeWindowMods = [];
    }

    public static BmsJudgementWindowTable GetTable(BmsLayoutVariant layout, int column, int rank, bool tail)
        => getTable(layout, column, RateForRank(layout, rank), tail);

    public static BmsJudgementWindowTable GetTable(BmsLayoutVariant layout, int column, double judgementRate, bool tail)
        => getTable(layout, column, judgementRate, tail);

    public static double RateForRank(int rank) => rank_rates[Math.Clamp(rank, 0, 4)];

    public static double RateForRank(BmsLayoutVariant layout, int rank) => rateForLayoutRank(layout, Math.Clamp(rank, 0, 4));

    public static double RateForExRank(BmsLayoutVariant layout, double exRank)
        // BMSPlayerRule first converts DEFEXRANK to an integer percentage.
    {
        if (exRank <= 0)
            return RateForRank(layout, 2);

        var integerExRank = Math.Truncate(exRank);
        var rate = Math.Truncate(integerExRank * Math.Round(RateForRank(layout, 2) * 100) / 100) / 100;

        // Keep EXRANK-derived rates distinct from the discrete #RANK profile markers.
        if (integerExRank != 100 && Array.IndexOf(rank_rates, rate) >= 0)
            rate = Math.BitIncrement(rate);

        return rate;
    }

    private static BmsJudgementWindowTable getTable(BmsLayoutVariant layout, int column, double judgementRate, bool tail)
    {
        var key = new ProfileKey(layout, judgementRate);
        var mods = simulationWindowMods ?? activeWindowMods;
        if (!lastWindowTables.IsCurrent(key, mods))
        {
            if (lastProfile.Profile == null || lastProfile.Key != key)
                lastProfile = (key, profiles.GetOrAdd(key, static k => createProfile(k.Layout, k.Rate)));

            var profile = lastProfile.Profile;
            lastWindowTables = new WindowTableCache(
                key,
                mods,
                applyWindowMods(profile.Normal, mods),
                applyWindowMods(profile.Scratch, mods),
                applyWindowMods(profile.LongNoteTail, mods),
                applyWindowMods(profile.LongScratchTail, mods));
        }

        var scratch = BmsLayout.IsScratchColumn(column, layout);
        return tail
            ? scratch ? lastWindowTables.LongScratchTail : lastWindowTables.LongNoteTail
            : scratch
                ? lastWindowTables.Scratch
                : lastWindowTables.Normal;
    }

    private static BmsJudgementWindowTable applyWindowMods(BmsJudgementWindowTable table, IReadOnlyList<IApplicableToJudgementWindow> mods)
    {
        for (var i = 0; i < mods.Count; i++)
            table = mods[i].ApplyToJudgementWindow(table);

        return table;
    }

    private static double rateForLayoutRank(BmsLayoutVariant layout, int rank) => rank_rates[rank];

    private static BmsJudgementProfile createProfile(BmsLayoutVariant layout, double rate)
        => layout switch
        {
            BmsLayoutVariant.Bms5K or BmsLayoutVariant.Bms5K2P or BmsLayoutVariant.Bms5KDouble => lr2Profile(rate),
            // ReSharper disable once RedundantSwitchExpressionArms
            BmsLayoutVariant.Bme7K or BmsLayoutVariant.Bme7K2P or BmsLayoutVariant.Bme7KDouble => lr2Profile(rate),
            BmsLayoutVariant.Pms9K or BmsLayoutVariant.Pms9K2P or BmsLayoutVariant.Pms9KDouble => lr2Profile(rate),
            _ => lr2Profile(rate),
        };

    private static BmsJudgementProfile lr2Profile(double rate)
    {
        // LR2 maps Very Easy to the Normal rank percentage.
        var rank = Array.IndexOf(rank_rates, rate);
        var perfect = 18d;
        var great = 40d;
        var good = 100d;

        switch (rank)
        {
            case 0:
                (perfect, great, good) = (8, 24, 40);
                break;
            case 1:
                (perfect, great, good) = (15, 30, 60);
                break;
            case 3 or 4:
                (perfect, great, good) = (21, 60, 120);
                break;
            default:
                var judgeRank = (int)Math.Round(rate * 100);
                perfect = scaleLr2Window(21000, judgeRank);
                great = scaleLr2Window(60000, judgeRank);
                good = scaleLr2Window(120000, judgeRank);
                break;
        }

        var windows = new (double slow, double fast)[]
        {
            (-perfect, perfect),
            (-great, great),
            (-good, good),
            (-200, 200),
            (0, 1000),
        };
        var table = createTable(1, false, windows, fixedBadAndPoor: true);
        var tailRadius = scaleLr2Window(120000, (int)Math.Round(rate * 100));
        var tail = createTable(1, false,
            [(-tailRadius, tailRadius), (-tailRadius, tailRadius), (-tailRadius, tailRadius), (-200, 200)], fixedBadAndPoor: true);
        return new BmsJudgementProfile(table, table, tail, tail);
    }

    private readonly record struct ProfileKey(BmsLayoutVariant Layout, double Rate);

    private static double scaleLr2Window(long basis, int judgeRank)
    {
        long scaled;
        if (judgeRank >= 100)
            scaled = basis * judgeRank / 100;
        else
        {
            // Integer microseconds preserve Java's truncation before the BAD clamp.
            ReadOnlySpan<long> points = basis switch
            {
                21000 => [0, 8000, 15000, 18000, 21000],
                60000 => [0, 24000, 30000, 40000, 60000],
                120000 => [0, 40000, 60000, 100000, 120000],
                _ => throw new ArgumentOutOfRangeException(nameof(basis)),
            };
            var index = judgeRank / 25;
            scaled = points[index] + (judgeRank - index * 25) * (points[index + 1] - points[index]) / 25;
        }
        return Math.Min(scaled, 200000) / 1000d;
    }

    private readonly record struct WindowTableCache(
        ProfileKey Key,
        IReadOnlyList<IApplicableToJudgementWindow>? Mods,
        BmsJudgementWindowTable Normal,
        BmsJudgementWindowTable Scratch,
        BmsJudgementWindowTable LongNoteTail,
        BmsJudgementWindowTable LongScratchTail)
    {
        internal bool IsCurrent(ProfileKey key, IReadOnlyList<IApplicableToJudgementWindow> mods)
            => Key == key && ReferenceEquals(Mods, mods);
    }

    public static bool IsPms(BmsLayoutVariant layout)
        => layout is BmsLayoutVariant.Pms9K or BmsLayoutVariant.Pms9K2P or BmsLayoutVariant.Pms9KDouble;

    public static bool EmptyPoorBreaksCombo(BmsLayoutVariant layout)
        => false;


    private static BmsJudgementWindowTable pmsHead(
        double rate,
        (double slow, double fast) perfect,
        (double slow, double fast) great,
        (double slow, double fast) good,
        (double slow, double fast) bad,
        (double slow, double fast) miss)
        => createTable(rate, true, [perfect, great, good, bad, miss]);

    private static BmsJudgementWindowTable pmsTail(
        double rate,
        (double slow, double fast) perfect,
        (double slow, double fast) great,
        (double slow, double fast) good,
        (double slow, double fast) bad)
        => createTable(rate, true, [perfect, great, good, bad]);

    private static BmsJudgementWindowTable createTable(double rate, bool pms, (double slow, double fast)[] rows, bool fixedBadAndPoor = false)
    {
        HitResult[] results = [HitResult.Perfect, HitResult.Great, HitResult.Good, HitResult.Ok, HitResult.Miss];
        var windows = new BmsJudgementWindow[rows.Length];
        for (var i = 0; i < rows.Length; i++)
        {
            var fixedWindow = i == 4 || (pms && i is 0 or 3) || (fixedBadAndPoor && i is 3 or 4);
            // LR2's BAD and empty-POOR windows remain fixed when EXRANK scales PG/GR/GD.
            var slow = fixedWindow ? rows[i].slow : Math.Truncate(rows[i].slow * 1000 * rate) / 1000;
            var fast = fixedWindow ? rows[i].fast : Math.Truncate(rows[i].fast * 1000 * rate) / 1000;
            if (pms && i is 1 or 2)
            {
                slow = Math.Clamp(slow, rows[3].slow, rows[0].slow);
                fast = Math.Clamp(fast, rows[0].fast, rows[3].fast);
            }

            windows[i] = new BmsJudgementWindow(results[i], slow, fast);
        }

        // Keep nested windows even for the unusual 5K long-scratch GOOD row.
        for (var i = 0; i < 3; i++)
        {
            var slow = Math.Max(windows[i].SlowDTime, windows[3].SlowDTime);
            var fast = Math.Min(windows[i].FastDTime, windows[3].FastDTime);
            if (i > 0)
            {
                slow = Math.Min(slow, windows[i - 1].SlowDTime);
                fast = Math.Max(fast, windows[i - 1].FastDTime);
            }

            windows[i] = new BmsJudgementWindow(results[i], slow, fast);
        }

        return new BmsJudgementWindowTable(windows);
    }
}
