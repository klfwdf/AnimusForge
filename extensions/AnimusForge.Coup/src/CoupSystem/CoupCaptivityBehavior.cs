using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace AnimusForge.CoupSystem;

// This is an exception to same-faction automatic peace release, not permanent captivity.
internal sealed class CoupCaptivityBehavior : CampaignBehaviorBase
{
    private static CoupCaptivityBehavior _instance;
    private readonly Dictionary<string, string> _detentions = new Dictionary<string, string>(StringComparer.Ordinal);
    private string _detentionsJson;
    private bool _saveValid = true;
    private static readonly MethodInfo IsHeroGetter = AccessTools.PropertyGetter(typeof(BasicCharacterObject), nameof(BasicCharacterObject.IsHero));
    private static readonly MethodInfo CharacterIsHeroGetter = AccessTools.PropertyGetter(typeof(CharacterObject), nameof(CharacterObject.IsHero));
    private static readonly MethodInfo ReleaseCandidateMethod = AccessTools.Method(typeof(CoupCaptivityBehavior), nameof(IsAutomaticPeaceReleaseCandidate));
    private static bool _patchesInstalled;
    internal static bool AutomaticReleaseProtectionAvailable => _patchesInstalled && _instance?._saveValid == true;

    public CoupCaptivityBehavior() { _instance = this; }

    public override void RegisterEvents()
    {
        _instance = this;
        CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, OnPrisonerReleased);
        CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
        CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, ValidateDetentions);
        CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
    }

    public override void SyncData(IDataStore dataStore)
    {
        if (dataStore.IsSaving && _saveValid) _detentionsJson = JsonConvert.SerializeObject(_detentions);
        dataStore.SyncData("af_coup_detentions_v1", ref _detentionsJson);
        if (!dataStore.IsLoading) return;
        _detentions.Clear();
        _saveValid = true;
        if (string.IsNullOrEmpty(_detentionsJson)) return;
        try
        {
            Dictionary<string, string> saved = JsonConvert.DeserializeObject<Dictionary<string, string>>(_detentionsJson)
                ?? throw new InvalidOperationException("Empty detention state.");
            foreach (KeyValuePair<string, string> item in saved)
            {
                if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value))
                    throw new InvalidOperationException("Invalid detention entry.");
            }
            foreach (KeyValuePair<string, string> item in saved) _detentions[item.Key] = item.Value;
        }
        catch (Exception ex)
        {
            _detentions.Clear();
            _saveValid = false;
            Logger.Log("Coup", "Detention state could not be restored; original save retained: " + ex.Message);
        }
    }

    internal static void RegisterDetention(string coupId, Hero king)
    {
        if (_instance == null || !_instance._saveValid || !AutomaticReleaseProtectionAvailable || string.IsNullOrWhiteSpace(coupId) || king == null || !IsInPlayerCustody(king))
            throw new InvalidOperationException("政变拘押保护不可用，或目标尚未由玩家拘押。");
        _instance._detentions[king.StringId] = coupId;
        Logger.Log("Coup", "Detention registered. coup=" + coupId + ", hero=" + king.StringId);
    }

    internal static void ClearDetention(Hero king)
    {
        if (king != null && _instance?._detentions.Remove(king.StringId) == true)
            Logger.Log("Coup", "Detention cleared. hero=" + king.StringId);
    }

    private static bool IsInPlayerCustody(Hero hero)
    {
        PartyBase captor = hero?.PartyBelongedToAsPrisoner;
        return hero != null && hero.IsAlive && hero.IsPrisoner && captor != null
            && (captor == PartyBase.MainParty || (captor.IsSettlement && captor.Settlement?.OwnerClan == Clan.PlayerClan));
    }

    private static bool ShouldKeepDetained(Hero hero)
    {
        if (!AutomaticReleaseProtectionAvailable || hero == null || _instance == null || !_instance._detentions.ContainsKey(hero.StringId)) return false;
        if (!IsInPlayerCustody(hero))
        {
            ClearDetention(hero);
            return false;
        }
        // Peace with another kingdom still releases its prisoners normally.
        IFaction faction = hero.PartyBelongedToAsPrisoner.MapFaction;
        return faction != null && hero.MapFaction == faction;
    }

    private static bool IsAutomaticPeaceReleaseCandidate(BasicCharacterObject character)
    {
        return character != null && character.IsHero && !ShouldKeepDetained((character as CharacterObject)?.HeroObject);
    }

    private void OnPrisonerReleased(Hero hero, PartyBase captor, IFaction faction, EndCaptivityDetail detail, bool showNotification) { ClearDetention(hero); }
    private void OnHeroKilled(Hero hero, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification) { ClearDetention(hero); }
    private void OnGameLoaded(CampaignGameStarter starter) { ValidateDetentions(); }

    private static void PrisonerCustodyChangedPostfix(Hero __instance)
    {
        // Called after the destination is installed, so moving between the main
        // party and an owned dungeon keeps the mark; handing the king away does not.
        if (_instance != null && __instance != null && _instance._detentions.ContainsKey(__instance.StringId) && !IsInPlayerCustody(__instance))
            ClearDetention(__instance);
    }

    private void ValidateDetentions()
    {
        // Only the small set of marked kings is inspected, once per campaign hour.
        if (_detentions.Count == 0) return;
        List<string> stale = null;
        foreach (string id in _detentions.Keys)
        {
            Hero hero = MBObjectManager.Instance.GetObject<Hero>(id);
            if (IsInPlayerCustody(hero)) continue;
            if (stale == null) stale = new List<string>();
            stale.Add(id);
        }
        if (stale != null) foreach (string id in stale) _detentions.Remove(id);
    }

    internal static void ResetPatches()
    {
        _patchesInstalled = false;
        _instance = null;
    }

    internal static void RegisterPatches(Harmony harmony)
    {
        _patchesInstalled = false;
        try
        {
            MethodInfo settlementRelease = AccessTools.Method(typeof(PrisonerReleaseCampaignBehavior), "ReleasePrisonersInternal", new[] { typeof(IFaction) });
            MethodInfo partyRelease = AccessTools.Method(typeof(PrisonerReleaseCampaignBehavior), "ReleasePartyPrisoners", new[] { typeof(MobileParty), typeof(IFaction) });
            MethodInfo custodyChanged = AccessTools.Method(typeof(Hero), "OnAddedToPartyAsPrisoner", new[] { typeof(PartyBase) });
            if (IsHeroGetter == null || CharacterIsHeroGetter == null || ReleaseCandidateMethod == null || settlementRelease == null || partyRelease == null || custodyChanged == null)
                throw new MissingMethodException("Automatic prisoner release targets unavailable");
            HarmonyMethod transpiler = new HarmonyMethod(AccessTools.Method(typeof(CoupCaptivityBehavior), nameof(AutomaticReleaseTranspiler)));
            harmony.Patch(settlementRelease, transpiler: transpiler);
            harmony.Patch(partyRelease, transpiler: transpiler);
            harmony.Patch(custodyChanged, postfix: new HarmonyMethod(AccessTools.Method(typeof(CoupCaptivityBehavior), nameof(PrisonerCustodyChangedPostfix))));
            _patchesInstalled = true;
        }
        catch (Exception ex) { Logger.Log("Coup", "Automatic detention protection unavailable; capture disabled: " + ex.Message); }
    }

    // Both supported source versions test Character.IsHero once, before the entire
    // release branch. Guarding that predicate also skips its settlement-release event.
    // A prefix on ApplyByPeace would leave that event firing for a prisoner still held.
    private static IEnumerable<CodeInstruction> AutomaticReleaseTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        List<CodeInstruction> result = new List<CodeInstruction>(instructions);
        int matches = 0;
        foreach (CodeInstruction instruction in result)
        {
            if (!instruction.Calls(IsHeroGetter) && !instruction.Calls(CharacterIsHeroGetter)) continue;
            instruction.opcode = OpCodes.Call;
            instruction.operand = ReleaseCandidateMethod;
            matches++;
        }
        if (matches != 1) throw new InvalidOperationException("Unexpected automatic release IL: " + __originalMethod.Name + ", IsHero matches=" + matches);
        return result;
    }
}
