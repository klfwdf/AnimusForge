using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Helpers;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed partial class ProactiveNpcRequestBehavior
{
	private static float GetProactiveContactRange(MobileParty mainParty)
	{
		return Math.Max(1f, mainParty.SeeingRange * 2f);
	}

	private static List<MobileParty> CaptureNearbyRequestParties(MobileParty mainParty)
	{
		float radius = GetProactiveContactRange(mainParty);
		float radiusSquared = radius * radius;
		var parties = new List<MobileParty>();
		// The native locator excludes its exact radius. Query a small margin, then retain
		// the inclusive contact boundary without enumerating the global lord-party list.
		var search = MobileParty.StartFindingLocatablesAroundPosition(mainParty.Position.ToVec2(), radius + 0.01f);
		for (MobileParty party = MobileParty.FindNextLocatable(ref search); party != null; party = MobileParty.FindNextLocatable(ref search))
		{
			if (party != mainParty && party.IsActive && party.IsVisible && party.IsLordParty
				&& party.Position.DistanceSquared(mainParty.Position) <= radiusSquared)
			{
				parties.Add(party);
			}
		}
		return parties;
	}

	private ProactiveCandidate FindBestRequestCandidate(DuelSettings settings, out CandidateScanStats stats, IEnumerable<MobileParty> sourceParties = null, Dictionary<string, TerritorialSettlementSnapshot> territorialSettlementSnapshots = null)
	{
		stats = new CandidateScanStats();
		MobileParty mainParty = MobileParty.MainParty;
		if (mainParty == null)
		{
			stats.MainPartyMissing = true;
			return null;
		}
		ProactiveCandidate bestCandidate = null;
		foreach (MobileParty party in sourceParties ?? CaptureNearbyRequestParties(mainParty))
		{
			stats.TotalLordParties++;
			if (!TryBuildBaseCandidate(party, mainParty, settings, out ProactiveCandidate candidate, out string skipReason))
			{
				if (skipReason == "out_of_range") stats.OutOfRange++;
				else stats.AddSkip(skipReason);
				continue;
			}
			stats.BaseEligible++;
			stats.InRange++;
            ProactiveCandidate experienceCandidate = BuildExperienceCandidate(candidate, settings);
            var currentScan = _candidateScanOwner.Current;
            if (currentScan != null && ProactiveCandidateScanOwner.IsCandidateBetter(experienceCandidate, currentScan.BestExperienceCandidate))
                currentScan.BestExperienceCandidate = experienceCandidate;
			List<ProactiveCandidate> needCandidates = new List<ProactiveCandidate>();
			if (!candidate.AtWarWithPlayer)
			{
				if (TryBuildFoodShortageCandidate(candidate, settings, out ProactiveCandidate foodCandidate))
				{
					stats.FoodShortage++;
					needCandidates.Add(foodCandidate);
				}
				if (TryBuildMoneyShortageCandidate(candidate, settings, out ProactiveCandidate moneyCandidate))
				{
					stats.MoneyShortage++;
					needCandidates.Add(moneyCandidate);
				}
				if (TryBuildTroopShortageCandidate(candidate, settings, out ProactiveCandidate troopCandidate))
				{
					stats.TroopShortage++;
					needCandidates.Add(troopCandidate);
				}
				if (TryBuildPrisonerOverloadCandidate(candidate, settings, out ProactiveCandidate prisonerCandidate))
				{
					stats.PrisonerOverload++;
					needCandidates.Add(prisonerCandidate);
				}
				if (TryBuildClanCaptiveCandidate(candidate, settings, out ProactiveCandidate clanCaptiveCandidate))
				{
					stats.ClanCaptive++;
					needCandidates.Add(clanCaptiveCandidate);
				}
				if (TryBuildLowMoraleCandidate(candidate, settings, out ProactiveCandidate lowMoraleCandidate))
				{
					stats.LowMorale++;
					needCandidates.Add(lowMoraleCandidate);
				}
				if (TryBuildMountShortageCandidate(candidate, settings, out ProactiveCandidate mountShortageCandidate))
				{
					stats.MountShortage++;
					needCandidates.Add(mountShortageCandidate);
				}
				if (TryBuildOverburdenedCandidate(candidate, settings, out ProactiveCandidate overburdenedCandidate))
				{
					stats.Overburdened++;
					needCandidates.Add(overburdenedCandidate);
				}
				if (TryBuildClanFinanceStrainCandidate(candidate, settings, out ProactiveCandidate clanFinanceCandidate))
				{
					stats.ClanFinanceStrain++;
					needCandidates.Add(clanFinanceCandidate);
				}
				if (TryBuildClanServiceCandidate(candidate, settings, out ProactiveCandidate clanServiceCandidate))
				{
					stats.ClanService++;
					needCandidates.Add(clanServiceCandidate);
				}
				if (TryBuildRomanticInteractionCandidate(candidate, settings, out ProactiveCandidate romanticInteractionCandidate))
				{
					stats.RomanticInteraction++;
					needCandidates.Add(romanticInteractionCandidate);
				}
				if (TryBuildGreetingCandidate(candidate, settings, out ProactiveCandidate greetingCandidate))
				{
					stats.Greeting++;
					needCandidates.Add(greetingCandidate);
				}
				if (TryBuildPolicyDiscussionCandidate(candidate, settings, out ProactiveCandidate policyDiscussionCandidate))
				{
					stats.PolicyDiscussion++;
					needCandidates.Add(policyDiscussionCandidate);
				}
				if (TryBuildFriendshipCandidate(candidate, settings, out ProactiveCandidate friendshipCandidate))
				{
					stats.Friendship++;
					needCandidates.Add(friendshipCandidate);
				}
				if (TryBuildCourtshipCandidate(candidate, settings, out ProactiveCandidate courtshipCandidate))
				{
					stats.Courtship++;
					needCandidates.Add(courtshipCandidate);
				}
				if (TryBuildBanditSuppressionCandidate(candidate, settings, out ProactiveCandidate banditSuppressionCandidate))
				{
					stats.BanditSuppression++;
					needCandidates.Add(banditSuppressionCandidate);
				}
				if (TryBuildTerritorialInterrogationCandidate(candidate, settings, territorialSettlementSnapshots, out ProactiveCandidate territorialInterrogationCandidate))
				{
					stats.TerritorialInterrogation++;
					needCandidates.Add(territorialInterrogationCandidate);
				}
				if (TryBuildMarriageAlliancePressureCandidate(candidate, settings, out ProactiveCandidate marriageCandidate))
				{
					stats.MarriageAlliancePressure++;
					needCandidates.Add(marriageCandidate);
				}
				if (TryBuildRevengePressureCandidate(candidate, settings, out ProactiveCandidate revengeCandidate))
				{
					stats.RevengePressure++;
					needCandidates.Add(revengeCandidate);
				}
				if (TryBuildFiefGovernanceAnxietyCandidate(candidate, settings, out ProactiveCandidate fiefGovernanceCandidate))
				{
					stats.FiefGovernanceAnxiety++;
					needCandidates.Add(fiefGovernanceCandidate);
				}
				if (TryBuildAllySupportCandidate(candidate, settings, out ProactiveCandidate allySupportCandidate))
				{
					stats.AllySupport++;
					needCandidates.Add(allySupportCandidate);
				}
				if (TryBuildKingdomMercenaryInviteCandidate(candidate, settings, out ProactiveCandidate mercenaryInviteCandidate))
				{
					stats.KingdomMercenaryInvite++;
					needCandidates.Add(mercenaryInviteCandidate);
				}
				if (TryBuildKingdomVassalInviteCandidate(candidate, settings, out ProactiveCandidate vassalInviteCandidate))
				{
					stats.KingdomVassalInvite++;
					needCandidates.Add(vassalInviteCandidate);
				}
				if (TryBuildPoliticalAgendaCandidate(candidate, settings, out ProactiveCandidate politicalAgendaCandidate))
				{
					stats.PoliticalAgenda++;
					needCandidates.Add(politicalAgendaCandidate);
				}
				if (TryBuildPolicySupportCandidate(candidate, settings, out ProactiveCandidate policySupportCandidate))
				{
					stats.PolicySupport++;
					needCandidates.Add(policySupportCandidate);
				}
				if (TryBuildPoliticalRivalSuppressionCandidate(candidate, settings, out ProactiveCandidate politicalRivalSuppressionCandidate))
				{
					stats.PoliticalRivalSuppression++;
					needCandidates.Add(politicalRivalSuppressionCandidate);
				}
				if (TryBuildSettlementPurchaseCandidate(candidate, settings, out ProactiveCandidate settlementPurchaseCandidate))
				{
					stats.SettlementPurchase++;
					needCandidates.Add(settlementPurchaseCandidate);
				}
				if (TryBuildSettlementSaleCandidate(candidate, settings, out ProactiveCandidate settlementSaleCandidate))
				{
					stats.SettlementSale++;
					needCandidates.Add(settlementSaleCandidate);
				}
			}
			if (TryBuildDiplomacyCandidate(candidate, settings, out ProactiveCandidate diplomacyCandidate))
			{
				stats.Diplomacy++;
				needCandidates.Add(diplomacyCandidate);
			}
			ProactiveCandidate combinedCandidate = BuildCombinedNeedCandidate(needCandidates, settings);
			if (combinedCandidate != null)
			{
				stats.NeedCandidates++;
				if (TryEvaluateCandidateTrigger(combinedCandidate, settings, stats))
				{
					if (ProactiveCandidateScanOwner.IsCandidateBetter(combinedCandidate, bestCandidate))
					{
						bestCandidate = combinedCandidate;
					}
				}
			}
		}
		return bestCandidate;
	}

	private bool TryEvaluateCandidateTrigger(ProactiveCandidate candidate, DuelSettings settings, CandidateScanStats stats)
	{
		if (candidate == null)
		{
			stats?.AddSkip("candidate_null");
			return false;
		}
		float urgency = Clamp(candidate.NeedUrgency, 0f, 100f);
		float minUrgency = GetEffectiveMinNeedUrgency(settings);
		if (urgency < minUrgency)
		{
			if (stats != null)
			{
				stats.BelowMinUrgency++;
			}
			return false;
		}
		int baseChance = GetEffectiveChancePercent(settings);
		if (baseChance <= 0)
		{
			if (stats != null)
			{
				stats.TriggerRollFailed++;
			}
			return false;
		}
		float globalScale = Clamp(baseChance / 100f, 0f, 1f);
		bool knownMajor = PlayerNotorietyBehavior.HasObserverUnlockedPlayerMajorForExternal(candidate.Hero);
		int effectiveNotoriety = PlayerNotorietyBehavior.GetEffectiveNotorietyForExternal(candidate.Hero);
		float knownMultiplier = knownMajor ? GetEffectiveKnownMajorMultiplier(settings) : 1f;
		float typeFatigueMultiplier = Clamp(candidate.NeedTypeFatigueMultiplier, 0f, 1f);
		float typeWeightMultiplier = Clamp(candidate.NeedTypeWeightMultiplier, 0f, 1f);
		if (typeFatigueMultiplier < 0.999f && stats != null)
		{
			stats.TypeFatiguedCandidates++;
		}
		float needChance = Clamp(urgency * globalScale * knownMultiplier * typeFatigueMultiplier * typeWeightMultiplier, 0f, 100f);
		float notorietyChance = knownMajor
			? 0f
			: Clamp(effectiveNotoriety * GetEffectiveNotorietyChanceMultiplier(settings) * (urgency / 100f) * globalScale * typeFatigueMultiplier * typeWeightMultiplier, 0f, 100f);
		candidate.KnownMajorBeforeRequest = knownMajor;
		candidate.EffectiveNotorietyAtRequest = effectiveNotoriety;
		candidate.NeedDrivenChance = needChance;
		candidate.NotorietyDrivenChance = notorietyChance;
		candidate.SelectedNeedUrgency = urgency;
		if (RollPercent(needChance))
		{
			candidate.TriggerSource = TriggerSourceNeedDriven;
			if (stats != null)
			{
				stats.NeedDrivenTriggered++;
			}
			return true;
		}
		if (RollPercent(notorietyChance))
		{
			candidate.TriggerSource = TriggerSourceNotorietyDriven;
			if (stats != null)
			{
				stats.NotorietyDrivenTriggered++;
			}
			return true;
		}
		if (stats != null)
		{
			stats.TriggerRollFailed++;
		}
		return false;
	}

	private List<LetterNeedSnapshot> BuildLetterNeedSnapshots(Hero hero)
	{
		List<LetterNeedSnapshot> result = new List<LetterNeedSnapshot>();
		DuelSettings settings = DuelSettings.GetSettings();
		if (hero == null || hero == Hero.MainHero || hero.IsDead || hero.IsPrisoner || hero.IsFugitive || hero.PartyBelongedToAsPrisoner != null)
		{
			return result;
		}
		ProactiveCandidate source = BuildLetterNeedBaseCandidate(hero, settings);
		if (source == null)
		{
			return result;
		}
		List<ProactiveCandidate> candidates = new List<ProactiveCandidate>();
		if (source.Party != null)
		{
			AddLetterNeedCandidate(candidates, TryBuildFoodShortageCandidate, source, settings);
			AddLetterNeedCandidate(candidates, TryBuildMoneyShortageCandidate, source, settings);
			AddLetterNeedCandidate(candidates, TryBuildTroopShortageCandidate, source, settings);
			AddLetterNeedCandidate(candidates, TryBuildPrisonerOverloadCandidate, source, settings);
			AddLetterNeedCandidate(candidates, TryBuildLowMoraleCandidate, source, settings);
			AddLetterNeedCandidate(candidates, TryBuildMountShortageCandidate, source, settings);
			AddLetterNeedCandidate(candidates, TryBuildOverburdenedCandidate, source, settings);
		}
		AddLetterNeedCandidate(candidates, TryBuildClanCaptiveCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildClanFinanceStrainCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildClanServiceCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildRomanticInteractionCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildPolicyDiscussionCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildFriendshipCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildCourtshipCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildArmyJoinRequestCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildBanditSuppressionCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildPoliticalRivalSuppressionCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildSettlementPurchaseCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildSettlementSaleCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildMarriageAlliancePressureCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildRevengePressureCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildFiefGovernanceAnxietyCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildAllySupportCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildKingdomMercenaryInviteCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildKingdomVassalInviteCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildPoliticalAgendaCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildPolicySupportCandidate, source, settings);
		AddLetterNeedCandidate(candidates, TryBuildDiplomacyCandidate, source, settings);

		float nowDays = NowDays();
		foreach (ProactiveCandidate candidate in candidates
			.Where(x => x != null && x.NeedUrgency > 0f)
			.OrderByDescending(x => x.NeedUrgency))
		{
			string needType = NormalizeNeedType(candidate.NeedType);
			if (string.IsNullOrWhiteSpace(needType) || IsNeedTypeActiveForExternal(needType))
			{
				continue;
			}
			float remaining = GetNeedTypeFatigueRemainingDays(needType, nowDays);
			float fatigueMultiplier = remaining > 0f ? GetEffectiveNeedTypeFatigueMultiplier(settings) : 1f;
			result.Add(new LetterNeedSnapshot
			{
				NeedType = needType,
				DisplayName = GetNeedDisplayName(needType),
				Urgency = Clamp(candidate.NeedUrgency, 0f, 100f),
				TypeFatigueMultiplier = fatigueMultiplier,
				TypeWeightMultiplier = Clamp(GetEffectiveNeedTypeWeightMultiplier(needType, settings, allowTestModeOverride: false)
					* Clamp(candidate.IntrinsicNeedTypeWeightMultiplier, 0f, 1f), 0f, 1f),
				FactText = BuildLetterNeedFact(candidate),
				IntentText = BuildLetterNeedIntent(candidate)
			});
		}
		return result;
	}

	private delegate bool TryBuildLetterNeedCandidateDelegate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate);

	private static void AddLetterNeedCandidate(List<ProactiveCandidate> result, TryBuildLetterNeedCandidateDelegate builder, ProactiveCandidate source, DuelSettings settings)
	{
		if (result == null || builder == null)
		{
			return;
		}
		if (builder(source, settings, out ProactiveCandidate candidate) && candidate != null)
		{
			result.Add(candidate);
		}
	}

	private ProactiveCandidate BuildLetterNeedBaseCandidate(Hero hero, DuelSettings settings)
	{
		try
		{
			Clan clan = hero?.Clan;
			MobileParty party = hero?.PartyBelongedTo;
			if (party == null || !party.IsActive || party.LeaderHero != hero)
			{
				party = null;
			}
			int foodDays = party == null ? int.MaxValue : SafeFoodDays(party);
			int partyGold = party == null ? 0 : SafePartyTradeGold(party);
			int totalWage = party == null ? 0 : SafeTotalWage(party);
			float unpaidWages = party == null ? 0f : SafeUnpaidWages(party);
			int memberCount = party == null ? 0 : SafeMemberCount(party);
			int partySizeLimit = party == null ? 0 : SafePartySizeLimit(party);
			int prisonerCount = party == null ? 0 : SafePrisonerCount(party);
			int prisonerSizeLimit = party == null ? 0 : SafePrisonerSizeLimit(party);
			int inventoryCapacity = party == null ? 0 : SafeInventoryCapacity(party);
			float totalWeightCarried = party == null ? 0f : SafeTotalWeightCarried(party);
			int mountCount = party == null ? 0 : SafeMountCount(party);
			int packAnimalCount = party == null ? 0 : SafePackAnimalCount(party);
			ClanCaptiveSnapshot captive = GetCachedClanCaptiveSnapshot(hero);
			Kingdom kingdom = ResolveHeroKingdom(hero);
			MarriageAllianceSnapshot marriage = BuildMarriageAllianceSnapshot(hero);
			FiefGovernanceSnapshot fiefs = GetCachedFiefGovernanceSnapshot(clan, settings);
			AllySupportSnapshot allies = GetCachedAllySupportSnapshot(clan, kingdom, settings);
			RevengePressureSnapshot revenge = BuildRevengePressureSnapshot(hero, kingdom, captive, fiefs);
			KingdomManpowerNeedSnapshot manpower = GetCachedKingdomManpowerNeedSnapshot(kingdom);
			bool atWar = false;
			try
			{
				atWar = hero?.MapFaction != null && Hero.MainHero?.MapFaction != null && hero.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction);
			}
			catch
			{
			}
			return new ProactiveCandidate
			{
				Party = party,
				Hero = hero,
				Distance = -1f,
				FoodDays = foodDays,
				PartyGold = partyGold,
				TotalWage = totalWage,
				UnpaidWages = unpaidWages,
				WageDays = CalculateWageDays(partyGold, totalWage),
				MemberCount = memberCount,
				PartySizeLimit = partySizeLimit,
				PartySizeRatio = CalculatePartySizeRatio(memberCount, partySizeLimit),
				AvailableWageBudget = party == null ? 0 : SafeAvailableWageBudget(party),
				PrisonerCount = prisonerCount,
				PrisonerSizeLimit = prisonerSizeLimit,
				HeroPrisonerCount = party == null ? 0 : SafeHeroPrisonerCount(party),
				PrisonerSizeRatio = CalculatePrisonerSizeRatio(prisonerCount, prisonerSizeLimit),
				Morale = party == null ? 100f : SafeMorale(party),
				InventoryCapacity = inventoryCapacity,
				TotalWeightCarried = totalWeightCarried,
				CarryRatio = CalculateCarryRatio(totalWeightCarried, inventoryCapacity),
				MountCount = mountCount,
				PackAnimalCount = packAnimalCount,
				MountRatio = CalculateAnimalRatio(mountCount, memberCount),
				PackAnimalRatio = CalculateAnimalRatio(packAnimalCount, memberCount),
				ClanGold = SafeClanGold(clan),
				ClanDebtToKingdom = SafeClanDebtToKingdom(clan),
				CaptiveClanHeroCount = captive.Count,
				CaptiveClanHeroName = captive.FirstHeroName,
				CaptiveClanHeroHolderName = captive.FirstHolderName,
				CaptiveClanLeaderHeld = captive.LeaderHeld,
				MarriageAdultClanHeroCount = marriage.AdultClanHeroCount,
				MarriageUnmarriedAdultCount = marriage.UnmarriedAdultCount,
				MarriageFirstUnmarriedName = marriage.FirstUnmarriedName,
				MarriageRequesterUnmarried = marriage.RequesterUnmarried,
				RevengePressureScore = revenge.PressureScore,
				RevengeTargetName = revenge.TargetName,
				RevengeReasonText = revenge.ReasonText,
				FiefProblemCount = fiefs.ProblemCount,
				FiefProblemName = fiefs.FirstProblemName,
				FiefLoyalty = fiefs.LowestLoyalty,
				FiefSecurity = fiefs.LowestSecurity,
				FiefGarrisonCount = fiefs.LowestGarrisonCount,
				FiefIssueText = fiefs.IssueText,
				FiefUnderAttack = fiefs.UnderAttack,
				ClanInfluence = allies.ClanInfluence,
				FriendlyClanCount = allies.FriendlyClanCount,
				HostileClanCount = allies.HostileClanCount,
				TargetKingdom = kingdom,
				TargetKingdomId = GetKingdomKey(kingdom),
				TargetKingdomName = GetKingdomName(kingdom),
				PlayerClanTier = SafePlayerClanTier(),
				TargetHeroIsKingdomLeader = IsKingdomLeader(hero, kingdom),
				TargetClanCanOfferKingdomService = CanClanRepresentKingdom(clan, kingdom),
				KingdomFormalVassalClanCount = manpower.FormalVassalClanCount,
				KingdomMercenaryClanCount = manpower.MercenaryClanCount,
				KingdomFiefScore = manpower.FiefScore,
				KingdomWarKingdomCount = manpower.WarKingdomCount,
				KingdomPowerRatioToEnemies = manpower.PowerRatioToEnemies,
				KingdomTargetMercenaryClanCount = manpower.TargetMercenaryClanCount,
				KingdomTargetVassalClanCount = manpower.TargetVassalClanCount,
				KingdomNeedsMercenaries = manpower.NeedsMercenaries,
				KingdomNeedsVassals = manpower.NeedsVassals,
				KingdomMercenaryNeedUrgency = manpower.MercenaryNeedUrgency,
				KingdomVassalNeedUrgency = manpower.VassalNeedUrgency,
				AtWarWithPlayer = atWar
			};
		}
		catch
		{
			return null;
		}
	}

	private static string BuildLetterNeedFact(ProactiveCandidate candidate)
	{
		string npcName = candidate?.Hero?.Name?.ToString() ?? "NPC";
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(candidate?.Hero) ?? Hero.MainHero?.Name?.ToString() ?? SocialSourceTexts.Get("S001");
		string needType = NormalizeNeedType(candidate?.NeedType);
		if (string.Equals(needType, NeedPolicyDiscussion, StringComparison.OrdinalIgnoreCase))
		{
			return SocialSourceTexts.Get("S308") + npcName + SocialSourceTexts.Get("S309") + playerName + "。"
				+ BuildPolicyDiscussionSituation(new PolicyDiscussionSnapshot
				{
					PolicyId = candidate?.PolicyDiscussionPolicyId,
					PolicyName = candidate?.PolicyDiscussionPolicyName,
					PolicyContent = candidate?.PolicyDiscussionPolicyContent,
					KingdomName = candidate?.PolicyDiscussionKingdomName,
					PublishedDay = candidate?.PolicyDiscussionPublishedDay ?? 0
				}, npcName, playerName)
				+ SocialSourceTexts.Get("S310") + playerName + SocialSourceTexts.Get("S311");
		}
		string evidence = BuildLetterNeedEvidence(candidate, needType);
		return SocialSourceTexts.Get("S308") + npcName + SocialSourceTexts.Get("S309") + playerName + "。" + evidence + SocialSourceTexts.Get("S312") + playerName + SocialSourceTexts.Get("S313");
	}

	private static string BuildLetterNeedIntent(ProactiveCandidate candidate)
	{
		string needType = NormalizeNeedType(candidate?.NeedType);
		return AIConfigHandler.GetProactiveNpcRequestLetterIntent(needType);
	}

	public static bool IsRomanticInteractionEligibleForExternal(Hero hero)
	{
		return IsRomanticInteractionEligible(hero, out _);
	}

	public static bool IsRomanticInteractionOnCooldownForExternal()
	{
		try
		{
			ProactiveNpcRequestBehavior instance = Instance;
			return instance != null && instance.GetNeedTypeFatigueRemainingDays(NeedRomanticInteraction, NowDays()) > 0f;
		}
		catch
		{
			return false;
		}
	}

	public static bool IsRomanticInteractionUnavailableForExternal()
	{
		return IsRomanticInteractionOnCooldownForExternal()
			|| IsNeedTypeActiveForExternal(NeedRomanticInteraction)
			|| CourierDeliveryBehavior.IsInboundNeedTypeReservedForExternal(NeedRomanticInteraction);
	}

	public static void RecordRomanticInteractionForExternal(string source)
	{
		try
		{
			Instance?.RecordNeedTypeFatigue(NeedRomanticInteraction, source ?? "romantic_interaction");
		}
		catch (Exception ex)
		{
			Logger.Log("ProactiveNpcRequest", "record romantic interaction cooldown failed source=" + (source ?? "") + " error=" + ex.Message);
		}
	}

	public static bool TryBuildPolicyDiscussionCompanionMotiveForExternal(Hero hero, out string factText, out string intentText, out float urgency)
	{
		factText = "";
		intentText = "";
		urgency = 0f;
		try
		{
			return Instance?.TryBuildPolicyDiscussionCompanionMotive(hero, out factText, out intentText, out urgency) == true;
		}
		catch (Exception ex)
		{
			Logger.Log("ProactiveNpcRequest", "policy discussion companion motive failed hero=" + GetHeroKey(hero) + " error=" + ex.Message);
			return false;
		}
	}

	public static bool IsPolicyDiscussionUnavailableForExternal()
	{
		try
		{
			ProactiveNpcRequestBehavior instance = Instance;
			return instance != null
				&& (instance.GetNeedTypeFatigueRemainingDays(NeedPolicyDiscussion, NowDays()) > 0f
					|| IsNeedTypeActiveForExternal(NeedPolicyDiscussion)
					|| CourierDeliveryBehavior.IsInboundNeedTypeReservedForExternal(NeedPolicyDiscussion));
		}
		catch
		{
			return false;
		}
	}

	public static void RecordPolicyDiscussionForExternal(string source)
	{
		try
		{
			Instance?.RecordNeedTypeFatigue(NeedPolicyDiscussion, source ?? "policy_discussion");
		}
		catch (Exception ex)
		{
			Logger.Log("ProactiveNpcRequest", "record policy discussion cooldown failed source=" + (source ?? "") + " error=" + ex.Message);
		}
	}

	public static bool IsGreetingUnavailableForExternal()
	{
		return IsGreetingOnCooldown()
			|| IsNeedTypeActiveForExternal(NeedGreeting)
			|| CourierDeliveryBehavior.IsInboundNeedTypeReservedForExternal(NeedGreeting);
	}

	private static string BuildLetterNeedEvidence(ProactiveCandidate candidate, string needType)
	{
		if (candidate == null)
		{
			return "";
		}
		if (string.Equals(needType, NeedFoodShortage, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S314");
		if (string.Equals(needType, NeedMoneyShortage, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S315");
		if (string.Equals(needType, NeedTroopShortage, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S316");
		if (string.Equals(needType, NeedPrisonerOverload, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S317");
		if (string.Equals(needType, NeedClanCaptive, StringComparison.OrdinalIgnoreCase)) return string.IsNullOrWhiteSpace(candidate.CaptiveClanHeroName) ? SocialSourceTexts.Get("S318") : candidate.CaptiveClanHeroName + SocialSourceTexts.Get("S319");
		if (string.Equals(needType, NeedLowMorale, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S320");
		if (string.Equals(needType, NeedMountShortage, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S321");
		if (string.Equals(needType, NeedOverburdened, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S322");
		if (string.Equals(needType, NeedClanFinanceStrain, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S323");
		if (string.Equals(needType, NeedClanService, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S324");
		if (string.Equals(needType, NeedRomanticInteraction, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S325");
		if (string.Equals(needType, NeedGreeting, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S326");
		if (string.Equals(needType, NeedFriendship, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S327");
		if (string.Equals(needType, NeedCourtship, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S328");
		if (string.Equals(needType, NeedArmyJoinRequest, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S329");
		if (string.Equals(needType, NeedBanditSuppression, StringComparison.OrdinalIgnoreCase)) return (candidate.BanditSuppressionSettlementName ?? SocialSourceTexts.Get("S330")) + SocialSourceTexts.Get("S331");
		if (string.Equals(needType, NeedPoliticalRivalSuppression, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S332") + (candidate.PoliticalRivalSuppressionRivalClanName ?? SocialSourceTexts.Get("S333")) + SocialSourceTexts.Get("S334");
		if (string.Equals(needType, NeedSettlementPurchase, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S335") + (candidate.SettlementPurchasePlayerFiefsText ?? SocialSourceTexts.Get("S336")) + "。";
		if (string.Equals(needType, NeedSettlementSale, StringComparison.OrdinalIgnoreCase)) return (candidate.SettlementSaleTargetSettlementName ?? SocialSourceTexts.Get("S337")) + SocialSourceTexts.Get("S338") + (candidate.SettlementSaleForeignFactionName ?? SocialSourceTexts.Get("S339")) + SocialSourceTexts.Get("S045") + (candidate.SettlementSaleForeignSettlementName ?? SocialSourceTexts.Get("S340")) + SocialSourceTexts.Get("S341");
		if (string.Equals(needType, NeedTerritorialInterrogation, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S342") + (candidate.TerritorialInterrogationSettlementName ?? SocialSourceTexts.Get("S343")) + SocialSourceTexts.Get("S344");
		if (string.Equals(needType, NeedMarriageAlliancePressure, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S345");
		if (string.Equals(needType, NeedRevengePressure, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S346") + (candidate.RevengeReasonText ?? SocialSourceTexts.Get("S347")) + SocialSourceTexts.Get("S348") + (string.IsNullOrWhiteSpace(candidate.RevengeTargetName) ? "。" : SocialSourceTexts.Get("S349") + candidate.RevengeTargetName + "。 ");
		if (string.Equals(needType, NeedFiefGovernanceAnxiety, StringComparison.OrdinalIgnoreCase)) return (candidate.FiefProblemName ?? SocialSourceTexts.Get("S350")) + SocialSourceTexts.Get("S351") + (candidate.FiefIssueText ?? SocialSourceTexts.Get("S352")) + SocialSourceTexts.Get("S353");
		if (string.Equals(needType, NeedAllySupport, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S354");
		if (string.Equals(needType, NeedKingdomMercenaryInvite, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S355");
		if (string.Equals(needType, NeedKingdomVassalInvite, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S356");
		if (string.Equals(needType, NeedPoliticalAgenda, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S357");
		if (string.Equals(needType, NeedPolicySupport, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S358") + (candidate.PolicySupportPolicyName ?? SocialSourceTexts.Get("S359")) + "》。";
		if (string.Equals(needType, NeedDiplomacy, StringComparison.OrdinalIgnoreCase)) return SocialSourceTexts.Get("S360");
		return "";
	}

	private static string GetNeedDisplayName(string needType)
	{
		if (string.Equals(needType, NeedFoodShortage, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_01");
		if (string.Equals(needType, NeedMoneyShortage, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_02");
		if (string.Equals(needType, NeedTroopShortage, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_03");
		if (string.Equals(needType, NeedPrisonerOverload, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_04");
		if (string.Equals(needType, NeedClanCaptive, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_05");
		if (string.Equals(needType, NeedLowMorale, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_06");
		if (string.Equals(needType, NeedMountShortage, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_07");
		if (string.Equals(needType, NeedOverburdened, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_08");
		if (string.Equals(needType, NeedClanFinanceStrain, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_09");
		if (string.Equals(needType, NeedClanService, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_10");
		if (string.Equals(needType, NeedRomanticInteraction, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_11");
		if (string.Equals(needType, NeedGreeting, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_12");
		if (string.Equals(needType, NeedPolicyDiscussion, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_13");
		if (string.Equals(needType, NeedFriendship, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_14");
		if (string.Equals(needType, NeedCourtship, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_15");
		if (string.Equals(needType, NeedArmyJoinRequest, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_16");
		if (string.Equals(needType, NeedBanditSuppression, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_17");
		if (string.Equals(needType, NeedPoliticalRivalSuppression, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_18");
		if (string.Equals(needType, NeedSettlementPurchase, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_19");
		if (string.Equals(needType, NeedSettlementSale, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_20");
		if (string.Equals(needType, NeedTerritorialInterrogation, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_21");
		if (string.Equals(needType, NeedMarriageAlliancePressure, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_22");
		if (string.Equals(needType, NeedRevengePressure, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_23");
		if (string.Equals(needType, NeedFiefGovernanceAnxiety, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_24");
		if (string.Equals(needType, NeedAllySupport, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_25");
		if (string.Equals(needType, NeedKingdomMercenaryInvite, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_26");
		if (string.Equals(needType, NeedKingdomVassalInvite, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_27");
		if (string.Equals(needType, NeedPoliticalAgenda, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_28");
		if (string.Equals(needType, NeedPolicySupport, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_29");
		if (string.Equals(needType, NeedDiplomacy, StringComparison.OrdinalIgnoreCase)) return SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_30");
		return string.IsNullOrWhiteSpace(needType) ? SocialPromptTexts.Get("AF_SOCIAL_UI_ProactiveNeed_31") : needType;
	}

	private bool TryBuildBaseCandidate(MobileParty party, MobileParty mainParty, DuelSettings settings, out ProactiveCandidate candidate, out string skipReason)
	{
		candidate = null;
		skipReason = "";
		try
		{
			if (party == null || mainParty == null || party == mainParty || !party.IsActive || !party.IsVisible || !party.IsLordParty)
			{
				skipReason = "not_active_visible_lord_party";
				return false;
			}
			float maxDistance = GetProactiveContactRange(mainParty);
			// Recheck after the incremental snapshot: either party may have moved.
			if (!(party.Position.DistanceSquared(mainParty.Position) <= maxDistance * maxDistance))
			{
				skipReason = "out_of_range";
				return false;
			}
			Hero hero = party.LeaderHero;
			if (hero == null || hero == Hero.MainHero || !hero.IsLord || hero.IsPrisoner || hero.IsDead || hero.Clan == null)
			{
				skipReason = "leader_invalid";
				return false;
			}
			if (party.MapEvent != null || party.CurrentSettlement != null || party.Army != null || party.BesiegedSettlement != null || MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(party))
			{
				skipReason = "party_busy_or_invalid_location";
				return false;
			}
			if (TryGetPlayerNativeActivityBusyReason(mainParty, out string mainBusyReason))
			{
				skipReason = "main_party_" + mainBusyReason;
				return false;
			}
			if (mainParty.MapEvent != null || mainParty.CurrentSettlement != null || MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(mainParty))
			{
				skipReason = "main_party_busy_or_invalid_location";
				return false;
			}
			if (party.MapFaction == null || mainParty.MapFaction == null)
			{
				skipReason = "missing_faction";
				return false;
			}
			bool atWarWithPlayer = party.MapFaction.IsAtWarWith(mainParty.MapFaction);
			string heroKey = GetHeroKey(hero);
			float nowDays = NowDays();
			if (_cooldownOwner.IsHeroOnCooldown(heroKey, nowDays))
			{
				skipReason = "hero_cooldown";
				return false;
			}
			Kingdom targetKingdom = ResolveHeroKingdom(hero);
			if (atWarWithPlayer && !CanBuildWartimeDiplomacyCandidate(hero, targetKingdom))
			{
				skipReason = "war_non_diplomacy";
				return false;
			}
			float distance = GetDistanceToMainParty(party, mainParty);
			if (distance < 0f)
			{
				skipReason = "distance_invalid";
				return false;
			}
			if (!(distance <= maxDistance))
			{
				skipReason = "out_of_range";
				return false;
			}
			int foodDays = SafeFoodDays(party);
			int partyGold = SafePartyTradeGold(party);
			int totalWage = SafeTotalWage(party);
			float unpaidWages = SafeUnpaidWages(party);
			int memberCount = SafeMemberCount(party);
			int partySizeLimit = SafePartySizeLimit(party);
			int availableWageBudget = SafeAvailableWageBudget(party);
			int prisonerCount = SafePrisonerCount(party);
			int prisonerSizeLimit = SafePrisonerSizeLimit(party);
			int heroPrisonerCount = SafeHeroPrisonerCount(party);
			float morale = SafeMorale(party);
			int inventoryCapacity = SafeInventoryCapacity(party);
			float totalWeightCarried = SafeTotalWeightCarried(party);
			int mountCount = SafeMountCount(party);
			int packAnimalCount = SafePackAnimalCount(party);
			int clanGold = SafeClanGold(hero.Clan);
			int clanDebtToKingdom = SafeClanDebtToKingdom(hero.Clan);
			ClanCaptiveSnapshot captiveSnapshot = GetCachedClanCaptiveSnapshot(hero);
			MarriageAllianceSnapshot marriageSnapshot = BuildMarriageAllianceSnapshot(hero);
			FiefGovernanceSnapshot fiefGovernanceSnapshot = GetCachedFiefGovernanceSnapshot(hero.Clan, settings);
			AllySupportSnapshot allySupportSnapshot = GetCachedAllySupportSnapshot(hero.Clan, targetKingdom, settings);
			RevengePressureSnapshot revengeSnapshot = BuildRevengePressureSnapshot(hero, targetKingdom, captiveSnapshot, fiefGovernanceSnapshot);
			KingdomManpowerNeedSnapshot kingdomNeed = GetCachedKingdomManpowerNeedSnapshot(targetKingdom);
			int playerClanTier = SafePlayerClanTier();
			bool targetHeroIsKingdomLeader = IsKingdomLeader(hero, targetKingdom);
			bool targetClanCanOfferKingdomService = CanClanRepresentKingdom(hero.Clan, targetKingdom);
			candidate = new ProactiveCandidate
			{
				Party = party,
				Hero = hero,
				Distance = distance,
				FoodDays = foodDays,
				PartyGold = partyGold,
				TotalWage = totalWage,
				UnpaidWages = unpaidWages,
				WageDays = CalculateWageDays(partyGold, totalWage),
				MemberCount = memberCount,
				PartySizeLimit = partySizeLimit,
				PartySizeRatio = CalculatePartySizeRatio(memberCount, partySizeLimit),
				AvailableWageBudget = availableWageBudget,
				PrisonerCount = prisonerCount,
				PrisonerSizeLimit = prisonerSizeLimit,
				HeroPrisonerCount = heroPrisonerCount,
				PrisonerSizeRatio = CalculatePrisonerSizeRatio(prisonerCount, prisonerSizeLimit),
				Morale = morale,
				InventoryCapacity = inventoryCapacity,
				TotalWeightCarried = totalWeightCarried,
				CarryRatio = CalculateCarryRatio(totalWeightCarried, inventoryCapacity),
				MountCount = mountCount,
				PackAnimalCount = packAnimalCount,
				MountRatio = CalculateAnimalRatio(mountCount, memberCount),
				PackAnimalRatio = CalculateAnimalRatio(packAnimalCount, memberCount),
				ClanGold = clanGold,
				ClanDebtToKingdom = clanDebtToKingdom,
				CaptiveClanHeroCount = captiveSnapshot.Count,
				CaptiveClanHeroName = captiveSnapshot.FirstHeroName,
				CaptiveClanHeroHolderName = captiveSnapshot.FirstHolderName,
				CaptiveClanLeaderHeld = captiveSnapshot.LeaderHeld,
				MarriageAdultClanHeroCount = marriageSnapshot.AdultClanHeroCount,
				MarriageUnmarriedAdultCount = marriageSnapshot.UnmarriedAdultCount,
				MarriageFirstUnmarriedName = marriageSnapshot.FirstUnmarriedName,
				MarriageRequesterUnmarried = marriageSnapshot.RequesterUnmarried,
				RevengePressureScore = revengeSnapshot.PressureScore,
				RevengeTargetName = revengeSnapshot.TargetName,
				RevengeReasonText = revengeSnapshot.ReasonText,
				FiefProblemCount = fiefGovernanceSnapshot.ProblemCount,
				FiefProblemName = fiefGovernanceSnapshot.FirstProblemName,
				FiefLoyalty = fiefGovernanceSnapshot.LowestLoyalty,
				FiefSecurity = fiefGovernanceSnapshot.LowestSecurity,
				FiefGarrisonCount = fiefGovernanceSnapshot.LowestGarrisonCount,
				FiefIssueText = fiefGovernanceSnapshot.IssueText,
				FiefUnderAttack = fiefGovernanceSnapshot.UnderAttack,
				ClanInfluence = allySupportSnapshot.ClanInfluence,
				FriendlyClanCount = allySupportSnapshot.FriendlyClanCount,
				HostileClanCount = allySupportSnapshot.HostileClanCount,
				TargetKingdom = targetKingdom,
				TargetKingdomId = GetKingdomKey(targetKingdom),
				TargetKingdomName = GetKingdomName(targetKingdom),
				PlayerClanTier = playerClanTier,
				TargetHeroIsKingdomLeader = targetHeroIsKingdomLeader,
				TargetClanCanOfferKingdomService = targetClanCanOfferKingdomService,
				KingdomFormalVassalClanCount = kingdomNeed.FormalVassalClanCount,
				KingdomMercenaryClanCount = kingdomNeed.MercenaryClanCount,
				KingdomFiefScore = kingdomNeed.FiefScore,
				KingdomWarKingdomCount = kingdomNeed.WarKingdomCount,
				KingdomPowerRatioToEnemies = kingdomNeed.PowerRatioToEnemies,
				KingdomTargetMercenaryClanCount = kingdomNeed.TargetMercenaryClanCount,
				KingdomTargetVassalClanCount = kingdomNeed.TargetVassalClanCount,
				KingdomNeedsMercenaries = kingdomNeed.NeedsMercenaries,
				KingdomNeedsVassals = kingdomNeed.NeedsVassals,
				KingdomMercenaryNeedUrgency = kingdomNeed.MercenaryNeedUrgency,
				KingdomVassalNeedUrgency = kingdomNeed.VassalNeedUrgency,
				AtWarWithPlayer = atWarWithPlayer
			};
			return true;
		}
		catch
		{
			skipReason = "exception";
			return false;
		}
	}

	private ProactiveCandidate TryBuildNeedCandidate(ProactiveCandidate source, DuelSettings settings, string needType, float urgency)
	{
		if (source == null || string.IsNullOrWhiteSpace(needType))
		{
			return null;
		}
		if (CourierDeliveryBehavior.IsInboundNeedTypeReservedForExternal(needType))
		{
			return null;
		}
		if (!IsPlayerEligibleForProactiveNeed(source, needType, out string ineligibleReason))
		{
			Logger.LogVerbose("ProactiveNpcRequest", "need_player_ineligible", () => "need skipped because player is ineligible. need=" + needType + " hero=" + (source.Hero?.StringId ?? "") + " reason=" + ineligibleReason, 30.0);
			return null;
		}
		return new ProactiveCandidate
		{
			Party = source.Party,
			Hero = source.Hero,
			Distance = source.Distance,
			FoodDays = source.FoodDays,
			PartyGold = source.PartyGold,
			TotalWage = source.TotalWage,
			UnpaidWages = source.UnpaidWages,
			WageDays = source.WageDays,
			MemberCount = source.MemberCount,
			PartySizeLimit = source.PartySizeLimit,
			PartySizeRatio = source.PartySizeRatio,
			AvailableWageBudget = source.AvailableWageBudget,
			PrisonerCount = source.PrisonerCount,
			PrisonerSizeLimit = source.PrisonerSizeLimit,
			HeroPrisonerCount = source.HeroPrisonerCount,
			PrisonerSizeRatio = source.PrisonerSizeRatio,
			Morale = source.Morale,
			InventoryCapacity = source.InventoryCapacity,
			TotalWeightCarried = source.TotalWeightCarried,
			CarryRatio = source.CarryRatio,
			MountCount = source.MountCount,
			PackAnimalCount = source.PackAnimalCount,
			MountRatio = source.MountRatio,
			PackAnimalRatio = source.PackAnimalRatio,
			ClanGold = source.ClanGold,
			ClanDebtToKingdom = source.ClanDebtToKingdom,
			ClanServiceTargetClanName = source.ClanServiceTargetClanName,
			ClanServiceCurrentKingName = source.ClanServiceCurrentKingName,
			ClanServicePlayerRelation = source.ClanServicePlayerRelation,
			ClanServiceCurrentKingRelation = source.ClanServiceCurrentKingRelation,
			ClanServiceRelationGap = source.ClanServiceRelationGap,
			RomanticInteractionPrivateRelation = source.RomanticInteractionPrivateRelation,
			GreetingPrivateRelation = source.GreetingPrivateRelation,
			ArmyJoinRequestArmyName = source.ArmyJoinRequestArmyName,
			ArmyJoinRequestOwnStrength = source.ArmyJoinRequestOwnStrength,
			ArmyJoinRequestEnemyStrength = source.ArmyJoinRequestEnemyStrength,
			ArmyJoinRequestEnemyKingdomCount = source.ArmyJoinRequestEnemyKingdomCount,
			ArmyJoinRequestOwnToEnemyRatio = source.ArmyJoinRequestOwnToEnemyRatio,
			BanditSuppressionSettlementName = source.BanditSuppressionSettlementName,
			BanditSuppressionBanditCount = source.BanditSuppressionBanditCount,
			BanditSuppressionRadius = source.BanditSuppressionRadius,
			BanditSuppressionTrust = source.BanditSuppressionTrust,
			BanditSuppressionPrivateRelation = source.BanditSuppressionPrivateRelation,
			PoliticalRivalSuppressionKingdomName = source.PoliticalRivalSuppressionKingdomName,
			PoliticalRivalSuppressionRequesterClanName = source.PoliticalRivalSuppressionRequesterClanName,
			PoliticalRivalSuppressionPlayerClanRelation = source.PoliticalRivalSuppressionPlayerClanRelation,
			PoliticalRivalSuppressionRivalClanName = source.PoliticalRivalSuppressionRivalClanName,
			PoliticalRivalSuppressionRivalClanRelation = source.PoliticalRivalSuppressionRivalClanRelation,
			PolicySupportKingdomName = source.PolicySupportKingdomName,
			PolicySupportPlayerClanRelation = source.PolicySupportPlayerClanRelation,
			PolicySupportPolicyName = source.PolicySupportPolicyName,
			PolicySupportDescription = source.PolicySupportDescription,
			PolicySupportEffects = source.PolicySupportEffects,
			PolicySupportScore = source.PolicySupportScore,
			PolicySupportHasPendingDecision = source.PolicySupportHasPendingDecision,
			PolicyDiscussionPolicyId = source.PolicyDiscussionPolicyId,
			PolicyDiscussionPolicyName = source.PolicyDiscussionPolicyName,
			PolicyDiscussionPolicyContent = source.PolicyDiscussionPolicyContent,
			PolicyDiscussionKingdomName = source.PolicyDiscussionKingdomName,
			PolicyDiscussionPublishedDay = source.PolicyDiscussionPublishedDay,
			SettlementPurchaseKingdomName = source.SettlementPurchaseKingdomName,
			SettlementPurchasePlayerTownCount = source.SettlementPurchasePlayerTownCount,
			SettlementPurchasePlayerCastleCount = source.SettlementPurchasePlayerCastleCount,
			SettlementPurchasePlayerFiefsText = source.SettlementPurchasePlayerFiefsText,
			SettlementPurchaseNpcFiefCount = source.SettlementPurchaseNpcFiefCount,
			SettlementPurchaseNpcTownCount = source.SettlementPurchaseNpcTownCount,
			SettlementPurchaseNpcCastleCount = source.SettlementPurchaseNpcCastleCount,
			SettlementSaleKingdomName = source.SettlementSaleKingdomName,
			SettlementSalePlayerClanRelation = source.SettlementSalePlayerClanRelation,
			SettlementSaleNpcFiefCount = source.SettlementSaleNpcFiefCount,
			SettlementSaleTargetSettlementName = source.SettlementSaleTargetSettlementName,
			SettlementSaleTargetSettlementType = source.SettlementSaleTargetSettlementType,
			SettlementSaleTargetDailyIncome = source.SettlementSaleTargetDailyIncome,
			SettlementSaleHighestFamilyDailyIncome = source.SettlementSaleHighestFamilyDailyIncome,
			SettlementSaleForeignSettlementName = source.SettlementSaleForeignSettlementName,
			SettlementSaleForeignFactionName = source.SettlementSaleForeignFactionName,
			SettlementSaleBorderDistance = source.SettlementSaleBorderDistance,
			SettlementSaleBorderRadius = source.SettlementSaleBorderRadius,
			TerritorialInterrogationEligible = source.TerritorialInterrogationEligible,
			TerritorialInterrogationKingdomName = source.TerritorialInterrogationKingdomName,
			TerritorialInterrogationSettlementName = source.TerritorialInterrogationSettlementName,
			TerritorialInterrogationSettlementDistance = source.TerritorialInterrogationSettlementDistance,
			TerritorialInterrogationNpcCultureName = source.TerritorialInterrogationNpcCultureName,
			TerritorialInterrogationCultureNotoriety = source.TerritorialInterrogationCultureNotoriety,
			CaptiveClanHeroCount = source.CaptiveClanHeroCount,
			CaptiveClanHeroName = source.CaptiveClanHeroName,
			CaptiveClanHeroHolderName = source.CaptiveClanHeroHolderName,
			CaptiveClanLeaderHeld = source.CaptiveClanLeaderHeld,
			MarriageAdultClanHeroCount = source.MarriageAdultClanHeroCount,
			MarriageUnmarriedAdultCount = source.MarriageUnmarriedAdultCount,
			MarriageFirstUnmarriedName = source.MarriageFirstUnmarriedName,
			MarriageRequesterUnmarried = source.MarriageRequesterUnmarried,
			RevengePressureScore = source.RevengePressureScore,
			RevengeTargetName = source.RevengeTargetName,
			RevengeReasonText = source.RevengeReasonText,
			FiefProblemCount = source.FiefProblemCount,
			FiefProblemName = source.FiefProblemName,
			FiefLoyalty = source.FiefLoyalty,
			FiefSecurity = source.FiefSecurity,
			FiefGarrisonCount = source.FiefGarrisonCount,
			FiefIssueText = source.FiefIssueText,
			FiefUnderAttack = source.FiefUnderAttack,
			ClanInfluence = source.ClanInfluence,
			FriendlyClanCount = source.FriendlyClanCount,
			HostileClanCount = source.HostileClanCount,
			TargetKingdom = source.TargetKingdom,
			TargetKingdomId = source.TargetKingdomId,
			TargetKingdomName = source.TargetKingdomName,
			PlayerClanTier = source.PlayerClanTier,
			TargetHeroIsKingdomLeader = source.TargetHeroIsKingdomLeader,
			TargetClanCanOfferKingdomService = source.TargetClanCanOfferKingdomService,
			KingdomFormalVassalClanCount = source.KingdomFormalVassalClanCount,
			KingdomMercenaryClanCount = source.KingdomMercenaryClanCount,
			KingdomFiefScore = source.KingdomFiefScore,
			KingdomWarKingdomCount = source.KingdomWarKingdomCount,
			KingdomPowerRatioToEnemies = source.KingdomPowerRatioToEnemies,
			KingdomTargetMercenaryClanCount = source.KingdomTargetMercenaryClanCount,
			KingdomTargetVassalClanCount = source.KingdomTargetVassalClanCount,
			KingdomNeedsMercenaries = source.KingdomNeedsMercenaries,
			KingdomNeedsVassals = source.KingdomNeedsVassals,
			KingdomMercenaryNeedUrgency = source.KingdomMercenaryNeedUrgency,
			KingdomVassalNeedUrgency = source.KingdomVassalNeedUrgency,
			AtWarWithPlayer = source.AtWarWithPlayer,
			NeedType = needType,
			NeedTypes = new List<string> { needType },
			NeedUrgency = urgency,
			IsTestFallback = source.IsTestFallback
		};
	}

	private static bool CanBuildWartimeDiplomacyCandidate(Hero hero, Kingdom targetKingdom)
	{
		try
		{
			Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
			return hero != null
				&& targetKingdom != null
				&& playerKingdom != null
				&& !playerKingdom.IsEliminated
				&& hero == targetKingdom.RulingClan?.Leader
				&& Hero.MainHero == playerKingdom.RulingClan?.Leader
				&& targetKingdom != playerKingdom;
		}
		catch
		{
			return false;
		}
	}

	private ProactiveCandidate BuildCombinedNeedCandidate(List<ProactiveCandidate> needCandidates, DuelSettings settings)
	{
		if (needCandidates == null || needCandidates.Count <= 0)
		{
			return null;
		}
		float nowDays = NowDays();
		foreach (ProactiveCandidate candidate in needCandidates.Where(c => c != null))
		{
			candidate.NeedTypeFatigueRemainingDays = GetNeedTypeFatigueRemainingDays(candidate.NeedType, nowDays);
			candidate.NeedTypeFatigueMultiplier = candidate.NeedTypeFatigueRemainingDays > 0f
				? GetEffectiveNeedTypeFatigueMultiplier(settings)
				: 1f;
			candidate.NeedTypeWeightMultiplier = Clamp(GetEffectiveNeedTypeWeightMultiplier(candidate.NeedType, settings)
				* Clamp(candidate.IntrinsicNeedTypeWeightMultiplier, 0f, 1f), 0f, 1f);
		}
		List<ProactiveCandidate> ordered = needCandidates
			.Where(c => c != null && !string.IsNullOrWhiteSpace(c.NeedType) && IsPlayerEligibleForProactiveNeed(c, c.NeedType, out _))
			.OrderByDescending(ProactiveCandidateScanOwner.GetWeightedUrgency)
			.ThenByDescending(c => c.NeedUrgency)
			.ThenByDescending(c => GetNeedPresentationPriority(c.NeedType))
			.ToList();
		if (ordered.Count <= 0)
		{
			return null;
		}
		ProactiveCandidate selected = ordered[0];
		string selectedNeedType = NormalizeNeedType(selected.NeedType);
		if (string.IsNullOrWhiteSpace(selectedNeedType))
		{
			return null;
		}
		selected.NeedTypes = new List<string> { selectedNeedType };
		selected.NeedType = selectedNeedType;
		selected.NeedUrgency = Clamp(selected.NeedUrgency, 0f, 100f);
		return selected;
	}

	private static List<string> FilterPlayerEligibleNeedTypes(ProactiveCandidate candidate, IEnumerable<string> needTypes, string fallbackNeedType)
	{
		List<string> raw = new List<string>();
		try
		{
			if (needTypes != null)
			{
				foreach (string needType in needTypes)
				{
					string normalized = NormalizeNeedType(needType);
					if (!string.IsNullOrWhiteSpace(normalized) && !raw.Any(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase)))
					{
						raw.Add(normalized);
					}
				}
			}
			string fallback = NormalizeNeedType(fallbackNeedType);
			if (raw.Count == 0 && !string.IsNullOrWhiteSpace(fallback))
			{
				raw.Add(fallback);
			}
			List<string> eligible = raw
				.Where(needType => IsPlayerEligibleForProactiveNeed(candidate, needType, out _))
				.ToList();
			return eligible.Count <= 0 ? new List<string>() : NormalizeSingleNeedType(eligible, eligible[0]);
		}
		catch
		{
			return new List<string>();
		}
	}

	private static bool IsPlayerEligibleForProactiveNeed(ProactiveCandidate candidate, string needType, out string reason)
	{
		reason = "";
		string normalized = NormalizeNeedType(needType);
		if (candidate == null || string.IsNullOrWhiteSpace(normalized))
		{
			reason = "candidate_or_need_invalid";
			return false;
		}
		try
		{
			if (string.Equals(normalized, NeedKingdomMercenaryInvite, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForMercenaryInvite(candidate, out reason);
			}
			if (string.Equals(normalized, NeedKingdomVassalInvite, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForVassalInvite(candidate, out reason);
			}
			if (string.Equals(normalized, NeedPoliticalAgenda, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForPoliticalAgendaRequest(candidate, out reason);
			}
			if (string.Equals(normalized, NeedPolicySupport, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForPolicySupport(candidate, out reason);
			}
			if (string.Equals(normalized, NeedDiplomacy, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForDiplomacyRequest(candidate, out reason);
			}
			if (string.Equals(normalized, NeedMarriageAlliancePressure, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForMarriageAllianceRequest(candidate, out reason);
			}
			if (string.Equals(normalized, NeedClanService, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForClanServiceRequest(candidate, out reason);
			}
			if (string.Equals(normalized, NeedRomanticInteraction, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForRomanticInteraction(candidate, out reason);
			}
			if (string.Equals(normalized, NeedGreeting, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForGreeting(candidate, out reason);
			}
			if (string.Equals(normalized, NeedFriendship, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForFriendship(candidate, out reason);
			}
			if (string.Equals(normalized, NeedCourtship, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForCourtship(candidate, out reason);
			}
			if (string.Equals(normalized, NeedArmyJoinRequest, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForArmyJoinRequest(candidate, out reason);
			}
			if (string.Equals(normalized, NeedBanditSuppression, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForBanditSuppression(candidate, out reason);
			}
			if (string.Equals(normalized, NeedPoliticalRivalSuppression, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForPoliticalRivalSuppression(candidate, out reason);
			}
			if (string.Equals(normalized, NeedSettlementPurchase, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForSettlementPurchase(candidate, out reason);
			}
			if (string.Equals(normalized, NeedSettlementSale, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForSettlementSale(candidate, out reason);
			}
			if (string.Equals(normalized, NeedTerritorialInterrogation, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForTerritorialInterrogation(candidate, out reason);
			}
			if (string.Equals(normalized, NeedAllySupport, StringComparison.OrdinalIgnoreCase))
			{
				return IsPlayerEligibleForAllySupportRequest(candidate, out reason);
			}
			return true;
		}
		catch (Exception ex)
		{
			reason = "exception:" + ex.Message;
			return false;
		}
	}

	private static bool IsPlayerEligibleForMercenaryInvite(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		Clan playerClan = Clan.PlayerClan;
		if (playerClan == null)
		{
			reason = "player_clan_missing";
			return false;
		}
		if (candidate.TargetKingdom == null || !candidate.TargetClanCanOfferKingdomService || !candidate.KingdomNeedsMercenaries)
		{
			reason = "target_cannot_offer_mercenary";
			return false;
		}
		if (candidate.PlayerClanTier < MercenaryInviteMinPlayerClanTier)
		{
			reason = "player_tier_below_mercenary";
			return false;
		}
		if (!HasMinimumTrustForKingdomServiceInvite(candidate.Hero))
		{
			reason = "npc_trust_below_kingdom_service_threshold";
			return false;
		}
		if (playerClan.Kingdom != null || playerClan.IsUnderMercenaryService)
		{
			reason = "player_already_serving";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForVassalInvite(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		Clan playerClan = Clan.PlayerClan;
		if (playerClan == null)
		{
			reason = "player_clan_missing";
			return false;
		}
		if (candidate.TargetKingdom == null || !candidate.TargetClanCanOfferKingdomService || !candidate.TargetHeroIsKingdomLeader || !candidate.KingdomNeedsVassals)
		{
			reason = "target_cannot_offer_vassalage";
			return false;
		}
		if (candidate.PlayerClanTier < VassalInviteMinPlayerClanTier)
		{
			reason = "player_tier_below_vassal";
			return false;
		}
		if (!HasMinimumTrustForKingdomServiceInvite(candidate.Hero))
		{
			reason = "npc_trust_below_kingdom_service_threshold";
			return false;
		}
		if (playerClan.Kingdom == null)
		{
			return true;
		}
		if (playerClan.IsUnderMercenaryService && playerClan.Kingdom == candidate.TargetKingdom)
		{
			return true;
		}
		reason = "player_not_independent_or_target_mercenary";
		return false;
	}

	private static bool HasMinimumTrustForKingdomServiceInvite(Hero hero)
	{
		try
		{
			int trust = Clamp(RewardSystemBehavior.Instance?.GetEffectiveTrust(hero) ?? 0, -100, 100);
			return trust >= KingdomServiceInviteMinNpcTrust;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPlayerEligibleForPoliticalAgendaRequest(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		Clan playerClan = Clan.PlayerClan;
		if (playerClan == null || candidate.TargetKingdom == null)
		{
			reason = "player_or_kingdom_missing";
			return false;
		}
		if (playerClan.Kingdom != candidate.TargetKingdom || playerClan.IsUnderMercenaryService)
		{
			reason = "player_not_formal_member_of_target_kingdom";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForDiplomacyRequest(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		Clan playerClan = Clan.PlayerClan;
		Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
		if (playerClan == null || playerKingdom == null || playerKingdom.IsEliminated || playerClan.IsUnderMercenaryService)
		{
			reason = "player_not_formal_kingdom_member";
			return false;
		}
		Clan npcClan = candidate?.Hero?.Clan;
		if (npcClan == null || npcClan == playerClan || npcClan.Kingdom != playerKingdom || npcClan.IsUnderMercenaryService)
		{
			reason = "target_not_same_kingdom_formal_lord";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForAllySupportRequest(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		Clan playerClan = Clan.PlayerClan;
		if (playerClan == null || candidate.TargetKingdom == null)
		{
			reason = "player_or_kingdom_missing";
			return false;
		}
		if (playerClan.Kingdom != candidate.TargetKingdom || playerClan.IsUnderMercenaryService)
		{
			reason = "player_cannot_vote_or_back_in_target_kingdom";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForMarriageAllianceRequest(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		Clan npcClan = candidate?.Hero?.Clan;
		Clan playerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
		if (npcClan == null || playerClan == null || npcClan == playerClan)
		{
			reason = "clan_invalid";
			return false;
		}
		List<Hero> npcCandidates = GetMarriageableClanHeroes(npcClan, includeMainHero: false);
		List<Hero> playerCandidates = GetMarriageableClanHeroes(playerClan, includeMainHero: true);
		if (npcCandidates.Count <= 0)
		{
			reason = "npc_clan_no_marriage_candidate";
			return false;
		}
		if (playerCandidates.Count <= 0)
		{
			reason = "player_clan_no_marriage_candidate";
			return false;
		}
		foreach (Hero npcHero in npcCandidates)
		{
			foreach (Hero playerHero in playerCandidates)
			{
				if (AreHeroesSuitableForMarriageRequest(npcHero, playerHero))
				{
					return true;
				}
			}
		}
		reason = "no_suitable_marriage_pair";
		return false;
	}

	private static bool IsPlayerEligibleForClanServiceRequest(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		if (!TryBuildClanServiceNeedSnapshot(candidate, out _))
		{
			reason = "clan_service_conditions_not_met";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForPolicySupport(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		ProactiveNpcRequestBehavior instance = Instance;
		if (instance == null || !instance.TryBuildPolicySupportSnapshot(candidate, out _))
		{
			reason = "policy_support_conditions_not_met";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForRomanticInteraction(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		if (IsRomanticInteractionUnavailableForExternal())
		{
			reason = "romantic_interaction_global_cooldown";
			return false;
		}
		if (!IsRomanticInteractionEligible(candidate?.Hero, out _))
		{
			reason = "romantic_interaction_conditions_not_met";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForTerritorialInterrogation(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		Kingdom playerKingdom = (Clan.PlayerClan ?? Hero.MainHero?.Clan)?.Kingdom;
		if (candidate?.TargetKingdom != null && playerKingdom == candidate.TargetKingdom)
		{
			reason = "player_serves_territorial_kingdom";
			return false;
		}
		if (IsTerritorialInterrogationOnCooldown())
		{
			reason = "territorial_interrogation_global_cooldown";
			return false;
		}
		if (candidate?.TerritorialInterrogationEligible == true)
		{
			return true;
		}
		if (!TryBuildTerritorialInterrogationSnapshot(candidate, DuelSettings.GetSettings(), null, out _))
		{
			reason = "territorial_interrogation_conditions_not_met";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForGreeting(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		if (IsGreetingOnCooldown())
		{
			reason = "greeting_global_cooldown";
			return false;
		}
		if (!IsGreetingEligible(candidate?.Hero, out _))
		{
			reason = "greeting_private_relation_too_low";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForFriendship(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		if (IsFriendshipOnCooldown())
		{
			reason = "friendship_global_cooldown";
			return false;
		}
		if (!TryBuildFriendshipNeedSnapshot(candidate, out _))
		{
			reason = "friendship_conditions_not_met";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForCourtship(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		if (IsCourtshipOnCooldown())
		{
			reason = "courtship_global_cooldown";
			return false;
		}
		if (!TryBuildCourtshipNeedSnapshot(candidate, out _))
		{
			reason = "courtship_conditions_not_met";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForArmyJoinRequest(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		if (!TryBuildArmyJoinRequestSnapshot(candidate, out _))
		{
			reason = "army_join_request_conditions_not_met";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForBanditSuppression(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		ProactiveNpcRequestBehavior instance = Instance;
		if (instance == null || !instance.TryBuildBanditSuppressionSnapshot(candidate, out _))
		{
			reason = "bandit_suppression_conditions_not_met";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForPoliticalRivalSuppression(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		if (!TryBuildPoliticalRivalSuppressionSnapshot(candidate, out _))
		{
			reason = "political_rival_suppression_conditions_not_met";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForSettlementPurchase(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		if (!TryBuildSettlementPurchaseSnapshot(candidate, out _))
		{
			reason = "settlement_purchase_conditions_not_met";
			return false;
		}
		return true;
	}

	private static bool IsPlayerEligibleForSettlementSale(ProactiveCandidate candidate, out string reason)
	{
		reason = "";
		ProactiveNpcRequestBehavior instance = Instance;
		if (instance == null || !instance.TryBuildSettlementSaleSnapshot(candidate, out _))
		{
			reason = "settlement_sale_conditions_not_met";
			return false;
		}
		return true;
	}

	private static List<Hero> GetMarriageableClanHeroes(Clan clan, bool includeMainHero)
	{
		List<Hero> result = new List<Hero>();
		try
		{
			if (clan?.Heroes != null)
			{
				foreach (Hero hero in clan.Heroes)
				{
					AddMarriageableHero(result, hero, clan);
				}
			}
			if (includeMainHero)
			{
				AddMarriageableHero(result, Hero.MainHero, clan);
			}
		}
		catch
		{
		}
		return result;
	}

	private static void AddMarriageableHero(List<Hero> result, Hero hero, Clan clan)
	{
		try
		{
			if (result == null || hero == null || clan == null || hero.Clan != clan || hero.IsDead || hero.IsPrisoner || hero.PartyBelongedToAsPrisoner != null)
			{
				return;
			}
			string heroKey = GetHeroKey(hero);
			if (result.Any(h => string.Equals(GetHeroKey(h), heroKey, StringComparison.OrdinalIgnoreCase)))
			{
				return;
			}
			if (!hero.CanMarry())
			{
				return;
			}
			result.Add(hero);
		}
		catch
		{
		}
	}

	private static bool AreHeroesSuitableForMarriageRequest(Hero left, Hero right)
	{
		try
		{
			return left != null
				&& right != null
				&& left != right
				&& left.Clan != null
				&& right.Clan != null
				&& left.Clan != right.Clan
				&& Campaign.Current?.Models?.MarriageModel?.IsCoupleSuitableForMarriage(left, right) == true;
		}
		catch
		{
			return false;
		}
	}

	private bool TryBuildFoodShortageCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null
			|| !IsFoodShortageNeedMet(source.Party, source.FoodDays, settings, out float urgency)
			|| !DoesPlayerHaveFoodForFoodRequest())
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedFoodShortage, urgency);
		return candidate != null;
	}

	private bool TryBuildMoneyShortageCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsMoneyShortageNeedMet(source, settings, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedMoneyShortage, urgency);
		return candidate != null;
	}

	private bool TryBuildTroopShortageCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null
			|| !IsTroopShortageNeedMet(source, settings, out float urgency)
			|| !DoesPlayerHaveTroopsForTroopRequest())
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedTroopShortage, urgency);
		return candidate != null;
	}

	private bool TryBuildPrisonerOverloadCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsPrisonerOverloadNeedMet(source, settings, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedPrisonerOverload, urgency);
		return candidate != null;
	}

	private bool TryBuildClanCaptiveCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsClanCaptiveNeedMet(source, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedClanCaptive, urgency);
		return candidate != null;
	}

	private bool TryBuildLowMoraleCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsLowMoraleNeedMet(source, settings, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedLowMorale, urgency);
		return candidate != null;
	}

	private bool TryBuildMountShortageCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null
			|| !IsMountShortageNeedMet(source, settings, out float urgency)
			|| !HasPlayerSurplusMountsCausingHerdPenalty())
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedMountShortage, urgency);
		return candidate != null;
	}

	private bool TryBuildOverburdenedCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsOverburdenedNeedMet(source, settings, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedOverburdened, urgency);
		return candidate != null;
	}

	private bool TryBuildClanFinanceStrainCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsClanFinanceStrainNeedMet(source, settings, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedClanFinanceStrain, urgency);
		return candidate != null;
	}

	private bool TryBuildClanServiceCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !TryBuildClanServiceNeedSnapshot(source, out ClanServiceNeedSnapshot snapshot))
		{
			return false;
		}
		float urgency = Clamp(60f + Math.Min(25f, Math.Max(0f, snapshot.RelationGap - 40) * 0.6f), 0f, 100f);
		candidate = TryBuildNeedCandidate(source, settings, NeedClanService, urgency);
		if (candidate == null)
		{
			return false;
		}
		candidate.ClanServiceTargetClanName = snapshot.TargetClanName;
		candidate.ClanServiceCurrentKingName = snapshot.CurrentKingName;
		candidate.ClanServicePlayerRelation = snapshot.PlayerRelation;
		candidate.ClanServiceCurrentKingRelation = snapshot.CurrentKingRelation;
		candidate.ClanServiceRelationGap = snapshot.RelationGap;
		return true;
	}

	private bool TryBuildRomanticInteractionCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || IsRomanticInteractionUnavailableForExternal() || !IsRomanticInteractionEligible(source.Hero, out int privateRelation))
		{
			return false;
		}
		float urgency = Clamp(60f + Math.Max(0, privateRelation - 30) * 0.4f, 0f, 100f);
		candidate = TryBuildNeedCandidate(source, settings, NeedRomanticInteraction, urgency);
		if (candidate == null)
		{
			return false;
		}
		candidate.RomanticInteractionPrivateRelation = privateRelation;
		return true;
	}

	private bool TryBuildTerritorialInterrogationCandidate(ProactiveCandidate source, DuelSettings settings, Dictionary<string, TerritorialSettlementSnapshot> territorialSettlementSnapshots, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null
			|| IsTerritorialInterrogationOnCooldown()
			|| !TryBuildTerritorialInterrogationSnapshot(source, settings, territorialSettlementSnapshots, out TerritorialInterrogationSnapshot snapshot))
		{
			return false;
		}
		source.TerritorialInterrogationEligible = true;
		source.TerritorialInterrogationKingdomName = snapshot.KingdomName;
		source.TerritorialInterrogationSettlementName = snapshot.SettlementName;
		source.TerritorialInterrogationSettlementDistance = snapshot.SettlementDistance;
		source.TerritorialInterrogationNpcCultureName = snapshot.NpcCultureName;
		source.TerritorialInterrogationCultureNotoriety = snapshot.CultureNotoriety;
		float range = Math.Max(1f, MobileParty.MainParty?.SeeingRange ?? 1f)
			* Clamp(settings?.ProactiveNpcRequestTerritorialInterrogationSettlementRangeMultiplier ?? 3f, 0.5f, 10f);
		float proximityUrgency = Clamp(1f - snapshot.SettlementDistance / Math.Max(1f, range), 0f, 1f) * 20f;
		float urgency = Clamp(60f + proximityUrgency, 0f, 100f);
		candidate = TryBuildNeedCandidate(source, settings, NeedTerritorialInterrogation, urgency);
		return candidate != null;
	}

	private bool TryBuildGreetingCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || IsGreetingUnavailableForExternal() || !IsGreetingEligible(source.Hero, out int privateRelation))
		{
			return false;
		}
		float urgency = Clamp(60f + Math.Max(0, privateRelation - 20) * 0.25f, 0f, 82.5f);
		candidate = TryBuildNeedCandidate(source, settings, NeedGreeting, urgency);
		if (candidate == null)
		{
			return false;
		}
		candidate.GreetingPrivateRelation = privateRelation;
		return true;
	}

	private bool TryBuildPolicyDiscussionCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null
			|| IsPolicyDiscussionUnavailableForExternal()
			|| !IsGreetingEligible(source.Hero, out int privateRelation)
			|| !TryGetRecentPolicyDiscussionSnapshot(out PolicyDiscussionSnapshot snapshot))
		{
			return false;
		}
		float urgency = Clamp(60f + Math.Max(0, privateRelation - 20) * 0.25f, 0f, 82.5f);
		candidate = TryBuildNeedCandidate(source, settings, NeedPolicyDiscussion, urgency);
		if (candidate == null)
		{
			return false;
		}
		candidate.GreetingPrivateRelation = privateRelation;
		candidate.PolicyDiscussionPolicyId = snapshot.PolicyId;
		candidate.PolicyDiscussionPolicyName = snapshot.PolicyName;
		candidate.PolicyDiscussionPolicyContent = snapshot.PolicyContent;
		candidate.PolicyDiscussionKingdomName = snapshot.KingdomName;
		candidate.PolicyDiscussionPublishedDay = snapshot.PublishedDay;
		return true;
	}

	private bool TryBuildPolicyDiscussionCompanionMotive(Hero hero, out string factText, out string intentText, out float urgency)
	{
		factText = "";
		intentText = "";
		urgency = 0f;
		if (IsPolicyDiscussionUnavailableForExternal()
			|| !IsGreetingEligible(hero, out int privateRelation)
			|| !TryGetRecentPolicyDiscussionSnapshot(out PolicyDiscussionSnapshot snapshot))
		{
			return false;
		}
		urgency = Clamp(60f + Math.Max(0, privateRelation - 20) * 0.25f, 0f, 82.5f);
		factText = BuildPolicyDiscussionSituation(snapshot, GetHeroDisplayName(hero), MyBehavior.BuildPlayerPublicDisplayNameForExternal(hero));
		intentText = AIConfigHandler.GetProactiveNpcRequestCompanionIntent(NeedPolicyDiscussion);
		return !string.IsNullOrWhiteSpace(factText) && !string.IsNullOrWhiteSpace(intentText);
	}

	private bool TryGetRecentPolicyDiscussionSnapshot(out PolicyDiscussionSnapshot snapshot)
	{
		snapshot = null;
		float cacheHour = (float)Math.Floor(NowHours());
		if (_policyDiscussionSnapshotCache != null
			&& Math.Abs(_policyDiscussionSnapshotCache.SampledAtHour - cacheHour) < 0.01f)
		{
			snapshot = _policyDiscussionSnapshotCache.Snapshot;
			return snapshot != null;
		}
		try
		{
			NpcRulerPolicyRecord record = NpcRulerPolicyBehavior.GetRecentPolicyRecordsForExternal(maxCount: 1).FirstOrDefault();
			string policyName = (record?.PolicyName ?? "").Trim();
			string policyContent = (record?.PolicyContent ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(policyName) && !string.IsNullOrWhiteSpace(policyContent))
			{
				snapshot = new PolicyDiscussionSnapshot
				{
					PolicyId = (record.PolicyId ?? "").Trim(),
					PolicyName = policyName,
					PolicyContent = policyContent,
					KingdomName = (record.KingdomName ?? "").Trim(),
					PublishedDay = Math.Max(0, record.Day)
				};
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ProactiveNpcRequest", "recent custom policy snapshot failed: " + ex.Message);
		}
		_policyDiscussionSnapshotCache = new PolicyDiscussionSnapshotCacheEntry
		{
			SampledAtHour = cacheHour,
			Snapshot = snapshot
		};
		return snapshot != null;
	}

	private static string BuildPolicyDiscussionSituation(PolicyDiscussionSnapshot snapshot, string npcName, string playerName)
	{
		if (snapshot == null)
		{
			return "";
		}
		string policyName = (snapshot.PolicyName ?? "").Trim();
		string policyContent = (snapshot.PolicyContent ?? "").Trim();
		string targetName = string.IsNullOrWhiteSpace(playerName) ? SocialSourceTexts.Get("S001") : playerName.Trim();
		if (string.IsNullOrWhiteSpace(policyName) || string.IsNullOrWhiteSpace(policyContent))
		{
			return "";
		}
		return SocialSourceTexts.Get("S392") + policyName + SocialSourceTexts.Get("S393") + policyContent
			+ SocialSourceTexts.Get("S394") + targetName + SocialSourceTexts.Get("S395");
	}

	private bool TryBuildFriendshipCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || IsFriendshipOnCooldown() || !TryBuildFriendshipNeedSnapshot(source, out FriendshipNeedSnapshot snapshot))
		{
			return false;
		}
		float notorietyStrength = Clamp((snapshot.CultureNotoriety - 10) / 90f, 0f, 1f);
		float clanTierStrength = Clamp((snapshot.PlayerClanTier - 1) / 5f, 0f, 1f);
		float urgency = Clamp(50f + notorietyStrength * 30f + clanTierStrength * 20f, 0f, 100f);
		candidate = TryBuildNeedCandidate(source, settings, NeedFriendship, urgency);
		return candidate != null;
	}

	private bool TryBuildCourtshipCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || IsCourtshipOnCooldown() || !TryBuildCourtshipNeedSnapshot(source, out CourtshipNeedSnapshot snapshot))
		{
			return false;
		}
		float notorietyStrength = Clamp((snapshot.CultureNotoriety - 10) / 90f, 0f, 1f);
		float playerTierStrength = Clamp((snapshot.PlayerClanTier - 1) / 5f, 0f, 1f);
		float relativeClanTierAdjustment = Clamp((snapshot.PlayerClanTier - snapshot.NpcClanTier) * 5f, -25f, 30f);
		float urgency = Clamp(60f + notorietyStrength * 15f + playerTierStrength * 10f + relativeClanTierAdjustment, 50f, 90f);
		candidate = TryBuildNeedCandidate(source, settings, NeedCourtship, urgency);
		if (candidate == null)
		{
			return false;
		}
		candidate.IntrinsicNeedTypeWeightMultiplier = snapshot.TriggerWeightMultiplier;
		return true;
	}

	private bool TryBuildArmyJoinRequestCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !TryBuildArmyJoinRequestSnapshot(source, out ArmyJoinRequestSnapshot snapshot))
		{
			return false;
		}
		float shortage = Clamp((0.66f - snapshot.OwnToEnemyRatio) / 0.66f, 0f, 1f);
		float urgency = Clamp(65f + shortage * 25f, 0f, 100f);
		candidate = TryBuildNeedCandidate(source, settings, NeedArmyJoinRequest, urgency);
		if (candidate == null)
		{
			return false;
		}
		candidate.ArmyJoinRequestArmyName = snapshot.ArmyName;
		candidate.ArmyJoinRequestOwnStrength = snapshot.OwnStrength;
		candidate.ArmyJoinRequestEnemyStrength = snapshot.EnemyStrength;
		candidate.ArmyJoinRequestEnemyKingdomCount = snapshot.EnemyKingdomCount;
		candidate.ArmyJoinRequestOwnToEnemyRatio = snapshot.OwnToEnemyRatio;
		return true;
	}

	private bool TryBuildBanditSuppressionCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !TryBuildBanditSuppressionSnapshot(source, out BanditSuppressionSnapshot snapshot))
		{
			return false;
		}
		float urgency = Clamp(65f + Math.Min(25f, Math.Max(0, snapshot.BanditCount - 9) * 3f), 0f, 100f);
		candidate = TryBuildNeedCandidate(source, settings, NeedBanditSuppression, urgency);
		if (candidate == null)
		{
			return false;
		}
		candidate.BanditSuppressionSettlementName = snapshot.SettlementName;
		candidate.BanditSuppressionBanditCount = snapshot.BanditCount;
		candidate.BanditSuppressionRadius = snapshot.Radius;
		candidate.BanditSuppressionTrust = snapshot.Trust;
		candidate.BanditSuppressionPrivateRelation = snapshot.PrivateRelation;
		return true;
	}

	private bool TryBuildPoliticalRivalSuppressionCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null
			|| IsPoliticalRivalSuppressionOnCooldown()
			|| !TryBuildPoliticalRivalSuppressionSnapshot(source, out PoliticalRivalSuppressionSnapshot snapshot))
		{
			return false;
		}
		float playerSupport = Clamp((snapshot.PlayerClanRelation - 20) / 80f, 0f, 1f);
		float rivalry = Clamp((-10 - snapshot.RivalClanRelation) / 90f, 0f, 1f);
		float urgency = Clamp(62f + playerSupport * 13f + rivalry * 15f, 0f, 100f);
		candidate = TryBuildNeedCandidate(source, settings, NeedPoliticalRivalSuppression, urgency);
		if (candidate == null)
		{
			return false;
		}
		candidate.PoliticalRivalSuppressionKingdomName = snapshot.KingdomName;
		candidate.PoliticalRivalSuppressionRequesterClanName = snapshot.RequesterClanName;
		candidate.PoliticalRivalSuppressionPlayerClanRelation = snapshot.PlayerClanRelation;
		candidate.PoliticalRivalSuppressionRivalClanName = snapshot.RivalClanName;
		candidate.PoliticalRivalSuppressionRivalClanRelation = snapshot.RivalClanRelation;
		return true;
	}

	private bool TryBuildSettlementPurchaseCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null
			|| IsSettlementPurchaseOnCooldown()
			|| !TryBuildSettlementPurchaseSnapshot(source, out SettlementPurchaseSnapshot snapshot))
		{
			return false;
		}
		float playerFiefPressure = Clamp((snapshot.PlayerFiefCount - 3) / 7f, 0f, 1f);
		float npcFiefNeed = snapshot.NpcFiefCount <= 0 ? 1f : 0.55f;
		float urgency = Clamp(64f + playerFiefPressure * 16f + npcFiefNeed * 10f, 0f, 100f);
		candidate = TryBuildNeedCandidate(source, settings, NeedSettlementPurchase, urgency);
		if (candidate == null)
		{
			return false;
		}
		candidate.SettlementPurchaseKingdomName = snapshot.KingdomName;
		candidate.SettlementPurchasePlayerTownCount = snapshot.PlayerTownCount;
		candidate.SettlementPurchasePlayerCastleCount = snapshot.PlayerCastleCount;
		candidate.SettlementPurchasePlayerFiefsText = snapshot.PlayerFiefsText;
		candidate.SettlementPurchaseNpcFiefCount = snapshot.NpcFiefCount;
		candidate.SettlementPurchaseNpcTownCount = snapshot.NpcTownCount;
		candidate.SettlementPurchaseNpcCastleCount = snapshot.NpcCastleCount;
		return true;
	}

	private bool TryBuildSettlementSaleCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !TryBuildSettlementSaleSnapshot(source, out SettlementSaleSnapshot snapshot))
		{
			return false;
		}
		float relationStrength = Clamp((snapshot.PlayerClanRelation - 30) / 70f, 0f, 1f);
		float incomeGap = snapshot.HighestFamilyDailyIncome <= 0
			? 0f
			: Clamp((snapshot.HighestFamilyDailyIncome - snapshot.TargetDailyIncome) / (float)snapshot.HighestFamilyDailyIncome, 0f, 1f);
		float borderPressure = snapshot.BorderRadius <= 0f
			? 0f
			: Clamp(1f - snapshot.BorderDistance / snapshot.BorderRadius, 0f, 1f);
		float urgency = Clamp(58f + relationStrength * 12f + incomeGap * 17f + borderPressure * 13f, 0f, 100f);
		candidate = TryBuildNeedCandidate(source, settings, NeedSettlementSale, urgency);
		if (candidate == null)
		{
			return false;
		}
		candidate.SettlementSaleKingdomName = snapshot.KingdomName;
		candidate.SettlementSalePlayerClanRelation = snapshot.PlayerClanRelation;
		candidate.SettlementSaleNpcFiefCount = snapshot.NpcFiefCount;
		candidate.SettlementSaleTargetSettlementName = snapshot.TargetSettlementName;
		candidate.SettlementSaleTargetSettlementType = snapshot.TargetSettlementType;
		candidate.SettlementSaleTargetDailyIncome = snapshot.TargetDailyIncome;
		candidate.SettlementSaleHighestFamilyDailyIncome = snapshot.HighestFamilyDailyIncome;
		candidate.SettlementSaleForeignSettlementName = snapshot.ForeignSettlementName;
		candidate.SettlementSaleForeignFactionName = snapshot.ForeignFactionName;
		candidate.SettlementSaleBorderDistance = snapshot.BorderDistance;
		candidate.SettlementSaleBorderRadius = snapshot.BorderRadius;
		return true;
	}

	private bool TryBuildMarriageAlliancePressureCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsMarriageAlliancePressureNeedMet(source, settings, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedMarriageAlliancePressure, urgency);
		return candidate != null;
	}

	private bool TryBuildRevengePressureCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsRevengePressureNeedMet(source, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedRevengePressure, urgency);
		return candidate != null;
	}

	private bool TryBuildFiefGovernanceAnxietyCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsFiefGovernanceAnxietyNeedMet(source, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedFiefGovernanceAnxiety, urgency);
		return candidate != null;
	}

	private bool TryBuildAllySupportCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsAllySupportNeedMet(source, settings, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedAllySupport, urgency);
		return candidate != null;
	}

	private bool TryBuildKingdomMercenaryInviteCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsKingdomMercenaryInviteNeedMet(source, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedKingdomMercenaryInvite, urgency);
		return candidate != null;
	}

	private bool TryBuildKingdomVassalInviteCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsKingdomVassalInviteNeedMet(source, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedKingdomVassalInvite, urgency);
		return candidate != null;
	}

	private bool TryBuildPoliticalAgendaCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !IsPoliticalAgendaNeedMet(source, out float urgency))
		{
			return false;
		}
		candidate = TryBuildNeedCandidate(source, settings, NeedPoliticalAgenda, urgency);
		return candidate != null;
	}

	private bool TryBuildPolicySupportCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source == null || !TryBuildPolicySupportSnapshot(source, out PolicySupportSnapshot snapshot))
		{
			return false;
		}
		float relationStrength = Clamp((snapshot.PlayerClanRelation - 30) / 70f, 0f, 1f);
		float supportStrength = Clamp((snapshot.SupportScore - 100f) / 150f, 0f, 1f);
		float pendingUrgency = snapshot.HasPendingDecision ? 12f : 0f;
		float urgency = Clamp(62f + relationStrength * 12f + supportStrength * 14f + pendingUrgency, 0f, 100f);
		candidate = TryBuildNeedCandidate(source, settings, NeedPolicySupport, urgency);
		if (candidate == null)
		{
			return false;
		}
		candidate.PolicySupportKingdomName = snapshot.KingdomName;
		candidate.PolicySupportPlayerClanRelation = snapshot.PlayerClanRelation;
		candidate.PolicySupportPolicyName = snapshot.PolicyName;
		candidate.PolicySupportDescription = snapshot.Description;
		candidate.PolicySupportEffects = snapshot.Effects;
		candidate.PolicySupportScore = snapshot.SupportScore;
		candidate.PolicySupportHasPendingDecision = snapshot.HasPendingDecision;
		return true;
	}

	private bool TryBuildDiplomacyCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)
	{
		candidate = null;
		if (source?.Hero == null
			|| !DiplomacyConversationBridge.TryBuildProactiveDiscussionForExternal(source.Hero, out string discussionKey, out string discussionFact, out float urgency)
			|| string.IsNullOrWhiteSpace(discussionKey)
			|| _cooldownOwner.IsDiscussionOnCooldown(discussionKey, NowDays()))
			return false;
		candidate = TryBuildNeedCandidate(source, settings, NeedDiplomacy, urgency);
		if (candidate == null) return false;
		candidate.DiplomacyDiscussionKey = discussionKey;
		candidate.DiplomacyDiscussionFact = discussionFact;
		return true;
	}

	private static bool IsFoodShortageNeedMet(MobileParty party, int foodDays, DuelSettings settings, out float urgency)
	{
		urgency = 0f;
		int threshold = Clamp(settings?.ProactiveNpcRequestFoodDaysThreshold ?? 3, 0, 15);
		try
		{
			if (party?.Party?.IsStarving == true)
			{
				urgency = 100f;
				return true;
			}
		}
		catch
		{
		}
		if (foodDays <= threshold)
		{
			urgency = 80f + Math.Max(0, threshold - foodDays);
			return true;
		}
		return false;
	}

	private static bool IsPoliticalAgendaNeedMet(ProactiveCandidate candidate, out float urgency)
	{
		urgency = 0f;
		try
		{
			Hero hero = candidate?.Hero;
			Clan npcClan = hero?.Clan;
			Kingdom kingdom = candidate?.TargetKingdom ?? npcClan?.Kingdom;
			Clan playerClan = Clan.PlayerClan;
			if (hero == null || npcClan == null || kingdom == null || playerClan == null)
			{
				return false;
			}
			if (npcClan.IsUnderMercenaryService || playerClan.IsUnderMercenaryService)
			{
				return false;
			}
			if (playerClan.Kingdom != kingdom)
			{
				return false;
			}
			if (hero != npcClan.Leader && hero != kingdom.RulingClan?.Leader)
			{
				return false;
			}
			int activeAgendaCount = CountActiveKingdomAgendas(kingdom);
			if (activeAgendaCount <= 0)
			{
				return false;
			}
			urgency = 52f + Math.Min(18f, activeAgendaCount * 4f);
			if (hero == kingdom.RulingClan?.Leader)
			{
				urgency += 6f;
			}
			return true;
		}
		catch
		{
			urgency = 0f;
			return false;
		}
	}

	private static int CountActiveKingdomAgendas(Kingdom kingdom)
	{
		try
		{
			if (kingdom?.UnresolvedDecisions == null)
			{
				return 0;
			}
			int count = 0;
			foreach (KingdomDecision decision in kingdom.UnresolvedDecisions)
			{
				if (decision == null)
				{
					continue;
				}
				try
				{
					if (decision.ShouldBeCancelled())
					{
						continue;
					}
				}
				catch
				{
				}
				count++;
			}
			return count;
		}
		catch
		{
			return 0;
		}
	}

	private static bool IsMoneyShortageNeedMet(ProactiveCandidate candidate, DuelSettings settings, out float urgency)
	{
		urgency = 0f;
		if (candidate == null)
		{
			return false;
		}
		if (candidate.UnpaidWages > 0f)
		{
			urgency = 95f + Clamp(candidate.UnpaidWages, 0f, 1f) * 5f;
			return true;
		}
		int goldThreshold = GetEffectiveMoneyGoldThreshold(settings);
		if (candidate.PartyGold < goldThreshold)
		{
			float deficitRatio = goldThreshold <= 0 ? 0f : Clamp((goldThreshold - candidate.PartyGold) / (float)goldThreshold, 0f, 1f);
			urgency = 70f + deficitRatio * 10f;
			return true;
		}
		int wageDaysThreshold = Clamp(settings?.ProactiveNpcRequestMoneyWageDaysThreshold ?? 3, 0, 30);
		if (wageDaysThreshold > 0 && candidate.TotalWage > 0 && candidate.WageDays <= wageDaysThreshold)
		{
			urgency = 60f + Clamp(wageDaysThreshold - candidate.WageDays, 0f, 30f);
			return true;
		}
		return false;
	}

	private static bool IsTroopShortageNeedMet(ProactiveCandidate candidate, DuelSettings settings, out float urgency)
	{
		urgency = 0f;
		if (candidate == null || candidate.PartySizeLimit <= 0 || candidate.MemberCount <= 0)
		{
			return false;
		}
		int thresholdPercent = Clamp(settings?.ProactiveNpcRequestTroopRatioThresholdPercent ?? 50, 1, 100);
		float thresholdRatio = thresholdPercent / 100f;
		if (candidate.PartySizeRatio > thresholdRatio)
		{
			return false;
		}
		float shortageRatio = Clamp(thresholdRatio - candidate.PartySizeRatio, 0f, 1f);
		urgency = 50f + shortageRatio * 25f;
		return true;
	}

	private static bool IsPrisonerOverloadNeedMet(ProactiveCandidate candidate, DuelSettings settings, out float urgency)
	{
		urgency = 0f;
		if (candidate == null || candidate.PrisonerCount <= 0 || candidate.PrisonerSizeLimit <= 0)
		{
			return false;
		}
		int thresholdPercent = Clamp(settings?.ProactiveNpcRequestPrisonerRatioThresholdPercent ?? 80, 1, 150);
		float thresholdRatio = thresholdPercent / 100f;
		float prisonerRatio = CalculatePrisonerSizeRatio(candidate.PrisonerCount, candidate.PrisonerSizeLimit);
		float heroBonus = Math.Min(9f, Math.Max(0, candidate.HeroPrisonerCount) * 3f);
		if (candidate.PrisonerCount > candidate.PrisonerSizeLimit)
		{
			float overflowRatio = Clamp((candidate.PrisonerCount - candidate.PrisonerSizeLimit) / (float)candidate.PrisonerSizeLimit, 0f, 1f);
			urgency = 90f + overflowRatio * 10f + heroBonus;
			return true;
		}
		if (prisonerRatio >= thresholdRatio)
		{
			float span = Math.Max(0.01f, 1f - Math.Min(thresholdRatio, 0.99f));
			float pressure = Clamp((prisonerRatio - thresholdRatio) / span, 0f, 1f);
			urgency = 62f + pressure * 18f + heroBonus;
			return true;
		}
		if (thresholdRatio <= 1f && candidate.HeroPrisonerCount > 0 && prisonerRatio >= Math.Min(0.5f, thresholdRatio))
		{
			urgency = 58f + heroBonus;
			return true;
		}
		return false;
	}

	private static bool IsClanCaptiveNeedMet(ProactiveCandidate candidate, out float urgency)
	{
		urgency = 0f;
		if (candidate == null || candidate.CaptiveClanHeroCount <= 0)
		{
			return false;
		}
		urgency = 64f + Math.Min(18f, candidate.CaptiveClanHeroCount * 6f);
		if (candidate.CaptiveClanLeaderHeld)
		{
			urgency += 12f;
		}
		return true;
	}

	private static bool IsLowMoraleNeedMet(ProactiveCandidate candidate, DuelSettings settings, out float urgency)
	{
		urgency = 0f;
		if (candidate == null || candidate.MemberCount <= 0)
		{
			return false;
		}
		int threshold = Clamp(settings?.ProactiveNpcRequestLowMoraleThreshold ?? 35, 0, 100);
		if (threshold <= 0 || candidate.Morale > threshold)
		{
			return false;
		}
		float deficitRatio = threshold <= 0 ? 0f : Clamp((threshold - candidate.Morale) / Math.Max(1f, threshold), 0f, 1f);
		urgency = 54f + deficitRatio * 30f;
		return true;
	}

	private static bool IsMountShortageNeedMet(ProactiveCandidate candidate, DuelSettings settings, out float urgency)
	{
		urgency = 0f;
		if (candidate == null || candidate.MemberCount <= 0)
		{
			return false;
		}
		int thresholdPercent = Clamp(settings?.ProactiveNpcRequestMountRatioThresholdPercent ?? 25, 0, 100);
		if (thresholdPercent <= 0)
		{
			return false;
		}
		float thresholdRatio = thresholdPercent / 100f;
		if (candidate.MountRatio >= thresholdRatio)
		{
			return false;
		}
		float deficitRatio = Clamp((thresholdRatio - candidate.MountRatio) / Math.Max(0.01f, thresholdRatio), 0f, 1f);
		urgency = 50f + deficitRatio * 26f;
		if (candidate.PackAnimalRatio < Math.Min(0.15f, thresholdRatio))
		{
			urgency += 4f;
		}
		return true;
	}

	private static bool IsOverburdenedNeedMet(ProactiveCandidate candidate, DuelSettings settings, out float urgency)
	{
		urgency = 0f;
		if (candidate == null || candidate.InventoryCapacity <= 0 || candidate.TotalWeightCarried <= 0f)
		{
			return false;
		}
		int thresholdPercent = Clamp(settings?.ProactiveNpcRequestOverburdenRatioThresholdPercent ?? 92, 50, 150);
		float thresholdRatio = thresholdPercent / 100f;
		if (candidate.CarryRatio < thresholdRatio)
		{
			return false;
		}
		if (candidate.CarryRatio >= 1f)
		{
			float overRatio = Clamp(candidate.CarryRatio - 1f, 0f, 1f);
			urgency = 76f + overRatio * 20f;
			return true;
		}
		float pressure = Clamp((candidate.CarryRatio - thresholdRatio) / Math.Max(0.01f, 1f - Math.Min(thresholdRatio, 0.99f)), 0f, 1f);
		urgency = 58f + pressure * 18f;
		return true;
	}

	private static bool IsClanFinanceStrainNeedMet(ProactiveCandidate candidate, DuelSettings settings, out float urgency)
	{
		urgency = 0f;
		if (candidate == null)
		{
			return false;
		}
		int goldThreshold = Clamp(settings?.ProactiveNpcRequestClanGoldThreshold ?? 15000, 0, 200000);
		int debtThreshold = Clamp(settings?.ProactiveNpcRequestClanDebtThreshold ?? 5000, 0, 200000);
		bool goldLow = goldThreshold > 0 && candidate.ClanGold < goldThreshold;
		bool debtHigh = debtThreshold > 0 && candidate.ClanDebtToKingdom > debtThreshold;
		if (!goldLow && !debtHigh)
		{
			return false;
		}
		float goldUrgency = 0f;
		if (goldLow)
		{
			float deficitRatio = Clamp((goldThreshold - candidate.ClanGold) / (float)Math.Max(1, goldThreshold), 0f, 1f);
			goldUrgency = 56f + deficitRatio * 22f;
		}
		float debtUrgency = 0f;
		if (debtHigh)
		{
			float debtRatio = Clamp((candidate.ClanDebtToKingdom - debtThreshold) / (float)Math.Max(1, debtThreshold), 0f, 2f);
			debtUrgency = 62f + Math.Min(24f, debtRatio * 12f);
		}
		urgency = Math.Max(goldUrgency, debtUrgency);
		return urgency > 0f;
	}

	private bool TryBuildPolicySupportSnapshot(ProactiveCandidate candidate, out PolicySupportSnapshot snapshot)
	{
		snapshot = null;
		try
		{
			Hero hero = candidate?.Hero;
			Clan npcClan = hero?.Clan;
			Clan playerClan = Clan.PlayerClan;
			Kingdom kingdom = npcClan?.Kingdom;
			if (hero == null
				|| npcClan == null
				|| playerClan == null
				|| kingdom == null
				|| kingdom.IsEliminated
				|| candidate.AtWarWithPlayer
				|| npcClan == playerClan
				|| npcClan.IsEliminated
				|| hero != npcClan.Leader
				|| npcClan.IsUnderMercenaryService
				|| playerClan.IsUnderMercenaryService
				|| playerClan.Kingdom != kingdom)
			{
				return false;
			}
			int playerClanRelation = Clamp(npcClan.GetRelationWithClan(playerClan), -100, 100);
			if (playerClanRelation <= 30)
			{
				return false;
			}
			PolicySupportSnapshot cachedSnapshot = GetClanPolicySupportSnapshot(npcClan);
			if (cachedSnapshot == null || cachedSnapshot.SupportScore < 100f || string.IsNullOrWhiteSpace(cachedSnapshot.PolicyName))
			{
				return false;
			}
			snapshot = new PolicySupportSnapshot
			{
				KingdomName = (kingdom.Name?.ToString() ?? SocialSourceTexts.Get("S396")).Trim(),
				PlayerClanRelation = playerClanRelation,
				PolicyName = cachedSnapshot.PolicyName,
				Description = cachedSnapshot.Description,
				Effects = cachedSnapshot.Effects,
				SupportScore = cachedSnapshot.SupportScore,
				HasPendingDecision = cachedSnapshot.HasPendingDecision
			};
			return true;
		}
		catch
		{
			return false;
		}
	}

	private PolicySupportSnapshot GetClanPolicySupportSnapshot(Clan clan)
	{
		string clanKey = (clan?.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(clanKey))
		{
			return null;
		}
		float cacheHour = (float)Math.Floor(NowHours());
		if (_policySupportSnapshotsByClan.TryGetValue(clanKey, out PolicySupportSnapshotCacheEntry cached)
			&& cached != null
			&& Math.Abs(cached.SampledAtHour - cacheHour) < 0.01f)
		{
			return cached.Snapshot;
		}
		PolicySupportSnapshot snapshot = ScanClanPolicySupportSnapshot(clan);
		_policySupportSnapshotsByClan[clanKey] = new PolicySupportSnapshotCacheEntry
		{
			SampledAtHour = cacheHour,
			Snapshot = snapshot
		};
		if (_policySupportSnapshotsByClan.Count > 128)
		{
			foreach (string key in _policySupportSnapshotsByClan
				.Where(pair => pair.Value == null || pair.Value.SampledAtHour < cacheHour - 1f)
				.Select(pair => pair.Key)
				.ToList())
			{
				_policySupportSnapshotsByClan.Remove(key);
			}
		}
		return snapshot;
	}

	private static PolicySupportSnapshot ScanClanPolicySupportSnapshot(Clan clan)
	{
		try
		{
			Kingdom kingdom = clan?.Kingdom;
			if (kingdom == null || clan.Leader == null || clan.IsUnderMercenaryService)
			{
				return null;
			}
			PolicyObject selectedPolicy = null;
			float selectedScore = float.MinValue;
			foreach (PolicyObject policy in PolicyObject.All ?? Enumerable.Empty<PolicyObject>())
			{
				if (policy == null || !policy.IsReady || kingdom.ActivePolicies.Contains(policy))
				{
					continue;
				}
				if (!Campaign.Current.Models.KingdomDecisionPermissionModel.IsPolicyDecisionAllowed(policy))
				{
					continue;
				}
				float supportScore;
				try
				{
					supportScore = new KingdomPolicyDecision(clan, policy, false).CalculateSupport(clan);
				}
				catch
				{
					continue;
				}
				if (supportScore < 100f || (selectedPolicy != null && supportScore <= selectedScore))
				{
					continue;
				}
				selectedPolicy = policy;
				selectedScore = supportScore;
			}
			if (selectedPolicy == null)
			{
				return null;
			}
			return new PolicySupportSnapshot
			{
				PolicyName = LimitPolicySupportPromptText(selectedPolicy.Name?.ToString(), 80),
				Description = LimitPolicySupportPromptText(selectedPolicy.Description?.ToString() ?? selectedPolicy.LogEntryDescription?.ToString(), 180),
				Effects = LimitPolicySupportPromptText(selectedPolicy.SecondaryEffects?.ToString(), 180),
				SupportScore = selectedScore,
				HasPendingDecision = HasPendingPolicyDecision(kingdom, selectedPolicy)
			};
		}
		catch
		{
			return null;
		}
	}

	private static bool HasPendingPolicyDecision(Kingdom kingdom, PolicyObject policy)
	{
		try
		{
			return kingdom?.UnresolvedDecisions != null
				&& kingdom.UnresolvedDecisions.Any(decision => decision is KingdomPolicyDecision policyDecision
					&& policyDecision.Policy == policy
					&& !policyDecision.ShouldBeCancelled());
		}
		catch
		{
			return false;
		}
	}

	private static string LimitPolicySupportPromptText(string text, int maxChars)
	{
		text = (text ?? "").Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ').Trim();
		while (text.Contains("  "))
		{
			text = text.Replace("  ", " ");
		}
		if (text.Length <= maxChars)
		{
			return text;
		}
		return text.Substring(0, Math.Max(1, maxChars - 1)).TrimEnd() + "…";
	}

	private static bool TryBuildClanServiceNeedSnapshot(ProactiveCandidate candidate, out ClanServiceNeedSnapshot snapshot)
	{
		snapshot = null;
		try
		{
			Hero clanLeader = candidate?.Hero;
			Clan targetClan = clanLeader?.Clan;
			Clan playerClan = Clan.PlayerClan;
			Kingdom playerKingdom = playerClan?.Kingdom;
			Kingdom targetKingdom = targetClan?.Kingdom;
			Clan currentRulingClan = targetKingdom?.RulingClan;
			if (clanLeader == null
				|| targetClan == null
				|| playerClan == null
				|| playerKingdom == null
				|| playerKingdom.IsEliminated
				|| Hero.MainHero != playerKingdom.RulingClan?.Leader
				|| targetClan == playerClan
				|| targetClan.IsEliminated
				|| targetClan.IsUnderMercenaryService
				|| targetClan.IsClanTypeMercenary
				|| clanLeader != targetClan.Leader
				|| targetKingdom == null
				|| targetKingdom.IsEliminated
				|| targetKingdom == playerKingdom
				|| currentRulingClan == null
				|| currentRulingClan.IsEliminated
				|| currentRulingClan == targetClan
				|| (targetClan.Fiefs != null && targetClan.Fiefs.Any(fief => fief != null)))
			{
				return false;
			}

			int playerRelation = playerClan.GetRelationWithClan(targetClan);
			int currentKingRelation = targetClan.GetRelationWithClan(currentRulingClan);
			int relationGap = playerRelation - currentKingRelation;
			if (relationGap <= 40)
			{
				return false;
			}
			snapshot = new ClanServiceNeedSnapshot
			{
				TargetClanName = targetClan.Name?.ToString() ?? SocialSourceTexts.Get("S397"),
				CurrentKingName = currentRulingClan.Leader?.Name?.ToString() ?? SocialSourceTexts.Get("S398"),
				PlayerRelation = playerRelation,
				CurrentKingRelation = currentKingRelation,
				RelationGap = relationGap
			};
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryBuildPoliticalRivalSuppressionSnapshot(ProactiveCandidate candidate, out PoliticalRivalSuppressionSnapshot snapshot)
	{
		snapshot = null;
		try
		{
			Hero hero = candidate?.Hero;
			Clan requesterClan = hero?.Clan;
			Clan playerClan = Clan.PlayerClan;
			Kingdom kingdom = requesterClan?.Kingdom;
			if (hero == null
				|| requesterClan == null
				|| playerClan == null
				|| kingdom == null
				|| kingdom.IsEliminated
				|| candidate.AtWarWithPlayer
				|| requesterClan.IsEliminated
				|| requesterClan == playerClan
				|| hero != requesterClan.Leader
				|| playerClan.Kingdom != kingdom)
			{
				return false;
			}

			int playerClanRelation = requesterClan.GetRelationWithClan(playerClan);
			if (playerClanRelation <= 20)
			{
				return false;
			}

			Clan rivalClan = null;
			int rivalClanRelation = int.MaxValue;
			foreach (Clan otherClan in kingdom.Clans ?? Enumerable.Empty<Clan>())
			{
				if (otherClan == null
					|| otherClan == requesterClan
					|| otherClan == playerClan
					|| otherClan.IsEliminated
					|| otherClan.Kingdom != kingdom)
				{
					continue;
				}
				int relation = requesterClan.GetRelationWithClan(otherClan);
				if (relation < -10 && relation < rivalClanRelation)
				{
					rivalClan = otherClan;
					rivalClanRelation = relation;
				}
			}
			if (rivalClan == null)
			{
				return false;
			}

			snapshot = new PoliticalRivalSuppressionSnapshot
			{
				KingdomName = (kingdom.Name?.ToString() ?? SocialSourceTexts.Get("S396")).Trim(),
				RequesterClanName = (requesterClan.Name?.ToString() ?? SocialSourceTexts.Get("S397")).Trim(),
				PlayerClanRelation = playerClanRelation,
				RivalClanName = (rivalClan.Name?.ToString() ?? SocialSourceTexts.Get("S399")).Trim(),
				RivalClanRelation = rivalClanRelation
			};
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPoliticalRivalSuppressionOnCooldown()
	{
		try
		{
			ProactiveNpcRequestBehavior instance = Instance;
			return instance != null && instance.GetNeedTypeFatigueRemainingDays(NeedPoliticalRivalSuppression, NowDays()) > 0f;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryBuildSettlementPurchaseSnapshot(ProactiveCandidate candidate, out SettlementPurchaseSnapshot snapshot)
	{
		snapshot = null;
		try
		{
			Hero hero = candidate?.Hero;
			Clan requesterClan = hero?.Clan;
			Clan playerClan = Clan.PlayerClan;
			Kingdom kingdom = requesterClan?.Kingdom;
			if (hero == null
				|| requesterClan == null
				|| playerClan == null
				|| kingdom == null
				|| kingdom.IsEliminated
				|| candidate.AtWarWithPlayer
				|| requesterClan.IsEliminated
				|| requesterClan == playerClan
				|| hero != requesterClan.Leader
				|| playerClan.Kingdom != kingdom)
			{
				return false;
			}

			List<Settlement> playerFiefs = GetClanTownAndCastleSettlements(playerClan);
			List<Settlement> requesterFiefs = GetClanTownAndCastleSettlements(requesterClan);
			if (playerFiefs.Count < 3 || requesterFiefs.Count > 1)
			{
				return false;
			}

			List<string> playerTowns = playerFiefs
				.Where(x => x.IsTown)
				.Select(GetSettlementDisplayName)
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.OrderBy(x => x, StringComparer.Ordinal)
				.ToList();
			List<string> playerCastles = playerFiefs
				.Where(x => x.IsCastle)
				.Select(GetSettlementDisplayName)
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.OrderBy(x => x, StringComparer.Ordinal)
				.ToList();
			int requesterTownCount = requesterFiefs.Count(x => x.IsTown);
			int requesterCastleCount = requesterFiefs.Count(x => x.IsCastle);
			snapshot = new SettlementPurchaseSnapshot
			{
				KingdomName = (kingdom.Name?.ToString() ?? SocialSourceTexts.Get("S396")).Trim(),
				PlayerFiefCount = playerFiefs.Count,
				PlayerTownCount = playerTowns.Count,
				PlayerCastleCount = playerCastles.Count,
				PlayerFiefsText = SocialSourceTexts.Get("S400") + (playerTowns.Count > 0 ? string.Join("、", playerTowns) : SocialSourceTexts.Get("S401")) + SocialSourceTexts.Get("S402") + (playerCastles.Count > 0 ? string.Join("、", playerCastles) : SocialSourceTexts.Get("S401")),
				NpcFiefCount = requesterFiefs.Count,
				NpcTownCount = requesterTownCount,
				NpcCastleCount = requesterCastleCount
			};
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static List<Settlement> GetClanTownAndCastleSettlements(Clan clan)
	{
		Dictionary<string, Settlement> settlements = new Dictionary<string, Settlement>(StringComparer.OrdinalIgnoreCase);
		try
		{
			foreach (Town fief in clan?.Fiefs ?? Enumerable.Empty<Town>())
			{
				Settlement settlement = fief?.Settlement;
				if (settlement == null || settlement.OwnerClan != clan || (!settlement.IsTown && !settlement.IsCastle))
				{
					continue;
				}
				string key = (settlement.StringId ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(key))
				{
					settlements[key] = settlement;
				}
			}
		}
		catch
		{
		}
		return settlements.Values.ToList();
	}

	private static bool IsSettlementPurchaseOnCooldown()
	{
		try
		{
			ProactiveNpcRequestBehavior instance = Instance;
			return instance != null && instance.GetNeedTypeFatigueRemainingDays(NeedSettlementPurchase, NowDays()) > 0f;
		}
		catch
		{
			return false;
		}
	}

	private bool TryBuildSettlementSaleSnapshot(ProactiveCandidate candidate, out SettlementSaleSnapshot snapshot)
	{
		snapshot = null;
		try
		{
			Hero hero = candidate?.Hero;
			Clan requesterClan = hero?.Clan;
			Clan playerClan = Clan.PlayerClan;
			Kingdom kingdom = requesterClan?.Kingdom;
			if (hero == null
				|| requesterClan == null
				|| playerClan == null
				|| kingdom == null
				|| kingdom.IsEliminated
				|| candidate.AtWarWithPlayer
				|| requesterClan.IsEliminated
				|| requesterClan == playerClan
				|| hero != requesterClan.Leader
				|| playerClan.Kingdom != kingdom)
			{
				return false;
			}
			int playerClanRelation = Clamp(requesterClan.GetRelationWithClan(playerClan), -100, 100);
			if (playerClanRelation <= 30)
			{
				return false;
			}
			SettlementSaleSnapshot clanSnapshot = GetClanSettlementSaleSnapshot(requesterClan);
			if (clanSnapshot == null || clanSnapshot.NpcFiefCount < 4 || string.IsNullOrWhiteSpace(clanSnapshot.TargetSettlementName))
			{
				return false;
			}
			snapshot = new SettlementSaleSnapshot
			{
				KingdomName = (kingdom.Name?.ToString() ?? SocialSourceTexts.Get("S396")).Trim(),
				PlayerClanRelation = playerClanRelation,
				NpcFiefCount = clanSnapshot.NpcFiefCount,
				TargetSettlementName = clanSnapshot.TargetSettlementName,
				TargetSettlementType = clanSnapshot.TargetSettlementType,
				TargetDailyIncome = clanSnapshot.TargetDailyIncome,
				HighestFamilyDailyIncome = clanSnapshot.HighestFamilyDailyIncome,
				ForeignSettlementName = clanSnapshot.ForeignSettlementName,
				ForeignFactionName = clanSnapshot.ForeignFactionName,
				BorderDistance = clanSnapshot.BorderDistance,
				BorderRadius = clanSnapshot.BorderRadius
			};
			return true;
		}
		catch
		{
			return false;
		}
	}

	private SettlementSaleSnapshot GetClanSettlementSaleSnapshot(Clan clan)
	{
		string clanKey = (clan?.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(clanKey))
		{
			return null;
		}
		float cacheHour = (float)Math.Floor(NowHours());
		if (_settlementSaleSnapshotsByClan.TryGetValue(clanKey, out SettlementSaleSnapshotCacheEntry cached)
			&& cached != null
			&& Math.Abs(cached.SampledAtHour - cacheHour) < 0.01f)
		{
			return cached.Snapshot;
		}
		SettlementSaleSnapshot snapshot = ScanClanSettlementSaleSnapshot(clan);
		_settlementSaleSnapshotsByClan[clanKey] = new SettlementSaleSnapshotCacheEntry
		{
			SampledAtHour = cacheHour,
			Snapshot = snapshot
		};
		if (_settlementSaleSnapshotsByClan.Count > 128)
		{
			foreach (string key in _settlementSaleSnapshotsByClan
				.Where(pair => pair.Value == null || pair.Value.SampledAtHour < cacheHour - 1f)
				.Select(pair => pair.Key)
				.ToList())
			{
				_settlementSaleSnapshotsByClan.Remove(key);
			}
		}
		return snapshot;
	}

	private static SettlementSaleSnapshot ScanClanSettlementSaleSnapshot(Clan clan)
	{
		try
		{
			Kingdom kingdom = clan?.Kingdom;
			List<Settlement> clanFiefs = GetClanTownAndCastleSettlements(clan);
			if (kingdom == null || clanFiefs.Count < 4)
			{
				return null;
			}
			float borderRadius = GetSettlementSaleBorderRadius();
			if (borderRadius <= 0f)
			{
				return null;
			}
			List<Settlement> foreignFiefs = (Settlement.All ?? Enumerable.Empty<Settlement>())
				.Where(settlement => IsForeignFactionFortification(settlement, kingdom))
				.ToList();
			if (foreignFiefs.Count == 0)
			{
				return null;
			}
			List<SettlementSaleFiefIncome> fiefIncomes = clanFiefs
				.Select(settlement => new SettlementSaleFiefIncome
				{
					Settlement = settlement,
					DailyIncome = CalculateSettlementDailyIncomeDenars(settlement, clan)
				})
				.Where(item => item.Settlement != null)
				.ToList();
			if (fiefIncomes.Count < 4)
			{
				return null;
			}
			int lowestIncome = fiefIncomes.Min(item => item.DailyIncome);
			int highestIncome = fiefIncomes.Max(item => item.DailyIncome);
			SettlementSaleFiefIncome target = null;
			foreach (SettlementSaleFiefIncome item in fiefIncomes
				.Where(item => item.DailyIncome == lowestIncome)
				.OrderBy(item => item.Settlement.StringId, StringComparer.Ordinal))
			{
				Settlement nearestForeign = null;
				float nearestDistance = float.MaxValue;
				foreach (Settlement foreignFief in foreignFiefs)
				{
					float distance = GetSettlementTravelDistance(item.Settlement, foreignFief);
					if (distance >= 0f && distance < nearestDistance)
					{
						nearestDistance = distance;
						nearestForeign = foreignFief;
					}
				}
				if (nearestForeign == null || nearestDistance > borderRadius)
				{
					continue;
				}
				item.NearestForeignSettlement = nearestForeign;
				item.NearestForeignDistance = nearestDistance;
				target = item;
				break;
			}
			if (target == null || target.NearestForeignSettlement == null)
			{
				return null;
			}
			Settlement foreignSettlement = target.NearestForeignSettlement;
			return new SettlementSaleSnapshot
			{
				NpcFiefCount = fiefIncomes.Count,
				TargetSettlementName = GetSettlementDisplayName(target.Settlement),
				TargetSettlementType = target.Settlement.IsTown ? SocialSourceTexts.Get("S403") : SocialSourceTexts.Get("S404"),
				TargetDailyIncome = target.DailyIncome,
				HighestFamilyDailyIncome = highestIncome,
				ForeignSettlementName = GetSettlementDisplayName(foreignSettlement),
				ForeignFactionName = GetSettlementFactionDisplayName(foreignSettlement),
				BorderDistance = target.NearestForeignDistance,
				BorderRadius = borderRadius
			};
		}
		catch
		{
			return null;
		}
	}

	private static bool IsForeignFactionFortification(Settlement settlement, Kingdom ownKingdom)
	{
		return settlement != null
			&& (settlement.IsTown || settlement.IsCastle)
			&& settlement.OwnerClan != null
			&& settlement.MapFaction != ownKingdom
			&& settlement.OwnerClan.Kingdom != ownKingdom;
	}

	private static float GetSettlementSaleBorderRadius()
	{
		try
		{
			return Math.Max(0f, Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Default) * 0.66f);
		}
		catch
		{
			return 0f;
		}
	}

	private static float GetSettlementTravelDistance(Settlement fromSettlement, Settlement toSettlement)
	{
		try
		{
			float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(fromSettlement, toSettlement, false, false, MobileParty.NavigationType.Default);
			return float.IsNaN(distance) || float.IsInfinity(distance) ? -1f : distance;
		}
		catch
		{
			return -1f;
		}
	}

	private static int CalculateSettlementDailyIncomeDenars(Settlement settlement, Clan ownerClan)
	{
		try
		{
			Town town = settlement?.Town;
			if (town == null || ownerClan == null || Campaign.Current?.Models == null)
			{
				return 0;
			}
			int income = 0;
			income += (int)Campaign.Current.Models.SettlementTaxModel.CalculateTownTax(town, includeDescriptions: false).ResultNumber;
			income += (int)Campaign.Current.Models.ClanFinanceModel.CalculateTownIncomeFromTariffs(ownerClan, town, applyWithdrawals: false).ResultNumber;
			income += Campaign.Current.Models.ClanFinanceModel.CalculateTownIncomeFromProjects(town);
			foreach (Village village in town.Villages)
			{
				if (village != null)
				{
					income += Campaign.Current.Models.ClanFinanceModel.CalculateVillageIncome(ownerClan, village, applyWithdrawals: false);
				}
			}
			return Math.Max(0, income);
		}
		catch
		{
			return 0;
		}
	}

	private static string GetSettlementFactionDisplayName(Settlement settlement)
	{
		try
		{
			return (settlement?.MapFaction?.Name?.ToString()
				?? settlement?.OwnerClan?.Kingdom?.Name?.ToString()
				?? settlement?.OwnerClan?.Name?.ToString()
				?? SocialSourceTexts.Get("S339")).Trim();
		}
		catch
		{
			return SocialSourceTexts.Get("S339");
		}
	}

	private static bool IsRomanticInteractionEligible(Hero hero, out int privateRelation)
	{
		privateRelation = 0;
		try
		{
			Hero player = Hero.MainHero;
			if (hero == null
				|| player == null
				|| hero == player
				|| hero.IsDead
				|| !hero.IsAlive
				|| hero.IsPrisoner
				|| hero.IsFugitive
				|| hero.PartyBelongedToAsPrisoner != null
				|| hero.IsChild
				|| player.IsChild
				|| hero.Age < 18f
				|| player.Age < 18f
				|| hero.IsFemale == player.IsFemale)
			{
				return false;
			}
			privateRelation = Clamp(RomanceSystemBehavior.Instance?.GetPrivateLove(hero) ?? 0, -100, 100);
			return privateRelation > 30;
		}
		catch
		{
			privateRelation = 0;
			return false;
		}
	}

	private static bool IsTerritorialInterrogationOnCooldown()
	{
		try
		{
			ProactiveNpcRequestBehavior instance = Instance;
			return instance != null && instance.GetNeedTypeFatigueRemainingDays(NeedTerritorialInterrogation, NowDays()) > 0f;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsGreetingOnCooldown()
	{
		try
		{
			ProactiveNpcRequestBehavior instance = Instance;
			return instance != null && instance.GetNeedTypeFatigueRemainingDays(NeedGreeting, NowDays()) > 0f;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsGreetingEligible(Hero hero, out int privateRelation)
	{
		privateRelation = 0;
		try
		{
			Hero player = Hero.MainHero;
			if (hero == null
				|| player == null
				|| hero == player
				|| hero.IsDead
				|| !hero.IsAlive
				|| hero.IsPrisoner
				|| hero.IsFugitive
				|| hero.PartyBelongedToAsPrisoner != null)
			{
				return false;
			}
			privateRelation = Clamp(RomanceSystemBehavior.Instance?.GetPrivateLove(hero) ?? 0, -100, 100);
			return privateRelation > 20;
		}
		catch
		{
			privateRelation = 0;
			return false;
		}
	}

	private static bool IsFriendshipOnCooldown()
	{
		try
		{
			ProactiveNpcRequestBehavior instance = Instance;
			return instance != null && instance.GetNeedTypeFatigueRemainingDays(NeedFriendship, NowDays()) > 0f;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsCourtshipOnCooldown()
	{
		try
		{
			ProactiveNpcRequestBehavior instance = Instance;
			return instance != null && instance.GetNeedTypeFatigueRemainingDays(NeedCourtship, NowDays()) > 0f;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryBuildFriendshipNeedSnapshot(ProactiveCandidate candidate, out FriendshipNeedSnapshot snapshot)
	{
		snapshot = null;
		try
		{
			Hero hero = candidate?.Hero;
			Hero player = Hero.MainHero;
			if (hero == null
				|| player == null
				|| hero == player
				|| candidate.AtWarWithPlayer
				|| hero.IsDead
				|| !hero.IsAlive
				|| hero.IsPrisoner
				|| hero.IsFugitive
				|| hero.PartyBelongedToAsPrisoner != null)
			{
				return false;
			}

			int playerClanTier = Math.Max(candidate.PlayerClanTier, SafePlayerClanTier());
			if (playerClanTier < 1)
			{
				return false;
			}
			string cultureId = (hero.Culture?.StringId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(cultureId))
			{
				return false;
			}
			int cultureNotoriety = PlayerNotorietyBehavior.GetCultureNotorietyForExternal(cultureId);
			if (cultureNotoriety < 10)
			{
				return false;
			}
			int privateRelation = Clamp(RomanceSystemBehavior.Instance?.GetPrivateLove(hero) ?? 0, -100, 100);
			if (privateRelation >= 5)
			{
				return false;
			}

			snapshot = new FriendshipNeedSnapshot
			{
				CultureNotoriety = cultureNotoriety,
				PlayerClanTier = playerClanTier,
				PrivateRelation = privateRelation
			};
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryBuildCourtshipNeedSnapshot(ProactiveCandidate candidate, out CourtshipNeedSnapshot snapshot)
	{
		snapshot = null;
		try
		{
			if (!TryBuildFriendshipNeedSnapshot(candidate, out FriendshipNeedSnapshot friendship))
			{
				return false;
			}
			Hero hero = candidate.Hero;
			Hero player = Hero.MainHero;
			if (hero == null
				|| player == null
				|| hero.IsFemale == player.IsFemale)
			{
				return false;
			}
			int honor = hero.GetTraitLevel(DefaultTraits.Honor);
			int calculating = hero.GetTraitLevel(DefaultTraits.Calculating);
			bool married = IsMarriedToLivingSpouse(hero);
			float triggerWeightMultiplier = 1f;
			if (married)
			{
				// An honourable spouse does not initiate an affair. Other married heroes need a matching disposition and remain rare.
				if (honor > 0 || (honor >= 0 && calculating <= 0))
				{
					return false;
				}
				triggerWeightMultiplier = honor < 0 ? 0.20f : 0.15f;
				if (calculating > 0)
				{
					triggerWeightMultiplier += Math.Min(calculating, 2) * 0.10f;
				}
			}
			snapshot = new CourtshipNeedSnapshot
			{
				CultureNotoriety = friendship.CultureNotoriety,
				PlayerClanTier = friendship.PlayerClanTier,
				NpcClanTier = Clamp(hero.Clan?.Tier ?? 0, 0, 6),
				TriggerWeightMultiplier = Clamp(triggerWeightMultiplier, 0f, 1f)
			};
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsMarriedToLivingSpouse(Hero hero)
	{
		try
		{
			Hero spouse = hero?.Spouse;
			return spouse != null && spouse.IsAlive && !spouse.IsDead;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryBuildArmyJoinRequestSnapshot(ProactiveCandidate candidate, out ArmyJoinRequestSnapshot snapshot)
	{
		snapshot = null;
		try
		{
			MobileParty npcParty = candidate?.Party;
			MobileParty playerParty = MobileParty.MainParty;
			Hero hero = candidate?.Hero;
			Kingdom kingdom = candidate?.TargetKingdom;
			Army army = npcParty?.Army;
			if (npcParty == null
				|| playerParty == null
				|| hero == null
				|| kingdom == null
				|| kingdom.IsEliminated
				|| playerParty.Army != null
				|| playerParty.MapFaction != kingdom
				|| army == null
				|| army.Kingdom != kingdom
				|| army.LeaderParty != npcParty
				|| npcParty.LeaderHero != hero)
			{
				return false;
			}
			float ownStrength = Math.Max(0f, kingdom.CurrentTotalStrength);
			float enemyStrength = 0f;
			int enemyKingdomCount = 0;
			foreach (IFaction enemy in kingdom.FactionsAtWarWith ?? Enumerable.Empty<IFaction>())
			{
				if (enemy == null || !enemy.IsKingdomFaction || enemy.IsEliminated)
				{
					continue;
				}
				enemyStrength += Math.Max(0f, enemy.CurrentTotalStrength);
				enemyKingdomCount++;
			}
			if (ownStrength <= 0f || enemyStrength <= 0f)
			{
				return false;
			}
			float ownToEnemyRatio = ownStrength / enemyStrength;
			if (ownToEnemyRatio > 0.66f)
			{
				return false;
			}
			snapshot = new ArmyJoinRequestSnapshot
			{
				ArmyName = (army.Name?.ToString() ?? (hero.Name?.ToString() ?? SocialSourceTexts.Get("S405")) + SocialSourceTexts.Get("S406")).Trim(),
				OwnStrength = ownStrength,
				EnemyStrength = enemyStrength,
				EnemyKingdomCount = enemyKingdomCount,
				OwnToEnemyRatio = ownToEnemyRatio
			};
			return true;
		}
		catch
		{
			return false;
		}
	}

	private bool TryBuildBanditSuppressionSnapshot(ProactiveCandidate candidate, out BanditSuppressionSnapshot snapshot)
	{
		snapshot = null;
		try
		{
			Hero hero = candidate?.Hero;
			if (hero == null || candidate.AtWarWithPlayer || hero.Clan == null)
			{
				return false;
			}
			int trust = Clamp(RewardSystemBehavior.Instance?.GetEffectiveTrust(hero) ?? 0, -100, 100);
			int privateRelation = Clamp(RomanceSystemBehavior.Instance?.GetPrivateLove(hero) ?? 0, -100, 100);
			if (trust <= 10 || privateRelation <= 10)
			{
				return false;
			}
			BanditSuppressionSnapshot fiefSnapshot = GetClanBanditSuppressionSnapshot(hero.Clan);
			if (fiefSnapshot == null || fiefSnapshot.BanditCount <= 8)
			{
				return false;
			}
			snapshot = new BanditSuppressionSnapshot
			{
				SettlementName = fiefSnapshot.SettlementName,
				BanditCount = fiefSnapshot.BanditCount,
				Radius = fiefSnapshot.Radius,
				Trust = trust,
				PrivateRelation = privateRelation
			};
			return true;
		}
		catch
		{
			return false;
		}
	}

	private BanditSuppressionSnapshot GetClanBanditSuppressionSnapshot(Clan clan)
	{
		string clanKey = (clan?.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(clanKey))
		{
			return null;
		}
		float nowHours = NowHours();
		float cacheHour = (float)Math.Floor(nowHours);
		if (_banditSuppressionSnapshotsByClan.TryGetValue(clanKey, out BanditSuppressionSnapshotCacheEntry cached)
			&& cached != null
			&& Math.Abs(cached.SampledAtHour - cacheHour) < 0.01f)
		{
			return cached.Snapshot;
		}
		BanditSuppressionSnapshot snapshot = ScanClanBanditSuppressionSnapshot(clan);
		_banditSuppressionSnapshotsByClan[clanKey] = new BanditSuppressionSnapshotCacheEntry
		{
			SampledAtHour = cacheHour,
			Snapshot = snapshot
		};
		if (_banditSuppressionSnapshotsByClan.Count > 128)
		{
			foreach (string key in _banditSuppressionSnapshotsByClan
				.Where(pair => pair.Value == null || pair.Value.SampledAtHour < cacheHour - 1f)
				.Select(pair => pair.Key)
				.ToList())
			{
				_banditSuppressionSnapshotsByClan.Remove(key);
			}
		}
		return snapshot;
	}

	private static BanditSuppressionSnapshot ScanClanBanditSuppressionSnapshot(Clan clan)
	{
		try
		{
			if (clan?.Fiefs == null || clan.Fiefs.Count <= 0)
			{
				return null;
			}
			float radius = GetBanditSuppressionRadius();
			float radiusSquared = radius * radius;
			BanditSuppressionSnapshot result = null;
			foreach (Town fief in clan.Fiefs)
			{
				Settlement settlement = fief?.Settlement;
				if (settlement == null)
				{
					continue;
				}
				int banditCount = 0;
				foreach (MobileParty party in MobileParty.All ?? Enumerable.Empty<MobileParty>())
				{
					if (party == null
						|| !party.IsActive
						|| party.MapEvent != null
						|| party.CurrentSettlement != null
						|| MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(party)
						|| !CourierDeliveryBehavior.IsBanditOrOutlawParty(party)
						|| party.Position.DistanceSquared(settlement.GatePosition) > radiusSquared)
					{
						continue;
					}
					banditCount++;
				}
				if (result == null || banditCount > result.BanditCount)
				{
					result = new BanditSuppressionSnapshot
					{
						SettlementName = (settlement.Name?.ToString() ?? settlement.StringId ?? SocialSourceTexts.Get("S407")).Trim(),
						BanditCount = banditCount,
						Radius = radius
					};
				}
			}
			return result;
		}
		catch
		{
			return null;
		}
	}

	private static float GetBanditSuppressionRadius()
	{
		try
		{
			return Math.Max(1f, Campaign.Current.EstimatedAverageBanditPartySpeed * CampaignTime.HoursInDay * 0.5f);
		}
		catch
		{
			return 1f;
		}
	}

	private static bool TryBuildTerritorialInterrogationSnapshot(ProactiveCandidate candidate, DuelSettings settings, Dictionary<string, TerritorialSettlementSnapshot> territorialSettlementSnapshots, out TerritorialInterrogationSnapshot snapshot)
	{
		snapshot = null;
		try
		{
			Hero player = Hero.MainHero;
			Hero hero = candidate?.Hero;
			Kingdom kingdom = candidate?.TargetKingdom;
			Kingdom playerKingdom = (Clan.PlayerClan ?? player?.Clan)?.Kingdom;
			MobileParty mainParty = MobileParty.MainParty;
			string playerCultureId = (player?.Culture?.StringId ?? "").Trim();
			string npcCultureId = (hero?.Culture?.StringId ?? "").Trim();
			if (player == null
				|| hero == null
				|| kingdom == null
				|| kingdom.IsEliminated
				|| playerKingdom == kingdom
				|| mainParty == null
				|| string.IsNullOrWhiteSpace(playerCultureId)
				|| string.IsNullOrWhiteSpace(npcCultureId)
				|| string.Equals(playerCultureId, npcCultureId, StringComparison.OrdinalIgnoreCase)
				|| PlayerNotorietyBehavior.HasObserverUnlockedPlayerMajorForExternal(hero))
			{
				return false;
			}
			int cultureNotoriety = PlayerNotorietyBehavior.GetCultureNotorietyForExternal(npcCultureId);
			if (cultureNotoriety >= 5)
			{
				return false;
			}
			TerritorialSettlementSnapshot nearest = GetNearestKingdomSettlementSnapshot(kingdom, mainParty, territorialSettlementSnapshots);
			if (nearest == null || nearest.Distance < 0f)
			{
				return false;
			}
			float rangeMultiplier = Clamp(settings?.ProactiveNpcRequestTerritorialInterrogationSettlementRangeMultiplier ?? 3f, 0.5f, 10f);
			float maxDistance = Math.Max(1f, mainParty.SeeingRange * rangeMultiplier);
			if (nearest.Distance > maxDistance)
			{
				return false;
			}
			snapshot = new TerritorialInterrogationSnapshot
			{
				KingdomName = GetKingdomName(kingdom),
				SettlementName = nearest.SettlementName,
				SettlementDistance = nearest.Distance,
				NpcCultureName = (hero.Culture?.Name?.ToString() ?? npcCultureId).Trim(),
				CultureNotoriety = cultureNotoriety
			};
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static TerritorialSettlementSnapshot GetNearestKingdomSettlementSnapshot(Kingdom kingdom, MobileParty mainParty, Dictionary<string, TerritorialSettlementSnapshot> cache)
	{
		if (kingdom == null || mainParty == null)
		{
			return null;
		}
		string kingdomKey = GetKingdomKey(kingdom);
		if (cache != null && !string.IsNullOrWhiteSpace(kingdomKey) && cache.TryGetValue(kingdomKey, out TerritorialSettlementSnapshot cached))
		{
			return cached;
		}
		TerritorialSettlementSnapshot result = new TerritorialSettlementSnapshot { Distance = -1f };
		foreach (Settlement settlement in Settlement.All ?? Enumerable.Empty<Settlement>())
		{
			if (settlement == null
				|| settlement.IsHideout
				|| (settlement.MapFaction != kingdom && settlement.OwnerClan?.Kingdom != kingdom))
			{
				continue;
			}
			float distance = settlement.GatePosition.Distance(mainParty.Position);
			if (distance < 0f || (result.Distance >= 0f && distance >= result.Distance))
			{
				continue;
			}
			result = new TerritorialSettlementSnapshot
			{
				SettlementName = (settlement.Name?.ToString() ?? settlement.StringId ?? SocialSourceTexts.Get("S408")).Trim(),
				Distance = distance
			};
		}
		if (cache != null && !string.IsNullOrWhiteSpace(kingdomKey))
		{
			cache[kingdomKey] = result;
		}
		return result;
	}

	private static bool IsMarriageAlliancePressureNeedMet(ProactiveCandidate candidate, DuelSettings settings, out float urgency)
	{
		urgency = 0f;
		if (candidate == null || candidate.MarriageUnmarriedAdultCount <= 0)
		{
			return false;
		}
		int adultThreshold = Clamp(settings?.ProactiveNpcRequestMarriageAdultClanThreshold ?? 3, 1, 12);
		if (candidate.MarriageAdultClanHeroCount > adultThreshold && !candidate.MarriageRequesterUnmarried)
		{
			return false;
		}
		float adultDeficit = Math.Max(0, adultThreshold - candidate.MarriageAdultClanHeroCount);
		urgency = 52f + adultDeficit * 8f + Math.Min(10f, candidate.MarriageUnmarriedAdultCount * 2f);
		if (candidate.MarriageRequesterUnmarried)
		{
			urgency += 8f;
		}
		if (candidate.MarriageAdultClanHeroCount <= 1)
		{
			urgency += 8f;
		}
		return urgency >= 50f;
	}

	private static bool IsRevengePressureNeedMet(ProactiveCandidate candidate, out float urgency)
	{
		urgency = 0f;
		if (candidate == null || candidate.RevengePressureScore <= 0f)
		{
			return false;
		}
		urgency = Clamp(candidate.RevengePressureScore, 0f, 100f);
		return urgency >= 55f;
	}

	private static bool IsFiefGovernanceAnxietyNeedMet(ProactiveCandidate candidate, out float urgency)
	{
		urgency = 0f;
		if (candidate == null || candidate.FiefProblemCount <= 0)
		{
			return false;
		}
		urgency = 56f + Math.Min(18f, candidate.FiefProblemCount * 4f);
		if (candidate.FiefUnderAttack)
		{
			urgency += 18f;
		}
		if (candidate.FiefLoyalty >= 0f && candidate.FiefLoyalty <= 25f)
		{
			urgency += 8f;
		}
		if (candidate.FiefSecurity >= 0f && candidate.FiefSecurity <= 25f)
		{
			urgency += 6f;
		}
		if (candidate.FiefGarrisonCount > 0 && candidate.FiefGarrisonCount <= 40)
		{
			urgency += 4f;
		}
		urgency = Clamp(urgency, 0f, 100f);
		return urgency >= 50f;
	}

	private static bool IsAllySupportNeedMet(ProactiveCandidate candidate, DuelSettings settings, out float urgency)
	{
		urgency = 0f;
		if (candidate == null || candidate.TargetKingdom == null || candidate.Hero?.Clan == null)
		{
			return false;
		}
		int influenceThreshold = Clamp(settings?.ProactiveNpcRequestIsolationInfluenceThreshold ?? 40, 0, 500);
		int maxFriendly = Clamp(settings?.ProactiveNpcRequestIsolationMaxFriendlyClans ?? 1, 0, 10);
		bool lowInfluence = influenceThreshold > 0 && candidate.ClanInfluence <= influenceThreshold;
		bool fewFriends = candidate.FriendlyClanCount <= maxFriendly;
		bool manyEnemies = candidate.HostileClanCount >= 3;
		if (!fewFriends || (!lowInfluence && !manyEnemies))
		{
			return false;
		}
		float influencePressure = lowInfluence
			? Clamp((influenceThreshold - candidate.ClanInfluence) / Math.Max(1f, influenceThreshold), 0f, 1f) * 18f
			: 0f;
		urgency = 52f + influencePressure + Math.Min(16f, candidate.HostileClanCount * 4f);
		if (candidate.FriendlyClanCount <= 0)
		{
			urgency += 8f;
		}
		return urgency >= 50f;
	}

	private static bool IsKingdomMercenaryInviteNeedMet(ProactiveCandidate candidate, out float urgency)
	{
		urgency = 0f;
		Clan playerClan = Clan.PlayerClan;
		if (candidate == null || playerClan == null || candidate.TargetKingdom == null || !candidate.TargetClanCanOfferKingdomService || !candidate.KingdomNeedsMercenaries)
		{
			return false;
		}
		if (candidate.PlayerClanTier < MercenaryInviteMinPlayerClanTier)
		{
			return false;
		}
		if (playerClan.Kingdom != null || playerClan.IsUnderMercenaryService)
		{
			return false;
		}
		urgency = Math.Max(candidate.TargetHeroIsKingdomLeader ? 58f : 54f, candidate.KingdomMercenaryNeedUrgency);
		return true;
	}

	private static bool IsKingdomVassalInviteNeedMet(ProactiveCandidate candidate, out float urgency)
	{
		urgency = 0f;
		Clan playerClan = Clan.PlayerClan;
		if (candidate == null || playerClan == null || candidate.TargetKingdom == null || !candidate.TargetClanCanOfferKingdomService || !candidate.TargetHeroIsKingdomLeader || !candidate.KingdomNeedsVassals)
		{
			return false;
		}
		if (candidate.PlayerClanTier < VassalInviteMinPlayerClanTier)
		{
			return false;
		}
		if (playerClan.Kingdom == null)
		{
			urgency = Math.Max(64f, candidate.KingdomVassalNeedUrgency);
			return true;
		}
		if (playerClan.IsUnderMercenaryService && playerClan.Kingdom == candidate.TargetKingdom)
		{
			urgency = Math.Max(66f, candidate.KingdomVassalNeedUrgency + 2f);
			return true;
		}
		return false;
	}

}
