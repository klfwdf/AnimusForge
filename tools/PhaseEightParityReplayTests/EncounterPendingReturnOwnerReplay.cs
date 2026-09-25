using System;
using System.Reflection;

internal static class EncounterPendingReturnOwnerReplay
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        Type generic = assembly.GetType("AnimusForge.Refactor.Modules.EncounterPendingReturnOwner`2", true);
        Type closed = generic.MakeGenericType(typeof(object), typeof(object));
        object owner = Activator.CreateInstance(closed, true);
        object encounter = new object(), party = new object();
        bool Call(string name, params object[] args) => (bool)closed.GetMethod(name, All).Invoke(owner, args);
        void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("Encounter pending return owner: " + label);
        }

        Check(!Call("Mark", null, party, 1L), "missing encounter queued menu return");
        Check(!Call("Mark", encounter, null, 1L), "missing party queued menu return");
        Check(Call("Mark", encounter, party, 1L)
            && Call("IsCurrent", encounter, party, 1L), "exact return context rejected");
        Check(!Call("IsCurrent", new object(), party, 1L)
            && !Call("IsCurrent", encounter, new object(), 1L)
            && !Call("IsCurrent", encounter, party, 2L), "stale encounter/party/save admitted");
        closed.GetMethod("Clear", All).Invoke(owner, null);
        Check(!Call("IsCurrent", encounter, party, 1L), "cleared return still reopened menu");
        Type host = assembly.GetType("AnimusForge.LordEncounterBehavior", true);
        Check(host.GetField("_pendingReturnOwner", All)?.FieldType.GetGenericTypeDefinition() == generic,
            "production LordEncounterBehavior does not hold pending return owner");
        Console.WriteLine("PASS EncounterPendingReturnOwnerReplay current-DLL exact encounter/party/save, missing and clear; live menu=NOT_RUN");
    }
}
