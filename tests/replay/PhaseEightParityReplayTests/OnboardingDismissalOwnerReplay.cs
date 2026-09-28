using System;
using System.Reflection;

internal static class OnboardingDismissalOwnerReplay
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags All = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type generic = assembly.GetType("AnimusForge.Refactor.Modules.OnboardingDismissalOwner`1", true);
        Type type = generic.MakeGenericType(typeof(int));
        object owner = Activator.CreateInstance(type, true);
        MethodInfo claim = type.GetMethod("TryClaim", All);
        MethodInfo reset = type.GetMethod("Reset", All);
        bool Claim(int stage, long ticks, out int resumed)
        {
            object[] args = { stage, ticks, 0 };
            bool result = (bool)claim.Invoke(owner, args);
            resumed = (int)args[2];
            return result;
        }
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Onboarding dismissal: " + message);
        }

        long start = new DateTime(2026, 9, 25, 0, 0, 0, DateTimeKind.Utc).Ticks;
        Check(!Claim(1, start, out _), "dismissal resumed immediately");
        Check(!Claim(1, start + TimeSpan.FromMilliseconds(149).Ticks, out _), "dismissal resumed before grace");
        Check(!Claim(2, start + TimeSpan.FromMilliseconds(150).Ticks, out _), "changed stage inherited old grace");
        Check(Claim(2, start + TimeSpan.FromMilliseconds(300).Ticks, out int resumed) && resumed == 2, "stable stage did not resume");
        Check(!Claim(2, start + TimeSpan.FromMilliseconds(301).Ticks, out _), "resume repeated");
        reset.Invoke(owner, null);
        Check(!Claim(2, start + TimeSpan.FromMilliseconds(500).Ticks, out _), "reset did not clear grace");
        Console.WriteLine("PASS OnboardingDismissalOwnerReplay current-DLL grace/stage-change/reset/one-shot; live inquiry=NOT_RUN");
    }
}
