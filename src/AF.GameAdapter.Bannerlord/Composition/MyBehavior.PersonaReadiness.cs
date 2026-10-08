using System;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class MyBehavior
{
    private static readonly Func<object> PersonaInstanceIdentity = () => Instance;
    private static readonly Func<object> PersonaCampaignIdentity = () => Campaign.Current?.GetCampaignBehavior<MyBehavior>();
    private static readonly Func<object, AnimusForge.Refactor.Adapters.NpcPersonaGenerationApplicationAdapter> PersonaApplicationLookup = owner => ((MyBehavior)owner).NpcPersonaGenerationApplication;

    // Callers dispatch through their existing channel owner before observing any game/profile state.
    // null = retired owner; Available=false = the legacy missing-owner fallback, not ready data.
    internal static NpcPersonaReadinessSnapshot CaptureNpcPersonaReadiness(Hero hero, MyBehavior expectedOwner)
    {
        return AnimusForge.Refactor.Adapters.ScenePersonaPreparationAdapter.CaptureNpcPersonaReadiness(hero, expectedOwner, PersonaInstanceIdentity, PersonaCampaignIdentity, PersonaApplicationLookup);
    }
}
