using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps.Objects;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Judgement;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Replays;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring;
using osu.Game.Rulesets.BmsRuleset.IO.Input;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Gameplay.Judgement;

[TestFixture]
public class BmsColumnNoteIndexTest
{
    [Test]
    public void MovingAJudgedNoteKeepsItInHistoricalEmptyPoorSelection()
    {
        var beatmap = new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K, Rank = 2 };
        var notes = Enumerable.Range(1, 3).Select(i => new BmsNote { Beatmap = beatmap, Column = 1, StartTime = i * 1000 }).ToArray();
        var column = new BmsJudgementEngine(notes, beatmap.LayoutVariant, 1, new BmsRecordingJudgementOutput()).GetColumn(1);
        var moved = notes[2];

        column.TryPressRegisteredNote(moved, HitResult.Perfect, new BmsPressContext(3000, new BmsFrameContext(3000, 1)));

        moved.StartTime = 500;
        column.RefreshNoteTiming(moved);
        // LR2 empty POOR is early-only: keep the press before the moved, already judged head.
        var outcome = column.HandleNotePress(new BmsPressContext(400, new BmsFrameContext(3001, 1)));
        Assert.That(outcome, Is.EqualTo(PressOutcome.ForEmptyPoor(500, 1)));
        Assert.That(column.GetNoteCandidates(), Is.EqualTo(notes.Take(2)));
    }

    [Test]
    public void DurationFixedJudgeKeepsEarlierTieAndLeavesGoodNeighboursPending()
    {
        var beatmap = new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K, TotalColumns = 8, Rank = 3, Total = 12 };
        var notes = new[] { 900d, 1000, 1060, 1150 }.Select(time => new BmsNote
        {
            Beatmap = beatmap, Column = 1, StartTime = time,
        }).ToArray();
        beatmap.HitObjects.AddRange(notes);
        using var score = new BmsScoreProcessor();
        using var health = new BmsHealthProcessor();
        score.ApplyBeatmap(beatmap);
        health.ApplyBeatmap(beatmap);
        score.Combo.Value = 3;
        var output = new BmsRecordingJudgementOutput();
        var column = new BmsJudgementEngine(notes, beatmap.LayoutVariant, 1, output).GetColumn(1);
        var order = new List<double>();
        output.BeforeResultCheckpoint += (note, _) =>
        {
            order.Add(note.StartTime);
            var result = new JudgementResult(note, note.CreateJudgement()) { Type = output.GetMainResult(note).Type };
            score.ApplyResult(result);
            health.ApplyResult(result);
        };
        // Original Java f099-fixed/Duration: BAD900, GREAT1000; both neighbours stay pending.
        column.HandleNotePress(new BmsPressContext(1030, new BmsFrameContext(1030, 1)), BmsJudgementAlgorithm.Duration);
        Assert.That(order, Is.EqualTo(new[] { 900d, 1000 }));
        Assert.That(output.GetMainResult(notes[0]).Type, Is.EqualTo(HitResult.Ok));
        Assert.That(output.GetMainResult(notes[1]).Type, Is.EqualTo(HitResult.Great));
        Assert.That(column.GetNoteCandidates(), Is.EqualTo(notes.Skip(2)));
        Assert.That(output.MainResults, Is.EqualTo(2));
        Assert.That(score.Combo.Value, Is.EqualTo(1));
        Assert.That(BmsExScore.CreateProgression(score.JudgementEvents).Last(), Is.EqualTo(1));
        Assert.That(health.Health.Value, Is.EqualTo(.2 - .04 + .03).Within(1e-10));
        foreach (var time in new[] { 1060d, 1150 })
        {
            column.HandleExecutionRelease(time - 1, new BmsFrameContext(time - 1, 1));
            column.HandleNotePress(new BmsPressContext(time, new BmsFrameContext(time, 1)), BmsJudgementAlgorithm.Duration);
        }
        foreach (var note in notes) column.CheckRegisteredNotePassive(note, new BmsFrameContext(1401, 1));
        Assert.That(order, Is.EqualTo(new[] { 900d, 1000, 1060, 1150 }));
        Assert.That(notes.Select(note => output.GetMainResult(note).Type), Is.EqualTo(new[] { HitResult.Ok, HitResult.Great, HitResult.Perfect, HitResult.Perfect }));
        Assert.That(column.GetNoteCandidates(), Is.Empty);
        Assert.That(output.MainResults, Is.EqualTo(4));
        Assert.That(score.Combo.Value, Is.EqualTo(3));
        Assert.That(BmsExScore.CreateProgression(score.JudgementEvents), Is.EqualTo(new[] { 0, 0, 1, 3, 5 }));
        Assert.That(health.Health.Value, Is.EqualTo(.25).Within(1e-10));
        var replay = new osu.Game.Replays.Replay { HasReceivedAllFrames = true };
        replay.Frames.AddRange(new[] { new BmsReplayFrame(0) { Algorithm = BmsJudgementAlgorithm.Duration },
            new BmsReplayFrame(1030, BmsAction.Key1), new BmsReplayFrame(1031), new BmsReplayFrame(1060, BmsAction.Key1),
            new BmsReplayFrame(1061), new BmsReplayFrame(1150, BmsAction.Key1), new BmsReplayFrame(1151), new BmsReplayFrame(1401) });
        var reconstructed = BmsReplaySimulation.Run(beatmap, replay, new ScoreInfo());
        Assert.That(reconstructed.Judgements.Select(item => item.Result), Is.EqualTo(new[] { HitResult.Ok, HitResult.Great, HitResult.Perfect, HitResult.Perfect }));
        Assert.That(reconstructed.Judgements.SelectMany(item => item.TimingObservations).Select(item => item.TimeOffset), Is.EqualTo(new[] { 130d, 30, 0, 0 }));
        Assert.That(BmsExScore.CreateProgression(reconstructed.Judgements).Last(), Is.EqualTo(5));
        Assert.That(reconstructed.Gauges.Last().States.Single().Health, Is.EqualTo(.25).Within(1e-10));
    }

    [Test]
    public void DurationSearchKeepsChartOrderForStackedHeadsAndPrefersEarlierDistanceTies()
    {
        var beatmap = new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K, Rank = 3 };
        var notes = new[] { 1000d, 1000, 1060 }.Select(time => new BmsNote { Beatmap = beatmap, Column = 1, StartTime = time }).ToArray();
        var output = new BmsRecordingJudgementOutput();
        var column = new BmsJudgementEngine(notes, beatmap.LayoutVariant, 1, output).GetColumn(1);
        column.HandleNotePress(new BmsPressContext(1030, new BmsFrameContext(1030, 1)), BmsJudgementAlgorithm.Duration);
        Assert.That(output.GetMainResult(notes[0]).HasResult, Is.True);
        Assert.That(output.MainResults, Is.EqualTo(1));
    }

    [Test]
    public void StackedPmsHeadsShareTheEmptyPoorMistakeMarker()
    {
        var beatmap = new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Pms9K, Rank = 3 };
        var notes = Enumerable.Range(0, 2).Select(_ => new BmsNote { Beatmap = beatmap, Column = 1, StartTime = 1000 }).ToArray();
        var output = new BmsRecordingJudgementOutput();
        var column = new BmsJudgementEngine(notes, beatmap.LayoutVariant, 1, output).GetColumn(1);
        Assert.That(column.HandleNotePress(new BmsPressContext(600, new BmsFrameContext(600, 1))).Kind, Is.EqualTo(PressOutcomeKind.EmptyPoor));
        // LR2 ALWAYS replaces the old PMS ONE marker: neither stacked head is exempt from its later POOR.
        Assert.That(notes.All(n => !column.HasPmsMistake(n)), Is.True);
        Assert.That(output.MainResults, Is.Zero);
        foreach (var note in notes)
            column.CheckRegisteredNotePassive(note, new BmsFrameContext(1200.001, 1));
        Assert.That(output.MainResults, Is.EqualTo(2));
        Assert.That(notes.Select(n => output.GetMainResult(n).Type), Is.All.EqualTo(HitResult.Meh));
        Assert.That(notes.Select(n => output.GetMainResult(n).SuppressPenalty), Is.All.False);
    }

    [TestCase(63)]
    [TestCase(64)]
    [TestCase(65)]
    [TestCase(129)]
    public void TimingChangesAndRebuildsRestorePendingHeadsAcrossIndexWords(int count)
    {
        var beatmap = new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K, Rank = 3 };
        var notes = Enumerable.Range(0, count).Select(i => new BmsNote { Beatmap = beatmap, Column = 1, StartTime = 1000 + i * 10 }).ToArray();
        var output = new BmsRecordingJudgementOutput();
        var column = new BmsJudgementEngine(notes, beatmap.LayoutVariant, 1, output).GetColumn(1);
        foreach (var note in notes)
            column.TryPressRegisteredNote(note, HitResult.Perfect, new BmsPressContext(note.StartTime, new BmsFrameContext(note.StartTime, 1)));
        Assert.That(column.FirstPendingNote, Is.Null);

        output = new BmsRecordingJudgementOutput();
        column = new BmsJudgementEngine(notes, beatmap.LayoutVariant, 1, output).GetColumn(1);
        foreach (var note in notes.Take(count / 2 + 1))
            column.TryPressRegisteredNote(note, HitResult.Perfect, new BmsPressContext(note.StartTime, new BmsFrameContext(note.StartTime, 1)));
        Assert.That(column.FirstPendingNote, Is.SameAs(notes[count / 2 + 1]));

        notes[^1].StartTime = 900;
        column.RefreshNoteTiming(notes[^1]);
        column.HandleNotePress(new BmsPressContext(900, new BmsFrameContext(900, 1)), BmsJudgementAlgorithm.Duration);
        Assert.That(output.GetMainResult(notes[^1]).Type, Is.EqualTo(HitResult.Perfect));
        Assert.That(column.FirstPendingNote, Is.SameAs(notes[count / 2 + 1]));
    }
}
