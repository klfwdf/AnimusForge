using System;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class MyBehavior
{
    // Callers dispatch through their existing channel owner before observing any game/profile state.
    // null = retired owner; Available=false = the legacy missing-owner fallback, not ready data.
    internal static NpcPersonaReadinessSnapshot CaptureNpcPersonaReadiness(Hero hero, MyBehavior expectedOwner)
    {
        if (!TWParallel.IsMainThread()) throw new InvalidOperationException("Persona readiness capture requires the game thread.");
        if (!ReferenceEquals(Instance, expectedOwner)) return null;
        MyBehavior owner = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
        if (!ReferenceEquals(owner, expectedOwner)) return null;
        string id = (hero?.StringId ?? "").Trim();
        string name = hero?.Name?.ToString();
        if (owner == null || hero == null)
            return new NpcPersonaReadinessSnapshot(id, name, "", "", false, false, false, false);
        owner.GetNpcPersonaStrings(hero, out string personality, out string background);
        owner.GetNpcPersonaGenerationRuntimeState(hero, out bool active, out bool coolingDown);
        bool needsGeneration = !string.IsNullOrWhiteSpace(id)
            && (string.IsNullOrWhiteSpace(personality) || string.IsNullOrWhiteSpace(background));
        return new NpcPersonaReadinessSnapshot(id, name, personality, background, true, needsGeneration, active, coolingDown);
    }
}
