using System;
using System.IO;
using System.Reflection;

internal static class ScenePeaceConflictOwnerReplay
{
    internal static void Run(Assembly assembly, string repo)
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        Type context = assembly.GetType("AnimusForge.Refactor.Modules.ScenePeaceConflictContext", true);
        MethodInfo decide = assembly.GetType("AnimusForge.Refactor.Modules.ScenePeaceConflictContextOwner", true)
            .GetMethod("CanInitialize", All);
        void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("Taunt context owner: " + label);
        }
        bool Allows(string location = "center", bool battle = false, bool siege = false,
            bool teamBattle = false, bool battleMode = false, bool underSiege = false,
            bool matchingSettlement = true, bool encounter = true)
        {
            object facts = Activator.CreateInstance(context, All, null,
                new object[] { true, true, encounter, true, matchingSettlement, battle,
                    siege, teamBattle, battleMode, underSiege, location }, null);
            return (bool)decide.Invoke(null, new[] { facts });
        }
        Check(Allows(), "peace center denied");
        Check(Allows("alley") && Allows("prison") && Allows("port"), "original peace locations denied");
        Check(!Allows("arena") && !Allows("training_field") && !Allows("unknown"),
            "special/unknown location admitted");
        Check(!Allows(battle: true) && !Allows(siege: true) && !Allows(teamBattle: true)
            && !Allows(battleMode: true) && !Allows(underSiege: true),
            "battle/siege/deployment context admitted");
        Check(!Allows(matchingSettlement: false) && !Allows(encounter: false),
            "mismatched or missing encounter admitted");

        string host = File.ReadAllText(Path.Combine(repo, "SceneTauntBehavior.cs"));
        Check(host.Contains("return ScenePeaceConflictContextOwner.CanInitialize(in facts);", StringComparison.Ordinal)
            && host.Contains("return CanInitializePeaceSceneConflict(settlement);", StringComparison.Ordinal)
            && host.Contains("if (!CanInitializePeaceSceneConflict(Settlement.CurrentSettlement))", StringComparison.Ordinal),
            "Mission fact adapter is disconnected from the tested owner");
        Check(host.Split("CanInitializePeaceSceneConflict(Settlement.CurrentSettlement)", StringSplitOptions.None).Length - 1 == 3,
            "custom, physical, and carryover initialization do not share the same gate");
        Console.WriteLine("PASS ScenePeaceConflictOwnerReplay current-DLL peace/battle/siege/location/encounter and host entry wiring; Mission/live=NOT_RUN");
    }
}
