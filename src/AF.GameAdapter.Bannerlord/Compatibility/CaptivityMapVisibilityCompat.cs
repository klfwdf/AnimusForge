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

    private static bool _missingFieldsLogged;

    // Returns false when the 1.5 cache contract is unavailable. Never throws: the
    // caller skips the visibility repair (a partial repair would be undone by the
    // next native frame) and the restore path continues. Logged once, not per tick.
    internal static bool ForgetTransientVisibility(Campaign campaign, PartyBase captor)
    {
        if (TickData == null || Previous == null || Current == null)
        {
            if (!_missingFieldsLogged)
            {
                _missingFieldsLogged = true;
                Logger.Log("PlayerCaptivity", "captor_visibility_skipped missing_tick_cache_fields");
            }
            return false;
        }
        object cache = TickData.GetValue(campaign);
        if (cache == null) return true;
        (Previous.GetValue(cache) as List<PartyBase>)?.Remove(captor);
        (Current.GetValue(cache) as List<PartyBase>)?.Remove(captor);
        return true;
    }
#else
    internal static bool ForgetTransientVisibility(Campaign campaign, PartyBase captor) => true;
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
#if BANNERLORD_1_5_OR_GREATER
        // Only 1.5 hides the persistent captor; 1.3/1.4 keep native visibility.
        if (!ForgetTransientVisibility(campaign, expectedHolder)) return;
        bool wasVisible = mobile.IsVisible;
        mobile.IsVisible = true; // Native setter also refreshes party visuals.
        if (!wasVisible)
            Logger.Log("PlayerCaptivity", "captor_visibility_restored party=" + mobile.StringId);
#endif
    }
}
