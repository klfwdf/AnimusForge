using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

// Engine adapter for AF core lifetime only. Team module gameplay remains with its own owner.
internal static class AfCampaignRuntimeLifecycle
{
    private static readonly List<CampaignBehaviorBase> Owners = new List<CampaignBehaviorBase>();
    private static readonly GameLifetimeCoordinator Lifetime = new GameLifetimeCoordinator(
        reason => SaveRuntimeGuard.AdvanceGeneration(reason), RetireOwners);

    internal static void Begin(Game game)
    {
        RequireMainThread();
        Lifetime.Begin(game);
    }

    internal static void CaptureOwners(Game game, CampaignGameStarter starter)
    {
        RequireMainThread();
        if (!Lifetime.IsCurrent(game) || starter == null) return;
        // Capture actual registered instances once, including a partially completed registration.
        // Game.Current/Campaign.Current may already be gone when OnGameEnd is delivered.
        foreach (CampaignBehaviorBase behavior in starter.CampaignBehaviors)
            if ((behavior is MyBehavior || behavior is ShoutBehavior || behavior is CourierDeliveryBehavior)
                && !Owners.Contains(behavior)) Owners.Add(behavior);
        // Constructors publish these two legacy singleton references before AddBehavior returns.
        // Include them when registration failed after construction, while this Game is still current.
        if (MyBehavior.Instance != null && !Owners.Contains(MyBehavior.Instance)) Owners.Add(MyBehavior.Instance);
        if (CourierDeliveryBehavior.Instance != null && !Owners.Contains(CourierDeliveryBehavior.Instance)) Owners.Add(CourierDeliveryBehavior.Instance);
    }

    internal static void End(Game game) { RequireMainThread(); Lifetime.End(game); }
    internal static void Stop() { RequireMainThread(); Lifetime.Stop(); }

    private static void RetireOwners(string reason)
    {
        CampaignBehaviorBase[] owners = Owners.ToArray();
        Owners.Clear();
        foreach (CampaignBehaviorBase owner in owners)
        {
            try
            {
                if (owner is MyBehavior memory) memory.RetireCampaignRuntime(reason);
                else if (owner is ShoutBehavior shout) shout.RetireCampaignRuntime(reason);
                else if (owner is CourierDeliveryBehavior courier) courier.RetireCampaignRuntime(reason);
            }
            catch (Exception error)
            {
                try { Logger.Log("CampaignLifetime", "[WARN] retirement failed: " + owner.GetType().Name + " " + error.Message); }
                catch { } // One owner/diagnostic must not strand the other owners.
            }
        }
    }

    private static void RequireMainThread()
    {
        if (!TWParallel.IsMainThread()) throw new InvalidOperationException("Campaign lifetime transition requires the game thread.");
    }
}
