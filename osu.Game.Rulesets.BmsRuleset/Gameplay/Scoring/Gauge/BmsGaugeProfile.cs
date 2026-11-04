using System.Collections.Generic;

namespace osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring.Gauge;

public sealed record BmsGaugeProfile(
    BmsGaugeAlgorithm Algorithm,
    double InitialHealth,
    double MaxHealth,
    double ClearThreshold,
    double PerfectGain,
    double GreatGain,
    double GoodGain,
    double BadDelta,
    double PoorDelta,
    double EmptyPoorDelta,
    BmsGaugeDisplayProfile Display,
    IReadOnlyList<BmsGaugeGutsRule> GutsRules
)
{
    public double DeathThreshold { get; init; }

    public double MinHealth => Algorithm == BmsGaugeAlgorithm.Total ? 0.02 : 0;

    internal double ClampHealth(double health)
    {
        var value = System.Math.Clamp(health, 0, MaxHealth);
        // Java compares float percentages; double subtraction must not kill an exact 2% boundary.
        return (float)(value * 100) < (float)(DeathThreshold * 100) ? 0 : value;
    }
}
