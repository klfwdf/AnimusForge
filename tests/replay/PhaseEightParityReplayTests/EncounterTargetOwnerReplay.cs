using System;
using System.Reflection;

internal static class EncounterTargetOwnerReplay
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        Type generic = assembly.GetType("AnimusForge.Refactor.Modules.EncounterTargetOwner`2", true);
        Type closed = generic.MakeGenericType(typeof(object), typeof(object));
        object owner = Activator.CreateInstance(closed, true);
        MethodInfo set = closed.GetMethod("Set", All);
        MethodInfo ensure = closed.GetMethod("Ensure", All);
        object selected = new object(), leader = new object(), secondLeader = new object();
        object firstParty = new object(), secondParty = new object();
        Func<object, object, bool> eligible = (target, party) =>
            ReferenceEquals(party, firstParty) && (ReferenceEquals(target, selected) || ReferenceEquals(target, leader))
            || ReferenceEquals(party, secondParty) && ReferenceEquals(target, secondLeader);
        Func<object, object> fallback = party => ReferenceEquals(party, firstParty) ? leader
            : ReferenceEquals(party, secondParty) ? secondLeader : null;
        object Resolve(object party, out bool refreshed, out bool cleared)
        {
            object[] args = { party, eligible, fallback, false, false };
            object target = ensure.Invoke(owner, args);
            refreshed = (bool)args[3];
            cleared = (bool)args[4];
            return target;
        }
        void Check(bool value, string label)
        {
            if (!value) throw new InvalidOperationException("Encounter target owner: " + label);
        }

        set.Invoke(owner, new[] { selected });
        Check(ReferenceEquals(Resolve(firstParty, out bool refreshed, out bool cleared), selected)
            && !refreshed && !cleared, "selected army member overwritten by leader");
        Check(ReferenceEquals(Resolve(firstParty, out refreshed, out cleared), selected)
            && !refreshed, "menu recheck replaced valid member");
        Check(ReferenceEquals(Resolve(secondParty, out refreshed, out cleared), secondLeader)
            && refreshed && !cleared, "new encounter retained stale member");
        Check(Resolve(null, out refreshed, out cleared) == null && !refreshed && cleared,
            "lost encounter did not clear target");
        Type host = assembly.GetType("AnimusForge.LordEncounterBehavior", true);
        Check(host.GetField("_targetOwner", All)?.FieldType.GetGenericTypeDefinition() == generic,
            "production LordEncounterBehavior does not hold target owner");
        Console.WriteLine("PASS EncounterTargetOwnerReplay current-DLL member precedence/recheck/new encounter/clear and host owner field; live menu=NOT_RUN");
    }
}
