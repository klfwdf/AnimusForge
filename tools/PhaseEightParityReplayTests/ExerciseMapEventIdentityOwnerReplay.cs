using System;
using System.Collections.Generic;
using System.Reflection;

internal static class ExerciseMapEventIdentityOwnerReplay
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        Type owner = assembly.GetType("AnimusForge.Refactor.Modules.ExerciseMapEventIdentityOwner", true);
        MethodInfo current = owner.GetMethod("IsCurrent", All).MakeGenericMethod(typeof(object), typeof(object));
        object exercise = new object(), realBattle = new object(), opponent = new object(), holding = new object();
        var parties = new Dictionary<object, HashSet<object>>
        {
            [exercise] = new HashSet<object> { opponent, holding },
            [realBattle] = new HashSet<object>()
        };
        Func<object, object, bool> contains = (mapEvent, party) =>
            parties.TryGetValue(mapEvent, out HashSet<object> members) && members.Contains(party);
        bool IsCurrent(object currentEvent, object candidate, object opponentParty, object holdingParty) =>
            (bool)current.Invoke(null, new object[] { currentEvent, candidate, opponentParty, holdingParty, contains });
        void Check(bool value, string label)
        {
            if (!value) throw new InvalidOperationException("Exercise MapEvent identity: " + label);
        }

        Check(IsCurrent(exercise, exercise, opponent, holding), "exact MapEvent was rejected");
        Check(!IsCurrent(exercise, realBattle, opponent, holding),
            "unrelated real player battle was treated as exercise");
        Check(!IsCurrent(exercise, null, opponent, holding), "null MapEvent was accepted");
        parties[realBattle].Add(opponent);
        Check(IsCurrent(exercise, realBattle, opponent, holding),
            "exact current dummy party MapEvent was rejected");
        parties[realBattle].Clear();
        parties[realBattle].Add(new object());
        Check(!IsCurrent(exercise, realBattle, opponent, holding),
            "different dummy party identity was treated as current runtime");
        Type host = assembly.GetType("AnimusForge.MilitaryExerciseBehavior", true);
        Check(host.GetField("MapEventContainsExerciseParty", All)?.FieldType == typeof(Func<,,>).MakeGenericType(
            assembly.GetType("TaleWorlds.CampaignSystem.MapEvents.MapEvent") ?? host.GetMethod("GetRuntimeForMapEvent", All).GetParameters()[0].ParameterType,
            assembly.GetType("TaleWorlds.CampaignSystem.Party.MobileParty") ?? host.GetMethod("MapEventContainsParty", All).GetParameters()[1].ParameterType,
            typeof(bool)),
            "production host lost exact party lookup delegate");
        Console.WriteLine("PASS ExerciseMapEventIdentityOwnerReplay current-DLL exact event/party and unrelated real battle exclusion; live MapEvent=NOT_RUN");
    }
}
