using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Game.IO;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps.Objects;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Mods.LongNoteMode;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Replays;
using osu.Game.Rulesets.BmsRuleset.IO.Input;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Gameplay.Judgement;

[TestFixture]
public class BmsExecutedJavaParserTest
{
    [Test]
    public void OriginalLnModeAndEndpointCountsMatchPlayerSelection()
    {
        var rows = BmsExecutedJavaJudgementTest.ReadOracle("parser");
        Assert.That(rows, Has.Length.EqualTo(15));
        foreach (var row in rows)
        {
            int number(int index) => int.Parse(row[index], CultureInfo.InvariantCulture);
            var header = number(0) < 0 ? "" : "#LNMODE " + row[0] + "\n";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(header + "#BPM 120\n#LNTYPE 1\n#00151:0102\n"));
            using var reader = new LineBufferedReader(stream);
            var decoded = (BmsDecodedBeatmap)new BmsBeatmapDecoder().Decode(reader);
            Assert.That((int)decoded.LockedLongNoteMode, Is.EqualTo(number(2)));
            var beatmap = (BmsBeatmap)new BmsBeatmapConverter(decoded, new BmsRuleset()).Convert();
            if (number(1) == 0) new BmsModLongNote().ApplyToBeatmap(beatmap);
            else if (number(1) == 1) new BmsModChargeNote().ApplyToBeatmap(beatmap);
            else new BmsModHellChargeNote().ApplyToBeatmap(beatmap);
            var note = (BmsLongNote)beatmap.HitObjects.Single();
            var replay = new osu.Game.Replays.Replay { HasReceivedAllFrames = true };
            replay.Frames.Add(new BmsReplayFrame(0));
            replay.Frames.Add(new BmsReplayFrame(note.StartTime, BmsAction.Key1));
            replay.Frames.Add(new BmsReplayFrame(note.EndTime));
            replay.Frames.Add(new BmsReplayFrame(note.EndTime + 201));
            Assert.That(BmsReplaySimulation.Run(beatmap, replay, new ScoreInfo()).Judgements.Count, Is.EqualTo(number(3)), string.Join(',', row));
        }
    }
}
