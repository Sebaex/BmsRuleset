using NUnit.Framework;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps.Objects;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring.Gauge;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Gameplay.Gauge;

[TestFixture]
public class BmsLr2CombinedGaugeTest
{
    // Independent Java transitions: max(note factor, TOTAL factor), guts before addition, then death/clamp.
    [TestCase(BmsGaugeType.Hard, 500, 200, 0.3199, 0.2479, 0.1279, 0.1289, true)]
    [TestCase(BmsGaugeType.Hard, 500, 200, 0.32, 0.20, 0.08, 0.081, true)]
    [TestCase(BmsGaugeType.Hard, 500, 200, 0.14, 0.068, 0, 0, false)]
    [TestCase(BmsGaugeType.Hard, 1000, 0, 0.32, 0.26, 0.20, 0.201, true)]
    [TestCase(BmsGaugeType.Hard, 1000, 200, 0.32, 0.2342857143, 0.1485714286, 0.1495714286, true)]
    [TestCase(BmsGaugeType.Normal, 500, 200, 0.03, 0.02, 0.02, 0.024, false)]
    public void DamageRecoveryAndFailureMatchJavaSequence(BmsGaugeType type, int count, double total, double seed,
        double afterBad, double afterPoor, double afterPerfect, bool passed)
    {
        var beatmap = new BmsBeatmap { LayoutVariant = BmsLayoutVariant.Bme7K, Total = total };
        for (var i = 0; i < count; i++)
            beatmap.HitObjects.Add(new BmsNote { Beatmap = beatmap, Column = 1, StartTime = 1000 * i });
        using var health = new BmsHealthProcessor();
        health.ApplyBeatmap(beatmap);
        health.SetGaugeTypes([type], replaceExisting: true, profileFamilyOverride: BmsGaugeProfileFamily.Lr2,
            initialStates: [new(type, seed, false)]);
        HitResult[] results = [HitResult.Ok, HitResult.Meh, HitResult.Perfect];
        double[] expected = [afterBad, afterPoor, afterPerfect];
        for (var i = 0; i < results.Length; i++)
        {
            var note = beatmap.HitObjects[i];
            health.ApplyResult(new JudgementResult(note, note.CreateJudgement()) { Type = results[i] });
            Assert.That(health.Health.Value, Is.EqualTo(expected[i]).Within(1e-7));
            Assert.That(health.CurrentGaugeStates[0].Failed, Is.EqualTo(expected[i] == 0));
        }
        Assert.That(health.HasPassedAtEnd(), Is.EqualTo(passed));
        Assert.That(health.HasFailed, Is.EqualTo(afterPerfect == 0));
    }
}
