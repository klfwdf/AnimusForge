using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using AnimusForge.Modules.Economy;
using Helpers;
using HarmonyLib;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace AnimusForge;

public partial class RewardSystemBehavior
{
    // Synchronous main-thread application owner. The original host supplies the
    // game adapter helpers and keeps public/tag/save identities. No live object
    // is retained or sent to a worker by this coordinator.
    private static class RecruitmentOwner
    {
	internal static bool TryApplyHeroJoinPlayerPartyCore(RewardSystemBehavior host, Hero joiningHero, bool asCompanion, out string statusText, out bool joinedWildernessParty, out int joinedWildernessMembers, out int joinedWildernessPrisoners)
	{
		statusText = "";
		joinedWildernessParty = false;
		joinedWildernessMembers = 0;
		joinedWildernessPrisoners = 0;
		try
		{
			if (joiningHero == null)
			{
				statusText = SocialSourceTexts.Get("S409");
				return false;
			}
			if (joiningHero == Hero.MainHero)
			{
				statusText = SocialSourceTexts.Get("S410");
				return false;
			}
			if (Clan.PlayerClan == null || MobileParty.MainParty == null)
			{
				statusText = SocialSourceTexts.Get("S411");
				return false;
			}
			bool preservePlayerFamilyIdentity = asCompanion && ShouldPreservePlayerFamilyIdentityForCompanionJoin(joiningHero);
			bool preservePlayerSpouseIdentity = preservePlayerFamilyIdentity && (joiningHero.Spouse == Hero.MainHero || Hero.MainHero?.Spouse == joiningHero);
			if (preservePlayerFamilyIdentity)
			{
				asCompanion = false;
				Logger.Log("RewardSystemBehavior", "[HeroJoin] companion_request_redirected_to_family hero=" + (joiningHero.StringId ?? "") + " spouse=" + preservePlayerSpouseIdentity);
			}
			if (asCompanion ? IsHeroPlayerCompanionInMainParty(joiningHero) : IsHeroPlayerClanLordInMainParty(joiningHero))
			{
				statusText = asCompanion
					? string.Format(SocialSourceTexts.Get("S412"), joiningHero.Name)
					: string.Format(SocialSourceTexts.Get("S413"), joiningHero.Name);
				return false;
			}
			MobileParty originalMobileParty = joiningHero.PartyBelongedTo;
			PartyBase originalCaptivityParty = joiningHero.PartyBelongedToAsPrisoner;
			PartyBase originalParty = originalMobileParty?.Party ?? originalCaptivityParty;
			bool shouldCleanupOriginalMapParty = ShouldScheduleOriginalMapPartyCleanupAfterHeroJoin(originalMobileParty);
			TryResolveWildernessHeroJoinParty(joiningHero, out MobileParty wildernessSourceParty);
			string wildernessSourcePartyName = wildernessSourceParty?.Name?.ToString() ?? "";
			bool wildernessRosterTransferred = false;
			int movedWildernessMembers = 0;
			int movedWildernessPrisoners = 0;
			List<string> transitionNotes = new List<string>();
			Clan originalClan = GetHeroBackingClan(joiningHero) ?? joiningHero.Clan;
			Kingdom originalKingdom = originalClan?.Kingdom;
			bool originalClanWasRulingClan = originalKingdom != null && originalKingdom.RulingClan == originalClan;
			Settlement currentSettlement = joiningHero.CurrentSettlement;
			Settlement originalSettlement = ResolveOriginalSettlementForHeroJoin(joiningHero, currentSettlement);
			host.RememberHeroJoinOriginalClan(joiningHero, originalClan, originalSettlement, "hero_join_party");
			Town governorTown = joiningHero.GovernorOf;
			if (governorTown != null)
			{
				string governorTownName = governorTown.Name?.ToString() ?? SocialSourceTexts.Get("S414");
				ChangeGovernorAction.RemoveGovernorOf(joiningHero);
				transitionNotes.Add(SocialSourceTexts.Get("S415") + governorTownName + SocialSourceTexts.Get("S416"));
			}
			if (originalClan != null && originalClan != Clan.PlayerClan && originalClan.Leader == joiningHero && !originalClan.IsEliminated)
			{
				string originalClanName = GetClanDisplayNameForNotification(originalClan);
				Dictionary<Hero, int> heirApparents = originalClan.GetHeirApparents();
				if (heirApparents != null && heirApparents.Count > 0)
				{
					ChangeClanLeaderAction.ApplyWithoutSelectedNewLeader(originalClan);
					Hero newLeader = originalClan.Leader;
					if (originalClanWasRulingClan && newLeader != null && newLeader != joiningHero)
					{
						RepairRulingClanAfterLeaderRecruitmentWithElection(originalKingdom, originalClan, joiningHero, transitionNotes);
					}
					if (newLeader == null || newLeader == joiningHero)
					{
						statusText = SocialSourceTexts.Get("S220") + originalClanName + SocialSourceTexts.Get("S417");
						return false;
					}
					transitionNotes.Add(originalClanName + SocialSourceTexts.Get("S418") + newLeader.Name + SocialSourceTexts.Get("S419"));
				}
				else
				{
					TransferWildernessHeroPartyRosterToMainParty(wildernessSourceParty, ref wildernessRosterTransferred, ref movedWildernessMembers, ref movedWildernessPrisoners);
					bool destroyOriginalKingdomAfterClanDestroyed = false;
					if (originalClanWasRulingClan)
					{
						PrepareRulingClanTransitionForDepartingClan(originalKingdom, originalClan, transitionNotes, out destroyOriginalKingdomAfterClanDestroyed);
					}
					DestroyClanAction.ApplyByClanLeaderDeath(originalClan);
					transitionNotes.Add(originalClanName + SocialSourceTexts.Get("S420"));
					FinalizeRulingClanTransitionForDepartedClan(originalKingdom, destroyOriginalKingdomAfterClanDestroyed, transitionNotes);
				}
			}
			if (!TryEndHeroCaptivityForPlayerJoin(joiningHero, originalCaptivityParty, asCompanion ? "hero_join_party_companion" : "hero_join_party_lord", transitionNotes, out string captivityStatus))
			{
				statusText = captivityStatus;
				return false;
			}
			if (currentSettlement != null && joiningHero.CurrentSettlement != null)
			{
				LeaveSettlementAction.ApplyForCharacterOnly(joiningHero);
			}
			TransferWildernessHeroPartyRosterToMainParty(wildernessSourceParty, ref wildernessRosterTransferred, ref movedWildernessMembers, ref movedWildernessPrisoners);
			bool moved = asCompanion
				? TryMoveHeroToPlayerClanAsCompanionAndMainParty(joiningHero, "hero_join_party_companion", out statusText)
				: TryMoveHeroToPlayerClanAsLordAndMainParty(joiningHero, "hero_join_party_lord", out statusText);
			if (!moved)
			{
				return false;
			}
			if (LocationComplex.Current != null)
			{
				LocationComplex.Current.RemoveCharacterIfExists(joiningHero);
			}
			PlayerEncounter.LocationEncounter?.RemoveAccompanyingCharacter(joiningHero);
			if (wildernessSourceParty != null)
			{
				joinedWildernessParty = true;
				joinedWildernessMembers = movedWildernessMembers;
				joinedWildernessPrisoners = movedWildernessPrisoners;
				transitionNotes.Add(BuildWildernessHeroPartyTransitionNote(wildernessSourcePartyName, movedWildernessMembers, movedWildernessPrisoners));
				Logger.Log("RewardSystemBehavior", "[HeroJoin] wilderness_party_join source=" + (wildernessSourceParty.StringId ?? "") + " members=" + movedWildernessMembers + " prisoners=" + movedWildernessPrisoners + " hero=" + (joiningHero.StringId ?? ""));
			}
			string transitionSummary = BuildRecruitmentTransitionSummary(transitionNotes);
			RecordHeroJoinedPlayerClanForExternal(joiningHero, preservePlayerFamilyIdentity ? "hero_join_party_family_preserved" : (asCompanion ? "hero_join_party_companion" : "hero_join_party_lord"), asCompanion, preservePlayerFamilyIdentity, joinedWildernessParty, joinedWildernessMembers, joinedWildernessPrisoners);
			statusText = preservePlayerFamilyIdentity
				? string.Format(SocialSourceTexts.Get("S422"), joiningHero.Name, (preservePlayerSpouseIdentity ? SocialSourceTexts.Get("S277") : SocialSourceTexts.Get("S421")), transitionSummary)
				: (asCompanion
					? string.Format(SocialSourceTexts.Get("S423"), joiningHero.Name, transitionSummary)
					: string.Format(SocialSourceTexts.Get("S424"), joiningHero.Name, transitionSummary));
			if (shouldCleanupOriginalMapParty && IsEmptyMapPartyAfterHeroJoin(originalMobileParty))
			{
				CloseHeroJoinMapPartyConversationImmediately(joiningHero, originalParty, originalMobileParty);
			}
			else
			{
				ScheduleHeroJoinConversationClose(joiningHero, originalParty, originalMobileParty, shouldCleanupOriginalMapParty);
			}
			return true;
		}
		catch (Exception ex)
		{
			statusText = SocialSourceTexts.Get("S425") + ex.Message;
			return false;
		}
	}

	internal static bool TryApplyNonHeroJoinPlayerPartyForExternal(CharacterObject joiningCharacter, int targetAgentIndex, string promptGivenName, string promptDisplayName, string latestReply, bool asCompanion, out string statusText, out Hero promotedHero)
	{
		statusText = "";
		promotedHero = null;
		try
		{
			if (joiningCharacter == null)
			{
				statusText = SocialSourceTexts.Get("S426");
				return false;
			}
			if (joiningCharacter.IsHero)
			{
				statusText = SocialSourceTexts.Get("S427");
				return false;
			}
			if (MobileParty.MainParty == null || Hero.MainHero == null || Clan.PlayerClan == null)
			{
				statusText = SocialSourceTexts.Get("S428");
				return false;
			}
			bool wildernessJoinHandled;
			bool wildernessJoinApplied = TryApplyWildernessNonHeroPartyJoinPlayerPartyForExternal(joiningCharacter, targetAgentIndex, promptGivenName, promptDisplayName, out wildernessJoinHandled, out statusText);
			if (wildernessJoinHandled)
			{
				return wildernessJoinApplied;
			}
			if (TryGetPromotedNonHeroCompanion(targetAgentIndex, out var existingPromotedHero))
			{
				promotedHero = existingPromotedHero;
				string heroName = existingPromotedHero?.Name?.ToString() ?? ResolveNonHeroFullDisplayName(joiningCharacter, promptDisplayName, promptGivenName, targetAgentIndex);
				statusText = string.Format(SocialSourceTexts.Get("S429"), heroName);
				return false;
			}
			TavernMercenaryPoolJoinResolution tavernPoolResolution = ResolveTavernMercenaryPoolJoin(joiningCharacter, targetAgentIndex, out var mercenaryData, out int count);
			if (tavernPoolResolution == TavernMercenaryPoolJoinResolution.Stale)
			{
				statusText = SocialSourceTexts.Get("S430");
				Logger.Log("RewardSystemBehavior", "[NonHeroJoin] stale tavern pool join intercepted target=" + (joiningCharacter.StringId ?? "") + " agentIndex=" + targetAgentIndex);
				return false;
			}
			if (tavernPoolResolution == TavernMercenaryPoolJoinResolution.Ready)
			{
				count = Math.Max(1, count);
				CloseTavernMercenaryJoinConversationImmediately(joiningCharacter);
				mercenaryData.ChangeMercenaryCount(-count);
				MobileParty.MainParty.MemberRoster.AddToCounts(joiningCharacter, count, false, 0, 0, true, -1);
				CampaignEventDispatcher.Instance.OnUnitRecruited(joiningCharacter, count);
				RemoveJoinedNonHeroLocationCharacters(joiningCharacter, targetAgentIndex, removeAllMatchingTavernMercenaries: true);
				RemoveJoinedLiveAgent(targetAgentIndex);
				string name = joiningCharacter.Name?.ToString() ?? SocialSourceTexts.Get("S431");
				statusText = string.Format(SocialSourceTexts.Get("S432"), count, name);
				return true;
			}
			return TryPromoteNonHeroToCompanion(joiningCharacter, targetAgentIndex, promptGivenName, promptDisplayName, latestReply, asCompanion, out statusText, out promotedHero);
		}
		catch (Exception ex)
		{
			statusText = SocialSourceTexts.Get("S425") + ex.Message;
			return false;
		}
	}

	internal static bool TryPromoteNonHeroToCompanion(CharacterObject joiningCharacter, int targetAgentIndex, string promptGivenName, string promptDisplayName, string latestReply, bool asCompanion, out string statusText, out Hero promotedHero)
	{
		statusText = "";
		promotedHero = null;
		Agent agent = ResolveAgentForIndex(targetAgentIndex);
		if (agent == null || !agent.IsActive())
		{
			statusText = SocialSourceTexts.Get("S433");
			return false;
		}
		CharacterObject template = agent.Character as CharacterObject;
		if (template == null)
		{
			template = joiningCharacter;
		}
		if (template == null || template.IsHero)
		{
			statusText = SocialSourceTexts.Get("S434");
			return false;
		}
		if (!TryCaptureAgentBodyProperties(agent, out var bodyProperties, out var bodyError))
		{
			statusText = SocialSourceTexts.Get("S435") + bodyError + SocialSourceTexts.Get("S436");
			return false;
		}
		if (!TryCaptureAgentEquipment(agent, out var capturedEquipment, out var equipmentError))
		{
			statusText = SocialSourceTexts.Get("S437") + equipmentError + SocialSourceTexts.Get("S436");
			return false;
		}
		string originalFullName = ResolveNonHeroFullDisplayName(template, promptDisplayName, promptGivenName, targetAgentIndex);
		string originalTroopName = template.Name?.ToString() ?? joiningCharacter.Name?.ToString() ?? SocialSourceTexts.Get("S438");
		string personalName = ResolveNonHeroPersonalName(promptGivenName, originalFullName, originalTroopName);
		if (string.IsNullOrWhiteSpace(personalName))
		{
			statusText = SocialSourceTexts.Get("S439");
			return false;
		}
		PlayerOwnedTroopPromotionReservation reservedPlayerTroop;
		if (!TryReservePlayerOwnedTroopForPromotion(agent, template, out reservedPlayerTroop, out var reserveError))
		{
			statusText = reserveError;
			return false;
		}
		bool promotionCompleted = false;
		try
		{
			Settlement bornSettlement = Settlement.CurrentSettlement ?? PlayerEncounter.EncounterSettlement ?? MobileParty.MainParty?.CurrentSettlement;
			int age = ResolvePromotedHeroAge(bodyProperties, template);
			Hero hero = HeroCreator.CreateSpecialHero(template, bornSettlement, null, null, age);
			TextObject heroName = new TextObject(personalName);
			hero.SetName(heroName, heroName);
			hero.StaticBodyProperties = bodyProperties.StaticProperties;
			hero.Weight = ClampBodyShape01(bodyProperties.DynamicProperties.Weight);
			hero.Build = ClampBodyShape01(bodyProperties.DynamicProperties.Build);
			TryActivatePromotedCompanionHero(hero, "new_nonhero_promotion");
			ApplyTemplateSkillsToHero(hero, template);
			ApplyPromotedCompanionRandomTraits(hero, template);
			CopyCapturedEquipmentToHero(hero, capturedEquipment);
			bool moved = asCompanion
				? TryMoveHeroToPlayerClanAsCompanionAndMainParty(hero, "nonhero_join_party_companion_promotion", out statusText)
				: TryMoveHeroToPlayerClanAsLordAndMainParty(hero, "nonhero_join_party_lord_promotion", out statusText);
			if (!moved)
			{
				return false;
			}
			promotionCompleted = true;
			promotedHero = hero;
			RememberPromotedNonHeroCompanion(targetAgentIndex, hero);
			LogPromotedCompanionGovernorEligibility(hero, "after_nonhero_promotion");
			bool sceneFollowStarted = ShoutBehavior.TryForceSceneFollowPlayerForExternal(targetAgentIndex, transient: true, reason: "nonhero_join_party_promotion");
			RemoveJoinedNonHeroLocationCharacters(template, targetAgentIndex, removeAllMatchingTavernMercenaries: false);
			string cultureName = template.Culture?.Name?.ToString() ?? template.Culture?.StringId ?? "";
			string sceneLabel = BuildCurrentSceneLabelForPrompt();
			List<string> dialogueHistory = ShoutBehavior.GetAuxiliarySceneDialogueHistoryLinesForExternal(targetAgentIndex, 40) ?? new List<string>();
			string cleanLatestReply = StripNonHeroJoinTag(latestReply);
			if (!string.IsNullOrWhiteSpace(cleanLatestReply))
			{
				dialogueHistory.Add((string.IsNullOrWhiteSpace(originalFullName) ? "NPC" : originalFullName) + ": " + cleanLatestReply);
			}
			string joinFact = asCompanion
				? string.Format(SocialSourceTexts.Get("S440"), hero.Name, originalTroopName, sceneLabel)
				: string.Format(SocialSourceTexts.Get("S441"), hero.Name, originalTroopName, sceneLabel);
			MyBehavior.AppendExternalDialogueHistory(hero, null, null, SocialSourceTexts.Get("S308") + joinFact);
			RecordHeroJoinedPlayerClanForExternal(hero, asCompanion ? "nonhero_join_party_companion_promotion" : "nonhero_join_party_lord_promotion", asCompanion);
			AppendPromotedHeroPriorHistory(hero, dialogueHistory);
			string equipmentSummary = BuildEquipmentSummaryForPrompt(capturedEquipment);
			_ = MyBehavior.GeneratePromotedNonHeroCompanionProfileForExternalAsync(hero, personalName, originalFullName, originalTroopName, template.StringId ?? "", cultureName, sceneLabel, joinFact, BuildDialogueHistoryForPrompt(dialogueHistory), equipmentSummary);
			statusText = asCompanion
				? string.Format(SocialSourceTexts.Get("S443"), originalFullName, hero.Name, (sceneFollowStarted ? SocialSourceTexts.Get("S442") : ""))
				: string.Format(SocialSourceTexts.Get("S444"), originalFullName, hero.Name, (sceneFollowStarted ? SocialSourceTexts.Get("S442") : ""));
			return true;
		}
		finally
		{
			if (!promotionCompleted && reservedPlayerTroop != null)
			{
				RestoreReservedPlayerOwnedTroopAfterFailedPromotion(reservedPlayerTroop);
			}
		}
	}
    }
}
