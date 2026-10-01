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
					name = "未知船只";
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
				return "当前在海上，未识别到舰船记录";
			}
			List<string> shipParts = countByName
				.OrderByDescending(x => capacityByName.TryGetValue(x.Key, out int capacity) ? capacity : 0)
				.ThenBy(x => x.Key, StringComparer.Ordinal)
				.Take(5)
				.Select(x => x.Key + "x" + x.Value)
				.ToList();
			StringBuilder sb = new StringBuilder();
			sb.Append(shipCount).Append("艘");
			if (totalCapacity > 0)
			{
				sb.Append("，总载员容量").Append(totalCapacity);
			}
			if (shipParts.Count > 0)
			{
				sb.Append("（").Append(string.Join("、", shipParts));
				if (countByName.Count > shipParts.Count)
				{
					sb.Append("等").Append(countByName.Count).Append("型");
				}
				sb.Append("）");
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
		return name + "（" + FormatSettlementTypeForPrompt(settlement) + "）";
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
			return "海上";
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
			return "平原";
		case TerrainType.Forest:
			return "森林";
		case TerrainType.Mountain:
			return "丘陵山地";
		case TerrainType.Snow:
			return "雪原";
		case TerrainType.Desert:
			return "沙漠";
		case TerrainType.Steppe:
			return "草原";
		case TerrainType.Swamp:
			return "沼泽";
		case TerrainType.Canyon:
			return "峡谷";
		case TerrainType.Dune:
			return "沙丘";
		case TerrainType.RuralArea:
			return "乡野";
		case TerrainType.Beach:
			return "海滩";
		case TerrainType.Cliff:
			return "峭壁";
		case TerrainType.Fording:
			return "浅滩";
		case TerrainType.Bridge:
			return "桥梁";
		case TerrainType.Lake:
			return "湖面";
		case TerrainType.River:
		case TerrainType.NonNavigableRiver:
			return "河面";
		case TerrainType.Water:
			return "水域";
		case TerrainType.CoastalSea:
			return "近海";
		case TerrainType.OpenSea:
			return "外海";
		case TerrainType.UnderBridge:
			return "桥下水域";
		case TerrainType.SeaRestriction:
			return "海上";
		case TerrainType.LandRestriction:
			return "受限陆地";
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
				return "定居点";
			}
			if (settlement.IsTown)
			{
				return "城镇";
			}
			if (settlement.IsCastle)
			{
				return "城堡";
			}
			if (settlement.IsVillage)
			{
				return "村庄";
			}
			if (settlement.IsFortification)
			{
				return "要塞";
			}
			if (settlement.IsHideout)
			{
				return "藏身处";
			}
		}
		catch
		{
		}
		return "定居点";
	}
}
