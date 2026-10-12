using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

internal static class MapSeaContextGuard
{
	internal static bool IsMobilePartyAtSeaOrOnWater(MobileParty party)
	{
		if (party == null)
		{
			return false;
		}
		try
		{
			if (party.IsInRaftState || party.IsCurrentlyAtSea)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			return party.Position.IsValid() && !party.Position.IsOnLand;
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsPartyBaseAtSeaOrOnWater(PartyBase party)
	{
		try
		{
			return party?.IsMobile == true && IsMobilePartyAtSeaOrOnWater(party.MobileParty);
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsHeroPartyAtSeaOrOnWater(Hero hero)
	{
		try
		{
			return IsMobilePartyAtSeaOrOnWater(hero?.PartyBelongedTo);
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsCurrentPlayerEncounterAtSea(Hero target = null)
	{
		if (IsMobilePartyAtSeaOrOnWater(MobileParty.MainParty)
			|| IsHeroPartyAtSeaOrOnWater(target))
		{
			return true;
		}
		try
		{
			if (IsPartyBaseAtSeaOrOnWater(PlayerEncounter.EncounteredParty))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (PlayerEncounter.Current != null && PlayerEncounter.IsNavalEncounter())
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (PlayerEncounterCompat.GetCurrentMapEventSafe()?.IsNavalMapEvent == true)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			return MapEvent.PlayerMapEvent?.IsNavalMapEvent == true;
		}
		catch
		{
			return false;
		}
	}

	internal static string BuildMobilePartyShipPromptText(MobileParty party)
	{
		if (!IsMobilePartyAtSeaOrOnWater(party))
		{
			return "";
		}
		try
		{
			int shipCount = 0;
			int totalCapacity = 0;
			Dictionary<string, int> countByName = new Dictionary<string, int>(StringComparer.Ordinal);
			Dictionary<string, int> capacityByName = new Dictionary<string, int>(StringComparer.Ordinal);
			for (int i = 0; party.Ships != null && i < party.Ships.Count; i++)
			{
				var ship = party.Ships[i];
				var hull = ship?.ShipHull;
				if (ship == null && hull == null)
				{
					continue;
				}
				shipCount++;
				string name = "";
				try
				{
					name = (hull?.Name?.ToString() ?? ship?.Name?.ToString() ?? "").Trim();
				}
				catch
				{
					name = "";
				}
				if (string.IsNullOrWhiteSpace(name))
				{
					name = AdapterPromptCaptureTexts.PcapCompat("MapSea_UnknownShipIdentity");
				}
				int capacity = 0;
				try
				{
					capacity = Math.Max(0, hull?.TotalCrewCapacity ?? 0);
				}
				catch
				{
					capacity = 0;
				}
				totalCapacity += capacity;
				if (countByName.ContainsKey(name))
				{
					countByName[name]++;
				}
				else
				{
					countByName[name] = 1;
					capacityByName[name] = capacity;
				}
			}
			if (shipCount <= 0)
			{
				return AdapterPromptCaptureTexts.PcapText("MapSea_NoShipRecords");
			}
			List<string> shipParts = countByName
				.OrderByDescending(x => capacityByName.TryGetValue(x.Key, out int capacity) ? capacity : 0)
				.ThenBy(x => x.Key, StringComparer.Ordinal)
				.Take(5)
				.Select(x => (x.Key == AdapterPromptCaptureTexts.PcapCompat("MapSea_UnknownShipIdentity") ? AdapterPromptCaptureTexts.PcapText("MapSea_UnknownShip") : x.Key) + "x" + x.Value)
				.ToList();
			StringBuilder sb = new StringBuilder();
			sb.Append(shipCount).Append(AdapterPromptCaptureTexts.PcapText("MapSea_ShipCountSuffix"));
			if (totalCapacity > 0)
			{
				sb.Append(AdapterPromptCaptureTexts.PcapText("MapSea_CapacityPrefix")).Append(totalCapacity);
			}
			if (shipParts.Count > 0)
			{
				sb.Append(AdapterPromptCaptureTexts.PcapText("MapSea_OpenParenthesis")).Append(string.Join(AdapterPromptCaptureTexts.PcapText("MapSea_ListSeparator"), shipParts));
				if (countByName.Count > shipParts.Count)
				{
					sb.Append(AdapterPromptCaptureTexts.PcapText("MapSea_AllTypesPrefix")).Append(countByName.Count).Append(AdapterPromptCaptureTexts.PcapText("MapSea_TypesSuffix"));
				}
				sb.Append(AdapterPromptCaptureTexts.PcapText("MapSea_CloseParenthesis"));
			}
			return sb.ToString();
		}
		catch
		{
			return "";
		}
	}

	internal static Settlement FindNearestSettlementForPrompt(MobileParty party)
	{
		if (party == null)
		{
			return null;
		}
		try
		{
			Settlement settlement = Helpers.SettlementHelper.FindNearestSettlementToMobileParty(party, MobileParty.NavigationType.All, s => s != null && !s.IsHideout);
			if (settlement != null)
			{
				return settlement;
			}
		}
		catch
		{
		}
		try
		{
			if (!party.Position.IsValid())
			{
				return null;
			}
			Vec2 partyPosition = party.Position.ToVec2();
			Settlement nearest = null;
			float nearestDistanceSquared = float.MaxValue;
			foreach (Settlement settlement in Settlement.All ?? Enumerable.Empty<Settlement>())
			{
				if (settlement == null || settlement.IsHideout || !settlement.GatePosition.IsValid())
				{
					continue;
				}
				Vec2 settlementPosition = settlement.GatePosition.ToVec2();
				float x = settlementPosition.x - partyPosition.x;
				float y = settlementPosition.y - partyPosition.y;
				float distanceSquared = x * x + y * y;
				if (distanceSquared < nearestDistanceSquared)
				{
					nearestDistanceSquared = distanceSquared;
					nearest = settlement;
				}
			}
			return nearest;
		}
		catch
		{
			return null;
		}
	}

	internal static string FormatSettlementNameWithTypeForPrompt(Settlement settlement)
	{
		if (settlement == null)
		{
			return "";
		}
		string name = (settlement.Name?.ToString() ?? "").Trim();
		if (string.IsNullOrWhiteSpace(name))
		{
			return "";
		}
		return name + AdapterPromptCaptureTexts.PcapText("MapSea_OpenParenthesis") + FormatSettlementTypeForPrompt(settlement) + AdapterPromptCaptureTexts.PcapText("MapSea_CloseParenthesis");
	}

	internal static string BuildMobilePartyLandTerrainPromptLabel(MobileParty party)
	{
		if (party == null || IsMobilePartyAtSeaOrOnWater(party))
		{
			return "";
		}
		if (!TryResolveMobilePartyTerrainType(party, out TerrainType terrainType) || IsWaterOrSeaTerrain(terrainType))
		{
			return "";
		}
		return BuildTerrainPromptLabel(terrainType);
	}

	internal static string BuildMobilePartyTerrainPromptLabel(MobileParty party)
	{
		if (party == null)
		{
			return "";
		}
		if (IsMobilePartyAtSeaOrOnWater(party))
		{
			return AdapterPromptCaptureTexts.PcapText("MapSea_AtSea");
		}
		if (!TryResolveMobilePartyTerrainType(party, out TerrainType terrainType))
		{
			return "";
		}
		return BuildTerrainPromptLabel(terrainType);
	}

	internal static string BuildTerrainPromptLabel(TerrainType terrainType)
	{
		switch (terrainType)
		{
		case TerrainType.Plain:
			return AdapterPromptCaptureTexts.PcapText("MapSea_Plain");
		case TerrainType.Forest:
			return AdapterPromptCaptureTexts.PcapText("MapSea_Forest");
		case TerrainType.Mountain:
			return AdapterPromptCaptureTexts.PcapText("MapSea_Mountain");
		case TerrainType.Snow:
			return AdapterPromptCaptureTexts.PcapText("MapSea_Snow");
		case TerrainType.Desert:
			return AdapterPromptCaptureTexts.PcapText("MapSea_Desert");
		case TerrainType.Steppe:
			return AdapterPromptCaptureTexts.PcapText("MapSea_Steppe");
		case TerrainType.Swamp:
			return AdapterPromptCaptureTexts.PcapText("MapSea_Swamp");
		case TerrainType.Canyon:
			return AdapterPromptCaptureTexts.PcapText("MapSea_Canyon");
		case TerrainType.Dune:
			return AdapterPromptCaptureTexts.PcapText("MapSea_Dune");
		case TerrainType.RuralArea:
			return AdapterPromptCaptureTexts.PcapText("MapSea_RuralArea");
		case TerrainType.Beach:
			return AdapterPromptCaptureTexts.PcapText("MapSea_Beach");
		case TerrainType.Cliff:
			return AdapterPromptCaptureTexts.PcapText("MapSea_Cliff");
		case TerrainType.Fording:
			return AdapterPromptCaptureTexts.PcapText("MapSea_Fording");
		case TerrainType.Bridge:
			return AdapterPromptCaptureTexts.PcapText("MapSea_Bridge");
		case TerrainType.Lake:
			return AdapterPromptCaptureTexts.PcapText("MapSea_Lake");
		case TerrainType.River:
		case TerrainType.NonNavigableRiver:
			return AdapterPromptCaptureTexts.PcapText("MapSea_River");
		case TerrainType.Water:
			return AdapterPromptCaptureTexts.PcapText("MapSea_Water");
		case TerrainType.CoastalSea:
			return AdapterPromptCaptureTexts.PcapText("MapSea_CoastalSea");
		case TerrainType.OpenSea:
			return AdapterPromptCaptureTexts.PcapText("MapSea_OpenSea");
		case TerrainType.UnderBridge:
			return AdapterPromptCaptureTexts.PcapText("MapSea_UnderBridge");
		case TerrainType.SeaRestriction:
			return AdapterPromptCaptureTexts.PcapText("MapSea_AtSea");
		case TerrainType.LandRestriction:
			return AdapterPromptCaptureTexts.PcapText("MapSea_LandRestriction");
		default:
			return terrainType.ToString();
		}
	}

	internal static bool IsWaterOrSeaTerrain(TerrainType terrainType)
	{
		return terrainType == TerrainType.Water
			|| terrainType == TerrainType.River
			|| terrainType == TerrainType.Lake
			|| terrainType == TerrainType.CoastalSea
			|| terrainType == TerrainType.OpenSea
			|| terrainType == TerrainType.NonNavigableRiver
			|| terrainType == TerrainType.SeaRestriction
			|| terrainType == TerrainType.UnderBridge;
	}

	private static bool TryResolveMobilePartyTerrainType(MobileParty party, out TerrainType terrainType)
	{
		terrainType = TerrainType.Plain;
		try
		{
			var mapSceneWrapper = Campaign.Current?.MapSceneWrapper;
			if (party == null || mapSceneWrapper == null)
			{
				return false;
			}
			try
			{
				if (party.Position.IsValid())
				{
					CampaignVec2 position = party.Position;
					terrainType = mapSceneWrapper.GetTerrainTypeAtPosition(in position);
					return true;
				}
			}
			catch
			{
			}
			try
			{
				if (party.Position.IsValid())
				{
					mapSceneWrapper.GetEnvironmentTerrainTypesCount(party.Position, out terrainType);
					return true;
				}
			}
			catch
			{
			}
			terrainType = BannerlordApiCompat.ResolveTerrainTypeForParty(party, TerrainType.Plain, allowNavigationFaceFallback: false);
			return true;
		}
		catch
		{
		}
		return false;
	}

	private static string FormatSettlementTypeForPrompt(Settlement settlement)
	{
		try
		{
			if (settlement == null)
			{
				return AdapterPromptCaptureTexts.PcapText("MapSea_Settlement");
			}
			if (settlement.IsTown)
			{
				return AdapterPromptCaptureTexts.PcapText("MapSea_Town");
			}
			if (settlement.IsCastle)
			{
				return AdapterPromptCaptureTexts.PcapText("MapSea_Castle");
			}
			if (settlement.IsVillage)
			{
				return AdapterPromptCaptureTexts.PcapText("MapSea_Village");
			}
			if (settlement.IsFortification)
			{
				return AdapterPromptCaptureTexts.PcapText("MapSea_Fortification");
			}
			if (settlement.IsHideout)
			{
				return AdapterPromptCaptureTexts.PcapText("MapSea_Hideout");
			}
		}
		catch
		{
		}
		return AdapterPromptCaptureTexts.PcapText("MapSea_Settlement");
	}
}
