using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using PatienceSnapshot = AnimusForge.MyBehavior.PatienceSnapshot;
namespace AnimusForge;
// Main-thread response capture and game-effect leaves. No Campaign scan or independent social store.
internal sealed class PatienceResponseBannerlordAdapter
{
    private readonly Func<string,int,float,float> _snapshot;
    private readonly Func<float> _now;
    private readonly Func<int,string> _relationLevel;
    private readonly Action<Town> _syncTown;
    private readonly PatienceResponseApplication _application;
    internal PatienceResponseBannerlordAdapter(Func<string,int,float,float> snapshot, Func<float> now,
        Func<int,string> relationLevel, Action<Town> syncTown, PatienceResponseApplication application)
    { _snapshot=snapshot; _now=now; _relationLevel=relationLevel; _syncTown=syncTown; _application=application; }
internal PatienceSnapshot GetHeroSnapshot(Hero hero)
	{
		PatienceSnapshot snap = new PatienceSnapshot
		{
			Key = "",
			DisplayName = (hero?.Name?.ToString() ?? "NPC"),
			Relation = 0,
			Trust = 0,
			PublicTrust = 0,
			PrivateLove = 0,
			Max = 30,
			Current = 30f,
			PatienceLevel = "一般",
			RelationLevel = "中立",
			TrustLevel = RewardSystemBehavior.GetTrustLevelText(0),
			PublicTrustLevel = RewardSystemBehavior.GetTrustLevelText(0),
			PrivateLoveLevel = RomanceSystemBehavior.GetPrivateLoveLevelText(0)
		};
		if (hero == null)
		{
			return snap;
		}
		string text = PatienceRules.HeroKey(hero?.StringId);
		if (string.IsNullOrEmpty(text))
		{
			return snap;
		}
		int relationWithPlayerSafe = GetRelationWithPlayerSafe(hero);
		int num = PatienceRules.ComputePatienceMaxFromRelation(relationWithPlayerSafe);
		float nowCampaignDay = _now();
		float value;
		value = _snapshot(text, num, nowCampaignDay);
		snap.Key = text;
		snap.Relation = relationWithPlayerSafe;
		snap.Max = num;
		snap.Current = value;
		snap.PatienceLevel = PatienceRules.GetPatienceLevelText(value, num);
		snap.RelationLevel = _relationLevel(relationWithPlayerSafe);
		FillTrustSnapshot(ref snap, hero);
		return snap;
	}
internal PatienceSnapshot GetUnnamedSnapshot(string unnamedKey, string npcName, string displayName = null)
	{
		string text = PatienceRules.UnnamedKey(unnamedKey, npcName);
		PatienceSnapshot result = new PatienceSnapshot
		{
			Key = text,
			DisplayName = (string.IsNullOrWhiteSpace(displayName) ? (string.IsNullOrWhiteSpace(npcName) ? "NPC" : npcName.Trim()) : displayName.Trim()),
			Relation = 0,
			Trust = 0,
			PublicTrust = 0,
			PrivateLove = 0,
			Max = 30,
			Current = 30f,
			PatienceLevel = PatienceRules.GetPatienceLevelText(30f, 30),
			RelationLevel = _relationLevel(0),
			TrustLevel = RewardSystemBehavior.GetTrustLevelText(0),
			PublicTrustLevel = RewardSystemBehavior.GetTrustLevelText(0),
			PrivateLoveLevel = RomanceSystemBehavior.GetPrivateLoveLevelText(0)
		};
		if (string.IsNullOrWhiteSpace(text))
		{
			return result;
		}
		float nowCampaignDay = _now();
		float value;
		value = _snapshot(text, 30, nowCampaignDay);
		result.Current = value;
		result.PatienceLevel = PatienceRules.GetPatienceLevelText(value, 30);
		try
		{
			CharacterObject oneToOneConversationCharacter = Campaign.Current?.ConversationManager?.OneToOneConversationCharacter;
			FillSettlementMerchantTrustSnapshot(ref result, oneToOneConversationCharacter);
		}
		catch
		{
		}
		return result;
	}
internal static int GetRelationWithPlayerSafe(Hero hero)
	{
		try
		{
			if (hero == null || Hero.MainHero == null)
			{
				return 0;
			}
			if (RomanceSystemBehavior.TryGetPrivateLoveAsPlayerRelation(hero, out var relation))
			{
				return relation;
			}
			return hero.GetRelation(Hero.MainHero);
		}
		catch
		{
			return 0;
		}
	}
internal static void FillTrustSnapshot(ref PatienceSnapshot snap, Hero hero)
	{
		snap.Trust = 0;
		snap.PublicTrust = 0;
		snap.PrivateLove = 0;
		snap.TrustLevel = RewardSystemBehavior.GetTrustLevelText(0);
		snap.PublicTrustLevel = RewardSystemBehavior.GetTrustLevelText(0);
		snap.PrivateLoveLevel = RomanceSystemBehavior.GetPrivateLoveLevelText(0);
		if (hero == null || RewardSystemBehavior.Instance == null)
		{
			return;
		}
		try
		{
			int publicTrust = RewardSystemBehavior.Instance.GetPublicTrust(hero);
			int trust = (snap.Trust = RewardSystemBehavior.Instance.GetEffectiveTrust(hero));
			snap.PublicTrust = publicTrust;
			snap.TrustLevel = RewardSystemBehavior.GetTrustLevelText(trust);
			snap.PublicTrustLevel = RewardSystemBehavior.GetTrustLevelText(publicTrust);
			int privateLove = (snap.PrivateLove = (RomanceSystemBehavior.Instance?.GetPrivateLove(hero)).GetValueOrDefault());
			snap.PrivateLoveLevel = RomanceSystemBehavior.GetPrivateLoveLevelText(privateLove);
		}
		catch
		{
		}
	}
internal static void FillSettlementMerchantTrustSnapshot(ref PatienceSnapshot snap, CharacterObject character)
	{
		snap.Trust = 0;
		snap.PublicTrust = 0;
		snap.TrustLevel = RewardSystemBehavior.GetTrustLevelText(0);
		snap.PublicTrustLevel = RewardSystemBehavior.GetTrustLevelText(0);
		if (character == null || RewardSystemBehavior.Instance == null)
		{
			return;
		}
		try
		{
			if (!RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(character, out var kind))
			{
				return;
			}
			Settlement currentSettlement = Settlement.CurrentSettlement;
			if (currentSettlement == null)
			{
				return;
			}
			int settlementPublicTrust = RewardSystemBehavior.Instance.GetSettlementLocalPublicTrust(currentSettlement);
			int settlementSharedPublicTrust = RewardSystemBehavior.Instance.GetSettlementSharedPublicTrust(currentSettlement);
			int num = settlementPublicTrust + settlementSharedPublicTrust;
			int settlementMerchantEffectiveTrust = RewardSystemBehavior.Instance.GetSettlementMerchantEffectiveTrust(currentSettlement, kind);
			snap.Trust = settlementMerchantEffectiveTrust;
			snap.PublicTrust = num;
			snap.TrustLevel = RewardSystemBehavior.GetTrustLevelText(settlementMerchantEffectiveTrust);
			snap.PublicTrustLevel = RewardSystemBehavior.GetTrustLevelText(num);
		}
		catch
		{
		}
	}
internal static Settlement GetCurrentRoyalDomainConversationSettlement()
	{
		Settlement settlement = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
		if (settlement?.Town == null || settlement.OwnerClan != Clan.PlayerClan)
		{
			return null;
		}
		return settlement;
	}
internal static bool CanApplyRoyalDomainConversationLoyaltyForHero(Hero targetHero)
	{
		if (targetHero == null)
		{
			return true;
		}
		Hero mainHero = Hero.MainHero;
		Clan playerClan = Clan.PlayerClan ?? mainHero?.Clan;
		// Loyalty reflects ordinary residents: this one-shot reply check uses only O(1) Hero/Clan flags and never scans rosters.
		bool isPlayerFamilyOrCompanion = targetHero.IsPlayerCompanion
			|| (playerClan != null && (targetHero.CompanionOf == playerClan || targetHero.Clan == playerClan))
			|| RomanceSystemBehavior.IsPlayerCompanionOrFamily(targetHero);
		return PatienceRules.RoyalLoyaltyEligible(true, targetHero == mainHero, targetHero.IsPrisoner, isPlayerFamilyOrCompanion, targetHero.IsLord);
	}
internal void ApplyRoyalDomainConversationLoyaltyFromMood(PatienceMood mood, Hero targetHero, string unnamedKey, string npcName, bool directConversation)
	{
		int num = PatienceRules.ComputeRoyalDomainConversationLoyaltyDelta(mood);
		if (num == 0 || !CanApplyRoyalDomainConversationLoyaltyForHero(targetHero))
		{
			return;
		}
		Settlement currentRoyalDomainConversationSettlement = GetCurrentRoyalDomainConversationSettlement();
		Town town = currentRoyalDomainConversationSettlement?.Town;
		if (town == null)
		{
			return;
		}
		try
		{
			float loyalty = town.Loyalty;
			float maximumLoyaltyInSettlement = Campaign.Current?.Models?.SettlementLoyaltyModel?.MaximumLoyaltyInSettlement ?? 100f;
			town.Loyalty = MBMath.ClampFloat(town.Loyalty + (float)num, 0f, maximumLoyaltyInSettlement);
			_syncTown(town);
			string text = targetHero?.StringId ?? unnamedKey ?? npcName ?? "";
			AnimusForgeQuickInfo.Show(currentRoyalDomainConversationSettlement.Name.ToString() + " 忠诚度 +" + num);
			Logger.Log("Patience", $"royal_domain_conversation_loyalty settlement={currentRoyalDomainConversationSettlement.StringId} npc={text} mood={mood} loyalty={loyalty:0.##}->{town.Loyalty:0.##} delta={num}");
			Logger.Obs("Patience", "royal_domain_conversation_loyalty", new Dictionary<string, object>
			{
				["settlementId"] = currentRoyalDomainConversationSettlement.StringId ?? "",
				["npcId"] = text,
				["mood"] = mood.ToString(),
				["loyaltyBefore"] = loyalty,
				["loyaltyAfter"] = town.Loyalty,
				["loyaltyDelta"] = num,
				["directConversation"] = directConversation
			});
			Logger.Metric("patience.royal_domain_conversation_loyalty");
		}
		catch (Exception ex)
		{
			Logger.Log("Patience", "[WARN] apply royal domain conversation loyalty failed: " + ex.Message);
		}
	}
    internal void ApplyHero(Hero hero, ref string text, bool directConversation, bool correction)
    {
        if (hero == null) { PatienceRules.ExtractMoodAndStripTag(ref text); return; }
        PatienceSnapshot snapshot = GetHeroSnapshot(hero);
        int relationAfter = snapshot.Relation;
        var result = _application.Apply(new PatienceResponseInput(snapshot.Key, snapshot.Max, snapshot.Relation, snapshot.PrivateLove, true),
            ref text, _now(), correction,
            mood => ApplyRoyalDomainConversationLoyaltyFromMood(mood, hero, null, hero.Name?.ToString(), directConversation),
            (delta, corrected) => relationAfter = ApplyRelation(hero, snapshot.Relation, delta, corrected),
            (delta, corrected) => RomanceSystemBehavior.Instance?.AdjustPrivateLove(hero, delta, corrected ? "mood_tag_postprocess" : "mood_tag"));
        if (!result.Applied) return;
        int loveAfter = RomanceSystemBehavior.Instance?.GetPrivateLove(hero) ?? snapshot.PrivateLove;
        Observe(snapshot, result, hero.StringId, relationAfter, loveAfter, directConversation);
    }
    internal void ApplyUnnamed(string key, string name, ref string text, bool correction)
    {
        PatienceSnapshot snapshot=GetUnnamedSnapshot(key,name);
        var result=_application.Apply(new PatienceResponseInput(snapshot.Key,snapshot.Max,0,0,false),ref text,_now(),correction,
            mood=>ApplyRoyalDomainConversationLoyaltyFromMood(mood,null,key,name,false),null,null);
        if(result.Applied) Observe(snapshot,result,null,0,0,false);
    }
    private static int ApplyRelation(Hero hero,int before,int delta,bool correction)
    {
        try
        {
            if(Hero.MainHero != null)
            {
                if(RomanceSystemBehavior.TryGetPrivateLoveAsPlayerRelation(hero,out _))
                    RomanceSystemBehavior.Instance?.AdjustPrivateLove(hero,delta,correction ? "mood_tag_postprocess_relation_delta" : "mood_tag_relation_delta");
                else ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero,hero,delta);
                return GetRelationWithPlayerSafe(hero);
            }
        }
        catch(Exception ex) { Logger.Log("Patience",correction ? "[WARN] apply postprocess relation delta failed: "+ex.Message : "[WARN] apply relation delta failed: "+ex.Message); }
        return before;
    }
    private static void Observe(PatienceSnapshot snapshot,PatienceResponseResult result,string heroId,int relationAfter,int loveAfter,bool direct)
    {
        bool hero=heroId!=null;
        string eventName=hero ? (result.Correction ? "hero_postprocess_mood_update" : "hero_update") : (result.Correction ? "unnamed_postprocess_mood_update" : "unnamed_update");
        try
        {
            string identity=hero ? "hero="+heroId : "unnamed="+snapshot.Key;
            string mood=result.Correction ? " postprocessMood=" : " mood=";
            string details=identity+mood+result.Mood+" value="+result.Change.Before+"->"+result.Change.After+"/"+snapshot.Max;
            if(result.Correction) details+=" delta="+result.Change.Delta;
            if(hero) details+=" relation="+snapshot.Relation+"->"+relationAfter+" delta="+result.RelationDelta+" privateLove="+snapshot.PrivateLove+"->"+loveAfter+" deltaLove="+result.PrivateLoveDelta;
            Logger.Log("Patience",details);
            var observation=new Dictionary<string,object> { [hero ? "heroId" : "key"]=heroId??snapshot.Key??"", ["mood"]=result.Mood.ToString(), ["patienceBefore"]=result.Change.Before,["patienceAfter"]=result.Change.After,["patienceMax"]=snapshot.Max };
            if(result.Correction) observation["patienceDelta"]=result.Change.Delta;
            if(hero)
            {
                observation["relationBefore"]=snapshot.Relation;observation["relationAfter"]=relationAfter;observation["relationDelta"]=result.RelationDelta;
                observation["privateLoveBefore"]=snapshot.PrivateLove;observation["privateLoveAfter"]=loveAfter;observation["privateLoveDelta"]=result.PrivateLoveDelta;observation["directConversation"]=direct;
                if(!result.Correction) observation["continuedAtZero"]=result.Change.Before<=0;
            }
            Logger.Obs("Patience",eventName,observation); Logger.Metric("patience."+eventName);
        }
        catch { }
        if(result.Change.BecameExhausted)
            try { Logger.Log("Patience",(hero ? "hero="+heroId : "unnamed="+snapshot.Key)+" reached_zero=true "+(result.Correction ? "postprocess_mood_override=" : "defer_exhausted_to_next_round=")+(direct ? "direct" : "scene")); } catch { }
    }
}
