using AnimusForge;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;

internal static class Program
{
    private static int _checks;
    private static readonly int[] Progress = { 448, 1018, 1462, 1500, 5136, 7801, 10000 };
    private static void Check(bool condition, string message)
    {
        _checks++;
        if (!condition) throw new Exception(message);
    }

    private static int Delta(int previous, int current) => TownPlunderConsequenceDelta
        .FromProgressCommit(new TownOperationProgressCommit(current, current - previous)).NotableRelationDelta;

    private static void Main()
    {
        var town = new Settlement { StringId = "town_EW1", IsTown = true };
        SiegeAiInterventionBehavior.Current = town;
        var heroes = Enumerable.Range(0, 11).Select(i => new Hero { StringId = "notable_" + i, Relation = 1 }).ToArray();
        var scope = SiegeAiInterventionBehavior.Begin();
        int previous = 0;
        var deltas = new List<int>();
        foreach (int current in Progress)
        {
            int delta = Delta(previous, current);
            deltas.Add(delta);
            foreach (Hero hero in heroes) SiegeAiInterventionBehavior.Apply(town, hero, delta);
            previous = current;
        }
        Check(deltas.SequenceEqual(new[] { -1, -2, -1, -1, -10, -8, -7 }), "replay exact seven deductions from game log");
        Check(ChangeRelationAction.Calls.Count == 0, "exit does not charge intermediate relation deltas");
        SiegeAiInterventionBehavior.Flush();
        Check(ChangeRelationAction.Calls.Count == 11, "77 updates become one native call for each of 11 targets");
        Check(ChangeRelationAction.Calls.All(c => c.Delta == -30 && c.ShowNotification), "full loss uses one native localized notification");
        Check(heroes.All(h => h.Relation == -29), "each notable goes directly from 1 to -29");
        Check(GcczDiagnosticLog.Lines.Count(l => l.Contains("requestedDelta=-30 before=1 after=-29 appliedDelta=-30")) == 11,
            "diagnostics record actual native before and after values");
        scope.Dispose();
        scope.Dispose();
        SiegeAiInterventionBehavior.Flush();
        Check(ChangeRelationAction.Calls.Count == 11, "flush and repeated disposal never replay a settled batch");

        Reset();
        Hero partial = new Hero { StringId = "already_charged", Relation = 1 };
        previous = 0;
        foreach (int current in Progress.Take(4))
        {
            SiegeAiInterventionBehavior.Apply(town, partial, Delta(previous, current));
            previous = current;
        }
        Check(partial.Relation == -4 && ChangeRelationAction.Calls.Count == 4, "ordinary scene deductions stay immediate");
        using (SiegeAiInterventionBehavior.Begin())
            foreach (int current in Progress.Skip(4))
            {
                SiegeAiInterventionBehavior.Apply(town, partial, Delta(previous, current));
                previous = current;
            }
        Check(partial.Relation == -29 && ChangeRelationAction.Calls.Last().Delta == -25,
            "exit only charges remaining -25 after already committed -5");

        Reset();
        using (SiegeAiInterventionBehavior.Begin()) { }
        Check(ChangeRelationAction.Calls.Count == 0, "empty exit has no relation side effects");
        Hero interrupted = new Hero { StringId = "interrupted" };
        try
        {
            using var interruptedScope = SiegeAiInterventionBehavior.Begin();
            SiegeAiInterventionBehavior.Apply(town, interrupted, Delta(0, 1018));
            throw new InvalidOperationException("simulate later finalization failure");
        }
        catch (InvalidOperationException) { }
        Check(interrupted.Relation == -3 && ChangeRelationAction.Calls.Count == 1, "exception settles already acquired partial progress once");
        SiegeAiInterventionBehavior.Apply(town, interrupted, -1);
        Check(interrupted.Relation == -4 && ChangeRelationAction.Calls.Count == 2, "exception releases batch scope");

        Reset();
        Hero bad = new Hero { StringId = "bad", ThrowAfterMutation = true };
        Hero good = new Hero { StringId = "good" };
        using (SiegeAiInterventionBehavior.Begin())
        {
            SiegeAiInterventionBehavior.Apply(town, bad, -30);
            SiegeAiInterventionBehavior.Apply(town, good, -30);
        }
        SiegeAiInterventionBehavior.Flush();
        Check(ChangeRelationAction.Calls.Count == 2 && bad.Relation == -30 && good.Relation == -30,
            "native post-mutation failure cannot replay target or stop remaining targets");
        Check(Logger.Lines.Count == 1 && Logger.Lines[0].Contains("Notable=bad"), "per-target failure remains diagnosable");

        Reset();
        Hero clamped = new Hero { StringId = "clamped", Relation = -90 };
        using (SiegeAiInterventionBehavior.Begin())
        {
            SiegeAiInterventionBehavior.Apply(town, clamped, -30);
            SiegeAiInterventionBehavior.Apply(town, clamped, 20, "mercy");
        }
        Check(clamped.Relation == -80 && ChangeRelationAction.Calls.Select(c => c.Delta).SequenceEqual(new[] { -30, 20 }),
            "flush before other relation effects preserves clamp and reward order");

        Reset();
        var otherTown = new Settlement { StringId = "other", IsTown = true };
        Hero first = new Hero { StringId = "first" };
        Hero second = new Hero { StringId = "second" };
        using (SiegeAiInterventionBehavior.Begin())
        {
            SiegeAiInterventionBehavior.Apply(town, first, -5);
            SiegeAiInterventionBehavior.Apply(otherTown, second, -2);
        }
        Check(ChangeRelationAction.Calls.Select(c => c.Hero).SequenceEqual(new[] { first, second }), "different settlement never joins original batch");

        Reset();
        using (SiegeAiInterventionBehavior.Begin())
        {
            SiegeAiInterventionBehavior.Apply(town, first, -2);
            IDisposable nested = SiegeAiInterventionBehavior.Begin();
            Check(nested == null, "nested finalization cannot replace outer owner");
            SiegeAiInterventionBehavior.Apply(town, first, -3);
        }
        Check(ChangeRelationAction.Calls.Count == 1 && ChangeRelationAction.Calls[0].Delta == -5, "nested exit settles once");
        foreach (Settlement excluded in new[] { null, new Settlement(), new Settlement { IsTown = true, IsCastle = true } })
        {
            SiegeAiInterventionBehavior.Current = excluded;
            Check(SiegeAiInterventionBehavior.Begin() == null, "missing, village and castle contexts are excluded");
        }
        SiegeAiInterventionBehavior.Current = town;

        Reset();
        Hero dying = new Hero { StringId = "dying" };
        using (SiegeAiInterventionBehavior.Begin())
        {
            SiegeAiInterventionBehavior.Apply(town, dying, -5);
            dying.IsAlive = false;
        }
        Check(ChangeRelationAction.Calls.Count == 0, "revalidate eligibility before native side effect");
        using (SiegeAiInterventionBehavior.Begin()) SiegeAiInterventionBehavior.Apply(town, second, -1);
        Check(ChangeRelationAction.Calls.Count == 1 && ChangeRelationAction.Calls[0].Hero == second, "new scene carries no old targets");

        VerifyWiring();
        Console.WriteLine("PASS plunder relation batch assertions=" + _checks + " (production batch source, simulated Bannerlord native calls)");
    }

    private static void Reset()
    {
        SiegeAiInterventionBehavior.Flush();
        ChangeRelationAction.Calls.Clear();
        GcczDiagnosticLog.Lines.Clear();
        Logger.Lines.Clear();
    }

    private static void VerifyWiring()
    {
        DirectoryInfo root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "AnimusForge.csproj"))) root = root.Parent;
        string folder = Path.Combine(root.FullName, "src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath");
        string source = File.ReadAllText(Path.Combine(folder, "SiegeAiInterventionBehavior.cs"));
        int exit = source.IndexOf("private static void FinalizeInterventionMissionEnd()");
        int batch = source.IndexOf("using var plunderRelations = BeginTownPlunderRelationBatch();", exit);
        Check(batch > exit && batch < source.IndexOf("EnsureMissionExitOutcomeBeforeFinalizing();", exit), "batch begins before automatic fallback plunder starts");
        int finalize = source.IndexOf("private static bool FinalizePendingAftermath(");
        int flush = source.IndexOf("FlushTownPlunderRelations();", finalize);
        Check(flush > finalize && flush < source.IndexOf("CommitPendingInterventionNotableDeaths(", finalize)
            && flush < source.IndexOf("RecordCompletedTownMemory(", finalize), "flush before notable deaths and final memory");
        string adapter = File.ReadAllText(Path.Combine(folder, "SiegeAiInterventionBehavior.TownSettlementEffectAdapter.cs"));
        Check(adapter.Contains("if (TryQueueTownPlunderRelation(settlement, notable, delta, reason))")
            && adapter.Contains("queued++;"), "native adapter diverts accepted batch entries");
        Check(adapter.Contains("queuedNotableRelation=") && source.Contains("QueuedNotableRelation="), "logs distinguish queued from applied relation deltas");
    }
}

namespace AnimusForge
{
    public partial class SiegeAiInterventionBehavior
    {
        internal static Settlement Current;
        private static Settlement ResolveCurrentSettlement() => Current;
        internal static IDisposable Begin() => BeginTownPlunderRelationBatch();
        internal static void Flush() => FlushTownPlunderRelations();
        internal static void Apply(Settlement settlement, Hero hero, int delta, string reason = TownPlunderConsequenceDelta.NotableRelationReason)
        {
            if (!TryQueueTownPlunderRelation(settlement, hero, delta, reason))
                ChangeRelationAction.ApplyPlayerRelation(hero, delta, true, true);
        }
    }
    internal static class GcczDiagnosticLog
    {
        internal static readonly List<string> Lines = new();
        internal static void Log(string category, string text) => Lines.Add(text);
    }
    internal static class Logger
    {
        internal static readonly List<string> Lines = new();
        internal static void Log(string category, string text) => Lines.Add(text);
    }
}

namespace TaleWorlds.CampaignSystem
{
    public sealed class Hero
    {
        public static Hero MainHero = new Hero { StringId = "player" };
        public string StringId;
        public bool IsAlive = true;
        public bool ThrowAfterMutation;
        public int Relation;
        public float GetRelation(Hero hero) => Relation;
    }
}

namespace TaleWorlds.CampaignSystem.Settlements
{
    public sealed class Settlement
    {
        public string StringId;
        public bool IsTown;
        public bool IsCastle;
    }
}

namespace TaleWorlds.CampaignSystem.Actions
{
    public static class ChangeRelationAction
    {
        public record Call(Hero Hero, int Delta, bool ShowNotification);
        public static readonly List<Call> Calls = new();
        public static void ApplyPlayerRelation(Hero hero, int delta, bool affectRelatives, bool showQuickNotification)
        {
            Calls.Add(new Call(hero, delta, showQuickNotification));
            hero.Relation = Math.Clamp(hero.Relation + delta, -100, 100);
            if (hero.ThrowAfterMutation) throw new InvalidOperationException("native event failed after mutation");
        }
    }
}
