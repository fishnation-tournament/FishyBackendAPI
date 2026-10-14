namespace FishyAPI.Tools.MatchFlow;

public sealed record SequenceStep(int Index, int Cycle, StepAction Action, int Side);

public sealed class SequencePlan
{
    public List<SequenceStep> Steps { get; } = new();
    public int Picks { get; init; }
    public int Bans { get; set; }
    public bool TiebreakerFromPool { get; init; }
    public int WinsNeeded { get; init; }
}

/// <summary>
/// Builds the ban/pick order from the best-of number and pool size:
///   picks = BestOf - 1; the tiebreaker is the leftover map (odd pool) or a secret map (even pool).
///   Cycles of ban, ban, pick, pick; odd cycles B1 B2 P2 P1, even cycles B2 B1 P1 P2.
///   A ban pair is skipped when it would leave fewer maps than the remaining picks (+ the pool tiebreaker);
///   once picks are done, remaining maps are banned in pairs until only the tiebreaker is left.
/// </summary>
public static class MatchSequence
{
    public static (SequencePlan? plan, string? problem) Build(int bestOf, int poolSize)
    {
        if (bestOf < 1 || bestOf % 2 == 0)
            return (null, $"BestOf must be a positive odd number (got {bestOf}).");

        int picks = bestOf - 1;
        bool tiebreakerFromPool = poolSize % 2 == 1;
        int reserve = tiebreakerFromPool ? 1 : 0;
        if (poolSize < picks + reserve)
            return (null, $"A pool of {poolSize} maps is too small for Bo{bestOf} ({picks} picks{(reserve == 1 ? " + tiebreaker" : "")}).");

        SequencePlan plan = new() { Picks = picks, TiebreakerFromPool = tiebreakerFromPool, WinsNeeded = (bestOf + 1) / 2 };
        int mapsLeft = poolSize, picksLeft = picks, cycle = 1;

        while (picksLeft > 0 || mapsLeft > reserve)
        {
            bool oddCycle = cycle % 2 == 1;
            int first = oddCycle ? 1 : 2, second = oddCycle ? 2 : 1;
            bool acted = false;

            if (mapsLeft - 2 >= picksLeft + reserve)
            {
                Add(plan, cycle, StepAction.Ban, first);
                Add(plan, cycle, StepAction.Ban, second);
                mapsLeft -= 2;
                plan.Bans += 2;
                acted = true;
            }

            // Picks go to the side that banned second, then the other.
            foreach (int side in new[] { second, first })
            {
                if (picksLeft == 0) break;
                Add(plan, cycle, StepAction.Pick, side);
                mapsLeft--;
                picksLeft--;
                acted = true;
            }

            if (!acted)
                return (null, $"A pool of {poolSize} maps doesn't divide into Bo{bestOf}: {mapsLeft - reserve} map(s) would be left unused.");
            cycle++;
        }

        return (plan, null);
    }

    private static void Add(SequencePlan plan, int cycle, StepAction action, int side)
        => plan.Steps.Add(new SequenceStep(plan.Steps.Count, cycle, action, side));
}
