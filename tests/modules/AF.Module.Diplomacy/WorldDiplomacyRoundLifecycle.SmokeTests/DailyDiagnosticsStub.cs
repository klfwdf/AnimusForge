using System;
namespace AnimusForge;

// Watchdog is a diagnostic leaf here; the real DailyTick application and ordered effects run unchanged.
internal static class FreezeWatchdog
{
    internal static ScopeToken Scope(string name) => default;
    internal readonly struct ScopeToken : IDisposable { public void Dispose() { } }
}
