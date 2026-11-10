using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps.Objects;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Replays;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring.Events;
using osu.Game.Rulesets.BmsRuleset.IO.Input;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.Scoring;
using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Visualize;

[HeadlessTest]
public partial class TestSceneBmsReplayRewindTiming : BmsPlayerTestScene
{
    private double inputOffset;
    private bool splitChords;

    protected override TestPlayer CreatePlayer(Ruleset ruleset)
        => CreateBmsPlayer(createReplay);

    private IList<ReplayFrame> createReplay(BmsBeatmap beatmap)
    {
        var frames = BmsTestReplays.CreateOffsetAutoPlayFrames(beatmap, inputOffset);
        if (!splitChords)
            return frames;

        var result = new List<ReplayFrame>();
        var pressed = new List<BmsAction>();
        // Live recording emits a frame per action, so chords can advance the drawable
        // lifecycle between two inputs with the same timestamp.
        foreach (var frame in frames.Cast<BmsReplayFrame>())
        {
            foreach (var action in pressed.Except(frame.Actions).ToArray())
            {
                pressed.Remove(action);
                result.Add(new BmsReplayFrame(frame.Time, pressed.ToArray()));
            }

            foreach (var action in frame.Actions.Except(pressed).ToArray())
            {
                pressed.Add(action);
                result.Add(new BmsReplayFrame(frame.Time, pressed.ToArray()));
            }
        }

        return result;
    }

    protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
    {
        var beatmap = new BmsBeatmap
        {
            LayoutVariant = BmsLayoutVariant.Bme7K,
            TotalColumns = 8,
            Rank = 2,
        };
        for (var i = 0; i < 90; i++)
        {
            beatmap.HitObjects.Add(new BmsNote { StartTime = 1000 + i * 100, Column = i % 7 + 1 });
            beatmap.HitObjects.Add(new BmsNote { StartTime = 1000 + i * 100, Column = (i + 3) % 7 + 1 });
        }

        beatmap.HitObjects.Add(new BmsNote { StartTime = 20000, Column = 1 });
        BmsTestBeatmaps.SetupBeatmapInfo(beatmap, ruleset);
        return beatmap;
    }

    [Test]
    public void TestShortNoteTimingAfterRepeatedRewinds([Values(-150, -25, 0, 25, 150)] double offset, [Values] bool split)
    {
        BmsTimingObservation[] original = [];
        AddStep("load dense replay", () =>
        {
            inputOffset = offset;
            splitChords = split;
            LoadPlayer();
        });
        AddUntilStep("player loaded", () => Player.IsLoaded && Player.LoadedBeatmapSuccessfully && Player.Alpha == 1);
        seek(11000);
        AddStep("capture original timings", () => original = observations());
        AddAssert("all chords judged at recorded time", () =>
        {
            if (offset != 150)
                return original.Length == 180 && original.All(o => Math.Abs(o.TimeOffset - offset) < 0.001);

            var expected = expectedLateBadTimings((BmsBeatmap)Player.GameplayState.Beatmap);
            var events = ((BmsScoreProcessor)Player.ScoreProcessor).JudgementEvents;
            foreach (var column in Enumerable.Range(1, 7))
                Assert.That(events.Where(e => e.Source.Column == column).SelectMany(e => e.TimingObservations),
                    Is.EqualTo(expected.Where(item => item.Column == column).Select(item => item.Observation)), $"column {column}");
            Assert.That(original.Select(o => o.ActualTime), Is.Ordered);
            Assert.That(events.Count(e => e.Source.IsScoring), Is.EqualTo(180));
            Assert.That(events.Where(e => e.Source.IsScoring).All(e => e.Result == HitResult.Ok), Is.True);
            Assert.That(events.Any(e => e.Source.IsScoring && e.Source.StartTime == 20000), Is.False);
            Assert.That(Player.ScoreProcessor.Combo.Value, Is.Zero);
            Assert.That(Player.HealthProcessor.Health.Value, Is.EqualTo(.02).Within(1e-10));
            return true;
        });

        double[] targets = [500, 1000 + offset, 1450, 4000 + offset - 1, 4000 + offset, 4000 + offset + 1, 7555];
        foreach (var target in targets)
        {
            seek(target);
            AddAssert($"timings restored at {target}", () => observations(),
                () => Is.EqualTo(original.Where(o => o.ActualTime <= target).ToArray()));
            seek(11000);
            AddAssert($"same timings after replay from {target}", () => observations(), () => Is.EqualTo(original));
        }
    }

    private static List<(int Column, BmsTimingObservation Observation)> expectedLateBadTimings(BmsBeatmap beatmap)
    {
        var notes = beatmap.HitObjects.OfType<BmsNote>().ToArray();
        var consumed = new HashSet<BmsNote>();
        var expected = new List<(int, BmsTimingObservation)>();
        foreach (var press in notes.Where(note => note.StartTime < 11000).OrderBy(note => note.StartTime).ThenBy(note => note.Column))
        {
            var time = press.StartTime + 150;
            var lane = notes.Where(note => note.Column == press.Column).OrderBy(note => note.StartTime).ToArray();
            var bad = lane.Where(note => !consumed.Contains(note) && Math.Abs(time - note.StartTime) <= 200).ToArray();
            if (bad.Length > 0)
            {
                // C02's homogeneous collector applies future BAD extras before the older BAD primary.
                foreach (var note in bad.Skip(1).Append(bad[0]))
                {
                    Assert.That(Math.Abs(time - note.StartTime), Is.GreaterThan(100));
                    consumed.Add(note);
                    expected.Add((note.Column, new(BmsTimingObservationKind.Note, note.StartTime, time, 1, HitResult.Ok)));
                }
            }
            else
            {
                var next = lane.FirstOrDefault(note => note.StartTime > time && note.StartTime - time < 1000);
                if (next != null)
                    expected.Add((next.Column, new(BmsTimingObservationKind.Note, next.StartTime, time, 1, HitResult.Miss)));
            }
        }
        Assert.That(consumed, Has.Count.EqualTo(180));
        return expected;
    }

    private BmsTimingObservation[] observations()
        => ((BmsScoreProcessor)Player.ScoreProcessor).JudgementEvents.SelectMany(e => e.TimingObservations).ToArray();

    private void seek(double time)
    {
        AddStep($"seek {time}", () =>
        {
            Player.GameplayClockContainer.Stop();
            Player.GameplayClockContainer.Seek(time);
        });
        AddUntilStep("simulation caught up", () => Math.Abs(Player.DrawableRuleset.FrameStableClock.CurrentTime - time) < 0.001);
    }
}
