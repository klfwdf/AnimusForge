using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
namespace AnimusForge.Refactor.Adapters;
internal static class SceneLocationPromptCaptureAdapter
{
internal static string GetSceneLocationDisplayName(Location location)
	{
		try
		{
			string text = (location?.Name?.ToString() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}
		catch
		{
		}
		string text2 = (location?.StringId ?? "").Trim();
		return string.IsNullOrWhiteSpace(text2) ? "当前位置" : text2;
	}
	internal static string ResolveCurrentMemorySceneLabel()
	{
		try
		{
			string text = (ShoutUtils.GetCurrentSceneDescription() ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (!UncompressedMemoryMessageAssemblyOwner.IsUnknownMemorySceneLabel(text))
			{
				return text;
			}
		}
		catch
		{
		}
		try
		{
			string text2 = (Settlement.CurrentSettlement?.Name?.ToString() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				return text2;
			}
		}
		catch
		{
		}
		try
		{
			string text3 = (MobileParty.MainParty?.CurrentSettlement?.Name?.ToString() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text3))
			{
				return text3;
			}
		}
		catch
		{
		}
		try
		{
			string text4 = BuildCurrentWildernessMemorySceneLabel();
			if (!string.IsNullOrWhiteSpace(text4))
			{
				return text4;
			}
		}
		catch
		{
		}
		return "大地图或未知场景";
	}

	internal static string BuildCurrentWildernessMemorySceneLabel()
	{
		try
		{
			MobileParty party = MobileParty.MainParty;
			if (party == null)
			{
				return "";
			}
			Settlement settlement = Helpers.SettlementHelper.FindNearestSettlementToMobileParty(party, MobileParty.NavigationType.All, (Settlement x) => x != null && !x.IsHideout);
			string settlementName = (settlement?.Name?.ToString() ?? "").Trim();
			string terrainLabel = "";
			if (MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(party))
			{
				terrainLabel = "海上";
			}
			else
			{
				terrainLabel = MapSeaContextGuard.BuildMobilePartyLandTerrainPromptLabel(party);
				if (string.IsNullOrWhiteSpace(terrainLabel))
				{
					terrainLabel = "野外";
				}
			}
			if (string.IsNullOrWhiteSpace(settlementName))
			{
				return terrainLabel;
			}
			return settlementName + "附近的" + terrainLabel;
		}
		catch
		{
			return "";
		}
	}

internal static string ConvertCountToChineseForPrompt(int count)
	{
		return count switch
		{
			2 => "两个",
			3 => "三个",
			4 => "四个",
			5 => "五个",
			6 => "六个",
			7 => "七个",
			8 => "八个",
			9 => "九个",
			10 => "十个",
			_ => count.ToString() + "个",
		};
	}
internal static string NormalizeFactionRelationLabelForPrompt(IFaction faction, IFaction referenceFaction)
	{
		try
		{
			if (faction == null || referenceFaction == null)
			{
				return "中立";
			}
			string factionId = (faction.StringId ?? "").Trim();
			string referenceId = (referenceFaction.StringId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(factionId) && string.Equals(factionId, referenceId, StringComparison.OrdinalIgnoreCase))
			{
				return "友方";
			}
			if (ReferenceEquals(faction, referenceFaction))
			{
				return "友方";
			}
			if (referenceFaction.IsAtWarWith(faction) || faction.IsAtWarWith(referenceFaction))
			{
				return "敌对";
			}
			return "中立";
		}
		catch
		{
			return "中立";
		}
	}
internal static string ConvertNpcSideRelationLabelForPrompt(string relation)
	{
		switch ((relation ?? "").Trim())
		{
		case "敌对":
			return "敌人";
		case "友方":
			return "友方";
		default:
			return "中立";
		}
	}
internal static void ParseCurrentScenePlaceAndSpotForPrompt(out string placeName, out string spotName)
	{
		placeName = "";
		spotName = "";
		try
		{
			string sceneDescription = (ShoutUtils.GetCurrentSceneDescription() ?? "").Replace("\r", "").Trim();
			if (string.IsNullOrWhiteSpace(sceneDescription))
			{
				return;
			}
			if (IsUnknownSceneDescriptionForPrompt(sceneDescription))
			{
				return;
			}
			int extraIndex = sceneDescription.IndexOf('|');
			if (extraIndex >= 0)
			{
				sceneDescription = sceneDescription.Substring(0, extraIndex).Trim();
			}
			if (IsUnknownSceneDescriptionForPrompt(sceneDescription))
			{
				return;
			}
			if (sceneDescription.StartsWith("你正位于", StringComparison.Ordinal))
			{
				string body = sceneDescription.Substring("你正位于".Length).Trim();
				body = body.TrimEnd('。', '.', ' ');
				const string seaSuffix = "附近的海上";
				if (body.EndsWith(seaSuffix, StringComparison.Ordinal))
				{
					placeName = body.Substring(0, body.Length - seaSuffix.Length).Trim();
					spotName = "海上";
					return;
				}
				if (string.Equals(body, "海上", StringComparison.Ordinal))
				{
					spotName = "海上";
					return;
				}
			}
			if (sceneDescription.StartsWith("位于 ", StringComparison.Ordinal))
			{
				string body = sceneDescription.Substring("位于 ".Length).Trim();
				int splitIndex = body.LastIndexOf(" 的 ", StringComparison.Ordinal);
				if (splitIndex > 0)
				{
					placeName = body.Substring(0, splitIndex).Trim();
					spotName = body.Substring(splitIndex + " 的 ".Length).Trim();
					return;
				}
			}
			if (sceneDescription.StartsWith("靠近 ", StringComparison.Ordinal))
			{
				string body = sceneDescription.Substring("靠近 ".Length).Trim();
				int splitIndex = body.LastIndexOf(" 的 ", StringComparison.Ordinal);
				if (splitIndex > 0)
				{
					placeName = body.Substring(0, splitIndex).Trim();
					spotName = body.Substring(splitIndex + " 的 ".Length).Trim();
					return;
				}
			}
			spotName = sceneDescription;
		}
		catch
		{
			placeName = "";
			spotName = "";
		}
	}
internal static bool IsUnknownSceneDescriptionForPrompt(string sceneDescription)
	{
		string text = (sceneDescription ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return true;
		}
		return string.Equals(text, "未知场景", StringComparison.Ordinal)
			|| string.Equals(text, "大地图或未知场景", StringComparison.Ordinal)
			|| string.Equals(text, "某个地方", StringComparison.Ordinal);
	}
internal static string NormalizeSettlementNameForPromptLookup(string name)
	{
		string text = (name ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		int num = text.IndexOf('（');
		if (num >= 0)
		{
			text = text.Substring(0, num).Trim();
		}
		int num2 = text.IndexOf('(');
		if (num2 >= 0)
		{
			text = text.Substring(0, num2).Trim();
		}
		return text;
	}
internal static Settlement FindNearestSettlementForPrompt(MobileParty referenceParty = null)
	{
		return MapSeaContextGuard.FindNearestSettlementForPrompt(referenceParty ?? MobileParty.MainParty);
	}
internal static MobileParty ResolveMapLocationReferencePartyForPrompt(Hero perspectiveHero)
	{
		try
		{
			MobileParty party = perspectiveHero?.PartyBelongedTo;
			if (party != null && party.IsActive && party.CurrentSettlement == null && party.Position.IsValid())
			{
				return party;
			}
		}
		catch
		{
		}
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			if (mainParty != null && mainParty.CurrentSettlement == null && mainParty.Position.IsValid())
			{
				return mainParty;
			}
		}
		catch
		{
		}
		return null;
	}
internal static MobileParty ResolveSeaLocationReferencePartyForPrompt(Hero perspectiveHero)
	{
		try
		{
			MobileParty party = perspectiveHero?.PartyBelongedTo;
			if (MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(party))
			{
				return party;
			}
		}
		catch
		{
		}
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			if (MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(mainParty))
			{
				return mainParty;
			}
		}
		catch
		{
		}
		return null;
	}
internal static Settlement ResolveSceneSettlementForPrompt(string placeName, Hero perspectiveHero = null)
	{
		Settlement settlement = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
		if (settlement != null)
		{
			return settlement;
		}
		string text = NormalizeSettlementNameForPromptLookup(placeName);
		if (!string.IsNullOrWhiteSpace(text))
		{
			try
			{
				settlement = Settlement.All?.FirstOrDefault((Settlement x) => x != null && string.Equals(NormalizeSettlementNameForPromptLookup(x.Name?.ToString()), text, StringComparison.OrdinalIgnoreCase));
			}
			catch
			{
				settlement = null;
			}
		}
		return settlement ?? FindNearestSettlementForPrompt(ResolveMapLocationReferencePartyForPrompt(perspectiveHero));
	}
internal static string BuildSceneLocationAndSettlementLineForPrompt(Hero perspectiveHero)
	{
		try
		{
			ParseCurrentScenePlaceAndSpotForPrompt(out var placeName, out var spotName);
			bool parsedPlaceName = !string.IsNullOrWhiteSpace(placeName);
			MobileParty mapReferenceParty = ResolveMapLocationReferencePartyForPrompt(perspectiveHero);
			bool seaContext = IsSeaSpotForPrompt(spotName) || ResolveSeaLocationReferencePartyForPrompt(perspectiveHero) != null;
			bool wildernessContext = !seaContext && (IsWildernessSpotForPrompt(spotName) || (!parsedPlaceName && !IsCurrentSettlementContextForPrompt()));
			if (seaContext)
			{
				spotName = "海上";
			}
			else
			{
				string terrainSpotName = MapSeaContextGuard.BuildMobilePartyLandTerrainPromptLabel(mapReferenceParty);
				if (!string.IsNullOrWhiteSpace(terrainSpotName) && (wildernessContext || IsWildernessSpotForPrompt(spotName)))
				{
					spotName = terrainSpotName;
					wildernessContext = true;
				}
			}
			Settlement settlement = ResolveSceneSettlementForPrompt(placeName, perspectiveHero);
			if (string.IsNullOrWhiteSpace(placeName))
			{
				placeName = (settlement?.Name?.ToString() ?? "").Trim();
			}
			if (string.IsNullOrWhiteSpace(placeName))
			{
				placeName = "当前区域";
			}
			if (string.IsNullOrWhiteSpace(spotName))
			{
				spotName = wildernessContext ? "野外" : "某处";
			}
			if (wildernessContext && settlement == null && string.Equals(placeName, "当前区域", StringComparison.Ordinal))
			{
				return "你现在身处" + spotName + "。";
			}
			string cultureName = (settlement?.Culture?.Name?.ToString() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(cultureName))
			{
				cultureName = "未知";
			}
			string rulerName = (settlement?.OwnerClan?.Leader?.Name?.ToString() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(rulerName))
			{
				rulerName = "未知人物";
			}
			string clanName = (settlement?.OwnerClan?.Name?.ToString() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(clanName))
			{
				clanName = "未知";
			}
			if (!clanName.EndsWith("家族", StringComparison.Ordinal))
			{
				clanName += "家族";
			}
			string factionName = (settlement?.MapFaction?.Name?.ToString() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(factionName))
			{
				factionName = "未知势力";
			}
			bool isRuledByPerspectiveHero = perspectiveHero != null && settlement?.OwnerClan?.Leader == perspectiveHero;
			IFaction settlementFaction = settlement?.MapFaction;
			IFaction npcReferenceFaction = perspectiveHero?.Clan?.Kingdom ?? perspectiveHero?.MapFaction ?? settlementFaction;
			IFaction playerReferenceFaction = Hero.MainHero?.Clan?.Kingdom ?? Hero.MainHero?.MapFaction ?? Clan.PlayerClan?.Kingdom ?? Clan.PlayerClan?.MapFaction;
			string npcRelation = NormalizeFactionRelationLabelForPrompt(settlementFaction, npcReferenceFaction);
			string playerRelation = NormalizeFactionRelationLabelForPrompt(settlementFaction, playerReferenceFaction);
			string playerName = SceneTradeBannerlordAdapter.GetPlayerDisplayNameForShout();
			if (string.IsNullOrWhiteSpace(playerName))
			{
				playerName = "玩家";
			}
			if (seaContext)
			{
				if (settlement == null)
				{
					if (string.Equals(placeName, "当前区域", StringComparison.Ordinal))
					{
						return "你正位于海上。";
					}
					return "你正位于" + placeName + "附近的海上。";
				}
				if (isRuledByPerspectiveHero)
				{
					return "你正位于" + placeName + "附近的海上；最近定居点是" + placeName + "，该定居点属" + cultureName + "文化，由你统治，隶属于" + factionName + "，与" + playerName + "保持" + playerRelation + "。";
				}
				return "你正位于" + placeName + "附近的海上；最近定居点是" + placeName + "，该定居点属" + cultureName + "文化，由" + clanName + "的" + rulerName + "统治，隶属于" + factionName + "，是你的" + ConvertNpcSideRelationLabelForPrompt(npcRelation) + "，但与" + playerName + "保持" + playerRelation + "。";
			}
			if (isRuledByPerspectiveHero)
			{
				if (wildernessContext)
				{
					return "你现在位于" + placeName + "附近的" + spotName + "；最近定居点是" + placeName + "，该定居点属" + cultureName + "文化，由你统治，隶属于" + factionName + "，与" + playerName + "保持" + playerRelation + "。";
				}
				return "你现在位于" + placeName + "的" + spotName + "；该定居点属" + cultureName + "文化，由你统治，隶属于" + factionName + "，与" + playerName + "保持" + playerRelation + "。";
			}
			if (wildernessContext)
			{
				return "你现在位于" + placeName + "附近的" + spotName + "；最近定居点是" + placeName + "，该定居点属" + cultureName + "文化，由" + clanName + "的" + rulerName + "统治，隶属于" + factionName + "，是你的" + ConvertNpcSideRelationLabelForPrompt(npcRelation) + "，但与" + playerName + "保持" + playerRelation + "。";
			}
			return "你现在位于" + placeName + "的" + spotName + "；该定居点属" + cultureName + "文化，由" + clanName + "的" + rulerName + "统治，隶属于" + factionName + "，是你的" + ConvertNpcSideRelationLabelForPrompt(npcRelation) + "，但与" + playerName + "保持" + playerRelation + "。";
		}
		catch
		{
			return "";
		}
	}
internal static bool IsWildernessSpotForPrompt(string spotName)
	{
		string text = (spotName ?? "").Trim();
		return string.Equals(text, "野外", StringComparison.Ordinal)
			|| string.Equals(text, "平原", StringComparison.Ordinal)
			|| string.Equals(text, "森林", StringComparison.Ordinal)
			|| string.Equals(text, "丘陵山地", StringComparison.Ordinal)
			|| string.Equals(text, "雪原", StringComparison.Ordinal)
			|| string.Equals(text, "沙漠", StringComparison.Ordinal)
			|| string.Equals(text, "草原", StringComparison.Ordinal)
			|| string.Equals(text, "沼泽", StringComparison.Ordinal)
			|| string.Equals(text, "峡谷", StringComparison.Ordinal)
			|| string.Equals(text, "沙丘", StringComparison.Ordinal)
			|| string.Equals(text, "乡野", StringComparison.Ordinal)
			|| string.Equals(text, "海滩", StringComparison.Ordinal)
			|| string.Equals(text, "峭壁", StringComparison.Ordinal)
			|| string.Equals(text, "浅滩", StringComparison.Ordinal)
			|| string.Equals(text, "桥梁", StringComparison.Ordinal)
			|| text.IndexOf("野外", StringComparison.Ordinal) >= 0;
	}
internal static bool IsSeaSpotForPrompt(string spotName)
	{
		string text = (spotName ?? "").Trim();
		return string.Equals(text, "海上", StringComparison.Ordinal)
			|| text.IndexOf("海上", StringComparison.Ordinal) >= 0;
	}
internal static bool IsCurrentSettlementContextForPrompt()
	{
		try
		{
			if (Settlement.CurrentSettlement != null)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			return MobileParty.MainParty?.CurrentSettlement != null;
		}
		catch
		{
			return false;
		}
	}
internal static string BuildSettlementFlavorLineForPrompt(Hero perspectiveHero)
	{
		try
		{
			Settlement settlement = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
			if (settlement == null)
			{
				return "";
			}
			string nativeInfo = (ShoutUtils.GetNativeSettlementInfoForPrompt(settlement) ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (string.IsNullOrWhiteSpace(nativeInfo))
			{
				return "";
			}
			string settlementName = (settlement.Name?.ToString() ?? "").Trim();
			string rulerName = (settlement.OwnerClan?.Leader?.Name?.ToString() ?? "").Trim();
			string clanName = (settlement.OwnerClan?.Name?.ToString() ?? "").Trim();
			string factionName = (settlement.MapFaction?.Name?.ToString() ?? "").Trim();
			string officialTitle = "";
			try
			{
				IFaction mapFaction = settlement.MapFaction;
				string cultureId = ((mapFaction?.Culture)?.StringId ?? "").Trim();
				if (settlement.OwnerClan?.Leader?.IsFemale ?? false)
				{
					cultureId += "_f";
				}
				officialTitle = ((mapFaction == null || !mapFaction.IsKingdomFaction || settlement.OwnerClan?.Leader == null || mapFaction.Leader != settlement.OwnerClan.Leader) ? GameTexts.FindText("str_faction_official", cultureId)?.ToString() : GameTexts.FindText("str_faction_ruler", cultureId)?.ToString());
				officialTitle = (officialTitle ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			}
			catch
			{
				officialTitle = "";
			}
			List<string> removablePrefixes = new List<string>();
			if (!string.IsNullOrWhiteSpace(settlementName) && !string.IsNullOrWhiteSpace(factionName) && !string.IsNullOrWhiteSpace(officialTitle) && !string.IsNullOrWhiteSpace(rulerName))
			{
				removablePrefixes.Add(settlementName + "被" + factionName + "的" + officialTitle + "，" + rulerName + "统治着。");
			}
			if (!string.IsNullOrWhiteSpace(settlementName) && !string.IsNullOrWhiteSpace(rulerName))
			{
				removablePrefixes.Add(settlementName + "由" + rulerName + "统治。");
				removablePrefixes.Add(settlementName + "由" + rulerName + "控制。");
			}
			if (!string.IsNullOrWhiteSpace(settlementName) && settlement.OwnerClan == Clan.PlayerClan)
			{
				removablePrefixes.Add(settlementName + "是你的封地。");
			}
			foreach (string prefix in removablePrefixes)
			{
				if (!string.IsNullOrWhiteSpace(prefix) && nativeInfo.StartsWith(prefix, StringComparison.Ordinal))
				{
					nativeInfo = nativeInfo.Substring(prefix.Length).Trim();
					break;
				}
			}
			if (!string.IsNullOrWhiteSpace(clanName))
			{
				string familyPrefix = "所属家族：" + clanName + "。";
				if (nativeInfo.StartsWith(familyPrefix, StringComparison.Ordinal))
				{
					nativeInfo = nativeInfo.Substring(familyPrefix.Length).Trim();
				}
			}
			return nativeInfo.Trim('。', ' ') + "。";
		}
		catch
		{
			return "";
		}
	}
internal static string BuildSettlementRulerPresenceLineForPrompt()
	{
		try
		{
			Settlement settlement = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
			Hero ruler = settlement?.OwnerClan?.Leader;
			if (settlement == null || ruler == null)
			{
				return "";
			}
			bool isPresent = false;
			try
			{
				isPresent = ruler.CurrentSettlement == settlement || ruler.PartyBelongedTo?.CurrentSettlement == settlement;
			}
			catch
			{
				isPresent = false;
			}
			return isPresent ? "当前这座城镇的统治者在该处。" : "当前这座城镇的统治者不在该处。";
		}
		catch
		{
			return "";
		}
	}
}
