using System;
using System.Reflection;

internal static class WeeklyReportPopupSessionOwnerReplay
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags All = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = assembly.GetType("AnimusForge.Refactor.Modules.WeeklyReportPopupSessionOwner", true);
        DateTime start = new DateTime(2026, 9, 25, 0, 0, 0, DateTimeKind.Utc);
        object owner = Activator.CreateInstance(type, All, null, new object[] { start, 10.0 }, null);
        object Call(string method, params object[] args) => type.GetMethod(method, All).Invoke(owner, args);
        bool Get(string name) => (bool)type.GetProperty(name, All).GetValue(owner);
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Weekly popup session: " + message);
        }

        Check(Get("CanProcessTick") && !Get("IsClosed"), "initial session unavailable");
        Check(!(bool)Call("TryClaimMinimumDwell", start.AddSeconds(9), true), "early reading credit");
        Check(!(bool)Call("TryClaimMinimumDwell", start.AddSeconds(10), false), "missing callback consumed credit");
        Check((bool)Call("Suspend", start.AddSeconds(5)) && !Get("CanProcessTick"), "encyclopedia suspension lost");
        Check(!(bool)Call("Suspend", start.AddSeconds(6)), "duplicate suspension shifted timer");
        Check(!(bool)Call("TryClaimMinimumDwell", start.AddMinutes(5), true), "hidden time counted as reading");
        Check((bool)Call("Resume", start.AddMinutes(5)), "encyclopedia return not accepted");
        Check(!(bool)Call("Resume", start.AddMinutes(6)), "duplicate resume shifted timer");
        Check(!(bool)Call("CanHandleEscape", start.AddMinutes(5).AddMilliseconds(349)), "returning Escape closed popup");
        Check((bool)Call("CanHandleEscape", start.AddMinutes(5).AddMilliseconds(350)), "Escape guard never released");
        Check(!(bool)Call("TryClaimMinimumDwell", start.AddMinutes(5).AddSeconds(4), true), "hidden time credited");
        Check((bool)Call("TryClaimMinimumDwell", start.AddMinutes(5).AddSeconds(5), true), "reading deadline lost");
        Check(!(bool)Call("TryClaimMinimumDwell", start.AddMinutes(6), true), "reading credit duplicated");
        Check((bool)Call("RequestClose") && !(bool)Call("RequestClose"), "close was not one-shot");
        Check((bool)Call("TryTakePendingClose") && !(bool)Call("TryTakePendingClose"), "deferred close replayed");
        Check((bool)Call("Close") && !(bool)Call("Close") && Get("IsClosed"), "close was not idempotent");
        Check(!(bool)Call("CanHandleEscape", start.AddHours(1))
            && !(bool)Call("TryClaimMinimumDwell", start.AddHours(1), true)
            && !(bool)Call("RequestClose"), "closed popup retained callbacks");
        Console.WriteLine("PASS WeeklyReportPopupSessionOwnerReplay current-DLL dwell/suspend/resume/escape/close; live Gauntlet=NOT_RUN");
    }
}
