using System;
using System.Reflection;

internal static class OnboardingUiDispatchOwnerReplay
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags All = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = assembly.GetType("AnimusForge.Refactor.Modules.OnboardingUiDispatchOwner", true);
        object owner = Activator.CreateInstance(type, true);
        object Call(string method, params object[] args) => type.GetMethod(method, All).Invoke(owner, args);
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Onboarding UI dispatch: " + message);
        }

        int value = 0;
        int first = (int)Call("BeginTest");
        Call("PostTest", first, (Action)(() => value += 1));
        Check(value == 0, "worker callback ran before pump");
        Check((int)Call("Pump", 1) == 1 && value == 1, "main-thread pump lost test callback");
        Call("PostTest", first, (Action)(() => value += 10));
        Call("CancelTest");
        Check((int)Call("Pump", 1) == 1 && value == 1, "cancelled test callback ran");
        int second = (int)Call("BeginTest");
        Call("PostTest", second, (Action)(() => value += 2));
        Call("Post", (Action)(() => value += 4));
        Check((int)Call("Pump", 1) == 1 && value == 3, "bounded pump did not stop");
        Check((int)Call("Pump", 1) == 1 && value == 7, "fetch callback not pumped");
        Call("Post", (Action)(() => value += 100));
        Call("Close");
        Check((int)Call("Pump", 8) == 0 && value == 7, "closed owner leaked callback");
        Check(!(bool)Call("Post", (Action)(() => value += 1000)), "closed owner accepted callback");
        Console.WriteLine("PASS OnboardingUiDispatchOwnerReplay current-DLL queued/bounded/cancelled/closed callbacks; live Gauntlet=NOT_RUN");
    }
}
