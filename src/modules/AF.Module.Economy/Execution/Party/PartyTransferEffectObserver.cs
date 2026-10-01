using System;
namespace AnimusForge;

// A main-thread port supplies roster observations. Never retry or roll back a partial write.
internal static class PartyTransferEffectObserver
{
    internal static PartyTransferEffectResult Execute(Func<int> sourceCount, Func<int> targetCount, Func<int> transfer)
    {
        int beforeSource, beforeTarget;
        try { beforeSource = sourceCount(); beforeTarget = targetCount(); }
        catch (Exception ex)
        {
            Logger.Log("Logic", "[PartyTransfer] source preflight unreadable: " + ex.Message);
            return new PartyTransferEffectResult(0, 0, true);
        }
        bool failed = false;
        try { transfer(); }
        catch (Exception ex)
        {
            failed = true;
            Logger.Log("Logic", "[PartyTransfer] non-transactional write failed: " + ex.Message);
        }
        int delivered = 0, debited = 0;
        bool indeterminate = false;
        try { debited = Math.Max(0, beforeSource - sourceCount()); } catch { indeterminate = true; }
        try { delivered = Math.Max(0, targetCount() - beforeTarget); } catch { indeterminate = true; }
        return new PartyTransferEffectResult(delivered, debited, failed || indeterminate, indeterminate);
    }
}
