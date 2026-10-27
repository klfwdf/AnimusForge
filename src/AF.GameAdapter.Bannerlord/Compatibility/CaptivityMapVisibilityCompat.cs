using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace AnimusForge;

internal static class CaptivityMapVisibilityCompat
{
#if BANNERLORD_1_5_OR_GREATER
    // 1.5 skips persistent parties during visibility recomputation, but its
    // previous-frame cleanup can still hide the newly persistent captor once.
    // Resolve fields once; touch only that party at the conversation boundary.
    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly FieldInfo TickData = typeof(Campaign).GetField("_tickData", InstanceFields);
    private static readonly FieldInfo Previous = typeof(CampaignTickCacheDataStore).GetField("_previousFrameUpdatedPartyBases", InstanceFields);
    private static readonly FieldInfo Current = typeof(CampaignTickCacheDataStore).GetField("_currentFrameUpdatedPartyBases", InstanceFields);

    internal static void ForgetTransientVisibility(Campaign campaign, PartyBase captor)
    {
        // Missing bindings must be reported by the caller, not a silent partial
        // restoration that the next native frame immediately undoes.
        if (TickData == null || Previous == null || Current == null)
            throw new System.MissingFieldException("Bannerlord 1.5 captivity visibility cache contract changed.");
        object cache = TickData.GetValue(campaign);
        if (cache == null) return;
        (Previous.GetValue(cache) as List<PartyBase>)?.Remove(captor);
        (Current.GetValue(cache) as List<PartyBase>)?.Remove(captor);
    }
#else
    internal static void ForgetTransientVisibility(Campaign campaign, PartyBase captor) { }
#endif

    internal static void RestoreCurrentCaptor(Campaign campaign, PartyBase expectedHolder)
    {
        if (!ReferenceEquals(campaign, Campaign.Current) || !PlayerCaptivity.IsCaptive
            || !ReferenceEquals(expectedHolder, PlayerCaptivity.CaptorParty)
            || !ReferenceEquals(expectedHolder, Hero.MainHero?.PartyBelongedToAsPrisoner)
            || Hero.MainHero?.IsPrisoner != true || expectedHolder?.IsActive != true
            || expectedHolder.PrisonRoster?.Contains(Hero.MainHero.CharacterObject) != true)
            return;
        MobileParty mobile = expectedHolder.MobileParty;
        // A party inside a settlement remains hidden according to native rules.
        if (mobile == null || mobile.CurrentSettlement != null) return;
        ForgetTransientVisibility(campaign, expectedHolder);
        bool wasVisible = mobile.IsVisible;
        mobile.IsVisible = true; // Native setter also refreshes party visuals.
        if (!wasVisible)
            Logger.Log("PlayerCaptivity", "captor_visibility_restored party=" + mobile.StringId);
    }
}
