using System;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring.Gauge;

public class BmsGaugeCalculator
{
    public BmsGaugeProfile Profile { get; }

    public double Total { get; }

    public int NoteCount { get; }

    private readonly double limitIncrementScale;

    public BmsGaugeCalculator(
        BmsGaugeProfile profile,
        double total,
        int noteCount,
        BmsGaugeProfileFamily profileFamily = BmsGaugeProfileFamily.SevenKeys)
    {
        Profile = profile;
        NoteCount = Math.Max(1, noteCount);

        Total = total > 0
            ? total
            : CalculateDefaultTotal(NoteCount, profileFamily);

        var perNoteMaxPercent = Math.Max(Math.Min(0.15, (2 * Total - 320) / NoteCount), 0);
        limitIncrementScale = perNoteMaxPercent / 0.15;
    }

    public static double CalculateDefaultTotal(int noteCount, BmsGaugeProfileFamily profileFamily = BmsGaugeProfileFamily.SevenKeys)
    {
        noteCount = Math.Max(1, noteCount);

        if (profileFamily == BmsGaugeProfileFamily.Lr2)
            return 160.0 + (noteCount + Math.Min(Math.Max(noteCount - 400, 0), 200)) * 0.16;

        return profileFamily == BmsGaugeProfileFamily.Keyboard
            ? Math.Max(300.0, 7.605 * (noteCount + 100) / (0.01 * noteCount + 6.5))
            : Math.Max(260.0, 7.605 * noteCount / (0.01 * noteCount + 6.5));
    }

    public double GetDeltaFor(HitResult result, double currentHealth)
    {
        var delta = result switch
        {
            HitResult.Perfect => gain(Profile.PerfectGain),
            HitResult.Great => gain(Profile.GreatGain),
            HitResult.Good => gain(Profile.GoodGain),
            HitResult.Ok => Profile.BadDelta,
            HitResult.Meh => Profile.PoorDelta,
            HitResult.Miss => Profile.EmptyPoorDelta,
            _ => 0,
        };

        if (delta < 0 && Profile.Algorithm == BmsGaugeAlgorithm.ModifyDamage)
            delta *= calculateDamageScale();

        return applyGuts(delta, currentHealth);
    }

    public double ApplyDelta(double currentHealth, double delta)
        => currentHealth <= 0 ? 0 : Profile.ClampHealth(Math.Clamp(currentHealth + delta, Profile.MinHealth, Profile.MaxHealth));

    private double gain(double value)
    {
        return Profile.Algorithm switch
        {
            BmsGaugeAlgorithm.Total => Total / 100.0 / NoteCount * value,
            BmsGaugeAlgorithm.LimitIncrement => value * limitIncrementScale,
            BmsGaugeAlgorithm.ModifyDamage => value,
            BmsGaugeAlgorithm.Fixed => value,
            _ => throw new ArgumentOutOfRangeException(nameof(Profile.Algorithm), Profile.Algorithm, null),
        };
    }

    private double calculateDamageScale()
    {
        var totalScale = (float)(10.0 / Math.Min(10.0, Math.Max(1.0, Math.Floor(Total / 16.0) - 5.0)));

        // LR2 uses distinct slopes at these chart-size boundaries.
        var noteScale = NoteCount switch
        {
            <= 20 => 10f,
            < 30 => 8f + 0.2f * (30 - NoteCount),
            < 60 => 5f + 0.2f * (60 - NoteCount) / 3f,
            < 125 => 4f + (125 - NoteCount) / 65f,
            < 250 => 3f + 0.008f * (250 - NoteCount),
            < 500 => 2f + 0.004f * (500 - NoteCount),
            < 1000 => 1f + 0.002f * (1000 - NoteCount),
            _ => 1f,
        };

        return Math.Max(totalScale, noteScale);
    }

    private double applyGuts(double delta, double currentHealth)
    {
        if (delta >= 0)
            return delta;

        foreach (var rule in Profile.GutsRules)
        {
            if (currentHealth < rule.HealthThreshold)
                return delta * rule.DamageMultiplier;
        }

        return delta;
    }
}
