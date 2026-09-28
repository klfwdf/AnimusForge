using System;
using System.Reflection;

internal static class OnboardingSessionOwnerReplay
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags All = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = assembly.GetType("AnimusForge.Refactor.Modules.OnboardingSessionOwner", true);
        object owner = Activator.CreateInstance(type, true);
        object Call(string method, params object[] args) => type.GetMethod(method, All).Invoke(owner, args);
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Onboarding session: " + message);
        }

        long start = new DateTime(2026, 9, 25, 0, 0, 0, DateTimeKind.Utc).Ticks;
        Call("MarkStartupNotice", start);
        Call("MarkWelcome", start);
        Call("MarkActionPostprocess", start);
        Check(!(bool)Call("TryClaimStartupNotice", start + TimeSpan.FromSeconds(2).Ticks, false), "pre-start notice fired");
        Check(!(bool)Call("TryClaimStartupNotice", start + TimeSpan.FromMilliseconds(999).Ticks, true), "notice fired early");
        Check((bool)Call("TryClaimStartupNotice", start + TimeSpan.FromSeconds(1).Ticks, true)
            && !(bool)Call("TryClaimStartupNotice", start + TimeSpan.FromSeconds(2).Ticks, true), "notice replayed");
        Check(!(bool)Call("TryClaimWelcome", start + TimeSpan.FromSeconds(2).Ticks, true, true), "completed setup reopened");
        Check(!(bool)Call("TryClaimWelcome", start + TimeSpan.FromMilliseconds(1999).Ticks, true, false), "welcome fired early");
        Check((bool)Call("TryClaimWelcome", start + TimeSpan.FromSeconds(2).Ticks, true, false)
            && !(bool)Call("TryClaimWelcome", start + TimeSpan.FromSeconds(3).Ticks, true, false), "welcome replayed");
        Check(!(bool)Call("TryClaimActionPostprocess", start + TimeSpan.FromSeconds(3).Ticks, true, false), "action prompt shown before setup");
        Check((bool)Call("TryClaimActionPostprocess", start + TimeSpan.FromSeconds(3).Ticks, true, true)
            && !(bool)Call("TryClaimActionPostprocess", start + TimeSpan.FromSeconds(4).Ticks, true, true), "action prompt replayed");
        Call("MarkWelcome", start + TimeSpan.FromSeconds(10).Ticks);
        Check(!(bool)Call("TryClaimWelcome", start + TimeSpan.FromSeconds(12).Ticks, true, false), "same session repeated welcome");
        Call("ResetWelcomeShown");
        Check((bool)Call("TryClaimWelcome", start + TimeSpan.FromSeconds(12).Ticks, true, false), "old-save welcome reset lost");
        Call("MarkWelcome", start + TimeSpan.FromSeconds(20).Ticks);
        Call("CancelWelcome");
        Check(!(bool)Call("TryClaimWelcome", start + TimeSpan.FromSeconds(30).Ticks, true, false), "cancelled welcome fired");
        Console.WriteLine("PASS OnboardingSessionOwnerReplay current-DLL notice/welcome/action delay/idempotence/cancel; live Campaign=NOT_RUN");
    }
}
