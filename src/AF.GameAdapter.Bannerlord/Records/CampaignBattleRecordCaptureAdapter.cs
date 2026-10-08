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
		return value.IndexOf("强盗", StringComparison.OrdinalIgnoreCase) >= 0
			|| value.IndexOf("劫匪", StringComparison.OrdinalIgnoreCase) >= 0
			|| value.IndexOf("土匪", StringComparison.OrdinalIgnoreCase) >= 0
			|| value.IndexOf("山贼", StringComparison.OrdinalIgnoreCase) >= 0
			|| value.IndexOf("海寇", StringComparison.OrdinalIgnoreCase) >= 0
			|| value.IndexOf("响马", StringComparison.OrdinalIgnoreCase) >= 0
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
			text = (hero == Hero.MainHero) ? "玩家" : "";
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
		return string.Join("、", list.Take(maxCount)) + "等" + list.Count + "人";
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
		return BuildTrackedHeroListText(side.Parties.Select((MapEventParty x) => x?.Party?.LeaderHero), side.LeaderParty?.LeaderHero, "统帅", maxCount);
	}

internal static string BuildTrackedHeroListText(SiegeEvent siegeEvent, BattleSideEnum side, int maxCount = 5)
	{
		Hero highlightedHero = ((side == BattleSideEnum.Attacker) ? (siegeEvent?.BesiegerCamp?.LeaderParty?.LeaderHero) : (siegeEvent?.BesiegedSettlement?.OwnerClan?.Leader));
		return BuildTrackedHeroListText(GetHeroesFromSiegeEventSide(siegeEvent, side), highlightedHero, "统帅", maxCount);
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
		return string.IsNullOrWhiteSpace(text) ? "附近一带" : text.Trim();
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
		return string.IsNullOrWhiteSpace(text) ? "野外" : text.Trim();
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
		return string.IsNullOrWhiteSpace(text) ? "敌军" : text.Trim();
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
		return "阵亡" + num + "、负伤" + num2;
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
		return string.Join("、", list.Take(count)) + "等" + list.Count + "支";
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
			text = string.IsNullOrWhiteSpace(text2) ? "非领主部队" : text2;
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
				list.Add("约" + mapEventPartyCommittedTroopCount + "人");
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
				return "村民队";
			}
			if (mobileParty.IsCaravan || mobileParty.PartyComponent is CaravanPartyComponent)
			{
				return "商队";
			}
			if (mobileParty.IsPatrolParty || mobileParty.PartyComponent is PatrolPartyComponent)
			{
				return "巡逻队";
			}
			if (mobileParty.IsMilitia || mobileParty.PartyComponent is MilitiaPartyComponent)
			{
				return "民兵队";
			}
			if (mobileParty.IsGarrison || mobileParty.PartyComponent is GarrisonPartyComponent)
			{
				return "驻军";
			}
			if (IsBanditOrOutlawPartyBase(partyBase) || mobileParty.PartyComponent is BanditPartyComponent)
			{
				return "匪帮";
			}
			if (mobileParty.PartyComponent is CustomPartyComponent)
			{
				return "自定义部队";
			}
		}
		catch
		{
		}
		return "非领主部队";
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
		return (mapEventSideCommittedTroopCount > 0) ? (mapEventSideCommittedTroopCount + "人") : "";
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
		return text + "统帅的军团";
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
			playerName = "玩家";
		}
		string place = string.IsNullOrWhiteSpace(locationLabel) ? "野外" : locationLabel.Trim();
		string battleKind = "战斗";
		if (mapEvent?.IsSiegeAssault == true || mapEvent?.IsSiegeOutside == true || mapEvent?.IsSallyOut == true)
		{
			battleKind = "攻守战";
		}
		else if (mapEvent?.IsRaid == true)
		{
			battleKind = "袭掠战";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("[AFEF玩家行为补充] 你刚在").Append(place).Append("的").Append(battleKind).Append("中被").Append(playerName).Append("一方击败；这场战败已经发生。");
		string winningLords = BuildTrackedHeroListText(defeatedSide?.OtherSide, 4);
		if (!string.IsNullOrWhiteSpace(winningLords))
		{
			stringBuilder.Append(" 胜方主要领主：").Append(winningLords).Append('。');
		}
		return stringBuilder.ToString();
	}

internal static string BuildNpcDefeatedByPlayerRecentActionText(MapEvent mapEvent, MapEventSide defeatedSide, string locationLabel)
	{
		string playerName = Hero.MainHero?.Name?.ToString()?.Trim();
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = "玩家";
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (mapEvent?.IsSiegeAssault == true || mapEvent?.IsSiegeOutside == true || mapEvent?.IsSallyOut == true)
		{
			stringBuilder.Append("你在").Append(string.IsNullOrWhiteSpace(locationLabel) ? "某处要塞" : locationLabel.Trim()).Append("的攻守战中被").Append(playerName).Append("一方击败。");
		}
		else if (mapEvent?.IsRaid == true)
		{
			stringBuilder.Append("你在").Append(string.IsNullOrWhiteSpace(locationLabel) ? "某处村庄" : locationLabel.Trim()).Append("的袭掠战中被").Append(playerName).Append("一方击败。");
		}
		else
		{
			stringBuilder.Append("你在").Append(string.IsNullOrWhiteSpace(locationLabel) ? "野外" : locationLabel.Trim()).Append("的战斗中被").Append(playerName).Append("一方击败。");
		}
		AppendBattleOpponentDetails(stringBuilder, defeatedSide?.OtherSide, "胜方");
		AppendBattleCasualtyDetails(stringBuilder, defeatedSide, defeatedSide?.OtherSide);
		return stringBuilder.ToString();
	}

internal static string BuildPlayerBattleDefeatRecentActionText(MapEvent mapEvent, MapEventSide playerSide, string locationLabel)
	{
		string opponentLabel = BuildMapEventSideNarrativeLabel(playerSide?.OtherSide);
		if (string.IsNullOrWhiteSpace(opponentLabel))
		{
			opponentLabel = "敌军";
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (mapEvent?.IsSiegeAssault == true || mapEvent?.IsSiegeOutside == true || mapEvent?.IsSallyOut == true)
		{
			stringBuilder.Append("玩家在").Append(string.IsNullOrWhiteSpace(locationLabel) ? "某处要塞" : locationLabel.Trim()).Append("的攻守战中败给了").Append(opponentLabel).Append("。");
		}
		else if (mapEvent?.IsRaid == true)
		{
			stringBuilder.Append("玩家在").Append(string.IsNullOrWhiteSpace(locationLabel) ? "某处村庄" : locationLabel.Trim()).Append("的袭掠战中败给了").Append(opponentLabel).Append("。");
		}
		else
		{
			stringBuilder.Append("玩家在").Append(string.IsNullOrWhiteSpace(locationLabel) ? "野外" : locationLabel.Trim()).Append("的战斗中败给了").Append(opponentLabel).Append("。");
		}
		AppendBattleOpponentDetails(stringBuilder, playerSide?.OtherSide, "胜方");
		AppendBattleCasualtyDetails(stringBuilder, playerSide, playerSide?.OtherSide);
		return stringBuilder.ToString();
	}

internal static void AppendBattleOpponentDetails(StringBuilder stringBuilder, MapEventSide opponentSide, string label)
	{
		if (stringBuilder == null || opponentSide == null)
		{
			return;
		}
		string sideLabel = string.IsNullOrWhiteSpace(label) ? "对方" : label.Trim();
		string lords = BuildTrackedHeroListText(opponentSide, 6);
		if (!string.IsNullOrWhiteSpace(lords))
		{
			stringBuilder.Append(' ').Append(sideLabel).Append("领主：").Append(lords).Append('。');
		}
		string nonLordParties = BuildMapEventNonLordPartyListText(opponentSide, 5);
		if (!string.IsNullOrWhiteSpace(nonLordParties))
		{
			stringBuilder.Append(' ').Append(sideLabel).Append("非领主部队：").Append(nonLordParties).Append('。');
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
			stringBuilder.Append(" 败方死伤：").Append(string.IsNullOrWhiteSpace(defeatedCasualties) ? "不详" : defeatedCasualties);
			stringBuilder.Append("；胜方死伤：").Append(string.IsNullOrWhiteSpace(winningCasualties) ? "不详" : winningCasualties).Append('。');
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
			text4 = ((side.MissionSide == BattleSideEnum.Attacker) ? (won ? (text + "，在" + (string.IsNullOrWhiteSpace(text3) ? "对" : (text3 + "对")) + text6 + "的领土" + text5.Trim() + "的围城战中获胜。") : (text + "，在" + (string.IsNullOrWhiteSpace(text3) ? "对" : (text3 + "对")) + text6 + "的领土" + text5.Trim() + "的围攻中失利。")) : (won ? (text + "，在" + (string.IsNullOrWhiteSpace(text3) ? "" : (text3 + "参与的")) + text5.Trim() + "保卫战中击退了" + text6 + "。") : (text + "，在" + (string.IsNullOrWhiteSpace(text3) ? "" : (text3 + "参与的")) + text5.Trim() + "保卫战中败给了" + text6 + "。")));
		}
		else if (mapEvent.IsRaid)
		{
			string raidLocation = string.IsNullOrWhiteSpace(locationLabel) ? "某处村庄" : locationLabel.Trim();
			string raidCommandPrefix = string.IsNullOrWhiteSpace(text3) ? "在" : ("随" + text3 + "在");
			if (side.MissionSide == BattleSideEnum.Attacker)
			{
				text4 = won ? (text + "，" + raidCommandPrefix + raidLocation + "对" + text2 + "发动的袭掠中得手。") : (text + "，" + raidCommandPrefix + raidLocation + "对" + text2 + "发动的袭掠中失利。");
			}
			else if (side.MissionSide == BattleSideEnum.Defender)
			{
				text4 = won ? (text + "，" + raidCommandPrefix + raidLocation + "保卫村庄，击退了" + text2 + "的袭掠。") : (text + "，" + raidCommandPrefix + raidLocation + "保卫村庄时失利，未能阻止" + text2 + "的袭掠。");
			}
			else
			{
				text4 = won ? (text + "，" + raidCommandPrefix + raidLocation + "的村庄袭掠冲突中获胜。") : (text + "，" + raidCommandPrefix + raidLocation + "的村庄袭掠冲突中失利。");
			}
		}
		else
		{
			text4 = (won ? (text + "，在" + (string.IsNullOrWhiteSpace(text3) ? "" : (text3 + "于")) + locationLabel + "击败了" + text2 + "。") : (text + "，在" + (string.IsNullOrWhiteSpace(text3) ? "" : (text3 + "于")) + locationLabel + "败给了" + text2 + "。"));
		}
		StringBuilder stringBuilder = new StringBuilder(text4);
		string text7 = BuildTrackedHeroListText(side, 5);
		string text8 = BuildTrackedHeroListText(side.OtherSide, 5);
		string text9 = BuildMapEventNonLordPartyListText(side, 5);
		string text10 = BuildMapEventNonLordPartyListText(side.OtherSide, 5);
		if (!string.IsNullOrWhiteSpace(text7))
		{
			stringBuilder.Append(" 我方领主：").Append(text7).Append('。');
		}
		if (!string.IsNullOrWhiteSpace(text8))
		{
			stringBuilder.Append(" 敌方领主：").Append(text8).Append('。');
		}
		if (!string.IsNullOrWhiteSpace(text9))
		{
			stringBuilder.Append(" 我方非领主部队：").Append(text9).Append('。');
		}
		if (!string.IsNullOrWhiteSpace(text10))
		{
			stringBuilder.Append(" 敌方非领主部队：").Append(text10).Append('。');
		}
		string text11 = BuildMapEventCommittedTroopText(side);
		string text12 = BuildMapEventCommittedTroopText(side.OtherSide);
		if (!string.IsNullOrWhiteSpace(text11) || !string.IsNullOrWhiteSpace(text12))
		{
			stringBuilder.Append(" 战前投入兵力：我方").Append(string.IsNullOrWhiteSpace(text11) ? "不详" : text11);
			stringBuilder.Append("；敌方").Append(string.IsNullOrWhiteSpace(text12) ? "不详" : text12).Append('。');
		}
		string text13 = BuildMapEventStandoutText(side, side.OtherSide);
		string text14 = BuildMapEventCasualtyText(side);
		string text15 = BuildMapEventCasualtyText(side.OtherSide);
		stringBuilder.Append(" 我方死伤：").Append(text14);
		stringBuilder.Append("；敌方死伤：").Append(text15).Append('。');
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
		string text = BuildBattleHeroDisplayName(leaderHero, side.LeaderParty?.LeaderHero == leaderHero, "统帅");
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		if (num <= 0)
		{
			return text + "战场贡献最突出。";
		}
		int num2 = (int)Math.Round((double)(100 * mapEventParty.ContributionToBattle) / (double)num);
		if (num2 < 30)
		{
			return "";
		}
		string text2 = MemoryEntityIdentityBannerlordAdapter.GetFactionDisplayName(leaderHero?.MapFaction ?? side.MapFaction, "某势力");
		if (num2 > 90)
		{
			return "万军辟易！这根本不是一场战争，而是" + text2 + "的" + text + "与他部队的专属杀戮秀！敌人在他面前如同草芥一般不堪一击！";
		}
		if (num2 > 75)
		{
			return text2 + "的" + text + "几乎包揽了绝大多数的战果！他的大军如同利刃般撕裂敌阵，所向披靡！";
		}
		if (num2 > 50)
		{
			return text2 + "的" + text + "与他的部队在战场上战无不胜！他是" + text2 + "的英雄！";
		}
		return text2 + "的" + text + "与他的部队是我方的中流砥柱！他拥有最高的贡献！";
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
			string raidLocation = string.IsNullOrWhiteSpace(locationLabel) ? "某处村庄" : locationLabel.Trim();
			if (side.MissionSide == BattleSideEnum.Attacker)
			{
				return won ? (raidLocation + "的袭掠已经结束，你正在清点缴获并整顿部队。") : (raidLocation + "的袭掠已经结束，你的袭掠未能得手，正在收拢部队并处理残局。");
			}
			if (side.MissionSide == BattleSideEnum.Defender)
			{
				return won ? (raidLocation + "的袭掠已经结束，你协助守军击退了袭掠者，正在整顿部队。") : (raidLocation + "的袭掠已经结束，袭掠者已经得手；你正在收拢部队并处理残局。");
			}
			return won ? (raidLocation + "的村庄袭掠冲突已经结束，你正在整顿部队。") : (raidLocation + "的村庄袭掠冲突已经结束，你正在收拢部队并处理残局。");
		}
		return won ? ("这场发生在" + locationLabel + "的战斗已经结束，你正在整顿部队并清点战果。") : ("这场发生在" + locationLabel + "的战斗已经结束，你正在收拢残部并处理败战残局。");
	}

internal static string BuildMapEventActorRoleClause(Hero actorHero, MapEventSide side)
	{
		string text = MemoryEntityIdentityBannerlordAdapter.GetHeroFactionDisplayName(actorHero, side?.MapFaction);
		if (actorHero?.IsLord == true)
		{
			return "你作为" + text + "的领主";
		}
		if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, "某势力", StringComparison.OrdinalIgnoreCase))
		{
			return "你作为" + text + "的一员";
		}
		return "你作为参战者";
	}

internal static string BuildMapEventSideNarrativeLabel(MapEventSide side)
	{
		if (side == null)
		{
			return "敌军";
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
		Logger.Log("BattleStatus", "玩家击败领主事实写入 reason=" + (reason ?? "") + " hero=" + defeatedHero.StringId + " name=" + (defeatedHero.Name?.ToString() ?? ""));
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
		if (string.IsNullOrWhiteSpace(enemyLabel) || string.Equals(enemyLabel, "敌军", StringComparison.OrdinalIgnoreCase))
		{
			enemyLabel = "一支野外匪帮";
		}
		int enemyCount = GetMapEventSideCommittedTroopCount(defeatedSide);
		StringBuilder stringBuilder = new StringBuilder();
		if (string.Equals(locationLabel, "野外", StringComparison.Ordinal))
		{
			stringBuilder.Append("你在野外击败了").Append(enemyLabel).Append("，取得了一场小规模遭遇战胜利。");
		}
		else
		{
			stringBuilder.Append("你在").Append(locationLabel).Append("附近击败了").Append(enemyLabel).Append("，取得了一场小规模遭遇战胜利。");
		}
		if (enemyCount > 0)
		{
			stringBuilder.Append(" 敌方投入约").Append(enemyCount).Append("人。");
		}
		if (GetMapEventSideCasualtyCount(defeatedSide) > 0)
		{
			stringBuilder.Append(" 敌方").Append(BuildMapEventCasualtyText(defeatedSide)).Append("。");
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
			settlementName = "一处藏身处";
		}
		string text = "你清剿了" + settlementName + "，击败了盘踞其中的匪帮。";
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
				string text3 = "你俘虏了" + MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(prisoner) + "。";
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
				string text4 = ((hero != null) ? ("你被" + MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(hero) + "俘虏了。") : "你被敌方俘虏了。");
				_record().RecordNpcMajorAction(prisoner, text4, text2 + ":prisoner", npcActionFacts2);
				_record().RecordNpcRecentAction(prisoner, text4, text2 + ":prisoner", facts: npcActionFacts2);
			}
			if (prisoner != null && prisoner != Hero.MainHero && prisoner.IsLord && (capturer?.LeaderHero == Hero.MainHero || capturer?.MobileParty?.ActualClan == Clan.PlayerClan))
			{
				Logger.Log("BattleStatus", $"NPC {prisoner.Name} 被玩家俘虏");
				_appendHistory(prisoner, null, null, $"你被 {Hero.MainHero.Name} 俘虏了，你现在是他的囚犯。");
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
				string text3 = string.IsNullOrWhiteSpace(endCaptivityDetailLabel) ? "你结束了囚禁状态。" : ("你" + endCaptivityDetailLabel + "并恢复了自由。");
				_record().RecordNpcMajorAction(prisoner, text3, text2 + ":prisoner", npcActionFacts);
				_record().RecordNpcRecentAction(prisoner, text3, text2 + ":prisoner", facts: npcActionFacts);
			}
			if (CampaignCharacterRecordCaptureAdapter.ShouldTrackNpcActionHero(hero))
			{
				NpcActionFacts npcActionFacts2 = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("prisoner_released_captor", hero);
				npcActionFacts2.LocationText = text;
				CampaignCharacterRecordCaptureAdapter.ApplySettlementFacts(npcActionFacts2, settlement, locationText: text);
				CampaignCharacterRecordCaptureAdapter.ApplyTargetFacts(npcActionFacts2, prisoner);
				string text4 = string.IsNullOrWhiteSpace(endCaptivityDetailLabel) ? ("你失去了对" + MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(prisoner) + "的控制。") : (MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(prisoner) + endCaptivityDetailLabel + "，不再是你的囚犯。");
				_record().RecordNpcMajorAction(hero, text4, text2 + ":captor", npcActionFacts2);
				_record().RecordNpcRecentAction(hero, text4, text2 + ":captor", facts: npcActionFacts2);
			}
			if (prisoner != null && prisoner != Hero.MainHero && prisoner.IsLord && (capturerFaction == Hero.MainHero?.MapFaction || party?.LeaderHero == Hero.MainHero || party?.MobileParty?.ActualClan == Clan.PlayerClan))
			{
				_record().RecentlyReleasedPrisoners.Add(prisoner.StringId);
				string text5 = detail switch
				{
					EndCaptivityDetail.Ransom => "通过支付赎金",
					EndCaptivityDetail.ReleasedByChoice => "被主动释放",
					EndCaptivityDetail.ReleasedAfterPeace => "因和平协议",
					EndCaptivityDetail.ReleasedAfterEscape => "成功逃脱",
					_ => "",
				};
				Logger.Log("BattleStatus", $"NPC {prisoner.Name} {text5}获得自由 (detail={detail})");
				_appendHistory(prisoner, null, null, $"你{text5}从 {Hero.MainHero.Name} 的囚禁中获得了自由。");
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
			return "通过赎金获释";
		case EndCaptivityDetail.ReleasedByChoice:
			return "被主动释放";
		case EndCaptivityDetail.ReleasedAfterPeace:
			return "因议和获释";
		case EndCaptivityDetail.ReleasedAfterEscape:
			return "成功逃脱";
		case EndCaptivityDetail.ReleasedAfterBattle:
			return "在战后获释";
		case EndCaptivityDetail.ReleasedByCompensation:
			return "在补偿后获释";
		case EndCaptivityDetail.Death:
			return "在囚禁中死亡";
		default:
			return "脱离囚禁";
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
			_record().RecordNpcMajorAction(leaderHero, "你加入了" + text + "。", "army_join:" + text, npcActionFacts);
			_record().RecordNpcRecentAction(leaderHero, "你加入了" + text + "。", "army_join:" + text, facts: npcActionFacts);
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
			_record().RecordNpcRecentAction(leaderHero, "你离开了" + text + "。", "army_leave:" + text, facts: npcActionFacts);
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
			string text = settlement?.Name?.ToString() ?? "某处要塞";
			NpcActionFacts npcActionFacts = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("siege_join", leaderHero);
			CampaignCharacterRecordCaptureAdapter.ApplySettlementFacts(npcActionFacts, settlement);
			_record().RecordNpcRecentAction(leaderHero, "你加入了对" + text + "的围城。", "siege_join:" + text, facts: npcActionFacts);
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
			string text = settlement?.Name?.ToString() ?? "某处要塞";
			bool flag = settlement != null && party?.MapFaction != null && settlement.MapFaction == party.MapFaction;
			string text2 = (flag ? (text + "结清了战利品和战俘") : (text + "处理完了围城中产生的战利品以及战俘。"));
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

internal static string BuildRecentPartyBehaviorText(MobileParty party)
	{
		if (party == null)
		{
			return "";
		}
		string locationText = GetNearestSettlementNameForParty(party);
		string armyDisplayName = party.Army != null ? MemoryEntityIdentityBannerlordAdapter.GetArmyDisplayName(party.Army) : "";
		return BuildRecentPartyBehaviorText(party, locationText, armyDisplayName);
	}

internal static string BuildRecentPartyBehaviorText(MobileParty party, string locationText, string armyDisplayName)
	{
		if (party == null)
		{
			return "";
		}
		MobileParty mobileParty = party.Army?.LeaderParty ?? party;
		string text = string.IsNullOrWhiteSpace(locationText) ? "附近一带" : locationText.Trim();
		if (party.Army != null)
		{
			string text2 = (party.Army.LeaderParty == party) ? "你最近正率领" : "你最近正随";
			string text3 = string.IsNullOrWhiteSpace(armyDisplayName) ? MemoryEntityIdentityBannerlordAdapter.GetArmyDisplayName(party.Army) : armyDisplayName;
			switch (mobileParty?.DefaultBehavior)
			{
			case TaleWorlds.CampaignSystem.Party.AiBehavior.BesiegeSettlement:
				return text2 + text3 + "围攻" + text + "。";
			case TaleWorlds.CampaignSystem.Party.AiBehavior.AssaultSettlement:
				return text2 + text3 + "强攻" + text + "。";
			case TaleWorlds.CampaignSystem.Party.AiBehavior.DefendSettlement:
				return text2 + text3 + "守备" + text + "。";
			case TaleWorlds.CampaignSystem.Party.AiBehavior.GoToSettlement:
				return text2 + text3 + "前往" + text + "。";
			case TaleWorlds.CampaignSystem.Party.AiBehavior.PatrolAroundPoint:
				return text2 + text3 + "在" + text + "附近巡逻。";
			case TaleWorlds.CampaignSystem.Party.AiBehavior.EngageParty:
				{
					string text4 = mobileParty?.TargetParty?.LeaderHero?.Name?.ToString() ?? mobileParty?.TargetParty?.Name?.ToString();
					return string.IsNullOrWhiteSpace(text4) ? (text2 + text3 + "追击一支部队。") : (text2 + text3 + "追击" + text4.Trim() + "。");
				}
			default:
				return text2 + text3 + "在" + text + "一带行动。";
			}
		}
		switch (party.DefaultBehavior)
		{
		case TaleWorlds.CampaignSystem.Party.AiBehavior.BesiegeSettlement:
			return "你最近在围攻" + text + "。";
		case TaleWorlds.CampaignSystem.Party.AiBehavior.AssaultSettlement:
			return "你最近在强攻" + text + "。";
		case TaleWorlds.CampaignSystem.Party.AiBehavior.DefendSettlement:
			return "你最近在守备" + text + "。";
		case TaleWorlds.CampaignSystem.Party.AiBehavior.RaidSettlement:
			return "你最近在袭扰" + text + "。";
		case TaleWorlds.CampaignSystem.Party.AiBehavior.PatrolAroundPoint:
			return "你最近在" + text + "附近巡逻。";
		case TaleWorlds.CampaignSystem.Party.AiBehavior.GoToSettlement:
			return "你最近正前往" + text + "。";
		case TaleWorlds.CampaignSystem.Party.AiBehavior.EngageParty:
			{
				string text2 = party.TargetParty?.LeaderHero?.Name?.ToString() ?? party.TargetParty?.Name?.ToString();
				return string.IsNullOrWhiteSpace(text2) ? "你最近在追击一支部队。" : ("你最近在追击" + text2.Trim() + "。");
			}
		case TaleWorlds.CampaignSystem.Party.AiBehavior.EscortParty:
			{
				string text3 = party.TargetParty?.LeaderHero?.Name?.ToString() ?? party.TargetParty?.Name?.ToString();
				return string.IsNullOrWhiteSpace(text3) ? "你最近在护送一支部队。" : ("你最近在护送" + text3.Trim() + "。");
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
		string text = string.IsNullOrWhiteSpace(locationText) ? "附近一带" : locationText.Trim();
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
			_record().RecordNpcMajorAction(hero, "你组建并统领了" + text + "。", "army_create:" + text, npcActionFacts);
			_record().RecordNpcRecentAction(hero, "你组建并统领了" + text + "。", "army_create:" + text, facts: npcActionFacts);
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
			_record().RecordNpcRecentAction(hero, "你率领" + MemoryEntityIdentityBannerlordAdapter.GetArmyDisplayName(army) + "在" + text + "集结。", "army_gather:" + MemoryEntityIdentityBannerlordAdapter.GetArmyDisplayName(army) + ":" + text, facts: npcActionFacts);
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
			_record().RecordNpcRecentAction(hero, "你统领的" + MemoryEntityIdentityBannerlordAdapter.GetArmyDisplayName(army) + "已解散。", "army_disperse:" + MemoryEntityIdentityBannerlordAdapter.GetArmyDisplayName(army), facts: npcActionFacts);
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
				string text2 = settlement?.Name?.ToString() ?? "某处要塞";
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
				string text4 = settlement?.Name?.ToString() ?? "某处要塞";
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
			string text = settlement?.Name?.ToString() ?? "某处要塞";
			foreach (Hero item in GetHeroesFromSiegeEventSide(siegeEvent, BattleSideEnum.Attacker))
			{
				NpcActionFacts npcActionFacts = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("siege_end_attack", item);
				CampaignCharacterRecordCaptureAdapter.ApplySettlementFacts(npcActionFacts, settlement);
				_record().RecordNpcRecentAction(item, "你结束了对" + text + "的围城。", "siege_end:" + text, facts: npcActionFacts);
			}
			foreach (Hero item2 in GetHeroesFromSiegeEventSide(siegeEvent, BattleSideEnum.Defender))
			{
				NpcActionFacts npcActionFacts2 = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("siege_end_defend", item2);
				CampaignCharacterRecordCaptureAdapter.ApplySettlementFacts(npcActionFacts2, settlement);
				_record().RecordNpcRecentAction(item2, text + "的守城战已经结束。", "siege_end_defend:" + text, facts: npcActionFacts2);
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
			string text = settlement?.Name?.ToString() ?? "某处要塞";
			string text2 = (siegeSuccess ? ("你在" + text + "的围城中获胜。") : ("你在" + text + "的围城中失利。"));
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
		if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, "集结地", StringComparison.OrdinalIgnoreCase) && !string.Equals(text, "Gathering Point", StringComparison.OrdinalIgnoreCase))
		{
			return text;
		}
		text = GetNearestSettlementNameForParty(fallbackParty);
		return string.IsNullOrWhiteSpace(text) ? "集结地" : text;
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
			string text = settlementDisplayName + "村庄正在遭到掠夺，结果尚未确定，不能视为掠夺成功。";
			string clanDisplayName = MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName(settlement?.OwnerClan);
			string kingdomDisplayName = MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(settlement?.MapFaction as Kingdom, "所属王国");
			if (!string.IsNullOrWhiteSpace(clanDisplayName) && !string.IsNullOrWhiteSpace(kingdomDisplayName))
			{
				text += " 该地由" + clanDisplayName + "掌控，隶属于" + kingdomDisplayName + "。";
			}
			_record().RecordEventSourceMaterial("village_raid_started", "掠夺开始 - " + settlementDisplayName + "村庄", text, "village_raid_started:" + MemoryEntityIdentityBannerlordAdapter.GetSettlementId(settlement), MemoryEntityIdentityBannerlordAdapter.GetKingdomId(settlement?.MapFaction), MemoryEntityIdentityBannerlordAdapter.GetSettlementId(settlement), includeInWorld: false, includeInKingdom: true);
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
			string text = settlementDisplayName + "村庄的掠夺结果是：" + raidOutcomeLabel + "。";
			if (winnerSide == BattleSideEnum.Attacker)
			{
				text += " 该村庄确已被成功掠夺。";
			}
			else
			{
				text += " 入侵者未能成功掠夺该村庄。";
			}
			Hero hero = raidEvent?.AttackerSide?.LeaderParty?.LeaderHero;
			if (hero != null)
			{
				text = text + " 发起掠夺的一方领袖是" + MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(hero) + "。";
			}
			_record().RecordEventSourceMaterial("raid_completed", "掠夺结果 - " + settlementDisplayName + "村庄", text, "raid_completed:" + MemoryEntityIdentityBannerlordAdapter.GetSettlementId(settlement) + ":" + winnerSide, MemoryEntityIdentityBannerlordAdapter.GetKingdomId(settlement?.MapFaction), MemoryEntityIdentityBannerlordAdapter.GetSettlementId(settlement), includeInWorld: false, includeInKingdom: true, MemoryEntityIdentityBannerlordAdapter.GetHeroId(hero), MemoryEntityIdentityBannerlordAdapter.GetKingdomId(hero?.MapFaction));
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
			BattleSideEnum.Attacker => "掠夺成功",
			BattleSideEnum.Defender => "掠夺被击退",
			_ => "掠夺中止"
		};
	}

internal void OnSiegeAftermathApplied(MobileParty attackerParty, Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermathType, Clan previousSettlementOwner, Dictionary<MobileParty, float> partyContributions)
	{
		try
		{
			string settlementDisplayName = MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName(settlement);
			string siegeAftermathLabel = GetSiegeAftermathLabel(aftermathType);
			string kingdomId = MemoryEntityIdentityBannerlordAdapter.GetKingdomId(attackerParty?.MapFaction);
			string text = MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(attackerParty?.LeaderHero) + "在攻取" + settlementDisplayName + "后选择了" + siegeAftermathLabel + "。";
			string text2 = MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName(previousSettlementOwner);
			string kingdomDisplayName = MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(previousSettlementOwner?.Kingdom, "原所属王国");
			if (!string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(kingdomDisplayName))
			{
				text += " 该地此前由" + text2 + "掌控，隶属于" + kingdomDisplayName + "。";
			}
			_record().RecordEventSourceMaterial("siege_aftermath", "围城后处理 - " + settlementDisplayName, text, "siege_aftermath:" + MemoryEntityIdentityBannerlordAdapter.GetSettlementId(settlement) + ":" + aftermathType + ":" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(attackerParty?.LeaderHero), kingdomId, MemoryEntityIdentityBannerlordAdapter.GetSettlementId(settlement), includeInWorld: settlement?.IsTown == true && aftermathType != SiegeAftermathAction.SiegeAftermath.ShowMercy, includeInKingdom: true);
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
			return "毁灭";
		case SiegeAftermathAction.SiegeAftermath.Pillage:
			return "劫掠";
		case SiegeAftermathAction.SiegeAftermath.ShowMercy:
			return "宽恕";
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
