using AnimusForge.Refactor.Runtime;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.SiegeAftermathIntervention;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SandBox.Tournaments.MissionLogics;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

using PartyTransferPromptEntry = AnimusForge.MyBehavior.PartyTransferPromptEntry;
using PartyTransferEntrySection = AnimusForge.MyBehavior.PartyTransferEntrySection;
using SettlementTransferPromptEntry = AnimusForge.MyBehavior.SettlementTransferPromptEntry;
using SettlementTransferEntrySection = AnimusForge.MyBehavior.SettlementTransferEntrySection;
using SettlementTransferAssetKind = AnimusForge.MyBehavior.SettlementTransferAssetKind;

namespace AnimusForge;

internal static class PartyAssetTransferBannerlordAdapter
{
	internal static int TransferItemsFromRosterByStringId(ItemRoster sourceRoster, ItemRoster targetRoster, string itemId, int amount, out ItemObject transferredItem)
	{
		transferredItem = null;
		if (sourceRoster == null || string.IsNullOrWhiteSpace(itemId) || amount <= 0)
		{
			return 0;
		}
		string text = itemId.Trim();
		int num = amount;
		int num2 = 0;
		while (num > 0)
		{
			bool flag = false;
			for (int i = 0; i < sourceRoster.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = sourceRoster.GetElementCopyAtIndex(i);
				EquipmentElement equipmentElement = elementCopyAtIndex.EquipmentElement;
				ItemObject item = equipmentElement.Item;
				if (item == null || elementCopyAtIndex.Amount <= 0 || !string.Equals(item.StringId ?? "", text, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				transferredItem = transferredItem ?? item;
				int num3 = Math.Min(elementCopyAtIndex.Amount, num);
				if (num3 <= 0)
				{
					continue;
				}
				sourceRoster.AddToCounts(equipmentElement, -num3);
				targetRoster?.AddToCounts(equipmentElement, num3);
				num -= num3;
				num2 += num3;
				flag = true;
				break;
			}
			if (!flag)
			{
				break;
			}
		}
		return num2;
	}

	internal static PartyBase ResolvePartyTransferCounterpartyInternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
	{
		try
		{
			Hero hero = targetHero ?? targetCharacter?.HeroObject;
			if (hero != null)
			{
				if (hero.PartyBelongedTo?.Party != null)
				{
					return hero.PartyBelongedTo.Party;
				}
				if (hero.IsPrisoner)
				{
					PartyBase captiveCounterparty = ResolvePartyTransferCounterpartyForCaptiveHero(hero);
					if (captiveCounterparty != null)
					{
						return captiveCounterparty;
					}
					return null;
				}
				if (hero.Clan?.Leader?.PartyBelongedTo?.Party != null)
				{
					return hero.Clan.Leader.PartyBelongedTo.Party;
				}
			}
		}
		catch
		{
		}
		try
		{
			if (targetAgentIndex >= 0)
			{
				Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex);
				PartyBase partyBase = agent?.Origin?.BattleCombatant as PartyBase;
				if (partyBase != null)
				{
					return partyBase;
				}
				Hero hero2 = MyBehavior.ResolveTransferTargetHeroFromAgent(agent);
				if (hero2?.PartyBelongedTo?.Party != null)
				{
					return hero2.PartyBelongedTo.Party;
				}
				if (hero2?.Clan?.Leader?.PartyBelongedTo?.Party != null)
				{
					return hero2.Clan.Leader.PartyBelongedTo.Party;
				}
			}
		}
		catch
		{
		}
		try
		{
			if (targetCharacter != null && targetCharacter.HeroObject == null && Settlement.CurrentSettlement == null && MobileParty.MainParty?.CurrentSettlement == null)
			{
				PartyBase partyBase = NormalizePartyTransferCounterparty(MobileParty.ConversationParty?.Party);
				if (IsNonHeroConversationCounterpartyForCharacter(partyBase, targetCharacter))
				{
					return partyBase;
				}
				partyBase = NormalizePartyTransferCounterparty(PlayerEncounter.EncounteredParty);
				if (IsNonHeroConversationCounterpartyForCharacter(partyBase, targetCharacter))
				{
					return partyBase;
				}
			}
		}
		catch
		{
		}
		try
		{
			if (Settlement.CurrentSettlement?.Town?.GarrisonParty?.Party != null)
			{
				return Settlement.CurrentSettlement.Town.GarrisonParty.Party;
			}
		}
		catch
		{
		}
		try
		{
			if (Settlement.CurrentSettlement?.OwnerClan?.Leader?.PartyBelongedTo?.Party != null)
			{
				return Settlement.CurrentSettlement.OwnerClan.Leader.PartyBelongedTo.Party;
			}
		}
		catch
		{
		}
		try
		{
			return Settlement.CurrentSettlement?.Party;
		}
		catch
		{
			return null;
		}
	}

	internal static PartyBase ResolvePartyTransferCounterpartyForCaptiveHero(Hero captiveHero)
	{
		if (captiveHero == null)
		{
			return null;
		}
		PartyBase party = NormalizePartyTransferCounterparty(captiveHero.PartyBelongedTo?.Party);
		if (party != null)
		{
			return party;
		}
		party = NormalizePartyTransferCounterparty(captiveHero.Clan?.Leader?.PartyBelongedTo?.Party);
		if (party != null)
		{
			return party;
		}
		try
		{
			foreach (Hero clanHero in captiveHero.Clan?.Heroes ?? Enumerable.Empty<Hero>())
			{
				if (clanHero == null || clanHero == captiveHero || !clanHero.IsAlive || clanHero.IsPrisoner)
				{
					continue;
				}
				party = NormalizePartyTransferCounterparty(clanHero.PartyBelongedTo?.Party);
				if (party != null)
				{
					return party;
				}
			}
		}
		catch
		{
		}
		try
		{
			Settlement home = captiveHero.HomeSettlement;
			if (home?.Party != null && home.OwnerClan == captiveHero.Clan)
			{
				party = NormalizePartyTransferCounterparty(home.Party);
				if (party != null)
				{
					return party;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	internal static bool IsNonHeroConversationCounterpartyForCharacter(PartyBase party, CharacterObject targetCharacter)
	{
		if (party == null || targetCharacter == null || targetCharacter.HeroObject != null || IsPlayerMainPartyBase(party))
		{
			return false;
		}
		try
		{
			if (party.MemberRoster != null && party.MemberRoster.Contains(targetCharacter))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			CharacterObject conversationLeader = TaleWorlds.CampaignSystem.Conversation.ConversationHelper.GetConversationCharacterPartyLeader(party);
			return conversationLeader == targetCharacter;
		}
		catch
		{
			return false;
		}
	}

	internal static PartyBase NormalizePartyTransferCounterparty(PartyBase party)
	{
		if (party == null || IsPlayerMainPartyBase(party))
		{
			return null;
		}
		return party;
	}

	internal static bool IsPlayerMainPartyBase(PartyBase party)
	{
		try
		{
			return party != null && (party == PartyBase.MainParty || party.MobileParty == MobileParty.MainParty);
		}
		catch
		{
			return false;
		}
	}

	public static PartyBase ResolvePartyTransferCounterpartyForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
	{
		return ResolvePartyTransferCounterpartyInternal(targetHero, targetCharacter, targetAgentIndex);
	}

	internal static string ResolvePartyTransferTargetDisplayName(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
	{
		string text = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		try
		{
			if (targetAgentIndex >= 0)
			{
				Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex);
				text = (agent?.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text;
				}
			}
		}
		catch
		{
		}
		return "对方";
	}

	internal static string GetPartyTransferEntryDisplayName(CharacterObject character)
	{
		if (character == null)
		{
			return "未知";
		}
		string text = (character.Name?.ToString() ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return (character.StringId ?? "未知").Trim();
	}

	internal static bool IsPartyTransferVolunteerEntry(PartyTransferPromptEntry entry)
	{
		return entry != null && entry.Section == PartyTransferEntrySection.NpcVolunteers && entry.VolunteerOwner != null && entry.Character != null;
	}

	internal static int GetMaximumRecruitableVolunteerIndex(Hero sellerHero)
	{
		if (sellerHero == null || Hero.MainHero == null || Campaign.Current?.Models?.VolunteerModel == null)
		{
			return -1;
		}
		try
		{
			return Math.Min(5, Campaign.Current.Models.VolunteerModel.MaximumIndexHeroCanRecruitFromHero(Hero.MainHero, sellerHero));
		}
		catch
		{
			return -1;
		}
	}

	internal static bool IsPartyTransferNotableRecruitEligible(Hero hero)
	{
		if (hero == null || !hero.IsAlive || !hero.IsNotable || hero.VolunteerTypes == null || Campaign.Current?.Models?.VolunteerModel == null)
		{
			return false;
		}
		try
		{
			return Campaign.Current.Models.VolunteerModel.CanHaveRecruits(hero);
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsSamePartyTransferCharacter(CharacterObject left, CharacterObject right)
	{
		if (left == null || right == null)
		{
			return false;
		}
		if (ReferenceEquals(left, right))
		{
			return true;
		}
		string leftId = (left.StringId ?? "").Trim();
		string rightId = (right.StringId ?? "").Trim();
		return !string.IsNullOrWhiteSpace(leftId) && string.Equals(leftId, rightId, StringComparison.OrdinalIgnoreCase);
	}

	// Used at preprocess, prompt, and action boundaries. Do not cache this authorization:
	// it checks only the current Agent and one live party, so revalidation stays bounded and
	// prevents a changed encounter or roster from reusing stale transfer authority.
	internal static bool TryResolveWildernessNonHeroPartyTransferSource(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, out PartyBase sourceParty)
	{
		sourceParty = null;
		try
		{
			if (targetHero != null || targetCharacter == null || targetCharacter.HeroObject != null || targetCharacter.IsHero)
			{
				return false;
			}
			if (Settlement.CurrentSettlement != null || MobileParty.MainParty?.CurrentSettlement != null || PlayerEncounterCompat.IsInPostBattleResultFlow())
			{
				return false;
			}

			PartyBase resolved;
			Mission mission = Mission.Current;
			if (targetAgentIndex >= 0)
			{
				if (mission == null)
				{
					return false;
				}
				MissionMode mode = mission.Mode;
				bool isPeacefulSceneMode = mode == MissionMode.StartUp
					|| mode == MissionMode.Conversation
					|| mode == MissionMode.Barter
					|| (mode == MissionMode.Battle
						&& LordEncounterBehavior.IsEncounterMeetingMissionActive
						&& !MeetingBattleRuntime.IsCombatEscalated);
				if (!isPeacefulSceneMode)
				{
					return false;
				}
				Agent agent = mission.Agents?.FirstOrDefault((Agent candidate) => candidate != null && candidate.Index == targetAgentIndex);
				CharacterObject agentCharacter = agent?.Character as CharacterObject;
				if (agent == null || !agent.IsActive() || agentCharacter == null || agentCharacter.HeroObject != null || !IsSamePartyTransferCharacter(agentCharacter, targetCharacter))
				{
					return false;
				}
				resolved = NormalizePartyTransferCounterparty(agent?.Origin?.BattleCombatant as PartyBase);
			}
			else
			{
				if (mission != null)
				{
					return false;
				}
				resolved = NormalizePartyTransferCounterparty(ResolvePartyTransferCounterpartyInternal(null, targetCharacter, -1));
			}

			MobileParty mobileParty = resolved?.MobileParty;
			if (resolved == null || !resolved.IsMobile || mobileParty == null || !mobileParty.IsActive
				|| mobileParty == MobileParty.MainParty || mobileParty.CurrentSettlement != null
				|| mobileParty.LeaderHero != null || CourierDeliveryBehavior.IsCourierParty(mobileParty)
				|| !IsNonHeroConversationCounterpartyForCharacter(resolved, targetCharacter))
			{
				return false;
			}
			sourceParty = resolved;
			return true;
		}
		catch
		{
			sourceParty = null;
			return false;
		}
	}

	public static bool IsWildernessNonHeroPartyTransferEligibleForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
	{
		return TryResolveWildernessNonHeroPartyTransferSource(targetHero, targetCharacter, targetAgentIndex, out var _);
	}

	internal static bool IsPartyTransferRuleEligible(Hero targetHero, CharacterObject targetCharacter = null, int targetAgentIndex = -1)
	{
		Hero hero = ResolvePartyTransferRuleHero(targetHero, targetCharacter);
		return IsPartyTransferLordEligible(targetHero, targetCharacter)
			|| IsPartyTransferNotableRecruitEligible(hero)
			|| TryResolveWildernessNonHeroPartyTransferSource(targetHero, targetCharacter, targetAgentIndex, out var _);
	}

	internal static void AddPartyTransferEntriesFromRoster(List<PartyTransferPromptEntry> entries, TroopRoster roster, PartyBase ownerParty, PartyTransferEntrySection section, ref int nextPromptIndex, CharacterObject reservedCharacter = null, int reservedCount = 0)
	{
		if (entries == null || roster == null)
		{
			return;
		}
		bool flag = section == PartyTransferEntrySection.PlayerPrisoners || section == PartyTransferEntrySection.NpcPrisoners;
		for (int i = 0; i < roster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = roster.GetElementCopyAtIndex(i);
			CharacterObject character = elementCopyAtIndex.Character;
			int availableCount = Math.Max(0, elementCopyAtIndex.Number);
			if (!flag && reservedCount > 0 && IsSamePartyTransferCharacter(character, reservedCharacter))
			{
				availableCount = Math.Max(0, availableCount - reservedCount);
			}
			if (character == null || availableCount <= 0)
			{
				continue;
			}
			if (flag && character.IsHero)
			{
				Hero heroObject = character.HeroObject;
				if (heroObject == null || !heroObject.IsPrisoner || heroObject.PartyBelongedToAsPrisoner != ownerParty)
				{
					continue;
				}
			}
			if (!flag && (character.IsHero || character == CharacterObject.PlayerCharacter))
			{
				continue;
			}
			PartyTransferPromptEntry item = new PartyTransferPromptEntry
			{
				PromptIndex = nextPromptIndex++,
				Section = section,
				Character = character,
				DisplayName = GetPartyTransferEntryDisplayName(character),
				Count = availableCount,
				WoundedCount = flag ? 0 : Math.Min(availableCount, Math.Max(0, elementCopyAtIndex.WoundedNumber)),
				WageDenarsPerDay = flag ? 0 : Math.Max(1, character.TroopWage),
				HirePriceDenarsPerUnit = flag ? 0 : GetPartyTransferHirePrice(character),
				BuyPriceDenarsPerUnit = flag ? GetPartyTransferPrisonerPrice(character) : 0,
				IsHero = character.IsHero,
				OwnerParty = ownerParty
			};
			entries.Add(item);
		}
	}

	internal static void AddPartyTransferDungeonHeroEntriesFromParty(List<PartyTransferPromptEntry> entries, PartyBase ownerParty, Settlement sourceSettlement, PartyTransferEntrySection section, ref int nextPromptIndex)
	{
		if (entries == null || ownerParty?.PrisonRoster == null || sourceSettlement == null)
		{
			return;
		}
		TroopRoster prisonRoster = ownerParty.PrisonRoster;
		for (int i = 0; i < prisonRoster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = prisonRoster.GetElementCopyAtIndex(i);
			CharacterObject character = elementCopyAtIndex.Character;
			Hero heroObject = character?.HeroObject;
			if (character == null || !character.IsHero || heroObject == null || elementCopyAtIndex.Number <= 0 || !heroObject.IsPrisoner || heroObject.PartyBelongedToAsPrisoner != ownerParty)
			{
				continue;
			}
			if (entries.Any((PartyTransferPromptEntry x) => x != null && x.Section == section && x.Character == character))
			{
				continue;
			}
			entries.Add(new PartyTransferPromptEntry
			{
				PromptIndex = nextPromptIndex++,
				Section = section,
				Character = character,
				DisplayName = GetPartyTransferEntryDisplayName(character),
				Count = 1,
				WoundedCount = 0,
				WageDenarsPerDay = 0,
				HirePriceDenarsPerUnit = 0,
				BuyPriceDenarsPerUnit = GetPartyTransferPrisonerPrice(character),
				IsHero = true,
				OwnerParty = ownerParty,
				SourceSettlement = sourceSettlement
			});
		}
	}

	internal static void AddPartyTransferDungeonHeroEntries(List<PartyTransferPromptEntry> entries, Clan ownerClan, PartyTransferEntrySection section, ref int nextPromptIndex)
	{
		if (entries == null || ownerClan == null)
		{
			return;
		}
		foreach (Town fief in ownerClan.Fiefs)
		{
			Settlement settlement = fief?.Settlement;
			if (settlement == null || !settlement.IsFortification || settlement.OwnerClan != ownerClan)
			{
				continue;
			}
			AddPartyTransferDungeonHeroEntriesFromParty(entries, settlement.Party, settlement, section, ref nextPromptIndex);
			foreach (MobileParty party in settlement.Parties)
			{
				if (party != null && party.IsGarrison)
				{
					AddPartyTransferDungeonHeroEntriesFromParty(entries, party.Party, settlement, section, ref nextPromptIndex);
				}
			}
		}
	}

	internal static void AddPartyTransferEntriesFromVolunteers(List<PartyTransferPromptEntry> entries, Hero notable, ref int nextPromptIndex)
	{
		if (entries == null || !IsPartyTransferNotableRecruitEligible(notable))
		{
			return;
		}
		int maxIndex = GetMaximumRecruitableVolunteerIndex(notable);
		if (maxIndex < 0)
		{
			return;
		}
		Dictionary<string, PartyTransferPromptEntry> dictionary = new Dictionary<string, PartyTransferPromptEntry>(StringComparer.OrdinalIgnoreCase);
		CharacterObject[] volunteerTypes = notable.VolunteerTypes;
		int num = Math.Min(Math.Min(5, maxIndex), volunteerTypes.Length - 1);
		for (int i = 0; i <= num; i++)
		{
			CharacterObject characterObject = volunteerTypes[i];
			if (characterObject == null || characterObject.IsHero)
			{
				continue;
			}
			string text = (characterObject.StringId ?? GetPartyTransferEntryDisplayName(characterObject)).Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = GetPartyTransferEntryDisplayName(characterObject);
			}
			if (!dictionary.TryGetValue(text, out var value))
			{
				value = new PartyTransferPromptEntry
				{
					PromptIndex = nextPromptIndex++,
					Section = PartyTransferEntrySection.NpcVolunteers,
					Character = characterObject,
					DisplayName = GetPartyTransferEntryDisplayName(characterObject),
					Count = 0,
					WoundedCount = 0,
					WageDenarsPerDay = Math.Max(1, characterObject.TroopWage),
					HirePriceDenarsPerUnit = GetPartyTransferHirePrice(characterObject),
					BuyPriceDenarsPerUnit = 0,
					IsHero = false,
					OwnerParty = null,
					VolunteerOwner = notable,
					VolunteerSlotIndices = new List<int>()
				};
				dictionary[text] = value;
				entries.Add(value);
			}
			value.Count++;
			value.VolunteerSlotIndices.Add(i);
		}
	}

	internal static int GetPartyTransferHirePrice(CharacterObject character)
	{
		if (character == null)
		{
			return 1;
		}
		try
		{
			return Math.Max(1, Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(character, Hero.MainHero, withoutItemCost: false).RoundedResultNumber);
		}
		catch
		{
			return 1;
		}
	}

	internal static int GetPartyTransferPrisonerPrice(CharacterObject character)
	{
		if (character == null)
		{
			return 1;
		}
		try
		{
			return Math.Max(1, Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(character, null));
		}
		catch
		{
			return 1;
		}
	}

	internal static List<PartyTransferPromptEntry> BuildPartyTransferPromptEntriesInternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
	{
		return BuildPartyTransferPromptEntriesInternal(targetHero, targetCharacter, targetAgentIndex, out var _);
	}

	internal static List<PartyTransferPromptEntry> BuildPartyTransferPromptEntriesInternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, out PartyBase wildernessNonHeroSource)
	{
		List<PartyTransferPromptEntry> list = new List<PartyTransferPromptEntry>();
		wildernessNonHeroSource = null;
		Hero hero = ResolvePartyTransferRuleHero(targetHero, targetCharacter);
		bool isLord = IsPartyTransferLordEligible(targetHero, targetCharacter);
		bool isNotable = IsPartyTransferNotableRecruitEligible(hero);
		bool isWildernessNonHero = TryResolveWildernessNonHeroPartyTransferSource(targetHero, targetCharacter, targetAgentIndex, out wildernessNonHeroSource);
		if (!isLord && !isNotable && !isWildernessNonHero)
		{
			return list;
		}
		PartyBase partyBase = Hero.MainHero?.PartyBelongedTo?.Party ?? MobileParty.MainParty?.Party;
		PartyBase partyBase2 = wildernessNonHeroSource ?? ResolvePartyTransferCounterpartyInternal(targetHero, targetCharacter, targetAgentIndex);
		int num = 1;
		if (isLord && partyBase != null)
		{
			AddPartyTransferEntriesFromRoster(list, partyBase.MemberRoster, partyBase, PartyTransferEntrySection.PlayerTroops, ref num);
			AddPartyTransferEntriesFromRoster(list, partyBase.PrisonRoster, partyBase, PartyTransferEntrySection.PlayerPrisoners, ref num);
		}
		if (isLord)
		{
			AddPartyTransferDungeonHeroEntries(list, Clan.PlayerClan, PartyTransferEntrySection.PlayerPrisoners, ref num);
		}
		if ((isLord || isWildernessNonHero) && partyBase2 != null)
		{
			AddPartyTransferEntriesFromRoster(list, partyBase2.MemberRoster, partyBase2, PartyTransferEntrySection.NpcTroops, ref num, isWildernessNonHero ? targetCharacter : null, isWildernessNonHero ? 1 : 0);
			AddPartyTransferEntriesFromRoster(list, partyBase2.PrisonRoster, partyBase2, PartyTransferEntrySection.NpcPrisoners, ref num);
		}
		if (isLord)
		{
			AddPartyTransferDungeonHeroEntries(list, hero?.Clan, PartyTransferEntrySection.NpcPrisoners, ref num);
		}
		AddPartyTransferEntriesFromVolunteers(list, hero, ref num);
		return list;
	}

	public static List<PartyTransferPromptEntry> BuildPartyTransferPromptEntriesForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
	{
		try
		{
			return BuildPartyTransferPromptEntriesInternal(targetHero, targetCharacter, targetAgentIndex);
		}
		catch
		{
			return new List<PartyTransferPromptEntry>();
		}
	}

	internal static bool IsSettlementTransferLeaderEligible(Hero targetHero, CharacterObject targetCharacter = null)
	{
		Hero hero = targetHero ?? targetCharacter?.HeroObject;
		if (AIConfigHandler.IsPlayerCompanionOrFamilyTradeTarget(hero))
		{
			return false;
		}
		return CanDiscussSettlementTransferAssets(hero);
	}

	public static bool IsSettlementTransferLeaderEligibleForExternal(Hero targetHero, CharacterObject targetCharacter = null)
	{
		try
		{
			return IsSettlementTransferLeaderEligible(targetHero, targetCharacter);
		}
		catch
		{
			return false;
		}
	}

	internal static int CalculateSettlementDailyIncomeDenars(Settlement settlement, Clan ownerClan)
	{
		try
		{
			Town town = settlement?.Town;
			if (town == null || ownerClan == null || Campaign.Current?.Models == null)
			{
				return 0;
			}
			int num = 0;
			try
			{
				num += (int)Campaign.Current.Models.SettlementTaxModel.CalculateTownTax(town, includeDescriptions: false).ResultNumber;
			}
			catch
			{
			}
			try
			{
				num += (int)Campaign.Current.Models.ClanFinanceModel.CalculateTownIncomeFromTariffs(ownerClan, town, applyWithdrawals: false).ResultNumber;
			}
			catch
			{
			}
			try
			{
				num += Campaign.Current.Models.ClanFinanceModel.CalculateTownIncomeFromProjects(town);
			}
			catch
			{
			}
			try
			{
				foreach (Village village in town.Villages)
				{
					if (village != null)
					{
						num += Campaign.Current.Models.ClanFinanceModel.CalculateVillageIncome(ownerClan, village, applyWithdrawals: false);
					}
				}
			}
			catch
			{
			}
			return Math.Max(0, num);
		}
		catch
		{
			return 0;
		}
	}

	internal static int CalculateSettlementGuidePriceDenars(Settlement settlement, IFaction buyerFaction)
	{
		try
		{
			if (settlement == null)
			{
				return 0;
			}
			float num = ((buyerFaction != null) ? settlement.GetSettlementValueForFaction(buyerFaction) : settlement.GetValue(null, countAlsoBoundedSettlements: true));
			return Math.Max(0, (int)Math.Round(num, MidpointRounding.AwayFromZero));
		}
		catch
		{
			return 0;
		}
	}

	internal static bool IsSettlementTransferClanLeader(Hero hero)
	{
		return hero != null && hero.Clan != null && hero.Clan.Leader == hero;
	}

	internal static bool HasPersonalFixedAssets(Hero hero)
	{
		try
		{
			return hero != null && ((hero.OwnedWorkshops != null && hero.OwnedWorkshops.Count > 0) || (hero.OwnedCaravans != null && hero.OwnedCaravans.Any((CaravanPartyComponent x) => x?.MobileParty != null && x.MobileParty.IsActive)));
		}
		catch
		{
			return false;
		}
	}

	internal static bool CanDiscussSettlementTransferAssets(Hero hero)
	{
		return IsSettlementTransferClanLeader(hero) || HasPersonalFixedAssets(hero);
	}

	internal static string BuildSettlementTransferAssetId(SettlementTransferAssetKind kind, Settlement settlement = null, Workshop workshop = null, MobileParty caravanParty = null)
	{
		try
		{
			if (kind == SettlementTransferAssetKind.Settlement)
			{
				return (settlement?.StringId ?? "").Trim();
			}
			if (kind == SettlementTransferAssetKind.Workshop)
			{
				Settlement workshopSettlement = workshop?.Settlement;
				string settlementId = (workshopSettlement?.StringId ?? "").Trim();
				int index = 0;
				try
				{
					index = Math.Max(0, workshopSettlement?.Town?.Workshops?.IndexOf(workshop) ?? 0);
				}
				catch
				{
					index = Math.Max(0, workshop?.GetHashCode() ?? 0);
				}
				return "workshop@" + settlementId + "@" + index;
			}
			if (kind == SettlementTransferAssetKind.Caravan)
			{
				string partyId = (caravanParty?.StringId ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(partyId))
				{
					return "caravan@" + partyId;
				}
				return "caravan@" + Math.Max(0, caravanParty?.GetHashCode() ?? 0);
			}
		}
		catch
		{
		}
		return "";
	}

	public static string GetSettlementTransferAssetIdForExternal(SettlementTransferPromptEntry entry)
	{
		string text = (entry?.AssetId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return (entry?.SettlementId ?? "").Trim();
	}

	public static bool IsSettlementTransferEntryValidForExternal(SettlementTransferPromptEntry entry)
	{
		if (entry == null)
		{
			return false;
		}
		switch (entry.AssetKind)
		{
		case SettlementTransferAssetKind.Settlement:
			return entry.Settlement != null;
		case SettlementTransferAssetKind.Workshop:
			return entry.Workshop != null;
		case SettlementTransferAssetKind.Caravan:
			return entry.CaravanParty != null && entry.CaravanParty.IsActive && entry.CaravanParty.CaravanPartyComponent != null;
		default:
			return false;
		}
	}

	public static string GetSettlementTransferAssetDisplayNameForExternal(SettlementTransferPromptEntry entry)
	{
		if (!string.IsNullOrWhiteSpace(entry?.DisplayName))
		{
			return entry.DisplayName;
		}
		return entry?.Settlement?.Name?.ToString() ?? entry?.Workshop?.Name?.ToString() ?? entry?.CaravanParty?.Name?.ToString() ?? "未知资产";
	}

	/// <summary>
	/// Checks only the stable fixed-asset ID grammar used by GIVE_ASSET. This deliberately
	/// does not treat display names or arbitrary item labels as fixed assets, so ordinary
	/// RP item literals keep their existing fallback behavior.
	/// </summary>
	public static bool LooksLikeFixedAssetTransferIdForExternal(string assetToken) => PartyTransferProjectionOwner.LooksLikeFixedAssetTransferIdForExternal(assetToken);

	/// <summary>
	/// Resolves a real fixed asset from its canonical runtime ID without consulting the
	/// current prompt snapshot. Settlement IDs are checked by exact runtime ID so custom
	/// module settlement IDs work too; there is no name matching or item-object scan here.
	/// </summary>
	public static bool TryResolveFixedAssetTransferEntryByIdForExternal(string assetToken, out SettlementTransferPromptEntry entry)
	{
		entry = null;
		string text = (assetToken ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || text.Length > 256 || text.IndexOf('\r') >= 0 || text.IndexOf('\n') >= 0)
		{
			return false;
		}
		try
		{
			if (text.StartsWith("workshop@", StringComparison.OrdinalIgnoreCase))
			{
				return TryResolveWorkshopFixedAssetTransferEntryById(text, out entry);
			}
			if (text.StartsWith("caravan@", StringComparison.OrdinalIgnoreCase))
			{
				return TryResolveCaravanFixedAssetTransferEntryById(text, out entry);
			}
			string settlementId = text.StartsWith("settlement:", StringComparison.OrdinalIgnoreCase)
				? text.Substring("settlement:".Length).Trim()
				: text;
			Settlement settlement = FindSettlementByExactRuntimeIdForFixedAssetTransfer(settlementId);
			if (settlement == null || !settlement.IsFortification)
			{
				return false;
			}
			Clan ownerClan = settlement.OwnerClan;
			entry = new SettlementTransferPromptEntry
			{
				Section = SettlementTransferEntrySection.NpcFiefs,
				AssetKind = SettlementTransferAssetKind.Settlement,
				Settlement = settlement,
				OwnerHero = ownerClan?.Leader,
				SettlementId = (settlement.StringId ?? "").Trim(),
				AssetId = (settlement.StringId ?? "").Trim(),
				DisplayName = settlement.Name?.ToString() ?? "未知定居点",
				TypeLabel = settlement.IsTown ? "城市" : "城堡",
				DailyIncomeDenars = CalculateSettlementDailyIncomeDenars(settlement, ownerClan),
				GuidePriceDenars = CalculateSettlementGuidePriceDenars(settlement, Clan.PlayerClan),
				OwnerClan = ownerClan
			};
			return true;
		}
		catch
		{
			entry = null;
			return false;
		}
	}

	internal static Settlement FindSettlementByExactRuntimeIdForFixedAssetTransfer(string settlementId)
	{
		string text = (settlementId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			return Settlement.Find(text);
		}
		catch
		{
			return null;
		}
	}

	internal static bool TryResolveWorkshopFixedAssetTransferEntryById(string assetId, out SettlementTransferPromptEntry entry)
	{
		entry = null;
		int firstSeparator = assetId.IndexOf('@');
		int lastSeparator = assetId.LastIndexOf('@');
		if (firstSeparator < 0 || lastSeparator <= firstSeparator + 1 || lastSeparator >= assetId.Length - 1)
		{
			return false;
		}
		string settlementId = assetId.Substring(firstSeparator + 1, lastSeparator - firstSeparator - 1).Trim();
		if (string.IsNullOrWhiteSpace(settlementId) || !int.TryParse(assetId.Substring(lastSeparator + 1), out int index) || index < 0)
		{
			return false;
		}
		Settlement settlement = FindSettlementByExactRuntimeIdForFixedAssetTransfer(settlementId);
		if (settlement?.Town?.Workshops == null)
		{
			return false;
		}
		Workshop workshop = null;
		if (index < settlement.Town.Workshops.Length)
		{
			workshop = settlement.Town.Workshops[index];
			if (!string.Equals(BuildSettlementTransferAssetId(SettlementTransferAssetKind.Workshop, workshop: workshop), assetId, StringComparison.OrdinalIgnoreCase))
			{
				workshop = null;
			}
		}
		if (workshop == null)
		{
			foreach (Workshop candidate in settlement.Town.Workshops)
			{
				if (candidate != null && string.Equals(BuildSettlementTransferAssetId(SettlementTransferAssetKind.Workshop, workshop: candidate), assetId, StringComparison.OrdinalIgnoreCase))
				{
					workshop = candidate;
					break;
				}
			}
		}
		if (workshop == null)
		{
			return false;
		}
		Hero owner = workshop.Owner;
		entry = new SettlementTransferPromptEntry
		{
			Section = SettlementTransferEntrySection.NpcFiefs,
			AssetKind = SettlementTransferAssetKind.Workshop,
			Settlement = settlement,
			Workshop = workshop,
			OwnerHero = owner,
			SettlementId = assetId,
			AssetId = assetId,
			DisplayName = BuildWorkshopDisplayName(workshop),
			TypeLabel = "工坊",
			DailyIncomeDenars = CalculateWorkshopDailyIncomeDenars(workshop),
			GuidePriceDenars = CalculateWorkshopGuidePriceDenars(workshop, playerIsBuyer: true),
			OwnerClan = owner?.Clan
		};
		return true;
	}

	internal static bool TryResolveCaravanFixedAssetTransferEntryById(string assetId, out SettlementTransferPromptEntry entry)
	{
		entry = null;
		if (assetId.Length <= "caravan@".Length)
		{
			return false;
		}
		foreach (MobileParty caravanParty in MobileParty.AllCaravanParties)
		{
			if (caravanParty == null || !caravanParty.IsActive || caravanParty.CaravanPartyComponent == null
				|| !string.Equals(BuildSettlementTransferAssetId(SettlementTransferAssetKind.Caravan, caravanParty: caravanParty), assetId, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			CaravanPartyComponent component = caravanParty.CaravanPartyComponent;
			Hero owner = component.Owner;
			entry = new SettlementTransferPromptEntry
			{
				Section = SettlementTransferEntrySection.NpcFiefs,
				AssetKind = SettlementTransferAssetKind.Caravan,
				Settlement = component.Settlement,
				CaravanParty = caravanParty,
				OwnerHero = owner,
				SettlementId = assetId,
				AssetId = assetId,
				DisplayName = BuildCaravanDisplayName(component),
				TypeLabel = component.CanHaveNavalNavigationCapability ? "商船队" : "商队",
				DailyIncomeDenars = CalculateCaravanDailyIncomeDenars(caravanParty),
				GuidePriceDenars = CalculateCaravanGuidePriceDenars(caravanParty),
				OwnerClan = owner?.Clan
			};
			return true;
		}
		return false;
	}

	internal static int CalculateWorkshopDailyIncomeDenars(Workshop workshop)
	{
		try
		{
			return Math.Max(0, Campaign.Current?.Models?.ClanFinanceModel?.CalculateOwnerIncomeFromWorkshop(workshop) ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	internal static int CalculateWorkshopGuidePriceDenars(Workshop workshop, bool playerIsBuyer)
	{
		try
		{
			WorkshopModel model = Campaign.Current?.Models?.WorkshopModel;
			if (workshop == null || model == null)
			{
				return 0;
			}
			return Math.Max(0, playerIsBuyer ? model.GetCostForPlayer(workshop) : model.GetCostForNotable(workshop));
		}
		catch
		{
			return 0;
		}
	}

	internal static int CalculateCaravanDailyIncomeDenars(MobileParty caravanParty)
	{
		try
		{
			return Math.Max(0, Campaign.Current?.Models?.ClanFinanceModel?.CalculateOwnerIncomeFromCaravan(caravanParty) ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	internal static int CalculateCaravanGuidePriceDenars(MobileParty caravanParty)
	{
		try
		{
			if (caravanParty?.CaravanPartyComponent == null || Campaign.Current?.Models?.CaravanModel == null)
			{
				return 0;
			}
			bool isElite = caravanParty.CaravanPartyComponent.IsElite;
			bool isNaval = caravanParty.CaravanPartyComponent.CanHaveNavalNavigationCapability;
			int formingCost = Campaign.Current.Models.CaravanModel.GetCaravanFormingCost(isElite, isNaval);
			return Math.Max(0, formingCost) + Math.Max(0, caravanParty.PartyTradeGold);
		}
		catch
		{
			return Math.Max(0, caravanParty?.PartyTradeGold ?? 0);
		}
	}

	internal static string BuildWorkshopDisplayName(Workshop workshop)
	{
		string workshopName = workshop?.Name?.ToString() ?? "工坊";
		string settlementName = workshop?.Settlement?.Name?.ToString();
		return string.IsNullOrWhiteSpace(settlementName) ? workshopName : (workshopName + "（" + settlementName + "）");
	}

	internal static string BuildCaravanDisplayName(CaravanPartyComponent component)
	{
		MobileParty party = component?.MobileParty;
		string name = party?.Name?.ToString();
		if (string.IsNullOrWhiteSpace(name))
		{
			name = component?.Name?.ToString() ?? "商队";
		}
		string leaderName = component?.Leader?.Name?.ToString();
		if (!string.IsNullOrWhiteSpace(leaderName))
		{
			name += "（" + leaderName + "）";
		}
		return name;
	}

	internal static void AddSettlementTransferWorkshopEntries(List<SettlementTransferPromptEntry> entries, Hero owner, SettlementTransferEntrySection section)
	{
		if (entries == null || owner?.OwnedWorkshops == null)
		{
			return;
		}
		foreach (Workshop workshop in owner.OwnedWorkshops)
		{
			if (workshop == null || workshop.WorkshopType == null || workshop.WorkshopType.IsHidden)
			{
				continue;
			}
			string assetId = BuildSettlementTransferAssetId(SettlementTransferAssetKind.Workshop, workshop: workshop);
			entries.Add(new SettlementTransferPromptEntry
			{
				Section = section,
				AssetKind = SettlementTransferAssetKind.Workshop,
				Workshop = workshop,
				OwnerHero = owner,
				Settlement = workshop.Settlement,
				SettlementId = assetId,
				AssetId = assetId,
				DisplayName = BuildWorkshopDisplayName(workshop),
				TypeLabel = "工坊",
				DailyIncomeDenars = CalculateWorkshopDailyIncomeDenars(workshop),
				GuidePriceDenars = CalculateWorkshopGuidePriceDenars(workshop, section == SettlementTransferEntrySection.NpcFiefs),
				OwnerClan = owner.Clan
			});
		}
	}

	internal static void AddSettlementTransferCaravanEntries(List<SettlementTransferPromptEntry> entries, Hero owner, SettlementTransferEntrySection section)
	{
		if (entries == null || owner?.OwnedCaravans == null)
		{
			return;
		}
		foreach (CaravanPartyComponent component in owner.OwnedCaravans)
		{
			MobileParty party = component?.MobileParty;
			if (party == null || !party.IsActive || party.CaravanPartyComponent == null)
			{
				continue;
			}
			string assetId = BuildSettlementTransferAssetId(SettlementTransferAssetKind.Caravan, caravanParty: party);
			bool isNaval = component.CanHaveNavalNavigationCapability;
			entries.Add(new SettlementTransferPromptEntry
			{
				Section = section,
				AssetKind = SettlementTransferAssetKind.Caravan,
				CaravanParty = party,
				OwnerHero = owner,
				Settlement = component.Settlement,
				SettlementId = assetId,
				AssetId = assetId,
				DisplayName = BuildCaravanDisplayName(component),
				TypeLabel = isNaval ? "商船队" : "商队",
				DailyIncomeDenars = CalculateCaravanDailyIncomeDenars(party),
				GuidePriceDenars = CalculateCaravanGuidePriceDenars(party),
				OwnerClan = owner.Clan
			});
		}
	}

	internal static List<SettlementTransferPromptEntry> BuildSettlementTransferPromptEntriesInternal(Hero targetHero, CharacterObject targetCharacter = null)
	{
		List<SettlementTransferPromptEntry> list = new List<SettlementTransferPromptEntry>();
		try
		{
			Clan playerClan = Clan.PlayerClan;
			Hero hero = targetHero ?? targetCharacter?.HeroObject;
			if (AIConfigHandler.IsPlayerCompanionOrFamilyTradeTarget(hero))
			{
				return list;
			}
			Clan clan = hero?.Clan;
			if (playerClan != null)
			{
				foreach (Town fief in playerClan.Fiefs)
				{
					Settlement settlement = fief?.Settlement;
					if (settlement != null && settlement.IsFortification)
					{
						list.Add(new SettlementTransferPromptEntry
						{
							Section = SettlementTransferEntrySection.PlayerFiefs,
							AssetKind = SettlementTransferAssetKind.Settlement,
							Settlement = settlement,
							SettlementId = (settlement.StringId ?? "").Trim(),
							AssetId = (settlement.StringId ?? "").Trim(),
							DisplayName = settlement.Name?.ToString() ?? "未知定居点",
							TypeLabel = (settlement.IsTown ? "城市" : "城堡"),
							DailyIncomeDenars = CalculateSettlementDailyIncomeDenars(settlement, playerClan),
							GuidePriceDenars = CalculateSettlementGuidePriceDenars(settlement, clan),
							OwnerClan = playerClan
						});
					}
				}
				AddSettlementTransferWorkshopEntries(list, Hero.MainHero, SettlementTransferEntrySection.PlayerFiefs);
				AddSettlementTransferCaravanEntries(list, Hero.MainHero, SettlementTransferEntrySection.PlayerFiefs);
			}
			if (clan != null && IsSettlementTransferClanLeader(hero))
			{
				foreach (Town fief2 in clan.Fiefs)
				{
					Settlement settlement2 = fief2?.Settlement;
					if (settlement2 != null && settlement2.IsFortification)
					{
						list.Add(new SettlementTransferPromptEntry
						{
							Section = SettlementTransferEntrySection.NpcFiefs,
							AssetKind = SettlementTransferAssetKind.Settlement,
							Settlement = settlement2,
							SettlementId = (settlement2.StringId ?? "").Trim(),
							AssetId = (settlement2.StringId ?? "").Trim(),
							DisplayName = settlement2.Name?.ToString() ?? "未知定居点",
							TypeLabel = (settlement2.IsTown ? "城市" : "城堡"),
							DailyIncomeDenars = CalculateSettlementDailyIncomeDenars(settlement2, clan),
							GuidePriceDenars = CalculateSettlementGuidePriceDenars(settlement2, playerClan),
							OwnerClan = clan
						});
					}
				}
			}
			AddSettlementTransferWorkshopEntries(list, hero, SettlementTransferEntrySection.NpcFiefs);
			AddSettlementTransferCaravanEntries(list, hero, SettlementTransferEntrySection.NpcFiefs);
		}
		catch
		{
		}
		return list;
	}

	public static List<SettlementTransferPromptEntry> BuildSettlementTransferPromptEntriesForExternal(Hero targetHero, CharacterObject targetCharacter = null)
	{
		try
		{
			return BuildSettlementTransferPromptEntriesInternal(targetHero, targetCharacter);
		}
		catch
		{
			return new List<SettlementTransferPromptEntry>();
		}
	}

	internal static List<SettlementTransferPromptEntry> BuildDisplayIndexedSettlementTransferEntries(IEnumerable<SettlementTransferPromptEntry> entries) => PartyTransferProjectionOwner.BuildDisplayIndexedSettlementTransferEntries(entries, IsSettlementTransferEntryValidForExternal);

	internal static void AppendSettlementTransferPromptSection(StringBuilder sb, string header, IEnumerable<SettlementTransferPromptEntry> entries, bool showPromptIndex) => PartyTransferProjectionOwner.AppendSettlementTransferPromptSection(sb, header, entries, showPromptIndex, IsSettlementTransferEntryValidForExternal, GetSettlementTransferAssetIdForExternal);

	internal static SettlementTransferPromptEntry FindSettlementTransferEntryByToken(IEnumerable<SettlementTransferPromptEntry> entries, string token) => PartyTransferProjectionOwner.FindSettlementTransferEntryByToken(entries, token, IsSettlementTransferEntryValidForExternal, GetSettlementTransferAssetIdForExternal);

	internal static SettlementTransferPromptEntry ResolveSettlementTransferEntryByToken(Hero targetHero, CharacterObject targetCharacter, string directionToken, string settlementToken)
	{
		List<SettlementTransferPromptEntry> list = BuildSettlementTransferPromptEntriesInternal(targetHero, targetCharacter);
		string text = (directionToken ?? "").Trim().ToUpperInvariant();
		IEnumerable<SettlementTransferPromptEntry> enumerable = list;
		if (text == "TO_PLAYER")
		{
			enumerable = list.Where((SettlementTransferPromptEntry x) => x.Section == SettlementTransferEntrySection.NpcFiefs);
		}
		else if (text == "TO_NPC")
		{
			enumerable = list.Where((SettlementTransferPromptEntry x) => x.Section == SettlementTransferEntrySection.PlayerFiefs);
		}
		return FindSettlementTransferEntryByToken(BuildDisplayIndexedSettlementTransferEntries(enumerable), settlementToken);
	}

	public static SettlementTransferPromptEntry ResolveSettlementTransferEntryForExternal(Hero targetHero, CharacterObject targetCharacter, string directionToken, string settlementToken)
	{
		try
		{
			return ResolveSettlementTransferEntryByToken(targetHero, targetCharacter, directionToken, settlementToken);
		}
		catch
		{
			return null;
		}
	}

	public static Settlement ResolveSettlementTransferSettlementForExternal(Hero targetHero, CharacterObject targetCharacter, string directionToken, string settlementToken)
	{
		try
		{
			return ResolveSettlementTransferEntryByToken(targetHero, targetCharacter, directionToken, settlementToken)?.Settlement;
		}
		catch
		{
			return null;
		}
	}

	public static string BuildSettlementTransferRuntimeInstructionForExternal(Hero targetHero, CharacterObject targetCharacter = null)
	{
		try
		{
			Hero hero = targetHero ?? targetCharacter?.HeroObject;
			string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal(hero);
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "玩家";
			}
			Clan clan = hero?.Clan;
			Hero leader = clan?.Leader;
			if (!IsSettlementTransferLeaderEligible(hero, targetCharacter))
			{
				if (leader != null)
				{
					return $"【固定资产转移规则】你当前没有可直接转移给{text}的固定资产；若{text}想谈家族城市或城堡转移，你必须明确引导{text}去找家族族长 {leader.Name?.ToString() ?? "家族族长"}。正文只口头引导，不写资产转移标签。";
				}
				return $"【固定资产转移规则】你当前没有可直接转移给{text}的固定资产。正文只口头拒绝或引导，不写资产转移标签。";
			}
			List<SettlementTransferPromptEntry> list = BuildSettlementTransferPromptEntriesInternal(hero, targetCharacter);
			List<SettlementTransferPromptEntry> list2All = BuildDisplayIndexedSettlementTransferEntries(((RewardSystemBehavior.Instance != null) ? RewardSystemBehavior.Instance.GetAllowedNpcSettlementTransferEntriesForPlayer(hero, targetCharacter) : list.Where((SettlementTransferPromptEntry x) => x.Section == SettlementTransferEntrySection.NpcFiefs)));
			List<SettlementTransferPromptEntry> list3All = BuildDisplayIndexedSettlementTransferEntries(list.Where((SettlementTransferPromptEntry x) => x.Section == SettlementTransferEntrySection.PlayerFiefs));
			MentionedWorldEntities mentions = AIConfigHandler.GetLatestAuxiliaryMentionedEntitiesForExternal();
			int promptListMax = PromptListRetrievalService.GetMaxCandidateCount();
			List<SettlementTransferPromptEntry> list2 = PromptListRetrievalService.FilterSettlementTransferEntries(list2All, mentions, promptListMax);
			List<SettlementTransferPromptEntry> list3 = PromptListRetrievalService.FilterSettlementTransferEntries(list3All, mentions, promptListMax);
			PromptListRetrievalService.PublishSettlementTransferSnapshot(PromptListRetrievalService.SettlementTransferNpcAssetsSnapshotScope, targetHero, targetCharacter, -1, list2);
			PromptListRetrievalService.PublishSettlementTransferSnapshot(PromptListRetrievalService.SettlementTransferAllNpcAssetsSnapshotScope, targetHero, targetCharacter, -1, list2All);
            return SettlementTransferPromptOwner.Build(new SettlementTransferPromptCapture {
                PlayerName = text, Guidance = RewardSystemBehavior.Instance?.BuildSettlementTransferPromptGuidanceForAI(hero, targetCharacter),
                NpcAssets = list2, PlayerAssets = list3, AllNpcAssets = list2All, AllPlayerAssets = list3All,
                NpcRemainder = PromptListRetrievalService.BuildRemainingSettlementTransferSummary(list2All, list2, "你"),
                PlayerRemainder = PromptListRetrievalService.BuildRemainingSettlementTransferSummary(list3All, list3, "玩家")
            }, IsSettlementTransferEntryValidForExternal, GetSettlementTransferAssetIdForExternal);
		}
		catch
		{
			return "";
		}
	}

	internal static void AppendPartyTransferPromptSection(StringBuilder sb, string header, IEnumerable<PartyTransferPromptEntry> entries, bool isPrisoner, bool showPromptIndex) => PartyTransferProjectionOwner.AppendPartyTransferPromptSection(sb, header, entries, isPrisoner, showPromptIndex, GetPartyTransferPrisonerSourceLabelForExternal, entry => GetPartyTransferTroopTypeLabelForExternal(entry.Character));

	public static string GetPartyTransferPrisonerSourceLabelForExternal(PartyTransferPromptEntry entry)
	{
		try
		{
			string settlementName = (entry?.SourceSettlement?.Name?.ToString() ?? "").Trim();
			return string.IsNullOrWhiteSpace(settlementName) ? "" : (settlementName + "地牢");
		}
		catch
		{
			return "";
		}
	}

	internal static long CalculateSettlementTransferTotalValueForExternal(IEnumerable<SettlementTransferPromptEntry> entries) => PartyTransferProjectionOwner.CalculateSettlementTransferTotalValueForExternal(entries, IsSettlementTransferEntryValidForExternal);

	internal static long CalculatePartyTransferTotalValueForExternal(IEnumerable<PartyTransferPromptEntry> entries, bool isPrisoner) => PartyTransferProjectionOwner.CalculatePartyTransferTotalValueForExternal(entries, isPrisoner);

	internal static int GetPartyTransferTroopTier(PartyTransferPromptEntry entry)
	{
		return Math.Max(0, entry?.Character?.Tier ?? 0);
	}

	public static string GetPartyTransferTroopTypeLabelForExternal(CharacterObject character)
	{
		try
		{
			switch (character?.DefaultFormationClass)
			{
			case FormationClass.Ranged:
				return "弓手";
			case FormationClass.Cavalry:
				return "骑兵";
			case FormationClass.HorseArcher:
				return "骑射手";
			case FormationClass.Infantry:
			case FormationClass.HeavyInfantry:
			case FormationClass.NumberOfDefaultFormations:
				return "步兵";
			default:
				if (character != null)
				{
					if (character.IsMounted)
					{
						return character.IsRanged ? "骑射手" : "骑兵";
					}
					if (character.IsRanged)
					{
						return "弓手";
					}
				}
				return "步兵";
			}
		}
		catch
		{
			return "步兵";
		}
	}

	internal static Hero ResolvePartyTransferRuleHero(Hero targetHero, CharacterObject targetCharacter)
	{
		return targetHero ?? targetCharacter?.HeroObject;
	}

	internal static bool IsPartyTransferLordEligible(Hero targetHero, CharacterObject targetCharacter = null)
	{
		Hero hero = ResolvePartyTransferRuleHero(targetHero, targetCharacter);
		return hero != null && hero.IsLord;
	}

	public static bool IsPartyTransferLordEligibleForExternal(Hero targetHero, CharacterObject targetCharacter = null)
	{
		try
		{
			return IsPartyTransferLordEligible(targetHero, targetCharacter);
		}
		catch
		{
			return false;
		}
	}

	internal static int ResolvePartyTransferRecruitTrustLevelIndex(Hero targetHero, CharacterObject targetCharacter)
	{
		int num = 6;
		try
		{
			Hero hero = targetHero ?? targetCharacter?.HeroObject;
			if (hero != null)
			{
				return RewardSystemBehavior.GetTrustLevelIndex(RewardSystemBehavior.Instance?.GetEffectiveTrust(hero) ?? 0);
			}
			if (targetCharacter != null && RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(targetCharacter, out var kind))
			{
				return RewardSystemBehavior.GetTrustLevelIndex(RewardSystemBehavior.Instance.GetSettlementMerchantEffectiveTrust(Settlement.CurrentSettlement, kind));
			}
		}
		catch
		{
		}
		return num;
	}

	internal static int ResolvePartyTransferRecruitMaxTier(int trustLevelIndex) => PartyTransferProjectionOwner.ResolvePartyTransferRecruitMaxTier(trustLevelIndex);

	internal static void AppendPartyTransferHiddenTroopSection(StringBuilder sb, string header, IEnumerable<PartyTransferPromptEntry> entries) => PartyTransferProjectionOwner.AppendPartyTransferHiddenTroopSection(sb, header, entries, entry => GetPartyTransferTroopTypeLabelForExternal(entry.Character), GetPartyTransferTroopTier);

	internal static List<PartyTransferPromptEntry> BuildDisplayIndexedPartyTransferEntries(IEnumerable<PartyTransferPromptEntry> entries) => PartyTransferProjectionOwner.BuildDisplayIndexedPartyTransferEntries(entries);

    internal static IEnumerable<PartyTransferPromptEntry> FilterAllowedNpcTransferTroopEntries(IEnumerable<PartyTransferPromptEntry> entries, Hero targetHero, CharacterObject targetCharacter)
        => PartyTransferAuthorizationOwner.FilterAllowed(entries,
            ResolvePartyTransferRecruitMaxTier(ResolvePartyTransferRecruitTrustLevelIndex(targetHero, targetCharacter)), GetPartyTransferTroopTier);




	internal static PartyTransferPromptEntry ResolveNpcTransferEntryByDisplayIndex(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, PartyTransferEntrySection section, int displayIndex)
	{
		if (displayIndex <= 0)
		{
			return null;
		}
		List<PartyTransferPromptEntry> list = BuildPartyTransferPromptEntriesInternal(targetHero, targetCharacter, targetAgentIndex);
		IEnumerable<PartyTransferPromptEntry> enumerable = Enumerable.Empty<PartyTransferPromptEntry>();
		if (section == PartyTransferEntrySection.NpcTroops)
		{
			enumerable = list.Where((PartyTransferPromptEntry x) => x != null && (x.Section == PartyTransferEntrySection.NpcTroops || x.Section == PartyTransferEntrySection.NpcVolunteers));
		}
		else if (section == PartyTransferEntrySection.NpcPrisoners)
		{
			enumerable = list.Where((PartyTransferPromptEntry x) => x != null && x.Section == PartyTransferEntrySection.NpcPrisoners);
		}
		return FindDisplayIndexedPartyTransferEntry(BuildDisplayIndexedPartyTransferEntries(enumerable), displayIndex);
	}

	public static string BuildPartyTransferRuntimeInstructionForExternal(Hero targetHero, CharacterObject targetCharacter = null, int targetAgentIndex = -1)
	{
		try
		{
			List<PartyTransferPromptEntry> list = BuildPartyTransferPromptEntriesInternal(targetHero, targetCharacter, targetAgentIndex, out PartyBase wildernessNonHeroSource);
			Hero hero = ResolvePartyTransferRuleHero(targetHero, targetCharacter);
			bool flag = IsPartyTransferLordEligible(targetHero, targetCharacter);
			bool flag2 = IsPartyTransferNotableRecruitEligible(hero);
			if (!PartyTransferAuthorizationOwner.CanDiscuss(flag, flag2, wildernessNonHeroSource != null))
			{
				string guardrailRuleNonHeroInstruction = AIConfigHandler.GetGuardrailRuleNonHeroInstruction("party_transfer");
				return string.IsNullOrWhiteSpace(guardrailRuleNonHeroInstruction) ? "" : StripPartyTransferTags(guardrailRuleNonHeroInstruction);
			}
			string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "玩家";
			}
			int partyTransferRecruitTrustLevelIndex = ResolvePartyTransferRecruitTrustLevelIndex(targetHero, targetCharacter);
			int partyTransferRecruitMaxTier = ResolvePartyTransferRecruitMaxTier(partyTransferRecruitTrustLevelIndex);
			bool canUseCounterpartyRoster = flag || wildernessNonHeroSource != null;
            var authorization = PartyTransferAuthorizationOwner.Build(list, partyTransferRecruitMaxTier,
                wildernessNonHeroSource != null, GetPartyTransferTroopTier);
            var list4 = authorization.HiddenTroops;
            var list6All = authorization.IndexedTroops;
            var authorizedAllTroops = authorization.AllTroops;
            var list7All = authorization.AllPrisoners;
			MentionedWorldEntities mentions = AIConfigHandler.GetLatestAuxiliaryMentionedEntitiesForExternal();
			int promptListMax = PromptListRetrievalService.GetMaxCandidateCount();
			List<PartyTransferPromptEntry> list4Filtered = PromptListRetrievalService.FilterPartyTransferEntries(list4, mentions, promptListMax, isPrisoner: false);
			List<PartyTransferPromptEntry> list6 = PromptListRetrievalService.FilterPartyTransferEntries(list6All, mentions, promptListMax, isPrisoner: false);
			List<PartyTransferPromptEntry> list7 = PromptListRetrievalService.FilterPartyTransferEntries(list7All, mentions, promptListMax, isPrisoner: true);
			PromptListRetrievalService.PublishPartyTransferSnapshot(PromptListRetrievalService.PartyTransferTroopsSnapshotScope, targetHero, targetCharacter, targetAgentIndex, list6);
			PromptListRetrievalService.PublishPartyTransferSnapshot(PromptListRetrievalService.PartyTransferPrisonersSnapshotScope, targetHero, targetCharacter, targetAgentIndex, list7);
			PromptListRetrievalService.PublishPartyTransferSnapshot(PromptListRetrievalService.PartyTransferAllTroopsSnapshotScope, targetHero, targetCharacter, targetAgentIndex, authorizedAllTroops);
			PromptListRetrievalService.PublishPartyTransferSnapshot(PromptListRetrievalService.PartyTransferAllPrisonersSnapshotScope, targetHero, targetCharacter, targetAgentIndex, list7All);
            string wildernessInstruction = wildernessNonHeroSource == null ? "" : AIConfigHandler.ResolveRuleRuntimeText("party_transfer", "wilderness_nonhero_party", forConstraint: false, null);
			string runtimeHint = flag
				? AIConfigHandler.BuildRuntimePartyTransferInstructionForExternal(targetHero, targetCharacter)
				: (wildernessNonHeroSource != null
					? AIConfigHandler.ResolveRuleRuntimeText("party_transfer", "level_" + partyTransferRecruitTrustLevelIndex, forConstraint: false, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["playerName"] = text })
					: "");
            return PartyTransferPromptOwner.Build(new PartyTransferPromptCapture {
                Wilderness = wildernessNonHeroSource != null, CanUseCounterpartyRoster = canUseCounterpartyRoster,
                Notable = flag2, PlayerName = text, MaximumTier = partyTransferRecruitMaxTier,
                MaximumRecruitableVolunteerIndex = flag2 ? GetMaximumRecruitableVolunteerIndex(hero) : -1,
                VolunteerCount = authorization.VolunteerCount, WildernessInstruction = wildernessInstruction, RuntimeHint = runtimeHint,
                Troops = list6, HiddenTroops = list4Filtered, AllTroops = authorizedAllTroops,
                Prisoners = list7, AllPrisoners = list7All,
                TroopRemainder = PromptListRetrievalService.BuildRemainingPartyTransferSummary(list6All, list6, isPrisoner: false, "你"),
                PrisonerRemainder = PromptListRetrievalService.BuildRemainingPartyTransferSummary(list7All, list7, isPrisoner: true, "你")
            }, GetPartyTransferPrisonerSourceLabelForExternal,
                entry => GetPartyTransferTroopTypeLabelForExternal(entry.Character), GetPartyTransferTroopTier);
		}
		catch
		{
			return "";
		}
	}

	internal static int ClampWildernessNonHeroPartyTransferAmount(PartyTransferPromptEntry entry, int requestedAmount, PartyBase wildernessNonHeroSource, CharacterObject representativeCharacter)
	{
		int amount = Math.Max(0, requestedAmount);
		if (wildernessNonHeroSource == null)
		{
			return amount;
		}
		if (entry == null || entry.OwnerParty != wildernessNonHeroSource)
		{
			return 0;
		}
		if (!IsSamePartyTransferCharacter(entry.Character, representativeCharacter))
		{
			return amount;
		}
		try
		{
			TroopRoster roster = wildernessNonHeroSource.MemberRoster;
			int index = roster?.FindIndexOfTroop(entry.Character) ?? (-1);
			if (index < 0)
			{
				return 0;
			}
			int currentCount = Math.Max(0, roster.GetElementCopyAtIndex(index).Number);
			return PartyTransferAuthorizationOwner.ClampWildernessAmount(amount, true, true, true, currentCount);
		}
		catch
		{
			return 0;
		}
	}

	internal static int TransferPartyMemberEntry(PartyTransferPromptEntry entry, int requestedAmount, PartyBase targetParty)
	{
		if (IsPartyTransferVolunteerEntry(entry))
		{
			return TransferPartyVolunteerEntry(entry, requestedAmount, targetParty);
		}
		if (entry == null || targetParty == null || entry.OwnerParty == null || entry.OwnerParty == targetParty || entry.Character == null || entry.Character.IsHero || entry.Character == CharacterObject.PlayerCharacter)
		{
			return 0;
		}
		TroopRoster memberRoster = entry.OwnerParty.MemberRoster;
		TroopRoster memberRoster2 = targetParty.MemberRoster;
		if (memberRoster == null || memberRoster2 == null)
		{
			return 0;
		}
		int num = memberRoster.FindIndexOfTroop(entry.Character);
		if (num < 0)
		{
			return 0;
		}
		TroopRosterElement elementCopyAtIndex = memberRoster.GetElementCopyAtIndex(num);
		int num2 = Math.Min(Math.Max(0, requestedAmount), Math.Max(0, elementCopyAtIndex.Number));
		if (num2 <= 0)
		{
			return 0;
		}
		int num3 = 0;
		if (elementCopyAtIndex.Number > 0 && elementCopyAtIndex.WoundedNumber > 0)
		{
			num3 = (num2 >= elementCopyAtIndex.Number) ? elementCopyAtIndex.WoundedNumber : Math.Min(num2, Math.Max(0, (int)Math.Round((double)elementCopyAtIndex.WoundedNumber * (double)num2 / (double)elementCopyAtIndex.Number, MidpointRounding.AwayFromZero)));
		}
		int num4 = 0;
		if (elementCopyAtIndex.Number > 0 && elementCopyAtIndex.Xp > 0)
		{
			num4 = (num2 >= elementCopyAtIndex.Number) ? elementCopyAtIndex.Xp : Math.Max(0, (int)Math.Round((double)elementCopyAtIndex.Xp * (double)num2 / (double)elementCopyAtIndex.Number, MidpointRounding.AwayFromZero));
		}
		memberRoster.AddToCounts(entry.Character, -num2, insertAtFront: false, -num3, 0, false, -1);
		if (num4 > 0)
		{
			memberRoster.AddXpToTroop(entry.Character, -num4);
		}
		memberRoster2.AddToCounts(entry.Character, num2, insertAtFront: false, num3, 0, false, -1);
		if (num4 > 0)
		{
			memberRoster2.AddXpToTroop(entry.Character, num4);
		}
		return num2;
	}

	internal static int TransferPartyVolunteerEntry(PartyTransferPromptEntry entry, int requestedAmount, PartyBase targetParty)
	{
		if (!IsPartyTransferVolunteerEntry(entry) || targetParty?.MemberRoster == null || requestedAmount <= 0)
		{
			return 0;
		}
		Hero volunteerOwner = entry.VolunteerOwner;
		CharacterObject character = entry.Character;
		CharacterObject[] volunteerTypes = volunteerOwner.VolunteerTypes;
		if (volunteerTypes == null)
		{
			return 0;
		}
		int maximumRecruitableVolunteerIndex = GetMaximumRecruitableVolunteerIndex(volunteerOwner);
		if (maximumRecruitableVolunteerIndex < 0)
		{
			return 0;
		}
		int num = 0;
		int num2 = Math.Min(Math.Min(5, maximumRecruitableVolunteerIndex), volunteerTypes.Length - 1);
		for (int i = 0; i <= num2 && num < requestedAmount; i++)
		{
			if (volunteerTypes[i] != character)
			{
				continue;
			}
			volunteerTypes[i] = null;
			targetParty.MemberRoster.AddToCounts(character, 1, insertAtFront: false, 0, 0, true, -1);
			num++;
		}
		if (num > 0)
		{
			CampaignEventDispatcher.Instance.OnUnitRecruited(character, num);
		}
		return num;
	}

	internal static bool IsPartyTransferDungeonSourceValid(PartyTransferPromptEntry entry, Clan expectedOwnerClan)
	{
		if (entry?.SourceSettlement == null)
		{
			return true;
		}
		Settlement sourceSettlement = entry.SourceSettlement;
		if (expectedOwnerClan == null || sourceSettlement.OwnerClan != expectedOwnerClan || !sourceSettlement.IsFortification)
		{
			return false;
		}
		if (entry.OwnerParty == sourceSettlement.Party)
		{
			return true;
		}
		try
		{
			return sourceSettlement.Parties.Any((MobileParty x) => x != null && x.IsGarrison && x.Party == entry.OwnerParty);
		}
		catch
		{
			return false;
		}
	}

	internal static int TransferPartyPrisonerEntry(PartyTransferPromptEntry entry, int requestedAmount, PartyBase targetParty, Clan expectedDungeonOwnerClan = null)
	{
		if (entry == null || targetParty == null || entry.OwnerParty == null || entry.OwnerParty == targetParty || entry.Character == null)
		{
			return 0;
		}
		if (entry.Character.IsHero)
		{
			Hero heroObject = entry.Character.HeroObject;
			TroopRoster sourceRoster = entry.OwnerParty.PrisonRoster;
			if (requestedAmount <= 0 || heroObject == null || !heroObject.IsPrisoner || heroObject.PartyBelongedToAsPrisoner != entry.OwnerParty || sourceRoster == null || sourceRoster.FindIndexOfTroop(entry.Character) < 0 || !IsPartyTransferDungeonSourceValid(entry, expectedDungeonOwnerClan))
			{
				return 0;
			}
			if (targetParty.PrisonRoster?.FindIndexOfTroop(entry.Character) >= 0)
			{
				return 0;
			}
			try
			{
				TransferPrisonerAction.Apply(entry.Character, entry.OwnerParty, targetParty);
				return heroObject.PartyBelongedToAsPrisoner == targetParty && (targetParty.PrisonRoster?.FindIndexOfTroop(entry.Character) ?? (-1)) >= 0 ? 1 : 0;
			}
			catch
			{
				return 0;
			}
		}
		TroopRoster prisonRoster = entry.OwnerParty.PrisonRoster;
		if (prisonRoster == null)
		{
			return 0;
		}
		int num = prisonRoster.FindIndexOfTroop(entry.Character);
		if (num < 0)
		{
			return 0;
		}
		TroopRosterElement elementCopyAtIndex = prisonRoster.GetElementCopyAtIndex(num);
		int num2 = Math.Min(Math.Max(0, requestedAmount), Math.Max(0, elementCopyAtIndex.Number));
		if (num2 <= 0)
		{
			return 0;
		}
		int num3 = 0;
		if (elementCopyAtIndex.Number > 0 && elementCopyAtIndex.Xp > 0)
		{
			num3 = (num2 >= elementCopyAtIndex.Number) ? elementCopyAtIndex.Xp : Math.Max(0, (int)Math.Round((double)elementCopyAtIndex.Xp * (double)num2 / (double)elementCopyAtIndex.Number, MidpointRounding.AwayFromZero));
		}
		prisonRoster.AddToCounts(entry.Character, -num2, insertAtFront: false, 0, 0, false, -1);
		if (num3 > 0)
		{
			prisonRoster.AddXpToTroop(entry.Character, -num3);
		}
		targetParty.AddPrisoner(entry.Character, num2);
		if (num3 > 0)
		{
			targetParty.PrisonRoster?.AddXpToTroop(entry.Character, num3);
		}
		return num2;
	}



	internal static string StripPartyTransferTags(string text) => PartyTransferTagCodec.Strip(text);



	public static int TransferPlayerPartyEntryToCounterpartyForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, PartyTransferPromptEntry entry, int amount)
	{
		if (!IsPartyTransferLordEligible(targetHero, targetCharacter))
		{
			return 0;
		}
		PartyBase partyBase = ResolvePartyTransferCounterpartyInternal(targetHero, targetCharacter, targetAgentIndex);
		if (entry == null || partyBase == null)
		{
			return 0;
		}
		if (entry.Section == PartyTransferEntrySection.PlayerTroops)
		{
			return TransferPartyMemberEntry(entry, amount, partyBase);
		}
		if (entry.Section == PartyTransferEntrySection.PlayerPrisoners)
		{
			return TransferPartyPrisonerEntry(entry, amount, partyBase, Clan.PlayerClan);
		}
		return 0;
	}

    internal static PartyTransferEffectResult TransferPlayerPartyEntryWithObservedEffects(Hero targetHero,
        CharacterObject targetCharacter, int targetAgentIndex, PartyTransferPromptEntry entry, int amount)
    {
        if (!TWParallel.IsMainThread() || !IsPartyTransferLordEligible(targetHero, targetCharacter)) return new(0,0,false);
        var target = ResolvePartyTransferCounterpartyInternal(targetHero, targetCharacter, targetAgentIndex);
        if (entry == null || target == null) return new(0,0,false);
        if (entry.Section == PartyTransferEntrySection.PlayerTroops)
            return ObserveTransfer(entry, target, false, () => TransferPartyMemberEntry(entry, amount, target));
        if (entry.Section == PartyTransferEntrySection.PlayerPrisoners)
            return ObserveTransfer(entry, target, true, () => TransferPartyPrisonerEntry(entry, amount, target, Clan.PlayerClan));
        return new(0,0,false);
    }

    public static bool TryApplyPartyTransferTagsForExternal(Hero targetHero, CharacterObject targetCharacter,
        int targetAgentIndex, ref string content, out List<string> generatedFacts, out List<string> notifications)
    {
        PartyTransferExecutionContext context;
        try { context = CaptureExecutionContext(targetHero, targetCharacter, targetAgentIndex); }
        catch (Exception ex)
        {
            content = PartyTransferTagCodec.Strip(content);
            generatedFacts = new List<string>();
            notifications = new List<string>();
            Logger.Log("Logic", "[PartyTransfer] capture failed before effects: " + ex.Message);
            return false;
        }
        return PartyTransferExecutionOwner.Execute(context, ref content, out generatedFacts, out notifications);
    }

    // Observe the original non-transactional operation; do not roll back or retry a
    // debit after a failed target write. AFEF can then distinguish loss from delivery.
    private static PartyTransferEffectResult ObserveTransfer(PartyTransferPromptEntry entry,
        PartyBase target, bool prisoner, Func<int> transfer)
    {
        return PartyTransferEffectObserver.Execute(
            () => ReadTransferSourceCount(entry, prisoner),
            () => ReadTransferRosterCount(prisoner ? target?.PrisonRoster : target?.MemberRoster, entry?.Character),
            transfer);
    }

    private static int ReadTransferRosterCount(TroopRoster roster, CharacterObject character)
    {
        if (roster == null || character == null) return 0;
        int index = roster.FindIndexOfTroop(character);
        return index < 0 ? 0 : Math.Max(0, roster.GetElementCopyAtIndex(index).Number);
    }

    private static int ReadTransferSourceCount(PartyTransferPromptEntry entry, bool prisoner)
    {
        if (entry == null) return 0;
        if (!prisoner && IsPartyTransferVolunteerEntry(entry))
        {
            var types = entry.VolunteerOwner?.VolunteerTypes;
            if (types == null) return 0;
            int maximum = Math.Min(Math.Min(5, GetMaximumRecruitableVolunteerIndex(entry.VolunteerOwner)), types.Length - 1);
            int count = 0;
            for (int index = 0; index <= maximum; index++)
                if (types[index] == entry.Character) count++;
            return count;
        }
        return ReadTransferRosterCount(prisoner ? entry.OwnerParty?.PrisonRoster : entry.OwnerParty?.MemberRoster, entry.Character);
    }

    private static PartyTransferExecutionContext CaptureExecutionContext(Hero targetHero,
        CharacterObject targetCharacter, int targetAgentIndex)
    {
        Hero ruleHero = ResolvePartyTransferRuleHero(targetHero, targetCharacter);
        bool wildernessEligible = TryResolveWildernessNonHeroPartyTransferSource(targetHero,
            targetCharacter, targetAgentIndex, out PartyBase wildernessSource);
        bool eligible = PartyTransferAuthorizationOwner.CanExecute(TWParallel.IsMainThread(),
            IsPartyTransferLordEligible(targetHero, targetCharacter), IsPartyTransferNotableRecruitEligible(ruleHero), wildernessEligible);
        var context = new PartyTransferExecutionContext { Eligible = eligible };
        if (!eligible) return context;
        context.HasTroops = PromptListRetrievalService.TryGetPartyTransferSnapshot(PromptListRetrievalService.PartyTransferTroopsSnapshotScope,
            targetHero, targetCharacter, targetAgentIndex, out context.Troops);
        context.HasPrisoners = PromptListRetrievalService.TryGetPartyTransferSnapshot(PromptListRetrievalService.PartyTransferPrisonersSnapshotScope,
            targetHero, targetCharacter, targetAgentIndex, out context.Prisoners);
        context.HasAllTroops = PromptListRetrievalService.TryGetPartyTransferSnapshot(PromptListRetrievalService.PartyTransferAllTroopsSnapshotScope,
            targetHero, targetCharacter, targetAgentIndex, out context.AllTroops);
        context.HasAllPrisoners = PromptListRetrievalService.TryGetPartyTransferSnapshot(PromptListRetrievalService.PartyTransferAllPrisonersSnapshotScope,
            targetHero, targetCharacter, targetAgentIndex, out context.AllPrisoners);
        PartyBase target = Hero.MainHero?.PartyBelongedTo?.Party ?? MobileParty.MainParty?.Party;
        context.HasTarget = target != null;
        context.DisplayName = ResolvePartyTransferTargetDisplayName(targetHero, targetCharacter, targetAgentIndex);
        context.IsVolunteer = IsPartyTransferVolunteerEntry;
        context.TransferTroop = (entry, requested) => ObserveTransfer(entry, target, false,
            () => TransferPartyMemberEntry(entry,
                ClampWildernessNonHeroPartyTransferAmount(entry, requested, wildernessSource, targetCharacter), target));
        context.TransferPrisoner = (entry, requested) => ObserveTransfer(entry, target, true,
            () => wildernessSource == null || entry?.OwnerParty == wildernessSource
                ? TransferPartyPrisonerEntry(entry, requested, target, ruleHero?.Clan) : 0);
        return context;
    }

    internal static PartyTransferPromptEntry FindDisplayIndexedPartyTransferEntry(IEnumerable<PartyTransferPromptEntry> entries,
        int displayIndex) => PartyTransferExecutionOwner.FindDisplayIndexedPartyTransferEntry(entries, displayIndex);

}
