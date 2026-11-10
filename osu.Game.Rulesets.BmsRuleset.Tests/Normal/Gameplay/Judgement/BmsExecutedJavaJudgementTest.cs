using System;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps.Objects;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Judgement;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Gameplay.Judgement;

[TestFixture]
public class BmsExecutedJavaJudgementTest
{
    internal static string[][] ReadOracle(string name)
    {
        using var stream = typeof(BmsExecutedJavaJudgementTest).Assembly.GetManifestResourceStream(
            "osu.Game.Rulesets.BmsRuleset.Tests.reference_fixtures.lr2_certification." + name + ".csv")
                           ?? throw new InvalidOperationException("Missing executed Java oracle: " + name);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Skip(1).Select(line => line.Split(',', StringSplitOptions.TrimEntries)).ToArray();
    }

    [Test]
    public void OriginalJavaWindowsMatchSupportedLayouts()
    {
        var rows = ReadOracle("windows");
        Assert.That(rows, Has.Length.EqualTo(126));
        HitResult[] results = [HitResult.Perfect, HitResult.Great, HitResult.Good, HitResult.Ok, HitResult.Miss];
        foreach (var row in rows)
        {
            foreach (var layout in Enum.GetValues<BmsLayoutVariant>())
            {
                var parameter = int.Parse(row[1], CultureInfo.InvariantCulture);
                var rate = row[0] == "rank" ? BmsJudgementProfileProvider.RateForRank(layout, parameter) : BmsJudgementProfileProvider.RateForExRank(layout, parameter);
                var table = BmsJudgementProfileProvider.GetTable(layout, row[2].Contains("SCRATCH") ? 0 : 1, rate, row[2].Contains("END"));
                var result = results[int.Parse(row[3], CultureInfo.InvariantCulture)];
                Assert.That(table.SlowWindowFor(result), Is.EqualTo(-long.Parse(row[4], CultureInfo.InvariantCulture) / 1000d), string.Join(',', row));
                Assert.That(table.FastWindowFor(result), Is.EqualTo(long.Parse(row[5], CultureInfo.InvariantCulture) / 1000d), string.Join(',', row));
            }
        }
    }

    [Test]
    public void OriginalComparisonsAndInlineLatePredicateMatchN3Selection()
    {
        var rows = ReadOracle("algorithms");
        Assert.That(rows, Has.Length.EqualTo(12));
        foreach (var row in rows)
        {
            var algorithm = Enum.Parse<BmsJudgementAlgorithm>(row[0]);
            var press = long.Parse(row[1], CultureInfo.InvariantCulture) / 1000d;
            var replace = bool.Parse(row[2]);
            var rejected = replace && bool.Parse(row[3]);
            BmsJudgementCandidate[] candidates = [new(1000, 1000, 1, 0.75, false), new(1050, 2050, 1, 0.75, true)];
            var selection = BmsJudgementSelector.SelectPress(BmsLayoutVariant.Bme7K, 1, candidates, press, algorithm);
            Assert.That(selection.Candidate?.StartTime, Is.EqualTo(rejected ? (double?)null : replace ? 1050 : 1000));
            Assert.That(selection.Result, Is.EqualTo(rejected ? HitResult.None : replace ? HitResult.Good : HitResult.Ok));
        }
    }

    [Test]
    public void OriginalCollectorMatchesRuntimeBadApplicationAndConsumption()
    {
        var rows = ReadOracle("collector");
        Assert.That(rows, Has.Length.EqualTo(9));
        foreach (var row in rows)
        {
            var beatmap = new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K, Rank = 2, LockedLongNoteMode = BmsLongNoteMode.ChargeNote };
            var times = row[4].Split('|').Select(v => double.Parse(v, CultureInfo.InvariantCulture)).ToArray();
            var notes = times.Select((time, i) =>
            {
                BmsHitObject note = row[3][i] == 'L' ? new BmsLongNote { Duration = 1000 } : new BmsNote();
                note.Beatmap = beatmap; note.Column = 1; note.StartTime = time;
                return note;
            }).ToArray();
            var output = new BmsRecordingJudgementOutput();
            var order = new System.Collections.Generic.List<int>();
            output.BeforeResultCheckpoint = (note, _) => order.Add(Array.IndexOf(notes, note));
            var column = new BmsJudgementEngine(notes, beatmap.LayoutVariant, 1, output).GetColumn(1);
            var press = double.Parse(row[1], CultureInfo.InvariantCulture);
            column.HandleNotePress(new BmsPressContext(press, new BmsFrameContext(press, 1)));
            var primary = int.Parse(row[2], CultureInfo.InvariantCulture);
            var extra = row[5] == "-" ? [] : row[5].Split('|').Select(v => int.Parse(v, CultureInfo.InvariantCulture)).ToArray();
            Assert.That(order, Is.EqualTo(extra.Append(primary)), row[0]);
            foreach (var index in extra)
                Assert.That(output.GetMainResult(notes[index]).Type, Is.EqualTo(HitResult.Ok), row[0]);
            for (var i = 0; i < notes.Length; i++)
                Assert.That(output.GetMainResult(notes[i]).HasResult, Is.EqualTo(i == primary || extra.Contains(i)), row[0]);
        }
    }
}
