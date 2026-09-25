using System;
using System.Linq.Expressions;
using System.Reflection;

internal static class EncounterReleaseOwnerReplay
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        Type host = assembly.GetType("AnimusForge.LordEncounterBehavior", true);
        Type requestType = host.GetNestedType("MeetingPlayerReleaseRequest", BindingFlags.NonPublic);
        Type generic = assembly.GetType("AnimusForge.Refactor.Modules.EncounterReleaseOwner`1", true);
        Type closed = generic.MakeGenericType(requestType);
        object owner = Activator.CreateInstance(closed, true);
        object request = Activator.CreateInstance(requestType, All, null,
            new object[] { null, null, null, 1L, null, "replay", 0f }, null);
        ParameterExpression input = Expression.Parameter(requestType, "request");
        Delegate valid = Expression.Lambda(typeof(Func<,>).MakeGenericType(requestType, typeof(bool)),
            Expression.Constant(true), input).Compile();
        Delegate invalid = Expression.Lambda(typeof(Func<,>).MakeGenericType(requestType, typeof(bool)),
            Expression.Constant(false), input).Compile();
        object Call(string name, params object[] args) => closed.GetMethod(name, All).Invoke(owner, args);
        object Pending() => closed.GetProperty("Pending", All).GetValue(owner);
        object Authorization() => closed.GetProperty("Authorization", All).GetValue(owner);
        void Check(bool value, string label)
        {
            if (!value) throw new InvalidOperationException("Encounter release owner: " + label);
        }

        Call("Schedule", request);
        Check(ReferenceEquals(Pending(), request) && ReferenceEquals(Authorization(), request),
            "scheduled release did not share exact ticket with authorization");
        Check(!(bool)Call("TryBeginPendingAttempt", 9f, true, true, 10f, 0.25f),
            "dialogue deadline fired early");
        Check(!(bool)Call("TryBeginPendingAttempt", 10f, true, false, 10f, 0.25f),
            "mission teardown attempted exit before map state");
        Check((bool)Call("TryBeginPendingAttempt", 10f, true, true, 10f, 0.25f)
            && !(bool)Call("TryBeginPendingAttempt", 10.1f, true, true, 10f, 0.25f),
            "deadline/retry debounce drifted");
        object[] pendingArgs = { invalid, 10f, 120f, null };
        Check(!(bool)Call("HasCurrentPending", pendingArgs) && (string)pendingArgs[3] == "context_changed",
            "stale callback not rejected");
        pendingArgs = new object[] { valid, 121f, 120f, null };
        Check(!(bool)Call("HasCurrentPending", pendingArgs) && (string)pendingArgs[3] == "expired",
            "expired callback not rejected");
        Check(!(bool)Call("ConsumeAuthorization", invalid, 11f, 120f)
            && Authorization() == null, "stale authorization was not consumed and revoked");
        Call("Schedule", request);
        Check((bool)Call("ConsumeAuthorization", valid, 120f, 120f)
            && !(bool)Call("ConsumeAuthorization", valid, 120f, 120f),
            "valid authorization was not exactly once");
        Call("Schedule", request);
        Check((bool)Call("ClearPending") && Pending() == null && Authorization() == null,
            "pending cancellation did not revoke the exact authorization");
        Check(host.GetField("_releaseOwner", All)?.FieldType == closed,
            "production LordEncounterBehavior does not hold release owner");
        Console.WriteLine("PASS EncounterReleaseOwnerReplay current-DLL deadline/teardown/retry/stale/expiry/exact authorization and host owner field; native exit/live=NOT_RUN");
    }
}
