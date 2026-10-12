using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Actions;
using NpcActionFacts=AnimusForge.MyBehavior.NpcActionFacts;
using System.Text;
using TaleWorlds.CampaignSystem.Party.PartyComponents;using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;using TaleWorlds.CampaignSystem.Party;using TaleWorlds.CampaignSystem.Siege;
using System.Collections.Generic;using System.Linq;using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.MapEvents;using TaleWorlds.CampaignSystem.Party;using TaleWorlds.CampaignSystem.Siege;using TaleWorlds.Core;
using System;
namespace AnimusForge;

// Original battle text marker rule, reused by record and summary readers.
internal sealed class CampaignBattleRecordCaptureAdapter
{
internal static string StripBattlePlayerMarker(string text)
	{
		return (text ?? "").Replace("\uFF08player\uFF09", "").Replace("(player)", "").Trim();
	}

internal static bool ContainsRoutineBanditText(string text)
	{
		string value = (text ?? "").Trim();
		return value.IndexOf(AdapterRecordTexts.Raw("AF_REC_Battle_0001"), StringComparison.OrdinalIgnoreCase) >= 0
			|| value.IndexOf(AdapterRecordTexts.Raw("AF_REC_Battle_0002"), StringComparison.OrdinalIgnoreCase) >= 0
			|| value.IndexOf(AdapterRecordTexts.Raw("AF_REC_Battle_0003"), StringComparison.OrdinalIgnoreCase) >= 0
			|| value.IndexOf(AdapterRecordTexts.Raw("AF_REC_Battle_0004"), StringComparison.OrdinalIgnoreCase) >= 0
			|| value.IndexOf(AdapterRecordTexts.Raw("AF_REC_Battle_0005"), StringComparison.OrdinalIgnoreCase) >= 0
			|| value.IndexOf(AdapterRecordTexts.Raw("AF_REC_Battle_0006"), StringComparison.OrdinalIgnoreCase) >= 0
			|| value.IndexOf("looter", StringComparison.OrdinalIgnoreCase) >= 0
			|| value.IndexOf("bandit", StringComparison.OrdinalIgnoreCase) >= 0
			|| value.IndexOf("raider", StringComparison.OrdinalIgnoreCase) >= 0
			|| value.IndexOf("outlaw", StringComparison.OrdinalIgnoreCase) >= 0;
	}

internal static bool ShouldMentionBattleHero(Hero hero)
	{
		if (hero == null)
		{
			return false;
		}
		if (hero == Hero.MainHero)
		{
			return true;
		}
		return hero.IsLord && !string.IsNullOrWhiteSpace(hero.StringId);
	}

internal static IEnumerable<Hero> GetHeroesFromSiegeEventSide(SiegeEvent siegeEvent, BattleSideEnum side)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<Hero> list = new List<Hero>();
		if (siegeEvent == null)
		{
			return list;
		}
		try
		{
			foreach (PartyBase involvedPartiesForEventType in siegeEvent.GetInvolvedPartiesForEventType(MapEvent.BattleTypes.Siege))
			{
				if (involvedPartiesForEventType == null || involvedPartiesForEventType.Side != side)
				{
					continue;
				}
				Hero leaderHero = involvedPartiesForEventType.LeaderHero;
				string npcActionHeroKey = (leaderHero == Hero.MainHero) ? "__player__" : CampaignCharacterRecordCaptureAdapter.GetNpcActionHeroKey(leaderHero);
				if (ShouldMentionBattleHero(leaderHero) && hashSet.Add(npcActionHeroKey))
				{
					list.Add(leaderHero);
				}
			}
		}
		catch
		{
		}
		return list;
	}

internal static string BuildBattleHeroDisplayName(Hero hero, bool isHighlighted, string highlightTag)
	{
		string text = hero?.Name?.ToString()?.Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = (hero == Hero.MainHero) ? AdapterRecordTexts.Raw("AF_REC_Battle_0007") : "";
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		if (isHighlighted && !string.IsNullOrWhiteSpace(highlightTag))
		{
			text += "（" + highlightTag + "）";
		}
		return text;
	}

internal static string BuildTrackedHeroListText(IEnumerable<Hero> heroes, Hero highlightedHero, string highlightTag, int maxCount = 5)
	{
		if (heroes == null)
		{
			return "";
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<string> list = new List<string>();
		foreach (Hero item in heroes.Where(ShouldMentionBattleHero).OrderByDescending((Hero x) => x == highlightedHero))
		{
			string text = BuildBattleHeroDisplayName(item, item == highlightedHero, highlightTag);
			string npcActionHeroKey = (item == Hero.MainHero) ? "__player__" : CampaignCharacterRecordCaptureAdapter.GetNpcActionHeroKey(item);
			if (string.IsNullOrWhiteSpace(text) || !hashSet.Add(npcActionHeroKey))
			{
				continue;
			}
			list.Add(text);
		}
		if (list.Count <= 0)
		{
			return "";
		}
		if (list.Count <= maxCount)
		{
			return string.Join("、", list);
		}
		return string.Join("、", list.Take(maxCount)) + AdapterRecordTexts.Raw("AF_REC_Battle_0008") + list.Count + AdapterRecordTexts.Raw("AF_REC_Battle_0009");
	}

internal static string BuildTrackedHeroListText(IEnumerable<Hero> heroes, int maxCount = 5)
	{
		return BuildTrackedHeroListText(heroes, null, "", maxCount);
	}

internal static string BuildTrackedHeroListText(MapEventSide side, int maxCount = 5)
	{
		if (side?.Parties == null)
		{
			return "";
		}
		return BuildTrackedHeroListText(side.Parties.Select((MapEventParty x) => x?.Party?.LeaderHero), side.LeaderParty?.LeaderHero, AdapterRecordTexts.Raw("AF_REC_Battle_0010"), maxCount);
	}

internal static string BuildTrackedHeroListText(SiegeEvent siegeEvent, BattleSideEnum side, int maxCount = 5)
	{
		Hero highlightedHero = ((side == BattleSideEnum.Attacker) ? (siegeEvent?.BesiegerCamp?.LeaderParty?.LeaderHero) : (siegeEvent?.BesiegedSettlement?.OwnerClan?.Leader));
		return BuildTrackedHeroListText(GetHeroesFromSiegeEventSide(siegeEvent, side), highlightedHero, AdapterRecordTexts.Raw("AF_REC_Battle_0011"), maxCount);
	}

internal static string GetNearestSettlementNameForParty(MobileParty party)
	{
		Settlement settlement = party?.BesiegedSettlement ?? ResolveSiegeSettlement(party?.SiegeEvent);
		if (settlement == null)
		{
			settlement = party?.Army?.LeaderParty?.BesiegedSettlement ?? ResolveSiegeSettlement(party?.Army?.LeaderParty?.SiegeEvent);
		}
		string text = settlement?.Name?.ToString();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		text = party?.Army?.LeaderParty?.TargetSettlement?.Name?.ToString();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		text = party?.TargetSettlement?.Name?.ToString();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		text = party?.CurrentSettlement?.Name?.ToString();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		text = party?.LastVisitedSettlement?.Name?.ToString();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		text = party?.BesiegedSettlement?.Name?.ToString();
		return string.IsNullOrWhiteSpace(text) ? AdapterRecordTexts.Raw("AF_REC_Battle_0012") : text.Trim();
	}
internal static Settlement ResolveSiegeSettlement(SiegeEvent siegeEvent)
	{
		if (siegeEvent == null)
		{
			return null;
		}
		try
		{
			foreach (Settlement item in Settlement.All)
			{
				if (item?.SiegeEvent == siegeEvent)
				{
					return item;
				}
			}
		}
		catch
		{
		}
		return null;
	}


internal static string GetMapEventLocationLabel(MapEvent mapEvent)
	{
		string text = mapEvent?.MapEventSettlement?.Name?.ToString();
		return string.IsNullOrWhiteSpace(text) ? AdapterRecordTexts.Raw("AF_REC_Battle_0013") : text.Trim();
	}

internal static string GetPrimaryOtherSideLabel(MapEventSide side)
	{
		string text = side?.LeaderParty?.LeaderHero?.Name?.ToString();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		text = BuildMapEventNonLordPartyListText(side, 3, includeCounts: false);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		text = side?.LeaderParty?.Name?.ToString();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		text = side?.MapFaction?.Name?.ToString();
		return string.IsNullOrWhiteSpace(text) ? AdapterRecordTexts.Raw("AF_REC_Battle_0014") : text.Trim();
	}

internal static int GetMapEventTroopCount(MapEvent mapEvent)
	{
		int num = GetMapEventSideCommittedTroopCount(mapEvent?.AttackerSide) + GetMapEventSideCommittedTroopCount(mapEvent?.DefenderSide);
		if (num > 0)
		{
			return num;
		}
		try
		{
			return Math.Max(0, mapEvent?.GetNumberOfInvolvedMen() ?? 0);
		}
		catch
		{
			return 0;
		}
	}

internal static string BuildMapEventCasualtyText(MapEventSide side)
	{
		if (side?.Parties == null)
		{
			return "";
		}
		int num = 0;
		int num2 = 0;
		try
		{
			foreach (MapEventParty party in side.Parties)
			{
				num += GetTroopRosterTotalManCount(party?.DiedInBattle);
				num2 += GetTroopRosterTotalManCount(party?.WoundedInBattle);
			}
		}
		catch
		{
		}
		return AdapterRecordTexts.Raw("AF_REC_Battle_0015") + num + AdapterRecordTexts.Raw("AF_REC_Battle_0016") + num2;
	}

internal static string BuildMapEventStableKey(MapEvent mapEvent, string locationLabel)
	{
		if (mapEvent == null)
		{
			return "";
		}
		List<string> list = new List<string>
		{
			MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe().ToString(),
			MemoryBusinessStateOwner.NormalizeWeeklyPromptKeyPart(locationLabel),
			MemoryEntityIdentityBannerlordAdapter.GetHeroId(mapEvent.AttackerSide?.LeaderParty?.LeaderHero),
			MemoryEntityIdentityBannerlordAdapter.GetHeroId(mapEvent.DefenderSide?.LeaderParty?.LeaderHero),
			BuildMapEventNonLordPartyListText(mapEvent.AttackerSide, 3, includeCounts: false),
			BuildMapEventNonLordPartyListText(mapEvent.DefenderSide, 3, includeCounts: false),
			BuildMapEventCommittedTroopText(mapEvent.AttackerSide),
			BuildMapEventCommittedTroopText(mapEvent.DefenderSide),
			BuildMapEventCasualtyText(mapEvent.AttackerSide),
			BuildMapEventCasualtyText(mapEvent.DefenderSide)
		};
		string text = string.Join("~", list.Select(MemoryBusinessStateOwner.NormalizeWeeklyPromptKeyPart)).Trim('~');
		if (string.IsNullOrWhiteSpace(text))
		{
			text = MemoryBusinessStateOwner.NormalizeWeeklyPromptKeyPart(mapEvent.StringId ?? locationLabel);
		}
		return "mapevent:" + text;
	}

internal static MapEventSide GetMapEventDefeatedSide(MapEvent mapEvent)
	{
		if (mapEvent == null)
		{
			return null;
		}
		if (mapEvent.WinningSide == BattleSideEnum.Attacker)
		{
			return mapEvent.DefenderSide;
		}
		if (mapEvent.WinningSide == BattleSideEnum.Defender)
		{
			return mapEvent.AttackerSide;
		}
		return null;
	}

internal static bool IsBanditOrMonsterMapEvent(MapEvent mapEvent)
	{
		return IsBanditOrMonsterMapEventSide(mapEvent?.AttackerSide) || IsBanditOrMonsterMapEventSide(mapEvent?.DefenderSide);
	}

internal static bool IsBanditOrMonsterMapEventSide(MapEventSide side)
	{
		if (side == null)
		{
			return false;
		}
		try
		{
			if (side.MapFaction?.IsBanditFaction == true || side.LeaderParty?.MapFaction?.IsBanditFaction == true)
			{
				return true;
			}
			foreach (MapEventParty party in side.Parties ?? Enumerable.Empty<MapEventParty>())
			{
				if (party?.Party?.MapFaction?.IsBanditFaction == true || party?.Party?.MobileParty?.MapFaction?.IsBanditFaction == true || party?.Party?.Owner?.MapFaction?.IsBanditFaction == true)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

internal static int GetMapEventSideCommittedTroopCount(MapEventSide side)
	{
		if (side?.Parties == null)
		{
			return 0;
		}
		int num = 0;
		try
		{
			foreach (MapEventParty party in side.Parties)
			{
				num += GetMapEventPartyCommittedTroopCount(party);
			}
		}
		catch
		{
		}
		return Math.Max(0, num);
	}

internal static int GetMapEventPartyCommittedTroopCount(MapEventParty party)
	{
		if (party == null)
		{
			return 0;
		}
		int troopRosterTotalManCount = GetTroopRosterTotalManCount(party.Party?.MemberRoster);
		int troopRosterTotalManCount2 = GetTroopRosterTotalManCount(party.DiedInBattle);
		int troopRosterTotalManCount3 = GetTroopRosterTotalManCount(party.WoundedInBattle);
		return Math.Max(0, troopRosterTotalManCount + troopRosterTotalManCount2 + troopRosterTotalManCount3);
	}

internal static string BuildMapEventNonLordPartyListText(MapEventSide side, int maxCount = 5, bool includeCounts = true)
	{
		if (side?.Parties == null)
		{
			return "";
		}
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			foreach (MapEventParty party in side.Parties)
			{
				PartyBase partyBase = party?.Party;
				if (partyBase == null || ShouldMentionBattleHero(partyBase.LeaderHero))
				{
					continue;
				}
				string text = BuildMapEventNonLordPartyDisplayText(partyBase, party, includeCounts);
				if (string.IsNullOrWhiteSpace(text) || !hashSet.Add(text))
				{
					continue;
				}
				list.Add(text);
			}
		}
		catch
		{
		}
		if (list.Count == 0)
		{
			return "";
		}
		int count = Math.Max(1, maxCount);
		if (list.Count <= count)
		{
			return string.Join("、", list);
		}
		return string.Join("、", list.Take(count)) + AdapterRecordTexts.Raw("AF_REC_Battle_0017") + list.Count + AdapterRecordTexts.Raw("AF_REC_Battle_0018");
	}

internal static string BuildMapEventNonLordPartyDisplayText(PartyBase partyBase, MapEventParty party, bool includeCounts)
	{
		if (partyBase == null)
		{
			return "";
		}
		string text = GetPartyBaseDisplayName(partyBase);
		string text2 = GetMapEventNonLordPartyTypeLabel(partyBase);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = string.IsNullOrWhiteSpace(text2) ? AdapterRecordTexts.Raw("AF_REC_Battle_0019") : text2;
			text2 = "";
		}
		List<string> list = new List<string>();
		if (!string.IsNullOrWhiteSpace(text2) && text.IndexOf(text2, StringComparison.OrdinalIgnoreCase) < 0)
		{
			list.Add(text2);
		}
		if (includeCounts)
		{
			int mapEventPartyCommittedTroopCount = GetMapEventPartyCommittedTroopCount(party);
			if (mapEventPartyCommittedTroopCount > 0)
			{
				list.Add(AdapterRecordTexts.Raw("AF_REC_Battle_0020") + mapEventPartyCommittedTroopCount + AdapterRecordTexts.Raw("AF_REC_Battle_0021"));
			}
		}
		return list.Count > 0 ? (text.Trim() + "（" + string.Join("，", list) + "）") : text.Trim();
	}

internal static string GetMapEventNonLordPartyTypeLabel(PartyBase partyBase)
	{
		try
		{
			MobileParty mobileParty = partyBase?.MobileParty;
			if (mobileParty == null)
			{
				return "";
			}
			if (mobileParty.IsVillager || mobileParty.PartyComponent is VillagerPartyComponent)
			{
				return AdapterRecordTexts.Raw("AF_REC_Battle_0022");
			}
			if (mobileParty.IsCaravan || mobileParty.PartyComponent is CaravanPartyComponent)
			{
				return AdapterRecordTexts.Raw("AF_REC_Battle_0023");
			}
			if (mobileParty.IsPatrolParty || mobileParty.PartyComponent is PatrolPartyComponent)
			{
				return AdapterRecordTexts.Raw("AF_REC_Battle_0024");
			}
			if (mobileParty.IsMilitia || mobileParty.PartyComponent is MilitiaPartyComponent)
			{
				return AdapterRecordTexts.Raw("AF_REC_Battle_0025");
			}
			if (mobileParty.IsGarrison || mobileParty.PartyComponent is GarrisonPartyComponent)
			{
				return AdapterRecordTexts.Raw("AF_REC_Battle_0026");
			}
			if (IsBanditOrOutlawPartyBase(partyBase) || mobileParty.PartyComponent is BanditPartyComponent)
			{
				return AdapterRecordTexts.Raw("AF_REC_Battle_0027");
			}
			if (mobileParty.PartyComponent is CustomPartyComponent)
			{
				return AdapterRecordTexts.Raw("AF_REC_Battle_0028");
			}
		}
		catch
		{
		}
		return AdapterRecordTexts.Raw("AF_REC_Battle_0029");
	}

internal static bool IsBanditOrOutlawPartyBase(PartyBase party)
	{
		if (party == null)
		{
			return false;
		}
		try
		{
			MobileParty mobileParty = party.MobileParty;
			return party.MapFaction?.IsBanditFaction == true
				|| mobileParty?.IsBandit == true
				|| mobileParty?.MapFaction?.IsBanditFaction == true
				|| mobileParty?.ActualClan?.IsBanditFaction == true
				|| party.Owner?.MapFaction?.IsBanditFaction == true;
		}
		catch
		{
			return false;
		}
	}

internal static string BuildMapEventCommittedTroopText(MapEventSide side)
	{
		int mapEventSideCommittedTroopCount = GetMapEventSideCommittedTroopCount(side);
		return (mapEventSideCommittedTroopCount > 0) ? (mapEventSideCommittedTroopCount + AdapterRecordTexts.Raw("AF_REC_Battle_0030")) : "";
	}

internal static int GetTroopRosterTotalManCount(TroopRoster roster)
	{
		try
		{
			return Math.Max(0, roster?.TotalManCount ?? 0);
		}
		catch
		{
			return 0;
		}
	}

internal static string GetPartyBaseDisplayName(PartyBase partyBase)
	{
		if (partyBase == null)
		{
			return "";
		}
		try
		{
			string text = partyBase.Name?.ToString();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Trim();
			}
			text = partyBase.MobileParty?.Name?.ToString();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Trim();
			}
			text = partyBase.MobileParty?.StringId;
			return string.IsNullOrWhiteSpace(text) ? "" : text.Trim();
		}
		catch
		{
			return "";
		}
	}


internal static string BuildArmyCommandClause(MapEventSide side, Hero actorHero)
	{
		Hero leaderHero = side?.LeaderParty?.LeaderHero;
		string text = leaderHero?.Name?.ToString()?.Trim();
		if (string.IsNullOrWhiteSpace(text) || leaderHero == actorHero)
		{
			return "";
		}
		return text + AdapterRecordTexts.Raw("AF_REC_Battle_0031");
	}

internal static int CountTrackedLordParties(MapEventSide side)
	{
		int num = 0;
		try
		{
			if (side?.Parties == null)
			{
				return 0;
			}
			foreach (MapEventParty party in side.Parties)
			{
				if (CampaignCharacterRecordCaptureAdapter.ShouldTrackNpcActionHero(party?.Party?.LeaderHero))
				{
					num++;
				}
			}
		}
		catch
		{
		}
		return num;
	}

internal static string BuildMapEventParticipantStableKey(MapEvent mapEvent, MapEventSide side, Hero hero, string locationLabel)
	{
		string mapEventStableKey = BuildMapEventStableKey(mapEvent, locationLabel);
		return mapEventStableKey + ":side:" + side?.MissionSide + ":hero:" + (hero?.StringId ?? "");
	}

internal static string BuildPlayerDefeatedHeroBattleFactKey(MapEvent mapEvent, MapEventSide defeatedSide, Hero defeatedHero, string locationLabel)
	{
		List<string> list = new List<string>
		{
			"player_defeated_hero_fact",
			MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe().ToString(),
			MemoryBusinessStateOwner.NormalizeWeeklyPromptKeyPart(mapEvent?.StringId),
			MemoryBusinessStateOwner.NormalizeWeeklyPromptKeyPart(locationLabel),
			MemoryBusinessStateOwner.NormalizeWeeklyPromptKeyPart(mapEvent == null ? "" : mapEvent.PlayerSide.ToString()),
			MemoryBusinessStateOwner.NormalizeWeeklyPromptKeyPart(defeatedSide == null ? "" : defeatedSide.MissionSide.ToString()),
			MemoryEntityIdentityBannerlordAdapter.GetHeroId(defeatedHero),
			MemoryEntityIdentityBannerlordAdapter.GetHeroId(defeatedSide?.LeaderParty?.LeaderHero),
			MemoryEntityIdentityBannerlordAdapter.GetHeroId(defeatedSide?.OtherSide?.LeaderParty?.LeaderHero),
			MemoryBusinessStateOwner.NormalizeWeeklyPromptKeyPart(BuildMapEventStableKey(mapEvent, locationLabel))
		};
		return string.Join(":", list.Where((string x) => !string.IsNullOrWhiteSpace(x)));
	}

internal static MapEventSide GetMapEventSideByBattleSide(MapEvent mapEvent, BattleSideEnum side)
	{
		if (mapEvent == null)
		{
			return null;
		}
		if (side == BattleSideEnum.Attacker)
		{
			return mapEvent.AttackerSide;
		}
		if (side == BattleSideEnum.Defender)
		{
			return mapEvent.DefenderSide;
		}
		return null;
	}

internal static string BuildPlayerDefeatedHeroDialogueFact(MapEvent mapEvent, MapEventSide defeatedSide, string locationLabel)
	{
		string playerName = Hero.MainHero?.Name?.ToString()?.Trim();
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = AdapterRecordTexts.Raw("AF_REC_Battle_0032");
		}
		string place = string.IsNullOrWhiteSpace(locationLabel) ? AdapterRecordTexts.Raw("AF_REC_Battle_0033") : locationLabel.Trim();
		string battleKind = AdapterRecordTexts.Raw("AF_REC_Battle_0034");
		if (mapEvent?.IsSiegeAssault == true || mapEvent?.IsSiegeOutside == true || mapEvent?.IsSallyOut == true)
		{
			battleKind = AdapterRecordTexts.Raw("AF_REC_Battle_0035");
		}
		else if (mapEvent?.IsRaid == true)
		{
			battleKind = AdapterRecordTexts.Raw("AF_REC_Battle_0036");
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(DialogueHistoryLedger.AfefPlayerPrefix).Append(AdapterRecordTexts.Raw("AF_REC_Battle_0037")).Append(place).Append(AdapterRecordTexts.Raw("AF_REC_Battle_0038")).Append(battleKind).Append(AdapterRecordTexts.Raw("AF_REC_Battle_0039")).Append(playerName).Append(AdapterRecordTexts.Raw("AF_REC_Battle_0040"));
		string winningLords = BuildTrackedHeroListText(defeatedSide?.OtherSide, 4);
		if (!string.IsNullOrWhiteSpace(winningLords))
		{
			stringBuilder.Append(AdapterRecordTexts.Raw("AF_REC_Battle_0041")).Append(winningLords).Append('。');
		}
		return stringBuilder.ToString();
	}

internal static string BuildNpcDefeatedByPlayerRecentActionText(MapEvent mapEvent, MapEventSide defeatedSide, string locationLabel)
	{
		string playerName = Hero.MainHero?.Name?.ToString()?.Trim();
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = AdapterRecordTexts.Raw("AF_REC_Battle_0032");
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (mapEvent?.IsSiegeAssault == true || mapEvent?.IsSiegeOutside == true || mapEvent?.IsSallyOut == true)
		{
			stringBuilder.Append(AdapterRecordTexts.Raw("AF_REC_Battle_0043")).Append(string.IsNullOrWhiteSpace(locationLabel) ? AdapterRecordTexts.Raw("AF_REC_Battle_0044") : locationLabel.Trim()).Append(AdapterRecordTexts.Raw("AF_REC_Battle_0045")).Append(playerName).Append(AdapterRecordTexts.Raw("AF_REC_Battle_0046"));
		}
		else if (mapEvent?.IsRaid == true)
		{
			stringBuilder.Append(AdapterRecordTexts.Raw("AF_REC_Battle_0047")).Append(string.IsNullOrWhiteSpace(locationLabel) ? AdapterRecordTexts.Raw("AF_REC_Battle_0048") : locationLabel.Trim()).Append(AdapterRecordTexts.Raw("AF_REC_Battle_0049")).Append(playerName).Append(AdapterRecordTexts.Raw("AF_REC_Battle_0050"));
		}
		else
		{
			stringBuilder.Append(AdapterRecordTexts.Raw("AF_REC_Battle_0051")).Append(string.IsNullOrWhiteSpace(locationLabel) ? AdapterRecordTexts.Raw("AF_REC_Battle_0052") : locationLabel.Trim()).Append(AdapterRecordTexts.Raw("AF_REC_Battle_0053")).Append(playerName).Append(AdapterRecordTexts.Raw("AF_REC_Battle_0054"));
		}
		AppendBattleOpponentDetails(stringBuilder, defeatedSide?.OtherSide, AdapterRecordTexts.Raw("AF_REC_Battle_0055"));
		AppendBattleCasualtyDetails(stringBuilder, defeatedSide, defeatedSide?.OtherSide);
		return stringBuilder.ToString();
	}

internal static string BuildPlayerBattleDefeatRecentActionText(MapEvent mapEvent, MapEventSide playerSide, string locationLabel)
	{
		string opponentLabel = BuildMapEventSideNarrativeLabel(playerSide?.OtherSide);
		if (string.IsNullOrWhiteSpace(opponentLabel))
		{
			opponentLabel = AdapterRecordTexts.Raw("AF_REC_Battle_0056");
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (mapEvent?.IsSiegeAssault == true || mapEvent?.IsSiegeOutside == true || mapEvent?.IsSallyOut == true)
		{
			stringBuilder.Append(AdapterRecordTexts.Raw("AF_REC_Battle_0057")).Append(string.IsNullOrWhiteSpace(locationLabel) ? AdapterRecordTexts.Raw("AF_REC_Battle_0058") : locationLabel.Trim()).Append(AdapterRecordTexts.Raw("AF_REC_Battle_0059")).Append(opponentLabel).Append("。");
		}
		else if (mapEvent?.IsRaid == true)
		{
			stringBuilder.Append(AdapterRecordTexts.Raw("AF_REC_Battle_0060")).Append(string.IsNullOrWhiteSpace(locationLabel) ? AdapterRecordTexts.Raw("AF_REC_Battle_0061") : locationLabel.Trim()).Append(AdapterRecordTexts.Raw("AF_REC_Battle_0062")).Append(opponentLabel).Append("。");
		}
		else
		{
			stringBuilder.Append(AdapterRecordTexts.Raw("AF_REC_Battle_0063")).Append(string.IsNullOrWhiteSpace(locationLabel) ? AdapterRecordTexts.Raw("AF_REC_Battle_0064") : locationLabel.Trim()).Append(AdapterRecordTexts.Raw("AF_REC_Battle_0065")).Append(opponentLabel).Append("。");
		}
		AppendBattleOpponentDetails(stringBuilder, playerSide?.OtherSide, AdapterRecordTexts.Raw("AF_REC_Battle_0066"));
		AppendBattleCasualtyDetails(stringBuilder, playerSide, playerSide?.OtherSide);
		return stringBuilder.ToString();
	}

internal static void AppendBattleOpponentDetails(StringBuilder stringBuilder, MapEventSide opponentSide, string label)
	{
		if (stringBuilder == null || opponentSide == null)
		{
			return;
		}
		string sideLabel = string.IsNullOrWhiteSpace(label) ? AdapterRecordTexts.Raw("AF_REC_Battle_0067") : label.Trim();
		string lords = BuildTrackedHeroListText(opponentSide, 6);
		if (!string.IsNullOrWhiteSpace(lords))
		{
			stringBuilder.Append(' ').Append(sideLabel).Append(AdapterRecordTexts.Raw("AF_REC_Battle_0068")).Append(lords).Append('。');
		}
		string nonLordParties = BuildMapEventNonLordPartyListText(opponentSide, 5);
		if (!string.IsNullOrWhiteSpace(nonLordParties))
		{
			stringBuilder.Append(' ').Append(sideLabel).Append(AdapterRecordTexts.Raw("AF_REC_Battle_0069")).Append(nonLordParties).Append('。');
		}
	}

internal static void AppendBattleCasualtyDetails(StringBuilder stringBuilder, MapEventSide defeatedSide, MapEventSide winningSide)
	{
		if (stringBuilder == null)
		{
			return;
		}
		string defeatedCasualties = BuildMapEventCasualtyText(defeatedSide);
		string winningCasualties = BuildMapEventCasualtyText(winningSide);
		if (!string.IsNullOrWhiteSpace(defeatedCasualties) || !string.IsNullOrWhiteSpace(winningCasualties))
		{
			stringBuilder.Append(AdapterRecordTexts.Raw("AF_REC_Battle_0070")).Append(string.IsNullOrWhiteSpace(defeatedCasualties) ? AdapterRecordTexts.Raw("AF_REC_Battle_0071") : defeatedCasualties);
			stringBuilder.Append(AdapterRecordTexts.Raw("AF_REC_Battle_0072")).Append(string.IsNullOrWhiteSpace(winningCasualties) ? AdapterRecordTexts.Raw("AF_REC_Battle_0073") : winningCasualties).Append('。');
		}
	}

internal static bool ShouldSkipRoutineBanditDefeatMapEvent(MapEvent mapEvent)
	{
		if (mapEvent == null || !mapEvent.HasWinner)
		{
			return false;
		}
		if (mapEvent.IsSiegeAssault || mapEvent.IsSiegeOutside || mapEvent.IsSallyOut || mapEvent.IsRaid)
		{
			return false;
		}
		MapEventSide defeatedSide = GetMapEventDefeatedSide(mapEvent);
		return IsRoutineBanditMapEventSide(defeatedSide);
	}

internal static bool IsRoutineBanditMapEventSide(MapEventSide side)
	{
		if (side == null)
		{
			return false;
		}
		try
		{
			if (ShouldMentionBattleHero(side.LeaderParty?.LeaderHero))
			{
				return false;
			}
			bool hasBanditParty = IsBanditOrOutlawPartyBase(side.LeaderParty) || side.MapFaction?.IsBanditFaction == true;
			foreach (MapEventParty party in side.Parties ?? Enumerable.Empty<MapEventParty>())
			{
				PartyBase partyBase = party?.Party;
				if (ShouldMentionBattleHero(partyBase?.LeaderHero))
				{
					return false;
				}
				if (IsBanditOrOutlawPartyBase(partyBase))
				{
					hasBanditParty = true;
				}
			}
			return hasBanditParty;
		}
		catch
		{
			return false;
		}
	}

internal static string BuildRoutineBanditDefeatStableKey(MapEvent mapEvent, MapEventSide defeatedSide, string locationLabel)
	{
		List<string> list = new List<string>
		{
			MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe().ToString(),
			MemoryBusinessStateOwner.NormalizeWeeklyPromptKeyPart(mapEvent?.StringId),
			MemoryBusinessStateOwner.NormalizeWeeklyPromptKeyPart(locationLabel),
			MemoryBusinessStateOwner.NormalizeWeeklyPromptKeyPart(GetPrimaryOtherSideLabel(defeatedSide)),
			GetMapEventSideCommittedTroopCount(defeatedSide).ToString(),
			MemoryBusinessStateOwner.NormalizeWeeklyPromptKeyPart(BuildMapEventCasualtyText(defeatedSide)),
			MemoryBusinessStateOwner.NormalizeWeeklyPromptKeyPart(BuildMapEventStableKey(mapEvent, locationLabel))
		};
		return "wild_party_defeated:" + string.Join(":", list.Where((string x) => !string.IsNullOrWhiteSpace(x)));
	}

internal static int GetPartyBaseTroopCount(PartyBase party)
	{
		try
		{
			return Math.Max(0, party?.MemberRoster?.TotalManCount ?? 0);
		}
		catch
		{
			return 0;
		}
	}

internal static int GetMapEventSideCasualtyCount(MapEventSide side)
	{
		if (side?.Parties == null)
		{
			return 0;
		}
		int num = 0;
		try
		{
			foreach (MapEventParty party in side.Parties)
			{
				num += GetTroopRosterTotalManCount(party?.DiedInBattle);
				num += GetTroopRosterTotalManCount(party?.WoundedInBattle);
			}
		}
		catch
		{
		}
		return Math.Max(0, num);
	}

internal static void ApplyMapEventOppositeSideFacts(NpcActionFacts facts, MapEventSide oppositeSide)
	{
		if (facts == null || oppositeSide == null)
		{
			return;
		}
		CampaignCharacterRecordCaptureAdapter.AddRelatedFactionFacts(facts, oppositeSide.MapFaction);
		Hero leaderHero = oppositeSide.LeaderParty?.LeaderHero;
		if (leaderHero != null)
		{
			if (string.IsNullOrWhiteSpace(facts.TargetHeroId))
			{
				facts.TargetHeroId = MemoryEntityIdentityBannerlordAdapter.GetHeroId(leaderHero);
			}
			if (string.IsNullOrWhiteSpace(facts.TargetClanId))
			{
				facts.TargetClanId = MemoryEntityIdentityBannerlordAdapter.GetClanId(leaderHero.Clan);
			}
			if (string.IsNullOrWhiteSpace(facts.TargetKingdomId))
			{
				facts.TargetKingdomId = MemoryEntityIdentityBannerlordAdapter.GetKingdomId(leaderHero.MapFaction);
			}
			CampaignCharacterRecordCaptureAdapter.AddUniqueId(facts.RelatedHeroIds, facts.TargetHeroId);
			CampaignCharacterRecordCaptureAdapter.AddUniqueId(facts.RelatedClanIds, facts.TargetClanId);
			CampaignCharacterRecordCaptureAdapter.AddUniqueId(facts.RelatedKingdomIds, facts.TargetKingdomId);
		}
		foreach (MapEventParty party in oppositeSide.Parties ?? Enumerable.Empty<MapEventParty>())
		{
			Hero hero = party?.Party?.LeaderHero;
			if (hero == null)
			{
				continue;
			}
			CampaignCharacterRecordCaptureAdapter.AddUniqueId(facts.RelatedHeroIds, MemoryEntityIdentityBannerlordAdapter.GetHeroId(hero));
			CampaignCharacterRecordCaptureAdapter.AddUniqueId(facts.RelatedClanIds, MemoryEntityIdentityBannerlordAdapter.GetClanId(hero.Clan));
			CampaignCharacterRecordCaptureAdapter.AddUniqueId(facts.RelatedKingdomIds, MemoryEntityIdentityBannerlordAdapter.GetKingdomId(hero.MapFaction));
		}
	}

internal static string BuildMapEventNarrative(MapEvent mapEvent, MapEventSide side, Hero actorHero, bool won, string locationLabel)
	{
		if (mapEvent == null || side == null)
		{
			return "";
		}
		string text = BuildMapEventActorRoleClause(actorHero, side);
		string text2 = BuildMapEventSideNarrativeLabel(side.OtherSide);
		Settlement mapEventSettlement = mapEvent.MapEventSettlement;
		string text3 = BuildArmyCommandClause(side, actorHero);
		string text4;
		if (mapEvent.IsSiegeAssault || mapEvent.IsSiegeOutside || mapEvent.IsSallyOut)
		{
			string text5 = mapEventSettlement?.Name?.ToString();
			if (string.IsNullOrWhiteSpace(text5))
			{
				text5 = locationLabel;
			}
			string text6 = MemoryEntityIdentityBannerlordAdapter.GetFactionDisplayName(side.OtherSide?.MapFaction, text2);
			text4 = ((side.MissionSide == BattleSideEnum.Attacker) ? (won ? (text + AdapterRecordTexts.Raw("AF_REC_Battle_0074") + (string.IsNullOrWhiteSpace(text3) ? AdapterRecordTexts.Raw("AF_REC_Battle_0075") : (text3 + AdapterRecordTexts.Raw("AF_REC_Battle_0076"))) + text6 + AdapterRecordTexts.Raw("AF_REC_Battle_0077") + text5.Trim() + AdapterRecordTexts.Raw("AF_REC_Battle_0078")) : (text + AdapterRecordTexts.Raw("AF_REC_Battle_0079") + (string.IsNullOrWhiteSpace(text3) ? AdapterRecordTexts.Raw("AF_REC_Battle_0080") : (text3 + AdapterRecordTexts.Raw("AF_REC_Battle_0081"))) + text6 + AdapterRecordTexts.Raw("AF_REC_Battle_0082") + text5.Trim() + AdapterRecordTexts.Raw("AF_REC_Battle_0083"))) : (won ? (text + AdapterRecordTexts.Raw("AF_REC_Battle_0084") + (string.IsNullOrWhiteSpace(text3) ? "" : (text3 + AdapterRecordTexts.Raw("AF_REC_Battle_0085"))) + text5.Trim() + AdapterRecordTexts.Raw("AF_REC_Battle_0086") + text6 + "。") : (text + AdapterRecordTexts.Raw("AF_REC_Battle_0087") + (string.IsNullOrWhiteSpace(text3) ? "" : (text3 + AdapterRecordTexts.Raw("AF_REC_Battle_0088"))) + text5.Trim() + AdapterRecordTexts.Raw("AF_REC_Battle_0089") + text6 + "。")));
		}
		else if (mapEvent.IsRaid)
		{
			string raidLocation = string.IsNullOrWhiteSpace(locationLabel) ? AdapterRecordTexts.Raw("AF_REC_Battle_0090") : locationLabel.Trim();
			string raidCommandPrefix = string.IsNullOrWhiteSpace(text3) ? AdapterRecordTexts.Raw("AF_REC_Battle_0091") : (AdapterRecordTexts.Raw("AF_REC_Battle_0092") + text3 + AdapterRecordTexts.Raw("AF_REC_Battle_0093"));
			if (side.MissionSide == BattleSideEnum.Attacker)
			{
				text4 = won ? (text + "，" + raidCommandPrefix + raidLocation + AdapterRecordTexts.Raw("AF_REC_Battle_0094") + text2 + AdapterRecordTexts.Raw("AF_REC_Battle_0095")) : (text + "，" + raidCommandPrefix + raidLocation + AdapterRecordTexts.Raw("AF_REC_Battle_0096") + text2 + AdapterRecordTexts.Raw("AF_REC_Battle_0097"));
			}
			else if (side.MissionSide == BattleSideEnum.Defender)
			{
				text4 = won ? (text + "，" + raidCommandPrefix + raidLocation + AdapterRecordTexts.Raw("AF_REC_Battle_0098") + text2 + AdapterRecordTexts.Raw("AF_REC_Battle_0099")) : (text + "，" + raidCommandPrefix + raidLocation + AdapterRecordTexts.Raw("AF_REC_Battle_0100") + text2 + AdapterRecordTexts.Raw("AF_REC_Battle_0101"));
			}
			else
			{
				text4 = won ? (text + "，" + raidCommandPrefix + raidLocation + AdapterRecordTexts.Raw("AF_REC_Battle_0102")) : (text + "，" + raidCommandPrefix + raidLocation + AdapterRecordTexts.Raw("AF_REC_Battle_0103"));
			}
		}
		else
		{
			text4 = (won ? (text + AdapterRecordTexts.Raw("AF_REC_Battle_0104") + (string.IsNullOrWhiteSpace(text3) ? "" : (text3 + AdapterRecordTexts.Raw("AF_REC_Battle_0105"))) + locationLabel + AdapterRecordTexts.Raw("AF_REC_Battle_0106") + text2 + "。") : (text + AdapterRecordTexts.Raw("AF_REC_Battle_0107") + (string.IsNullOrWhiteSpace(text3) ? "" : (text3 + AdapterRecordTexts.Raw("AF_REC_Battle_0108"))) + locationLabel + AdapterRecordTexts.Raw("AF_REC_Battle_0109") + text2 + "。"));
		}
		StringBuilder stringBuilder = new StringBuilder(text4);
		string text7 = BuildTrackedHeroListText(side, 5);
		string text8 = BuildTrackedHeroListText(side.OtherSide, 5);
		string text9 = BuildMapEventNonLordPartyListText(side, 5);
		string text10 = BuildMapEventNonLordPartyListText(side.OtherSide, 5);
		if (!string.IsNullOrWhiteSpace(text7))
		{
			stringBuilder.Append(AdapterRecordTexts.Raw("AF_REC_Battle_0110")).Append(text7).Append('。');
		}
		if (!string.IsNullOrWhiteSpace(text8))
		{
			stringBuilder.Append(AdapterRecordTexts.Raw("AF_REC_Battle_0111")).Append(text8).Append('。');
		}
		if (!string.IsNullOrWhiteSpace(text9))
		{
			stringBuilder.Append(AdapterRecordTexts.Raw("AF_REC_Battle_0112")).Append(text9).Append('。');
		}
		if (!string.IsNullOrWhiteSpace(text10))
		{
			stringBuilder.Append(AdapterRecordTexts.Raw("AF_REC_Battle_0113")).Append(text10).Append('。');
		}
		string text11 = BuildMapEventCommittedTroopText(side);
		string text12 = BuildMapEventCommittedTroopText(side.OtherSide);
		if (!string.IsNullOrWhiteSpace(text11) || !string.IsNullOrWhiteSpace(text12))
		{
			stringBuilder.Append(AdapterRecordTexts.Raw("AF_REC_Battle_0114")).Append(string.IsNullOrWhiteSpace(text11) ? AdapterRecordTexts.Raw("AF_REC_Battle_0115") : text11);
			stringBuilder.Append(AdapterRecordTexts.Raw("AF_REC_Battle_0116")).Append(string.IsNullOrWhiteSpace(text12) ? AdapterRecordTexts.Raw("AF_REC_Battle_0117") : text12).Append('。');
		}
		string text13 = BuildMapEventStandoutText(side, side.OtherSide);
		string text14 = BuildMapEventCasualtyText(side);
		string text15 = BuildMapEventCasualtyText(side.OtherSide);
		stringBuilder.Append(AdapterRecordTexts.Raw("AF_REC_Battle_0118")).Append(text14);
		stringBuilder.Append(AdapterRecordTexts.Raw("AF_REC_Battle_0119")).Append(text15).Append('。');
		if (!string.IsNullOrWhiteSpace(text13))
		{
			stringBuilder.Append(" ：").Append(text13);
		}
		return stringBuilder.ToString();
	}

internal static string BuildMapEventStandoutText(MapEventSide side, MapEventSide oppositeSide = null)
	{
		if (side?.Parties == null)
		{
			return "";
		}
		if (oppositeSide != null && CountTrackedLordParties(oppositeSide) <= 0 && !string.IsNullOrWhiteSpace(BuildMapEventNonLordPartyListText(oppositeSide, 1, includeCounts: false)))
		{
			return "";
		}
		List<MapEventParty> list = side.Parties.Where((MapEventParty x) => ShouldMentionBattleHero(x?.Party?.LeaderHero)).ToList();
		if (list.Count <= 4)
		{
			return "";
		}
		MapEventParty mapEventParty = list.OrderByDescending((MapEventParty x) => x.ContributionToBattle).FirstOrDefault();
		if (mapEventParty == null || mapEventParty.ContributionToBattle <= 0)
		{
			return "";
		}
		int num = list.Sum((MapEventParty x) => Math.Max(0, x.ContributionToBattle));
		Hero leaderHero = mapEventParty.Party?.LeaderHero;
		string text = BuildBattleHeroDisplayName(leaderHero, side.LeaderParty?.LeaderHero == leaderHero, AdapterRecordTexts.Raw("AF_REC_Battle_0120"));
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		if (num <= 0)
		{
			return text + AdapterRecordTexts.Raw("AF_REC_Battle_0121");
		}
		int num2 = (int)Math.Round((double)(100 * mapEventParty.ContributionToBattle) / (double)num);
		if (num2 < 30)
		{
			return "";
		}
		string text2 = MemoryEntityIdentityBannerlordAdapter.GetFactionDisplayName(leaderHero?.MapFaction ?? side.MapFaction, AdapterRecordTexts.Raw("AF_REC_Battle_0122"));
		if (num2 > 90)
		{
			return AdapterRecordTexts.Raw("AF_REC_Battle_0123") + text2 + AdapterRecordTexts.Raw("AF_REC_Battle_0124") + text + AdapterRecordTexts.Raw("AF_REC_Battle_0125");
		}
		if (num2 > 75)
		{
			return text2 + AdapterRecordTexts.Raw("AF_REC_Battle_0126") + text + AdapterRecordTexts.Raw("AF_REC_Battle_0127");
		}
		if (num2 > 50)
		{
			return text2 + AdapterRecordTexts.Raw("AF_REC_Battle_0128") + text + AdapterRecordTexts.Raw("AF_REC_Battle_0129") + text2 + AdapterRecordTexts.Raw("AF_REC_Battle_0130");
		}
		return text2 + AdapterRecordTexts.Raw("AF_REC_Battle_0131") + text + AdapterRecordTexts.Raw("AF_REC_Battle_0132");
	}

internal static string BuildMapEventAftermathText(MapEvent mapEvent, MapEventSide side, bool won, string locationLabel)
	{
		if (mapEvent == null || side == null)
		{
			return "";
		}
		if (mapEvent.IsSiegeAssault || mapEvent.IsSiegeOutside || mapEvent.IsSallyOut)
		{
			return "";
		}
		if (mapEvent.IsRaid)
		{
			string raidLocation = string.IsNullOrWhiteSpace(locationLabel) ? AdapterRecordTexts.Raw("AF_REC_Battle_0090") : locationLabel.Trim();
			if (side.MissionSide == BattleSideEnum.Attacker)
			{
				return won ? (raidLocation + AdapterRecordTexts.Raw("AF_REC_Battle_0134")) : (raidLocation + AdapterRecordTexts.Raw("AF_REC_Battle_0135"));
			}
			if (side.MissionSide == BattleSideEnum.Defender)
			{
				return won ? (raidLocation + AdapterRecordTexts.Raw("AF_REC_Battle_0136")) : (raidLocation + AdapterRecordTexts.Raw("AF_REC_Battle_0137"));
			}
			return won ? (raidLocation + AdapterRecordTexts.Raw("AF_REC_Battle_0138")) : (raidLocation + AdapterRecordTexts.Raw("AF_REC_Battle_0139"));
		}
		return won ? (AdapterRecordTexts.Raw("AF_REC_Battle_0140") + locationLabel + AdapterRecordTexts.Raw("AF_REC_Battle_0141")) : (AdapterRecordTexts.Raw("AF_REC_Battle_0142") + locationLabel + AdapterRecordTexts.Raw("AF_REC_Battle_0143"));
	}

internal static string BuildMapEventActorRoleClause(Hero actorHero, MapEventSide side)
	{
		string text = MemoryEntityIdentityBannerlordAdapter.GetHeroFactionDisplayName(actorHero, side?.MapFaction);
		if (actorHero?.IsLord == true)
		{
			return AdapterRecordTexts.Raw("AF_REC_Battle_0144") + text + AdapterRecordTexts.Raw("AF_REC_Battle_0145");
		}
		if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, AdapterRecordTexts.Raw("AF_REC_Battle_0146"), StringComparison.OrdinalIgnoreCase))
		{
			return AdapterRecordTexts.Raw("AF_REC_Battle_0147") + text + AdapterRecordTexts.Raw("AF_REC_Battle_0148");
		}
		return AdapterRecordTexts.Raw("AF_REC_Battle_0149");
	}

internal static string BuildMapEventSideNarrativeLabel(MapEventSide side)
	{
		if (side == null)
		{
			return AdapterRecordTexts.Raw("AF_REC_Battle_0150");
		}
		if (CountTrackedLordParties(side) > 0)
		{
			return MemoryEntityIdentityBannerlordAdapter.GetFactionDisplayName(side.MapFaction, GetPrimaryOtherSideLabel(side));
		}
		string text = BuildMapEventNonLordPartyListText(side, 3, includeCounts: false);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		return MemoryEntityIdentityBannerlordAdapter.GetFactionDisplayName(side.MapFaction, GetPrimaryOtherSideLabel(side));
	}

internal static bool ShouldRecordMajorBattleAction(int troopCount)
	{
		return troopCount > 500;
	}

internal HashSet<string> RecentlyDefeatedByPlayer = new HashSet<string>();
internal readonly HashSet<string> PlayerDefeatedHeroBattleFactKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
private readonly Func<CampaignCharacterRecordCaptureAdapter> _record;
private readonly Action<Hero,string,string,string> _appendHistory;
internal CampaignBattleRecordCaptureAdapter(Func<CampaignCharacterRecordCaptureAdapter> record,Action<Hero,string,string,string> appendHistory,RemovedPartyMemoryPorts removed=null) {_record=record;_appendHistory=appendHistory;_removed=removed;}
internal void OnMapEventEnded(MapEvent mapEvent)
	{
		try
		{
			if (mapEvent != null && mapEvent.IsPlayerMapEvent && mapEvent.HasWinner && mapEvent.WinningSide == mapEvent.PlayerSide)
			{
				MapEventSide mapEventSide = ((mapEvent.PlayerSide == BattleSideEnum.Attacker) ? mapEvent.DefenderSide : mapEvent.AttackerSide);
				foreach (MapEventParty party in mapEventSide.Parties)
				{
					Hero hero = party.Party?.LeaderHero;
					if (hero != null && hero != Hero.MainHero && hero.IsLord)
					{
						RecordPlayerDefeatedHeroBattleFact(mapEvent, mapEventSide, hero, "map_event_ended");
					}
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("BattleStatus", "[ERROR] OnMapEventEnded: " + ex.Message);
		}
		try
		{
			RecordPlayerBattleDefeatRecentAction(mapEvent);
		}
		catch (Exception ex2)
		{
			Logger.Log("NpcAction", "[ERROR] RecordPlayerBattleDefeatRecentAction: " + ex2.Message);
		}
		try
		{
			TrackNpcActionsFromMapEvent(mapEvent);
		}
		catch (Exception ex3)
		{
			Logger.Log("NpcAction", "[ERROR] TrackNpcActionsFromMapEvent: " + ex3.Message);
		}
		try
		{
			RecordPlayerRoutineBanditDefeatRecentAction(mapEvent);
		}
		catch (Exception ex4)
		{
			Logger.Log("NpcAction", "[ERROR] RecordPlayerRoutineBanditDefeatRecentAction: " + ex4.Message);
		}
		try
		{
			RecordPlayerHideoutClearRecentAction(mapEvent);
		}
		catch (Exception ex5)
		{
			Logger.Log("NpcAction", "[ERROR] RecordPlayerHideoutClearRecentAction: " + ex5.Message);
		}
	}

internal void OnPlayerBattleEndForDefeatMemory(MapEvent mapEvent)
	{
		try
		{
			if (mapEvent == null || !mapEvent.IsPlayerMapEvent || !mapEvent.HasWinner || mapEvent.WinningSide != mapEvent.PlayerSide)
			{
				return;
			}
			MapEventSide defeatedSide = GetMapEventDefeatedSide(mapEvent);
			if (defeatedSide?.Parties == null)
			{
				return;
			}
			foreach (MapEventParty party in defeatedSide.Parties)
			{
				Hero hero = party?.Party?.LeaderHero;
				if (hero != null && hero != Hero.MainHero && hero.IsLord)
				{
					RecordPlayerDefeatedHeroBattleFact(mapEvent, defeatedSide, hero, "player_battle_end");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("BattleStatus", "[ERROR] OnPlayerBattleEndForDefeatMemory: " + ex.Message);
		}
	}

internal void RecordPlayerDefeatedHeroBattleFact(MapEvent mapEvent, MapEventSide defeatedSide, Hero defeatedHero, string reason)
	{
		if (mapEvent == null || defeatedHero == null || defeatedHero == Hero.MainHero || string.IsNullOrWhiteSpace(defeatedHero.StringId) || !mapEvent.IsPlayerMapEvent || !mapEvent.HasWinner || mapEvent.WinningSide != mapEvent.PlayerSide)
		{
			return;
		}
		defeatedSide ??= GetMapEventDefeatedSide(mapEvent);
		if (defeatedSide == null)
		{
			return;
		}
		string locationLabel = GetMapEventLocationLabel(mapEvent);
		string stableKey = BuildPlayerDefeatedHeroBattleFactKey(mapEvent, defeatedSide, defeatedHero, locationLabel);
		if (!PlayerDefeatedHeroBattleFactKeys.Add(stableKey))
		{
			return;
		}
		RecentlyDefeatedByPlayer.Add(defeatedHero.StringId);
		Logger.Log("BattleStatus", "Player defeat of lord fact recorded reason=" + (reason ?? "") + " hero=" + defeatedHero.StringId + " name=" + (defeatedHero.Name?.ToString() ?? ""));
		_appendHistory(defeatedHero, null, null, BuildPlayerDefeatedHeroDialogueFact(mapEvent, defeatedSide, locationLabel));
		RecordNpcDefeatedByPlayerRecentAction(mapEvent, defeatedSide, defeatedHero);
	}

internal void RecordNpcDefeatedByPlayerRecentAction(MapEvent mapEvent, MapEventSide defeatedSide, Hero defeatedHero)
	{
		if (mapEvent == null || defeatedHero == null || defeatedHero == Hero.MainHero || !mapEvent.IsPlayerMapEvent || !mapEvent.HasWinner || mapEvent.WinningSide != mapEvent.PlayerSide)
		{
			return;
		}
		defeatedSide ??= GetMapEventDefeatedSide(mapEvent);
		if (defeatedSide == null || !CampaignCharacterRecordCaptureAdapter.ShouldTrackNpcActionHero(defeatedHero))
		{
			return;
		}
		string locationLabel = GetMapEventLocationLabel(mapEvent);
		string text = BuildNpcDefeatedByPlayerRecentActionText(mapEvent, defeatedSide, locationLabel);
		string stableKey = BuildMapEventParticipantStableKey(mapEvent, defeatedSide, defeatedHero, locationLabel);
		bool isMajor = ShouldRecordMajorBattleAction(GetMapEventTroopCount(mapEvent));
		_record().RecordExternalNpcAction(defeatedHero, text, stableKey, "map_event", isMajor, isRecent: true, targetHero: Hero.MainHero, settlement: mapEvent.MapEventSettlement, locationText: locationLabel, allowNonLordHero: false, won: false);
	}

internal void RecordPlayerBattleDefeatRecentAction(MapEvent mapEvent)
	{
		if (mapEvent == null || !mapEvent.IsPlayerMapEvent || !mapEvent.HasWinner || mapEvent.WinningSide == mapEvent.PlayerSide || Hero.MainHero == null)
		{
			return;
		}
		MapEventSide playerSide = GetMapEventSideByBattleSide(mapEvent, mapEvent.PlayerSide);
		if (playerSide == null)
		{
			return;
		}
		string locationLabel = GetMapEventLocationLabel(mapEvent);
		string text = BuildPlayerBattleDefeatRecentActionText(mapEvent, playerSide, locationLabel);
		string stableKey = BuildMapEventParticipantStableKey(mapEvent, playerSide, Hero.MainHero, locationLabel);
		Hero targetHero = playerSide.OtherSide?.LeaderParty?.LeaderHero;
		bool isMajor = ShouldRecordMajorBattleAction(GetMapEventTroopCount(mapEvent));
		_record().RecordExternalPlayerAction(text, stableKey, "map_event", isMajor, targetHero, mapEvent.MapEventSettlement, locationLabel, won: false);
	}

internal void RecordPlayerRoutineBanditDefeatRecentAction(MapEvent mapEvent)
	{
		if (mapEvent == null || !mapEvent.IsPlayerMapEvent || !mapEvent.HasWinner || mapEvent.WinningSide != mapEvent.PlayerSide || mapEvent.IsHideoutBattle)
		{
			return;
		}
		if (!ShouldSkipRoutineBanditDefeatMapEvent(mapEvent))
		{
			return;
		}
		MapEventSide defeatedSide = GetMapEventDefeatedSide(mapEvent);
		if (defeatedSide == null)
		{
			return;
		}
		string locationLabel = GetMapEventLocationLabel(mapEvent);
		string enemyLabel = GetPrimaryOtherSideLabel(defeatedSide);
		if (string.IsNullOrWhiteSpace(enemyLabel) || string.Equals(enemyLabel, AdapterRecordTexts.Raw("AF_REC_Battle_0151"), StringComparison.OrdinalIgnoreCase))
		{
			enemyLabel = AdapterRecordTexts.Raw("AF_REC_Battle_0152");
		}
		int enemyCount = GetMapEventSideCommittedTroopCount(defeatedSide);
		StringBuilder stringBuilder = new StringBuilder();
		if (string.Equals(locationLabel, AdapterRecordTexts.Raw("AF_REC_Battle_0153"), StringComparison.Ordinal))
		{
			stringBuilder.Append(AdapterRecordTexts.Raw("AF_REC_Battle_0154")).Append(enemyLabel).Append(AdapterRecordTexts.Raw("AF_REC_Battle_0155"));
		}
		else
		{
			stringBuilder.Append(AdapterRecordTexts.Raw("AF_REC_Battle_0156")).Append(locationLabel).Append(AdapterRecordTexts.Raw("AF_REC_Battle_0157")).Append(enemyLabel).Append(AdapterRecordTexts.Raw("AF_REC_Battle_0158"));
		}
		if (enemyCount > 0)
		{
			stringBuilder.Append(AdapterRecordTexts.Raw("AF_REC_Battle_0159")).Append(enemyCount).Append(AdapterRecordTexts.Raw("AF_REC_Battle_0160"));
		}
		if (GetMapEventSideCasualtyCount(defeatedSide) > 0)
		{
			stringBuilder.Append(AdapterRecordTexts.Raw("AF_REC_Battle_0161")).Append(BuildMapEventCasualtyText(defeatedSide)).Append("。");
		}
		string stableKey = BuildRoutineBanditDefeatStableKey(mapEvent, defeatedSide, locationLabel);
		_record().RecordExternalPlayerAction(stringBuilder.ToString(), stableKey, "wild_party_defeated", isMajor: false, targetHero: null, settlement: mapEvent.MapEventSettlement, locationText: locationLabel, won: true);
	}

internal void RecordPlayerHideoutClearRecentAction(MapEvent mapEvent)
	{
		if (mapEvent == null || !mapEvent.IsPlayerMapEvent || !mapEvent.HasWinner || mapEvent.WinningSide != mapEvent.PlayerSide || !mapEvent.IsHideoutBattle)
		{
			return;
		}
		Settlement settlement = mapEvent.MapEventSettlement;
		string settlementName = MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName(settlement);
		if (string.IsNullOrWhiteSpace(settlementName))
		{
			settlementName = AdapterRecordTexts.Raw("AF_REC_Battle_0162");
		}
		string text = AdapterRecordTexts.Raw("AF_REC_Battle_0163") + settlementName + AdapterRecordTexts.Raw("AF_REC_Battle_0164");
		string stableKey = "hideout_cleared:" + (MemoryEntityIdentityBannerlordAdapter.GetSettlementId(settlement) ?? "") + ":" + MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe();
		_record().RecordExternalPlayerAction(text, stableKey, "hideout_cleared", isMajor: false, targetHero: null, settlement: settlement, locationText: settlementName, won: true);
	}

internal void TrackNpcActionsFromMapEvent(MapEvent mapEvent)
	{
		if (mapEvent == null || !mapEvent.HasWinner)
		{
			return;
		}
		if (mapEvent.IsHideoutBattle)
		{
			return;
		}
		if (ShouldSkipRoutineBanditDefeatMapEvent(mapEvent))
		{
			return;
		}
		bool hasBanditSide = IsBanditOrMonsterMapEvent(mapEvent);
		int mapEventTroopCount = GetMapEventTroopCount(mapEvent);
		bool isMajor = ShouldRecordMajorBattleAction(mapEventTroopCount);
		string mapEventLocationLabel = GetMapEventLocationLabel(mapEvent);
		string mapEventStableKey = BuildMapEventStableKey(mapEvent, mapEventLocationLabel);
		TrackNpcActionsFromMapEventSide(mapEvent, mapEvent.AttackerSide, mapEvent.WinningSide == mapEvent.AttackerSide?.MissionSide, isMajor, mapEventLocationLabel, mapEventStableKey, hasBanditSide);
		TrackNpcActionsFromMapEventSide(mapEvent, mapEvent.DefenderSide, mapEvent.WinningSide == mapEvent.DefenderSide?.MissionSide, isMajor, mapEventLocationLabel, mapEventStableKey, hasBanditSide);
	}

internal void TrackNpcActionsFromMapEventSide(MapEvent mapEvent, MapEventSide side, bool won, bool isMajor, string locationLabel, string mapEventStableKey, bool allowNonLordHero = false)
	{
		if (mapEvent == null || side?.Parties == null)
		{
			return;
		}
		foreach (MapEventParty party in side.Parties)
		{
			Hero leaderHero = party?.Party?.LeaderHero;
			if (!CampaignCharacterRecordCaptureAdapter.ShouldTrackNpcActionHero(leaderHero, allowNonLordHero))
			{
				continue;
			}
			if (!won && mapEvent.IsPlayerMapEvent && mapEvent.HasWinner && mapEvent.WinningSide == mapEvent.PlayerSide && HasRecordedPlayerDefeatedHeroBattleFact(mapEvent, side, leaderHero))
			{
				continue;
			}
			string text = BuildMapEventNarrative(mapEvent, side, leaderHero, won, locationLabel);
			string text2 = (string.IsNullOrWhiteSpace(mapEventStableKey) ? BuildMapEventStableKey(mapEvent, locationLabel) : mapEventStableKey) + ":side:" + side.MissionSide + ":hero:" + (leaderHero.StringId ?? "");
			NpcActionFacts npcActionFacts = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("map_event", leaderHero);
			npcActionFacts.LocationText = locationLabel;
			npcActionFacts.Won = won;
			CampaignCharacterRecordCaptureAdapter.ApplySettlementFacts(npcActionFacts, mapEvent.MapEventSettlement, locationText: locationLabel);
			ApplyMapEventOppositeSideFacts(npcActionFacts, side.OtherSide);
			if (isMajor)
			{
				_record().RecordNpcMajorAction(leaderHero, text, text2, npcActionFacts, allowNonLordHero);
			}
			_record().RecordNpcRecentAction(leaderHero, text, text2, facts: npcActionFacts, allowNonLordHero: allowNonLordHero);
			string text3 = BuildMapEventAftermathText(mapEvent, side, won, locationLabel);
			if (!string.IsNullOrWhiteSpace(text3))
			{
				NpcActionFacts npcActionFacts2 = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("map_event_aftermath", leaderHero);
				npcActionFacts2.LocationText = locationLabel;
				npcActionFacts2.Won = won;
				CampaignCharacterRecordCaptureAdapter.ApplySettlementFacts(npcActionFacts2, mapEvent.MapEventSettlement, locationText: locationLabel);
				ApplyMapEventOppositeSideFacts(npcActionFacts2, side.OtherSide);
				_record().RecordNpcRecentAction(leaderHero, text3, "mapevent_aftermath:" + (string.IsNullOrWhiteSpace(mapEventStableKey) ? BuildMapEventStableKey(mapEvent, locationLabel) : mapEventStableKey).Substring("mapevent:".Length) + ":side:" + side.MissionSide + ":hero:" + (leaderHero.StringId ?? ""), facts: npcActionFacts2, allowNonLordHero: allowNonLordHero);
			}
		}
	}

internal bool HasRecordedPlayerDefeatedHeroBattleFact(MapEvent mapEvent, MapEventSide defeatedSide, Hero defeatedHero)
	{
		if (mapEvent == null || defeatedSide == null || defeatedHero == null)
		{
			return false;
		}
		string locationLabel = GetMapEventLocationLabel(mapEvent);
		string stableKey = BuildPlayerDefeatedHeroBattleFactKey(mapEvent, defeatedSide, defeatedHero, locationLabel);
		return !string.IsNullOrWhiteSpace(stableKey) && PlayerDefeatedHeroBattleFactKeys.Contains(stableKey);
	}


internal void OnHeroPrisonerTaken(PartyBase capturer, Hero prisoner)
	{
		try
		{
			Hero hero = capturer?.LeaderHero ?? capturer?.MobileParty?.LeaderHero;
			Settlement settlement = ResolveSettlementForPartyBase(capturer);
			string text = GetLocationLabelForPartyBase(capturer);
			string text2 = BuildPrisonerTakenStableKey(hero, prisoner);
			if (CampaignCharacterRecordCaptureAdapter.ShouldTrackNpcActionHero(hero))
			{
				NpcActionFacts npcActionFacts = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("prisoner_taken_captor", hero);
				npcActionFacts.LocationText = text;
				CampaignCharacterRecordCaptureAdapter.ApplySettlementFacts(npcActionFacts, settlement, locationText: text);
				CampaignCharacterRecordCaptureAdapter.ApplyTargetFacts(npcActionFacts, prisoner);
				if (prisoner?.MapFaction != null)
				{
					CampaignCharacterRecordCaptureAdapter.AddRelatedFactionFacts(npcActionFacts, prisoner.MapFaction);
				}
				string text3 = AdapterRecordTexts.Raw("AF_REC_Battle_0165") + MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(prisoner) + "。";
				_record().RecordNpcMajorAction(hero, text3, text2 + ":captor", npcActionFacts);
				_record().RecordNpcRecentAction(hero, text3, text2 + ":captor", facts: npcActionFacts);
			}
			if (CampaignCharacterRecordCaptureAdapter.ShouldTrackNpcActionHero(prisoner))
			{
				NpcActionFacts npcActionFacts2 = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("prisoner_taken_prisoner", prisoner);
				npcActionFacts2.LocationText = text;
				CampaignCharacterRecordCaptureAdapter.ApplySettlementFacts(npcActionFacts2, settlement, locationText: text);
				if (hero != null)
				{
					CampaignCharacterRecordCaptureAdapter.ApplyTargetFacts(npcActionFacts2, hero);
				}
				else if (capturer?.MobileParty?.MapFaction != null)
				{
					CampaignCharacterRecordCaptureAdapter.AddRelatedFactionFacts(npcActionFacts2, capturer.MobileParty.MapFaction);
				}
				string text4 = ((hero != null) ? (AdapterRecordTexts.Raw("AF_REC_Battle_0166") + MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(hero) + AdapterRecordTexts.Raw("AF_REC_Battle_0167")) : AdapterRecordTexts.Raw("AF_REC_Battle_0168"));
				_record().RecordNpcMajorAction(prisoner, text4, text2 + ":prisoner", npcActionFacts2);
				_record().RecordNpcRecentAction(prisoner, text4, text2 + ":prisoner", facts: npcActionFacts2);
			}
			if (prisoner != null && prisoner != Hero.MainHero && prisoner.IsLord && (capturer?.LeaderHero == Hero.MainHero || capturer?.MobileParty?.ActualClan == Clan.PlayerClan))
			{
				Logger.Log("BattleStatus", $"NPC {prisoner.Name} captured by player");
				_appendHistory(prisoner, null, null, AdapterRecordTexts.Raw("AF_REC_Battle_0169") + Hero.MainHero.Name + AdapterRecordTexts.Raw("AF_REC_Battle_0170"));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("BattleStatus", "[ERROR] OnHeroPrisonerTaken: " + ex.Message);
		}
	}

internal void OnHeroPrisonerReleased(Hero prisoner, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification)
	{
		try
		{
			Hero hero = party?.LeaderHero ?? party?.MobileParty?.LeaderHero;
			Settlement settlement = ResolveSettlementForPartyBase(party);
			string text = GetLocationLabelForPartyBase(party);
			string text2 = BuildPrisonerReleasedStableKey(hero, prisoner, detail);
			string endCaptivityDetailLabel = GetEndCaptivityDetailLabel(detail);
			if (CampaignCharacterRecordCaptureAdapter.ShouldTrackNpcActionHero(prisoner))
			{
				NpcActionFacts npcActionFacts = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("prisoner_released_prisoner", prisoner);
				npcActionFacts.LocationText = text;
				CampaignCharacterRecordCaptureAdapter.ApplySettlementFacts(npcActionFacts, settlement, locationText: text);
				if (hero != null)
				{
					CampaignCharacterRecordCaptureAdapter.ApplyTargetFacts(npcActionFacts, hero);
				}
				else
				{
					CampaignCharacterRecordCaptureAdapter.AddRelatedFactionFacts(npcActionFacts, capturerFaction);
				}
				string text3 = string.IsNullOrWhiteSpace(endCaptivityDetailLabel) ? AdapterRecordTexts.Raw("AF_REC_Battle_0171") : (AdapterRecordTexts.Raw("AF_REC_Battle_0172") + endCaptivityDetailLabel + AdapterRecordTexts.Raw("AF_REC_Battle_0173"));
				_record().RecordNpcMajorAction(prisoner, text3, text2 + ":prisoner", npcActionFacts);
				_record().RecordNpcRecentAction(prisoner, text3, text2 + ":prisoner", facts: npcActionFacts);
			}
			if (CampaignCharacterRecordCaptureAdapter.ShouldTrackNpcActionHero(hero))
			{
				NpcActionFacts npcActionFacts2 = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("prisoner_released_captor", hero);
				npcActionFacts2.LocationText = text;
				CampaignCharacterRecordCaptureAdapter.ApplySettlementFacts(npcActionFacts2, settlement, locationText: text);
				CampaignCharacterRecordCaptureAdapter.ApplyTargetFacts(npcActionFacts2, prisoner);
				string text4 = string.IsNullOrWhiteSpace(endCaptivityDetailLabel) ? (AdapterRecordTexts.Raw("AF_REC_Battle_0174") + MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(prisoner) + AdapterRecordTexts.Raw("AF_REC_Battle_0175")) : (MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(prisoner) + endCaptivityDetailLabel + AdapterRecordTexts.Raw("AF_REC_Battle_0176"));
				_record().RecordNpcMajorAction(hero, text4, text2 + ":captor", npcActionFacts2);
				_record().RecordNpcRecentAction(hero, text4, text2 + ":captor", facts: npcActionFacts2);
			}
			if (prisoner != null && prisoner != Hero.MainHero && prisoner.IsLord && (capturerFaction == Hero.MainHero?.MapFaction || party?.LeaderHero == Hero.MainHero || party?.MobileParty?.ActualClan == Clan.PlayerClan))
			{
				_record().RecentlyReleasedPrisoners.Add(prisoner.StringId);
				string text5 = detail switch
				{
					EndCaptivityDetail.Ransom => AdapterRecordTexts.Raw("AF_REC_Battle_0177"),
					EndCaptivityDetail.ReleasedByChoice => AdapterRecordTexts.Raw("AF_REC_Battle_0178"),
					EndCaptivityDetail.ReleasedAfterPeace => AdapterRecordTexts.Raw("AF_REC_Battle_0179"),
					EndCaptivityDetail.ReleasedAfterEscape => AdapterRecordTexts.Raw("AF_REC_Battle_0180"),
					_ => "",
				};
				Logger.Log("BattleStatus", $"NPC {prisoner.Name} {text5}regained freedom (detail={detail})");
				_appendHistory(prisoner, null, null, AdapterRecordTexts.Raw("AF_REC_Battle_0181") + text5 + AdapterRecordTexts.Raw("AF_REC_Battle_0182") + Hero.MainHero.Name + AdapterRecordTexts.Raw("AF_REC_Battle_0183"));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("BattleStatus", "[ERROR] OnHeroPrisonerReleased: " + ex.Message);
		}
	}

internal static string BuildPrisonerTakenStableKey(Hero capturerHero, Hero prisoner)
	{
		return "prisoner_taken:" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(capturerHero) + ":" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(prisoner);
	}

internal static string BuildPrisonerReleasedStableKey(Hero capturerHero, Hero prisoner, EndCaptivityDetail detail)
	{
		return "prisoner_released:" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(capturerHero) + ":" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(prisoner) + ":" + detail;
	}

internal static string GetEndCaptivityDetailLabel(EndCaptivityDetail detail)
	{
		switch (detail)
		{
		case EndCaptivityDetail.Ransom:
			return AdapterRecordTexts.Raw("AF_REC_Battle_0184");
		case EndCaptivityDetail.ReleasedByChoice:
			return AdapterRecordTexts.Raw("AF_REC_Battle_0185");
		case EndCaptivityDetail.ReleasedAfterPeace:
			return AdapterRecordTexts.Raw("AF_REC_Battle_0186");
		case EndCaptivityDetail.ReleasedAfterEscape:
			return AdapterRecordTexts.Raw("AF_REC_Battle_0187");
		case EndCaptivityDetail.ReleasedAfterBattle:
			return AdapterRecordTexts.Raw("AF_REC_Battle_0188");
		case EndCaptivityDetail.ReleasedByCompensation:
			return AdapterRecordTexts.Raw("AF_REC_Battle_0189");
		case EndCaptivityDetail.Death:
			return AdapterRecordTexts.Raw("AF_REC_Battle_0190");
		default:
			return AdapterRecordTexts.Raw("AF_REC_Battle_0191");
		}
	}

internal static Settlement ResolveSettlementForPartyBase(PartyBase party)
	{
		MobileParty mobileParty = party?.MobileParty;
		if (mobileParty == null)
		{
			return null;
		}
		return mobileParty.BesiegedSettlement ?? ResolveSiegeSettlement(mobileParty.SiegeEvent) ?? mobileParty.TargetSettlement ?? mobileParty.CurrentSettlement ?? mobileParty.LastVisitedSettlement;
	}

internal static string GetLocationLabelForPartyBase(PartyBase party)
	{
		string text = GetNearestSettlementNameForParty(party?.MobileParty);
		return string.IsNullOrWhiteSpace(text) ? "" : text;
	}


internal void OnPartyJoinedArmy(MobileParty party)
	{
		try
		{
			Hero leaderHero = party?.LeaderHero;
			Army army = party?.Army;
			if (!CampaignCharacterRecordCaptureAdapter.ShouldTrackNpcActionHero(leaderHero) || army == null)
			{
				return;
			}
			if (party == army.LeaderParty || leaderHero == army.ArmyOwner || leaderHero == army.LeaderParty?.LeaderHero)
			{
				return;
			}
			string text = MemoryEntityIdentityBannerlordAdapter.GetArmyDisplayName(army);
			NpcActionFacts npcActionFacts = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("army_join", leaderHero);
			npcActionFacts.LocationText = GetNearestSettlementNameForParty(party);
			_record().RecordNpcMajorAction(leaderHero, AdapterRecordTexts.Raw("AF_REC_Battle_0192") + text + "。", "army_join:" + text, npcActionFacts);
			_record().RecordNpcRecentAction(leaderHero, AdapterRecordTexts.Raw("AF_REC_Battle_0193") + text + "。", "army_join:" + text, facts: npcActionFacts);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnPartyJoinedArmy: " + ex.Message);
		}
	}

internal void OnPartyLeftArmy(MobileParty party, Army army)
	{
		try
		{
			Hero leaderHero = party?.LeaderHero;
			if (!CampaignCharacterRecordCaptureAdapter.ShouldTrackNpcActionHero(leaderHero))
			{
				return;
			}
			string text = MemoryEntityIdentityBannerlordAdapter.GetArmyDisplayName(army);
			NpcActionFacts npcActionFacts = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("army_leave", leaderHero);
			npcActionFacts.LocationText = GetNearestSettlementNameForParty(party);
			_record().RecordNpcRecentAction(leaderHero, AdapterRecordTexts.Raw("AF_REC_Battle_0194") + text + "。", "army_leave:" + text, facts: npcActionFacts);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnPartyLeftArmy: " + ex.Message);
		}
	}

internal void OnMobilePartyJoinedSiege(MobileParty party)
	{
		try
		{
			Hero leaderHero = party?.LeaderHero;
			if (!CampaignCharacterRecordCaptureAdapter.ShouldTrackNpcActionHero(leaderHero))
			{
				return;
			}
			Settlement settlement = party.BesiegedSettlement ?? ResolveSiegeSettlement(party.SiegeEvent);
			string text = settlement?.Name?.ToString() ?? AdapterRecordTexts.Raw("AF_REC_Battle_0195");
			NpcActionFacts npcActionFacts = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("siege_join", leaderHero);
			CampaignCharacterRecordCaptureAdapter.ApplySettlementFacts(npcActionFacts, settlement);
			_record().RecordNpcRecentAction(leaderHero, AdapterRecordTexts.Raw("AF_REC_Battle_0196") + text + AdapterRecordTexts.Raw("AF_REC_Battle_0197"), "siege_join:" + text, facts: npcActionFacts);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnMobilePartyJoinedSiege: " + ex.Message);
		}
	}

internal void OnMobilePartyLeftSiege(MobileParty party)
	{
		try
		{
			Hero leaderHero = party?.LeaderHero;
			if (!CampaignCharacterRecordCaptureAdapter.ShouldTrackNpcActionHero(leaderHero))
			{
				return;
			}
			Settlement settlement = party.BesiegedSettlement ?? ResolveSiegeSettlement(party.SiegeEvent);
			string text = settlement?.Name?.ToString() ?? AdapterRecordTexts.Raw("AF_REC_Battle_0195");
			bool flag = settlement != null && party?.MapFaction != null && settlement.MapFaction == party.MapFaction;
			string text2 = (flag ? (text + AdapterRecordTexts.Raw("AF_REC_Battle_0199")) : (text + AdapterRecordTexts.Raw("AF_REC_Battle_0200")));
			NpcActionFacts npcActionFacts = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("siege_leave", leaderHero);
			CampaignCharacterRecordCaptureAdapter.ApplySettlementFacts(npcActionFacts, settlement);
			_record().RecordNpcRecentAction(leaderHero, text2, "siege_leave:" + text + ":" + flag, facts: npcActionFacts);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnMobilePartyLeftSiege: " + ex.Message);
		}
	}

internal void OnDailyTickParty(MobileParty party)
	{
		try
		{
			Hero leaderHero = party?.LeaderHero;
			if (!CampaignCharacterRecordCaptureAdapter.ShouldTrackNpcActionHero(leaderHero))
			{
				return;
			}
			string locationText = GetNearestSettlementNameForParty(party);
			string armyDisplayName = party.Army != null ? MemoryEntityIdentityBannerlordAdapter.GetArmyDisplayName(party.Army) : "";
			string stableKey = BuildRecentPartyBehaviorStableKey(party, locationText, armyDisplayName);
			int currentDay = MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe();
			if (string.IsNullOrWhiteSpace(stableKey) || _record().HasRecentNpcActionStableKeyWithinWindow(leaderHero, stableKey, currentDay))
			{
				return;
			}
			string text = BuildRecentPartyBehaviorText(party, locationText, armyDisplayName);
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			NpcActionFacts npcActionFacts = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("daily_behavior", leaderHero);
			MobileParty mobileParty = party.Army?.LeaderParty ?? party;
			Settlement settlement = mobileParty?.BesiegedSettlement ?? ResolveSiegeSettlement(mobileParty?.SiegeEvent) ?? mobileParty?.TargetSettlement ?? party.TargetSettlement ?? party.CurrentSettlement ?? party.LastVisitedSettlement;
			CampaignCharacterRecordCaptureAdapter.ApplySettlementFacts(npcActionFacts, settlement, locationText: locationText);
			if (mobileParty?.TargetParty?.LeaderHero != null)
			{
				CampaignCharacterRecordCaptureAdapter.ApplyTargetFacts(npcActionFacts, mobileParty.TargetParty.LeaderHero);
			}
			_record().RecordNpcRecentAction(leaderHero, text, stableKey, dedupeAcrossWindow: true, facts: npcActionFacts);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnDailyTickParty: " + ex.Message);
		}
	}

internal static string BuildRecentPartyBehaviorText(MobileParty party, bool forPrompt = false)
	{
		if (party == null)
		{
			return "";
		}
		string locationText = GetNearestSettlementNameForParty(party);
		string armyDisplayName = party.Army != null ? MemoryEntityIdentityBannerlordAdapter.GetArmyDisplayName(party.Army) : "";
		return BuildRecentPartyBehaviorText(party, locationText, armyDisplayName, forPrompt);
	}

internal static string BuildRecentPartyBehaviorText(MobileParty party, string locationText, string armyDisplayName, bool forPrompt = false)
	{
		if (party == null)
		{
			return "";
		}
		string BehaviorText(string key) => forPrompt ? AdapterRecordTexts.Prompt(key) : AdapterRecordTexts.Raw(key);
		string period = forPrompt ? AdapterRecordTexts.Prompt("Period") : "。";
		MobileParty mobileParty = party.Army?.LeaderParty ?? party;
		string text = string.IsNullOrWhiteSpace(locationText) ? BehaviorText("AF_REC_Battle_0201") : locationText.Trim();
		if (party.Army != null)
		{
			string text2 = (party.Army.LeaderParty == party) ? BehaviorText("AF_REC_Battle_0202") : BehaviorText("AF_REC_Battle_0203");
			string text3 = string.IsNullOrWhiteSpace(armyDisplayName) ? MemoryEntityIdentityBannerlordAdapter.GetArmyDisplayName(party.Army) : armyDisplayName;
			switch (mobileParty?.DefaultBehavior)
			{
			case TaleWorlds.CampaignSystem.Party.AiBehavior.BesiegeSettlement:
				return text2 + text3 + BehaviorText("AF_REC_Battle_0204") + text + period;
			case TaleWorlds.CampaignSystem.Party.AiBehavior.AssaultSettlement:
				return text2 + text3 + BehaviorText("AF_REC_Battle_0205") + text + period;
			case TaleWorlds.CampaignSystem.Party.AiBehavior.DefendSettlement:
				return text2 + text3 + BehaviorText("AF_REC_Battle_0206") + text + period;
			case TaleWorlds.CampaignSystem.Party.AiBehavior.GoToSettlement:
				return text2 + text3 + BehaviorText("AF_REC_Battle_0207") + text + period;
			case TaleWorlds.CampaignSystem.Party.AiBehavior.PatrolAroundPoint:
				return text2 + text3 + BehaviorText("AF_REC_Battle_0208") + text + BehaviorText("AF_REC_Battle_0209");
			case TaleWorlds.CampaignSystem.Party.AiBehavior.EngageParty:
				{
					string text4 = mobileParty?.TargetParty?.LeaderHero?.Name?.ToString() ?? mobileParty?.TargetParty?.Name?.ToString();
					return string.IsNullOrWhiteSpace(text4) ? (text2 + text3 + BehaviorText("AF_REC_Battle_0210")) : (text2 + text3 + BehaviorText("AF_REC_Battle_0211") + text4.Trim() + period);
				}
			default:
				return text2 + text3 + BehaviorText("AF_REC_Battle_0212") + text + BehaviorText("AF_REC_Battle_0213");
			}
		}
		switch (party.DefaultBehavior)
		{
		case TaleWorlds.CampaignSystem.Party.AiBehavior.BesiegeSettlement:
			return BehaviorText("AF_REC_Battle_0214") + text + period;
		case TaleWorlds.CampaignSystem.Party.AiBehavior.AssaultSettlement:
			return BehaviorText("AF_REC_Battle_0215") + text + period;
		case TaleWorlds.CampaignSystem.Party.AiBehavior.DefendSettlement:
			return BehaviorText("AF_REC_Battle_0216") + text + period;
		case TaleWorlds.CampaignSystem.Party.AiBehavior.RaidSettlement:
			return BehaviorText("AF_REC_Battle_0217") + text + period;
		case TaleWorlds.CampaignSystem.Party.AiBehavior.PatrolAroundPoint:
			return BehaviorText("AF_REC_Battle_0218") + text + BehaviorText("AF_REC_Battle_0219");
		case TaleWorlds.CampaignSystem.Party.AiBehavior.GoToSettlement:
			return BehaviorText("AF_REC_Battle_0220") + text + period;
		case TaleWorlds.CampaignSystem.Party.AiBehavior.EngageParty:
			{
				string text2 = party.TargetParty?.LeaderHero?.Name?.ToString() ?? party.TargetParty?.Name?.ToString();
				return string.IsNullOrWhiteSpace(text2) ? BehaviorText("AF_REC_Battle_0221") : (BehaviorText("AF_REC_Battle_0222") + text2.Trim() + period);
			}
		case TaleWorlds.CampaignSystem.Party.AiBehavior.EscortParty:
			{
				string text3 = party.TargetParty?.LeaderHero?.Name?.ToString() ?? party.TargetParty?.Name?.ToString();
				return string.IsNullOrWhiteSpace(text3) ? BehaviorText("AF_REC_Battle_0223") : (BehaviorText("AF_REC_Battle_0224") + text3.Trim() + period);
			}
		default:
			return "";
		}
	}

internal static string BuildRecentPartyBehaviorStableKey(MobileParty party)
	{
		if (party == null)
		{
			return "";
		}
		string locationText = GetNearestSettlementNameForParty(party);
		string armyDisplayName = party.Army != null ? MemoryEntityIdentityBannerlordAdapter.GetArmyDisplayName(party.Army) : "";
		return BuildRecentPartyBehaviorStableKey(party, locationText, armyDisplayName);
	}

internal static string BuildRecentPartyBehaviorStableKey(MobileParty party, string locationText, string armyDisplayName)
	{
		if (party == null)
		{
			return "";
		}
		MobileParty mobileParty = party.Army?.LeaderParty ?? party;
		string text = string.IsNullOrWhiteSpace(locationText) ? AdapterRecordTexts.Raw("AF_REC_Battle_0201") : locationText.Trim();
		string text2 = mobileParty?.TargetParty?.StringId ?? mobileParty?.TargetParty?.Name?.ToString() ?? "";
		string text3 = (party.Army != null) ? (string.IsNullOrWhiteSpace(armyDisplayName) ? MemoryEntityIdentityBannerlordAdapter.GetArmyDisplayName(party.Army) : armyDisplayName) : "";
		if (party.Army != null)
		{
			return "daily_behavior:army:" + (mobileParty?.DefaultBehavior).GetValueOrDefault() + ":" + text + ":" + text2 + ":" + text3;
		}
		return "daily_behavior:" + party.DefaultBehavior + ":" + text + ":" + text2 + ":" + text3;
	}

internal const int DestroyedPartyMemoryCleanupDedupLimit=4096;
internal readonly HashSet<PartyBase> DestroyedPartyMemoryCleanupDedup=new HashSet<PartyBase>();
private readonly RemovedPartyMemoryPorts _removed;
internal bool TryReserveDestroyedPartyMemoryCleanup(PartyBase partyBase, MobileParty party)
	{
		try
		{
			PartyBase identity = partyBase ?? party?.Party;
			if (identity == null)
			{
				return true;
			}
			if (DestroyedPartyMemoryCleanupDedup.Count >= DestroyedPartyMemoryCleanupDedupLimit)
			{
				DestroyedPartyMemoryCleanupDedup.Clear();
			}
			return DestroyedPartyMemoryCleanupDedup.Add(identity);
		}
		catch
		{
			return true;
		}
	}

internal static bool IsNonHeroMemoryIdForParty(string memoryId, List<string> partyNeedles)
	{
		string text = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
		if (string.IsNullOrWhiteSpace(text) || !MemoryBusinessStateOwner.IsNonHeroMemoryId(text) || partyNeedles == null || partyNeedles.Count == 0)
		{
			return false;
		}
		return partyNeedles.Any((string needle) => MemoryEntityIdentityBannerlordAdapter.ContainsExactNonHeroPartyNeedle(text, needle));
	}

internal void CleanupNonHeroMemoryForRemovedParty(PartyBase partyBase, MobileParty mobileParty, string reason)
	{
		try
		{
			MobileParty party = mobileParty ?? partyBase?.MobileParty;
			List<string> needles = _removed.Identity().BuildNonHeroPartyMemoryNeedles(party, partyBase);
			if (needles.Count == 0)
			{
				return;
			}
			HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			void AddMatchingKeys(IEnumerable<string> keys)
			{
				if (keys == null)
				{
					return;
				}
				foreach (string key in keys)
				{
					string memoryId = MemoryRecordRules.NormalizeMemoryHeroId(key);
					if (IsNonHeroMemoryIdForParty(memoryId, needles))
					{
						ids.Add(memoryId);
					}
				}
			}
			AddMatchingKeys(_removed.State.History?.Keys);
			AddMatchingKeys(_removed.State.HistoryStorage?.Keys);
			AddMatchingKeys(_removed.State.Drafts?.Keys);
			AddMatchingKeys(_removed.State.DraftStorage?.Keys);
			AddMatchingKeys(_removed.State.Blocks?.Keys);
			AddMatchingKeys(_removed.State.BlockStorage?.Keys);
			AddMatchingKeys(_removed.State.Overviews?.Keys);
			AddMatchingKeys(_removed.State.OverviewStorage?.Keys);
			AddMatchingKeys(_removed.State.MajorSummaries?.Keys);
			AddMatchingKeys(_removed.State.MajorStorage?.Keys);
			AddMatchingKeys(_removed.State.MajorActions?.Keys);
			AddMatchingKeys(_removed.State.MajorActionStorage?.Keys);
			AddMatchingKeys(_removed.State.RecentActions?.Keys);
			AddMatchingKeys(_removed.State.RecentActionStorage?.Keys);
			AddMatchingKeys((_removed.State.DailyQueue ?? new List<MemorySummaryJob>()).Select((MemorySummaryJob x) => x?.HeroId));
			AddMatchingKeys((_removed.State.OverviewQueue ?? new List<MemoryOverviewJob>()).Select((MemoryOverviewJob x) => x?.HeroId));
			AddMatchingKeys((_removed.State.MajorQueue ?? new List<MajorActionSummaryJob>()).Select((MajorActionSummaryJob x) => x?.HeroId));
			AddMatchingKeys((_removed.State.PendingWeeklyTriggers ?? new List<WeeklyMemoryMaterialTrigger>()).Select((WeeklyMemoryMaterialTrigger x) => x?.MemoryId));
			AddMatchingKeys(_removed.Recovery().GetInteractionMemoryRecoveryProjectionSubjects());
			foreach (string id in ids.ToList())
			{
				_removed.IdentityState().RemoveMemoryEntityDataById(id);
			}
			MemoryHistoryCommitBannerlordAdapter.LogNonHeroMemoryTrace(() => "stage=cleanup_removed_party reason=" + (reason ?? "") + " party=" + (party?.StringId ?? party?.Name?.ToString() ?? "") + " partyIndex=" + (partyBase?.Index ?? party?.Party?.Index ?? -1) + " needles=" + string.Join(",", needles) + " removed=" + ids.Count);
			if (ids.Count > 0)
			{
				Logger.Log("DialogueHistory", "cleaned destroyed non-hero party memory count=" + ids.Count + " partyIndex=" + (partyBase?.Index ?? party?.Party?.Index ?? -1) + " party=" + (party?.StringId ?? party?.Name?.ToString() ?? "") + " reason=" + (reason ?? ""));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("DialogueHistory", "[WARN] clean destroyed non-hero party memory failed: " + ex.Message);
		}
	}

internal void OnMobilePartyDestroyedForNonHeroMemoryCleanup(MobileParty mobileParty, PartyBase destroyerParty)
	{
		// 清理只按本存档这一支 MobileParty 的精确 key 执行：StringId 为主，GUID/party_index 仅兼容旧 fallback。
		// 不能按名字/兵种模糊删除，否则同名劫匪、商队会互相清理并重新表现为共享记忆。
		_removed.Identity().RemoveWildernessNonHeroPartyMemory(this, mobileParty, mobileParty?.Party, "mobile_party_destroyed");
	}

internal void OnPartyRemovedForNonHeroMemoryCleanup(PartyBase party)
	{
		// 只删除这一支被移除部队的精确 party 记忆，避免同名队伍共享/误删。
		_removed.Identity().RemoveWildernessNonHeroPartyMemory(this, party?.MobileParty, party, "party_removed");
	}

internal void OnArmyCreated(Army army)
	{
		try
		{
			Hero hero = army?.ArmyOwner ?? army?.LeaderParty?.LeaderHero;
			if (!CampaignCharacterRecordCaptureAdapter.ShouldTrackNpcActionHero(hero))
			{
				return;
			}
			string text = MemoryEntityIdentityBannerlordAdapter.GetArmyDisplayName(army);
			NpcActionFacts npcActionFacts = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("army_create", hero);
			Settlement settlement = ResolveGatheringPointSettlement(army?.LeaderParty, army?.LeaderParty);
			npcActionFacts.LocationText = GetNearestSettlementNameForParty(army?.LeaderParty);
			CampaignCharacterRecordCaptureAdapter.ApplySettlementFacts(npcActionFacts, settlement, locationText: npcActionFacts.LocationText);
			_record().RecordNpcMajorAction(hero, AdapterRecordTexts.Raw("AF_REC_Battle_0226") + text + "。", "army_create:" + text, npcActionFacts);
			_record().RecordNpcRecentAction(hero, AdapterRecordTexts.Raw("AF_REC_Battle_0227") + text + "。", "army_create:" + text, facts: npcActionFacts);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnArmyCreated: " + ex.Message);
		}
	}

internal void OnArmyGathered(Army army, IMapPoint gatheringPoint)
	{
		try
		{
			Hero hero = army?.ArmyOwner ?? army?.LeaderParty?.LeaderHero;
			if (!CampaignCharacterRecordCaptureAdapter.ShouldTrackNpcActionHero(hero))
			{
				return;
			}
			Settlement settlement = ResolveGatheringPointSettlement(gatheringPoint, army?.LeaderParty);
			string text = ResolveGatheringPointLabel(gatheringPoint, army?.LeaderParty);
			NpcActionFacts npcActionFacts = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("army_gather", hero);
			npcActionFacts.LocationText = text;
			CampaignCharacterRecordCaptureAdapter.ApplySettlementFacts(npcActionFacts, settlement, locationText: text);
			_record().RecordNpcRecentAction(hero, AdapterRecordTexts.Raw("AF_REC_Battle_0228") + MemoryEntityIdentityBannerlordAdapter.GetArmyDisplayName(army) + AdapterRecordTexts.Raw("AF_REC_Battle_0229") + text + AdapterRecordTexts.Raw("AF_REC_Battle_0230"), "army_gather:" + MemoryEntityIdentityBannerlordAdapter.GetArmyDisplayName(army) + ":" + text, facts: npcActionFacts);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnArmyGathered: " + ex.Message);
		}
	}

internal void OnArmyDispersed(Army army, Army.ArmyDispersionReason reason, bool isNoNotification)
	{
		try
		{
			Hero hero = army?.ArmyOwner ?? army?.LeaderParty?.LeaderHero;
			if (!CampaignCharacterRecordCaptureAdapter.ShouldTrackNpcActionHero(hero))
			{
				return;
			}
			NpcActionFacts npcActionFacts = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("army_disperse", hero);
			npcActionFacts.LocationText = GetNearestSettlementNameForParty(army?.LeaderParty);
			_record().RecordNpcRecentAction(hero, AdapterRecordTexts.Raw("AF_REC_Battle_0231") + MemoryEntityIdentityBannerlordAdapter.GetArmyDisplayName(army) + AdapterRecordTexts.Raw("AF_REC_Battle_0232"), "army_disperse:" + MemoryEntityIdentityBannerlordAdapter.GetArmyDisplayName(army), facts: npcActionFacts);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnArmyDispersed: " + ex.Message);
		}
	}

internal void OnSiegeEventStarted(SiegeEvent siegeEvent)
	{
		try
		{
			Settlement settlement = ResolveSiegeSettlement(siegeEvent);
			bool isMajorSiege = ShouldRecordMajorBattleAction(GetSiegeEventTroopCount(siegeEvent));
			foreach (Hero item in GetHeroesFromSiegeEventSide(siegeEvent, BattleSideEnum.Attacker))
			{
				string text = AnimusForge.Refactor.Adapters.PersonaIdentityPromptCaptureAdapter.BuildSiegeStartNarrative(settlement, isAttacker: true, siegeEvent);
				string text2 = settlement?.Name?.ToString() ?? AdapterRecordTexts.Raw("AF_REC_Battle_0233");
				NpcActionFacts npcActionFacts = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("siege_start_attack", item);
				CampaignCharacterRecordCaptureAdapter.ApplySettlementFacts(npcActionFacts, settlement);
				npcActionFacts.TargetKingdomId = MemoryEntityIdentityBannerlordAdapter.GetKingdomId(settlement?.MapFaction);
				if (isMajorSiege)
				{
					_record().RecordNpcMajorAction(item, text, "siege_start:" + text2, npcActionFacts);
				}
				_record().RecordNpcRecentAction(item, text, "siege_start:" + text2, facts: npcActionFacts);
			}
			foreach (Hero item2 in GetHeroesFromSiegeEventSide(siegeEvent, BattleSideEnum.Defender))
			{
				string text3 = AnimusForge.Refactor.Adapters.PersonaIdentityPromptCaptureAdapter.BuildSiegeStartNarrative(settlement, isAttacker: false, siegeEvent);
				string text4 = settlement?.Name?.ToString() ?? AdapterRecordTexts.Raw("AF_REC_Battle_0234");
				NpcActionFacts npcActionFacts2 = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("siege_start_defend", item2);
				CampaignCharacterRecordCaptureAdapter.ApplySettlementFacts(npcActionFacts2, settlement);
				npcActionFacts2.TargetKingdomId = MemoryEntityIdentityBannerlordAdapter.GetKingdomId(siegeEvent?.BesiegerCamp?.LeaderParty?.MapFaction);
				if (isMajorSiege)
				{
					_record().RecordNpcMajorAction(item2, text3, "siege_defend:" + text4, npcActionFacts2);
				}
				_record().RecordNpcRecentAction(item2, text3, "siege_defend:" + text4, facts: npcActionFacts2);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnSiegeEventStarted: " + ex.Message);
		}
	}

internal void OnSiegeEventEnded(SiegeEvent siegeEvent)
	{
		try
		{
			Settlement settlement = ResolveSiegeSettlement(siegeEvent);
			string text = settlement?.Name?.ToString() ?? AdapterRecordTexts.Raw("AF_REC_Battle_0195");
			foreach (Hero item in GetHeroesFromSiegeEventSide(siegeEvent, BattleSideEnum.Attacker))
			{
				NpcActionFacts npcActionFacts = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("siege_end_attack", item);
				CampaignCharacterRecordCaptureAdapter.ApplySettlementFacts(npcActionFacts, settlement);
				_record().RecordNpcRecentAction(item, AdapterRecordTexts.Raw("AF_REC_Battle_0236") + text + AdapterRecordTexts.Raw("AF_REC_Battle_0237"), "siege_end:" + text, facts: npcActionFacts);
			}
			foreach (Hero item2 in GetHeroesFromSiegeEventSide(siegeEvent, BattleSideEnum.Defender))
			{
				NpcActionFacts npcActionFacts2 = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("siege_end_defend", item2);
				CampaignCharacterRecordCaptureAdapter.ApplySettlementFacts(npcActionFacts2, settlement);
				_record().RecordNpcRecentAction(item2, text + AdapterRecordTexts.Raw("AF_REC_Battle_0238"), "siege_end_defend:" + text, facts: npcActionFacts2);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnSiegeEventEnded: " + ex.Message);
		}
	}

internal void OnSiegeCompleted(Settlement settlement, MobileParty party, bool siegeSuccess, MapEvent.BattleTypes battleType)
	{
		try
		{
			Hero leaderHero = party?.LeaderHero;
			if (!CampaignCharacterRecordCaptureAdapter.ShouldTrackNpcActionHero(leaderHero))
			{
				return;
			}
			string text = settlement?.Name?.ToString() ?? AdapterRecordTexts.Raw("AF_REC_Battle_0195");
			string text2 = (siegeSuccess ? (AdapterRecordTexts.Raw("AF_REC_Battle_0240") + text + AdapterRecordTexts.Raw("AF_REC_Battle_0241")) : (AdapterRecordTexts.Raw("AF_REC_Battle_0242") + text + AdapterRecordTexts.Raw("AF_REC_Battle_0243")));
			NpcActionFacts npcActionFacts = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("siege_complete", leaderHero);
			CampaignCharacterRecordCaptureAdapter.ApplySettlementFacts(npcActionFacts, settlement);
			npcActionFacts.Won = siegeSuccess;
			if (ShouldRecordMajorBattleAction(GetSiegeEventTroopCount(party?.SiegeEvent ?? settlement?.SiegeEvent ?? party?.Army?.LeaderParty?.SiegeEvent)))
			{
				_record().RecordNpcMajorAction(leaderHero, text2, "siege_complete:" + text + ":" + siegeSuccess, npcActionFacts);
			}
			_record().RecordNpcRecentAction(leaderHero, text2, "siege_complete:" + text + ":" + siegeSuccess, facts: npcActionFacts);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnSiegeCompleted: " + ex.Message);
		}
	}

internal static Settlement ResolveGatheringPointSettlement(IMapPoint gatheringPoint, MobileParty fallbackParty = null)
	{
		if (gatheringPoint is Settlement settlement)
		{
			return settlement;
		}
		try
		{
			if (gatheringPoint != null)
			{
				Settlement settlement2 = Helpers.SettlementHelper.FindNearestSettlementToPoint(gatheringPoint.Position, (Settlement x) => x != null && !x.IsHideout);
				if (settlement2 != null)
				{
					return settlement2;
				}
			}
		}
		catch
		{
		}
		try
		{
			if (fallbackParty != null)
			{
				return Helpers.SettlementHelper.FindNearestSettlementToMobileParty(fallbackParty, MobileParty.NavigationType.All, (Settlement x) => x != null && !x.IsHideout);
			}
		}
		catch
		{
		}
		return null;
	}

internal static string ResolveGatheringPointLabel(IMapPoint gatheringPoint, MobileParty fallbackParty = null)
	{
		Settlement settlement = ResolveGatheringPointSettlement(gatheringPoint, fallbackParty);
		string text = (settlement?.Name?.ToString() ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		text = (gatheringPoint?.Name?.ToString() ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, AdapterRecordTexts.Raw("AF_REC_Battle_0244"), StringComparison.OrdinalIgnoreCase) && !string.Equals(text, "Gathering Point", StringComparison.OrdinalIgnoreCase))
		{
			return text;
		}
		text = GetNearestSettlementNameForParty(fallbackParty);
		return string.IsNullOrWhiteSpace(text) ? AdapterRecordTexts.Raw("AF_REC_Battle_0245") : text;
	}

internal static int GetSiegeEventTroopCount(SiegeEvent siegeEvent)
	{
		if (siegeEvent == null)
		{
			return 0;
		}
		int num = 0;
		HashSet<PartyBase> hashSet = new HashSet<PartyBase>();
		try
		{
			foreach (PartyBase involvedPartiesForEventType in siegeEvent.GetInvolvedPartiesForEventType(MapEvent.BattleTypes.Siege))
			{
				if (involvedPartiesForEventType != null && hashSet.Add(involvedPartiesForEventType))
				{
					num += GetPartyBaseTroopCount(involvedPartiesForEventType);
				}
			}
		}
		catch
		{
		}
		return Math.Max(0, num);
	}

internal void OnVillageBeingRaided(Village village)
	{
		try
		{
			Settlement settlement = village?.Settlement;
			string settlementDisplayName = MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName(settlement);
			string text = settlementDisplayName + AdapterRecordTexts.Raw("AF_REC_Battle_0246");
			string clanDisplayName = MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName(settlement?.OwnerClan);
			string kingdomDisplayName = MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(settlement?.MapFaction as Kingdom, AdapterRecordTexts.Raw("AF_REC_Battle_0247"));
			if (!string.IsNullOrWhiteSpace(clanDisplayName) && !string.IsNullOrWhiteSpace(kingdomDisplayName))
			{
				text += AdapterRecordTexts.Raw("AF_REC_Battle_0248") + clanDisplayName + AdapterRecordTexts.Raw("AF_REC_Battle_0249") + kingdomDisplayName + "。";
			}
			_record().RecordEventSourceMaterial("village_raid_started", AdapterRecordTexts.Raw("AF_REC_Battle_0250") + settlementDisplayName + AdapterRecordTexts.Raw("AF_REC_Battle_0251"), text, "village_raid_started:" + MemoryEntityIdentityBannerlordAdapter.GetSettlementId(settlement), MemoryEntityIdentityBannerlordAdapter.GetKingdomId(settlement?.MapFaction), MemoryEntityIdentityBannerlordAdapter.GetSettlementId(settlement), includeInWorld: false, includeInKingdom: true);
		}
		catch (Exception ex)
		{
			Logger.Log("EventMaterial", "[ERROR] OnVillageBeingRaided: " + ex.Message);
		}
	}

internal void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent)
	{
		try
		{
			Settlement settlement = raidEvent?.MapEventSettlement;
			string settlementDisplayName = MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName(settlement);
			string raidOutcomeLabel = GetRaidOutcomeLabel(winnerSide);
			string text = settlementDisplayName + AdapterRecordTexts.Raw("AF_REC_Battle_0252") + raidOutcomeLabel + "。";
			if (winnerSide == BattleSideEnum.Attacker)
			{
				text += AdapterRecordTexts.Raw("AF_REC_Battle_0253");
			}
			else
			{
				text += AdapterRecordTexts.Raw("AF_REC_Battle_0254");
			}
			Hero hero = raidEvent?.AttackerSide?.LeaderParty?.LeaderHero;
			if (hero != null)
			{
				text = text + AdapterRecordTexts.Raw("AF_REC_Battle_0255") + MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(hero) + "。";
			}
			_record().RecordEventSourceMaterial("raid_completed", AdapterRecordTexts.Raw("AF_REC_Battle_0256") + settlementDisplayName + AdapterRecordTexts.Raw("AF_REC_Battle_0257"), text, "raid_completed:" + MemoryEntityIdentityBannerlordAdapter.GetSettlementId(settlement) + ":" + winnerSide, MemoryEntityIdentityBannerlordAdapter.GetKingdomId(settlement?.MapFaction), MemoryEntityIdentityBannerlordAdapter.GetSettlementId(settlement), includeInWorld: false, includeInKingdom: true, MemoryEntityIdentityBannerlordAdapter.GetHeroId(hero), MemoryEntityIdentityBannerlordAdapter.GetKingdomId(hero?.MapFaction));
		}
		catch (Exception ex)
		{
			Logger.Log("EventMaterial", "[ERROR] OnRaidCompleted: " + ex.Message);
		}
	}

internal static string GetRaidOutcomeLabel(BattleSideEnum side)
	{
		return side switch
		{
			BattleSideEnum.Attacker => AdapterRecordTexts.Raw("AF_REC_Battle_0258"),
			BattleSideEnum.Defender => AdapterRecordTexts.Raw("AF_REC_Battle_0259"),
			_ => AdapterRecordTexts.Raw("AF_REC_Battle_0260")
		};
	}

internal void OnSiegeAftermathApplied(MobileParty attackerParty, Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermathType, Clan previousSettlementOwner, Dictionary<MobileParty, float> partyContributions)
	{
		try
		{
			string settlementDisplayName = MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName(settlement);
			string siegeAftermathLabel = GetSiegeAftermathLabel(aftermathType);
			string kingdomId = MemoryEntityIdentityBannerlordAdapter.GetKingdomId(attackerParty?.MapFaction);
			string text = MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(attackerParty?.LeaderHero) + AdapterRecordTexts.Raw("AF_REC_Battle_0261") + settlementDisplayName + AdapterRecordTexts.Raw("AF_REC_Battle_0262") + siegeAftermathLabel + "。";
			string text2 = MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName(previousSettlementOwner);
			string kingdomDisplayName = MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(previousSettlementOwner?.Kingdom, AdapterRecordTexts.Raw("AF_REC_Battle_0263"));
			if (!string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(kingdomDisplayName))
			{
				text += AdapterRecordTexts.Raw("AF_REC_Battle_0264") + text2 + AdapterRecordTexts.Raw("AF_REC_Battle_0265") + kingdomDisplayName + "。";
			}
			_record().RecordEventSourceMaterial("siege_aftermath", AdapterRecordTexts.Raw("AF_REC_Battle_0266") + settlementDisplayName, text, "siege_aftermath:" + MemoryEntityIdentityBannerlordAdapter.GetSettlementId(settlement) + ":" + aftermathType + ":" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(attackerParty?.LeaderHero), kingdomId, MemoryEntityIdentityBannerlordAdapter.GetSettlementId(settlement), includeInWorld: settlement?.IsTown == true && aftermathType != SiegeAftermathAction.SiegeAftermath.ShowMercy, includeInKingdom: true);
		}
		catch (Exception ex)
		{
			Logger.Log("EventMaterial", "[ERROR] OnSiegeAftermathApplied: " + ex.Message);
		}
	}

internal static string GetSiegeAftermathLabel(SiegeAftermathAction.SiegeAftermath aftermathType)
	{
		switch (aftermathType)
		{
		case SiegeAftermathAction.SiegeAftermath.Devastate:
			return AdapterRecordTexts.Raw("AF_REC_Battle_0267");
		case SiegeAftermathAction.SiegeAftermath.Pillage:
			return AdapterRecordTexts.Raw("AF_REC_Battle_0268");
		case SiegeAftermathAction.SiegeAftermath.ShowMercy:
			return AdapterRecordTexts.Raw("AF_REC_Battle_0269");
		default:
			return aftermathType.ToString();
		}
	}


internal void OnBattleStartedForEncounterDiag(PartyBase attackerParty, PartyBase defenderParty, object subject, bool showNotification)
	{
		if (!ShouldWriteBattleStartEncounterDiagnostic(attackerParty, defenderParty))
		{
			return;
		}
		try
		{
			Logger.LogImmediate("Logic", "[EncounterDiag] stage=MyBehavior.OnBattleStarted | subject=" + MemoryBusinessStateOwner.EncounterDiagEscape(subject?.GetType().FullName ?? "null")
				+ " | showNotification=" + showNotification
				+ " | attacker=" + DescribeBattleStartPartyForEncounterDiag(attackerParty)
				+ " | defender=" + DescribeBattleStartPartyForEncounterDiag(defenderParty));
			LordEncounterBehavior.LogEncounterDiagnostic("MyBehavior.OnBattleStarted", "battle_started_subject_" + (subject?.GetType().Name ?? "null"), null, null, defenderParty);
		}
		catch
		{
		}
	}

internal static bool ShouldWriteBattleStartEncounterDiagnostic(PartyBase attackerParty, PartyBase defenderParty)
	{
		try
		{
			if (!Logger.IsModLogicEnabled)
			{
				return false;
			}
			if (Logger.IsVerboseModLogicEnabled)
			{
				return true;
			}
			PartyBase mainParty = PartyBase.MainParty;
			return mainParty != null && (ReferenceEquals(attackerParty, mainParty) || ReferenceEquals(defenderParty, mainParty));
		}
		catch
		{
			return false;
		}
	}

internal static string DescribeBattleStartPartyForEncounterDiag(PartyBase party)
	{
		if (party == null)
		{
			return "null";
		}
		List<string> parts = new List<string>();
		try
		{
			parts.Add("index=" + party.Index);
		}
		catch
		{
		}
		try
		{
			parts.Add("name=" + MemoryBusinessStateOwner.EncounterDiagEscape(party.Name?.ToString()));
		}
		catch
		{
		}
		try
		{
			parts.Add("leader=" + MemoryBusinessStateOwner.EncounterDiagEscape(party.LeaderHero?.StringId ?? party.LeaderHero?.Name?.ToString()));
		}
		catch
		{
		}
		try
		{
			parts.Add("mobile=" + (party.IsMobile ? "1" : "0"));
			parts.Add("settlement=" + (party.IsSettlement ? "1" : "0"));
		}
		catch
		{
		}
		try
		{
			parts.Add("healthy=" + party.NumberOfHealthyMembers);
			parts.Add("all=" + party.NumberOfAllMembers);
		}
		catch
		{
		}
		try
		{
			MobileParty mobileParty = party.MobileParty;
			if (mobileParty != null)
			{
				parts.Add("mobileId=" + MemoryBusinessStateOwner.EncounterDiagEscape(mobileParty.StringId));
				parts.Add("default=" + mobileParty.DefaultBehavior);
				parts.Add("short=" + mobileParty.ShortTermBehavior);
				parts.Add("targetSettlement=" + MemoryBusinessStateOwner.EncounterDiagEscape(mobileParty.TargetSettlement?.StringId ?? mobileParty.TargetSettlement?.Name?.ToString()));
				parts.Add("currentSettlement=" + MemoryBusinessStateOwner.EncounterDiagEscape(mobileParty.CurrentSettlement?.StringId ?? mobileParty.CurrentSettlement?.Name?.ToString()));
			}
		}
		catch
		{
		}
		try
		{
			Settlement settlement = party.Settlement;
			if (settlement != null)
			{
				parts.Add("settlementId=" + MemoryBusinessStateOwner.EncounterDiagEscape(settlement.StringId));
				parts.Add("isVillage=" + (settlement.IsVillage ? "1" : "0"));
				parts.Add("isUnderRaid=" + (settlement.IsUnderRaid ? "1" : "0"));
				parts.Add("lastAttacker=" + MemoryBusinessStateOwner.EncounterDiagEscape(settlement.LastAttackerParty?.StringId ?? settlement.LastAttackerParty?.Name?.ToString()));
			}
		}
		catch
		{
		}
		try
		{
			MapEvent mapEvent = party.MapEvent;
			if (mapEvent != null)
			{
				parts.Add("mapType=" + mapEvent.EventType);
				parts.Add("mapRaid=" + (mapEvent.IsRaid ? "1" : "0"));
				parts.Add("mapForceSupplies=" + (mapEvent.IsForcingSupplies ? "1" : "0"));
				parts.Add("mapForceVolunteers=" + (mapEvent.IsForcingVolunteers ? "1" : "0"));
				parts.Add("mapSettlement=" + MemoryBusinessStateOwner.EncounterDiagEscape(mapEvent.MapEventSettlement?.StringId ?? mapEvent.MapEventSettlement?.Name?.ToString()));
				parts.Add("attTroops=" + (mapEvent.AttackerSide?.TroopCount.ToString() ?? "null"));
				parts.Add("defTroops=" + (mapEvent.DefenderSide?.TroopCount.ToString() ?? "null"));
			}
		}
		catch
		{
		}
		return string.Join(",", parts);
	}
}

internal sealed class RemovedPartyMemoryPorts { internal MemoryBusinessStateOwner State; internal Func<MemoryBusinessStateOwner> IdentityState; internal Func<MemoryRecoveryStateOwner> Recovery; internal Func<MemoryEntityIdentityBannerlordAdapter> Identity; }
