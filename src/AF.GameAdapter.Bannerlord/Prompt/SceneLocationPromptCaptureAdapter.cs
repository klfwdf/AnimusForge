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
		return string.IsNullOrWhiteSpace(text2) ? AdapterPromptCaptureTexts.PcapCompat("LocationRaw_000") : text2;
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
		return AdapterPromptCaptureTexts.PcapCompat("LocationRaw_001");
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
				terrainLabel = AdapterPromptCaptureTexts.PcapCompat("Project_Sea");
			}
			else
			{
				terrainLabel = MapSeaContextGuard.BuildMobilePartyLandTerrainPromptLabel(party);
				if (string.IsNullOrWhiteSpace(terrainLabel))
				{
					terrainLabel = AdapterPromptCaptureTexts.PcapCompat("Project_Wilderness");
				}
			}
			if (string.IsNullOrWhiteSpace(settlementName))
			{
				return terrainLabel;
			}
			return settlementName + AdapterPromptCaptureTexts.PcapCompat("LocationRaw_004") + terrainLabel;
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
			2 => AdapterPromptCaptureTexts.PcapText("Location_Count_2"),
			3 => AdapterPromptCaptureTexts.PcapText("Location_Count_3"),
			4 => AdapterPromptCaptureTexts.PcapText("Location_Count_4"),
			5 => AdapterPromptCaptureTexts.PcapText("Location_Count_5"),
			6 => AdapterPromptCaptureTexts.PcapText("Location_Count_6"),
			7 => AdapterPromptCaptureTexts.PcapText("Location_Count_7"),
			8 => AdapterPromptCaptureTexts.PcapText("Location_Count_8"),
			9 => AdapterPromptCaptureTexts.PcapText("Location_Count_9"),
			10 => AdapterPromptCaptureTexts.PcapText("Location_Count_10"),
			_ => count.ToString() + AdapterPromptCaptureTexts.PcapText("Location_CountSuffix"),
		};
	}
internal static string NormalizeFactionRelationLabelForPrompt(IFaction faction, IFaction referenceFaction)
	{
		try
		{
			if (faction == null || referenceFaction == null)
			{
				return AdapterPromptCaptureTexts.PcapCompat("Project_Neutral");
			}
			string factionId = (faction.StringId ?? "").Trim();
			string referenceId = (referenceFaction.StringId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(factionId) && string.Equals(factionId, referenceId, StringComparison.OrdinalIgnoreCase))
			{
				return AdapterPromptCaptureTexts.PcapCompat("Project_Friendly");
			}
			if (ReferenceEquals(faction, referenceFaction))
			{
				return AdapterPromptCaptureTexts.PcapCompat("Project_Friendly");
			}
			if (referenceFaction.IsAtWarWith(faction) || faction.IsAtWarWith(referenceFaction))
			{
				return AdapterPromptCaptureTexts.PcapCompat("Project_Hostile");
			}
			return AdapterPromptCaptureTexts.PcapCompat("Project_Neutral");
		}
		catch
		{
			return AdapterPromptCaptureTexts.PcapCompat("Project_Neutral");
		}
	}
internal static string ConvertNpcSideRelationLabelForPrompt(string relation)
	{
		string value = (relation ?? "").Trim();
		if (string.Equals(value, AdapterPromptCaptureTexts.PcapCompat("Project_Hostile"), StringComparison.Ordinal) || string.Equals(value, "hostile", StringComparison.OrdinalIgnoreCase))
			return AdapterPromptCaptureTexts.PcapText("Location_NpcEnemy");
		if (string.Equals(value, AdapterPromptCaptureTexts.PcapCompat("Project_Friendly"), StringComparison.Ordinal) || string.Equals(value, "friendly", StringComparison.OrdinalIgnoreCase))
			return AdapterPromptCaptureTexts.PcapText("Location_NpcFriendly");
		return AdapterPromptCaptureTexts.PcapText("Location_NpcNeutral");
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
			if (sceneDescription.StartsWith(AdapterPromptCaptureTexts.PcapCompat("LocationRaw_021"), StringComparison.Ordinal))
			{
				string body = sceneDescription.Substring(AdapterPromptCaptureTexts.PcapCompat("LocationRaw_021").Length).Trim();
				body = body.TrimEnd('。', '.', ' ');
				string seaSuffix = AdapterPromptCaptureTexts.PcapCompat("LocationRaw_023");
				if (body.EndsWith(seaSuffix, StringComparison.Ordinal))
				{
					placeName = body.Substring(0, body.Length - seaSuffix.Length).Trim();
					spotName = AdapterPromptCaptureTexts.PcapCompat("Project_Sea");
					return;
				}
				if (string.Equals(body, AdapterPromptCaptureTexts.PcapCompat("Project_Sea"), StringComparison.Ordinal))
				{
					spotName = AdapterPromptCaptureTexts.PcapCompat("Project_Sea");
					return;
				}
			}
			if (sceneDescription.StartsWith(AdapterPromptCaptureTexts.PcapCompat("LocationRaw_027"), StringComparison.Ordinal))
			{
				string body = sceneDescription.Substring(AdapterPromptCaptureTexts.PcapCompat("LocationRaw_027").Length).Trim();
				int splitIndex = body.LastIndexOf(AdapterPromptCaptureTexts.PcapCompat("LocationRaw_029"), StringComparison.Ordinal);
				if (splitIndex > 0)
				{
					placeName = body.Substring(0, splitIndex).Trim();
					spotName = body.Substring(splitIndex + AdapterPromptCaptureTexts.PcapCompat("LocationRaw_029").Length).Trim();
					return;
				}
			}
			if (sceneDescription.StartsWith(AdapterPromptCaptureTexts.PcapCompat("LocationRaw_031"), StringComparison.Ordinal))
			{
				string body = sceneDescription.Substring(AdapterPromptCaptureTexts.PcapCompat("LocationRaw_031").Length).Trim();
				int splitIndex = body.LastIndexOf(AdapterPromptCaptureTexts.PcapCompat("LocationRaw_029"), StringComparison.Ordinal);
				if (splitIndex > 0)
				{
					placeName = body.Substring(0, splitIndex).Trim();
					spotName = body.Substring(splitIndex + AdapterPromptCaptureTexts.PcapCompat("LocationRaw_029").Length).Trim();
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
		return string.Equals(text, AdapterPromptCaptureTexts.PcapCompat("LocationRaw_035"), StringComparison.Ordinal)
			|| string.Equals(text, AdapterPromptCaptureTexts.PcapCompat("LocationRaw_001"), StringComparison.Ordinal)
			|| string.Equals(text, AdapterPromptCaptureTexts.PcapCompat("LocationRaw_037"), StringComparison.Ordinal);
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
				spotName = AdapterPromptCaptureTexts.PcapCompat("Project_Sea");
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
				placeName = AdapterPromptCaptureTexts.PcapText("Project_CurrentArea");
			}
			if (string.IsNullOrWhiteSpace(spotName))
			{
				spotName = wildernessContext ? AdapterPromptCaptureTexts.PcapCompat("Project_Wilderness") : AdapterPromptCaptureTexts.PcapCompat("Project_Somewhere");
			}
			if (wildernessContext && settlement == null && string.Equals(placeName, AdapterPromptCaptureTexts.PcapText("Project_CurrentArea"), StringComparison.Ordinal))
			{
				return AdapterPromptCaptureTexts.PcapFormat("Location_WildernessUnknown", "You are currently in {SPOT}.", ("SPOT", AdapterPromptCaptureTexts.PcapProjectKnown(spotName)));
			}
			string cultureName = (settlement?.Culture?.Name?.ToString() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(cultureName))
			{
				cultureName = AdapterPromptCaptureTexts.PcapText("Project_Unknown");
			}
			string rulerName = (settlement?.OwnerClan?.Leader?.Name?.ToString() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(rulerName))
			{
				rulerName = AdapterPromptCaptureTexts.PcapText("Project_UnknownPerson");
			}
			string clanName = (settlement?.OwnerClan?.Name?.ToString() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(clanName))
			{
				clanName = AdapterPromptCaptureTexts.PcapText("Project_Unknown");
			}
			string clanSuffix = AdapterPromptCaptureTexts.PcapText("Location_ClanSuffix");
			if (!clanName.EndsWith(AdapterPromptCaptureTexts.PcapCompat("Location_ClanSuffix"), StringComparison.Ordinal) && !clanName.EndsWith(clanSuffix, StringComparison.OrdinalIgnoreCase))
			{
				clanName += clanSuffix;
			}
			string factionName = (settlement?.MapFaction?.Name?.ToString() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(factionName))
			{
				factionName = AdapterPromptCaptureTexts.PcapText("Project_UnknownFaction");
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
				playerName = AdapterPromptCaptureTexts.PcapText("Project_Player");
			}
			string Describe(string key) => AdapterPromptCaptureTexts.PcapFormat(key, "",
				("PLACE", placeName), ("SPOT", AdapterPromptCaptureTexts.PcapProjectKnown(spotName)),
				("CULTURE", cultureName), ("RULER", rulerName),
				("CLAN", clanName), ("FACTION", factionName),
				("PLAYER", playerName), ("RELATION", AdapterPromptCaptureTexts.PcapProjectKnown(playerRelation)),
				("NPC_RELATION", ConvertNpcSideRelationLabelForPrompt(npcRelation)));
			if (seaContext)
			{
				if (settlement == null)
				{
					if (string.Equals(placeName, AdapterPromptCaptureTexts.PcapText("Project_CurrentArea"), StringComparison.Ordinal))
					{
						return AdapterPromptCaptureTexts.PcapText("Location_AtSea");
					}
					return Describe("Location_SeaNear");
				}
				if (isRuledByPerspectiveHero)
				{
					return Describe("Location_SeaOwn");
				}
				return Describe("Location_SeaOther");
			}
			if (isRuledByPerspectiveHero)
			{
				if (wildernessContext)
				{
					return Describe("Location_WildernessOwn");
				}
				return Describe("Location_SettlementOwn");
			}
			if (wildernessContext)
			{
				return Describe("Location_WildernessOther");
			}
			return Describe("Location_SettlementOther");
		}
		catch
		{
			return "";
		}
	}
internal static bool IsWildernessSpotForPrompt(string spotName)
	{
		string text = (spotName ?? "").Trim();
		return string.Equals(text, AdapterPromptCaptureTexts.PcapCompat("Project_Wilderness"), StringComparison.Ordinal)
			|| string.Equals(text, AdapterPromptCaptureTexts.PcapCompat("Project_Plains"), StringComparison.Ordinal)
			|| string.Equals(text, AdapterPromptCaptureTexts.PcapCompat("Project_Forest"), StringComparison.Ordinal)
			|| string.Equals(text, AdapterPromptCaptureTexts.PcapCompat("Project_Hills"), StringComparison.Ordinal)
			|| string.Equals(text, AdapterPromptCaptureTexts.PcapCompat("Project_Snow"), StringComparison.Ordinal)
			|| string.Equals(text, AdapterPromptCaptureTexts.PcapCompat("Project_Desert"), StringComparison.Ordinal)
			|| string.Equals(text, AdapterPromptCaptureTexts.PcapCompat("Project_Steppe"), StringComparison.Ordinal)
			|| string.Equals(text, AdapterPromptCaptureTexts.PcapCompat("Project_Swamp"), StringComparison.Ordinal)
			|| string.Equals(text, AdapterPromptCaptureTexts.PcapCompat("Project_Canyon"), StringComparison.Ordinal)
			|| string.Equals(text, AdapterPromptCaptureTexts.PcapCompat("Project_Dunes"), StringComparison.Ordinal)
			|| string.Equals(text, AdapterPromptCaptureTexts.PcapCompat("Project_Countryside"), StringComparison.Ordinal)
			|| string.Equals(text, AdapterPromptCaptureTexts.PcapCompat("Project_Beach"), StringComparison.Ordinal)
			|| string.Equals(text, AdapterPromptCaptureTexts.PcapCompat("Project_Cliff"), StringComparison.Ordinal)
			|| string.Equals(text, AdapterPromptCaptureTexts.PcapCompat("Project_Shallows"), StringComparison.Ordinal)
			|| string.Equals(text, AdapterPromptCaptureTexts.PcapCompat("Project_Bridge"), StringComparison.Ordinal)
			|| text.IndexOf(AdapterPromptCaptureTexts.PcapCompat("Project_Wilderness"), StringComparison.Ordinal) >= 0;
	}
internal static bool IsSeaSpotForPrompt(string spotName)
	{
		string text = (spotName ?? "").Trim();
		return string.Equals(text, AdapterPromptCaptureTexts.PcapCompat("Project_Sea"), StringComparison.Ordinal)
			|| text.IndexOf(AdapterPromptCaptureTexts.PcapCompat("Project_Sea"), StringComparison.Ordinal) >= 0;
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
			string settlementName = (NativePromptLiteralProjection.Render(settlement.Name) ?? "").Trim();
			string rulerName = (NativePromptLiteralProjection.Render(settlement.OwnerClan?.Leader?.Name) ?? "").Trim();
			string clanName = (NativePromptLiteralProjection.Render(settlement.OwnerClan?.Name) ?? "").Trim();
			string factionName = (NativePromptLiteralProjection.Render(settlement.MapFaction?.Name) ?? "").Trim();
			string officialTitle = "";
			try
			{
				IFaction mapFaction = settlement.MapFaction;
				string cultureId = ((mapFaction?.Culture)?.StringId ?? "").Trim();
				if (settlement.OwnerClan?.Leader?.IsFemale ?? false)
				{
					cultureId += "_f";
				}
				officialTitle = ((mapFaction == null || !mapFaction.IsKingdomFaction || settlement.OwnerClan?.Leader == null || mapFaction.Leader != settlement.OwnerClan.Leader) ? NativePromptLiteralProjection.Render(GameTexts.FindText("str_faction_official", cultureId)) : NativePromptLiteralProjection.Render(GameTexts.FindText("str_faction_ruler", cultureId)));
				officialTitle = (officialTitle ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			}
			catch
			{
				officialTitle = "";
			}
			List<string> removablePrefixes = new List<string>();
			if (!string.IsNullOrWhiteSpace(settlementName) && !string.IsNullOrWhiteSpace(factionName) && !string.IsNullOrWhiteSpace(officialTitle) && !string.IsNullOrWhiteSpace(rulerName))
			{
				removablePrefixes.Add(settlementName + ConversationRuntimeTexts.Prompt("AF_CONV_Runtime_0801") + factionName + ConversationRuntimeTexts.Prompt("AF_CONV_Runtime_0802") + officialTitle + "，" + rulerName + ConversationRuntimeTexts.Prompt("AF_CONV_Runtime_0803"));
			}
			if (!string.IsNullOrWhiteSpace(settlementName) && !string.IsNullOrWhiteSpace(rulerName))
			{
				removablePrefixes.Add(settlementName + ConversationRuntimeTexts.Prompt("AF_CONV_Runtime_0804") + rulerName + ConversationRuntimeTexts.Prompt("AF_CONV_Runtime_0805"));
				removablePrefixes.Add(settlementName + ConversationRuntimeTexts.Prompt("AF_CONV_Runtime_0804") + rulerName + ConversationRuntimeTexts.Prompt("AF_CONV_Runtime_0808"));
			}
			if (!string.IsNullOrWhiteSpace(settlementName) && settlement.OwnerClan == Clan.PlayerClan)
			{
				removablePrefixes.Add(settlementName + ConversationRuntimeTexts.Prompt("AF_CONV_Runtime_0800"));
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
				string familyPrefix = ConversationRuntimeTexts.Prompt("AF_CONV_Runtime_0810") + clanName + "。";
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
			return isPresent ? AdapterPromptCaptureTexts.PcapText("Location_RulerPresent") : AdapterPromptCaptureTexts.PcapText("Location_RulerAbsent");
		}
		catch
		{
			return "";
		}
	}
}
