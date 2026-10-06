using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace NavalDLC.GameComponents;

public class NavalPartyNavigationModel : PartyNavigationModel
{
	private readonly Dictionary<MobileParty.NavigationType, int[]> _invalidTypesIntegerCache = new Dictionary<MobileParty.NavigationType, int[]>();

	private readonly PartyNavigationModel _baseModel;

	public override float GetEmbarkDisembarkThresholdDistance()
	{
		return 0.5f;
	}

	private static bool IsTerrainTypeValidForNaval(TerrainType t)
	{
		if (t != TerrainType.Lake && t != TerrainType.Water && t != TerrainType.River && t != TerrainType.CoastalSea && t != TerrainType.OpenSea && t != TerrainType.LandRestriction && t != TerrainType.SeaRestriction)
		{
			return t == TerrainType.UnderBridge;
		}
		return true;
	}

	public NavalPartyNavigationModel(PartyNavigationModel partyNavigationModel)
	{
		_baseModel = partyNavigationModel;
		InitializeInvalidTypesCache();
	}

	private void InitializeInvalidTypesCache()
	{
		_invalidTypesIntegerCache.Clear();
		List<int> list = new List<int>();
		List<int> list2 = new List<int>();
		List<int> list3 = new List<int>();
		foreach (TerrainType value in Enum.GetValues(typeof(TerrainType)))
		{
			if (!IsTerrainTypeValidForNavigationType(value, MobileParty.NavigationType.All))
			{
				list.Add((int)value);
			}
			if (!IsTerrainTypeValidForNavigationType(value, MobileParty.NavigationType.Naval))
			{
				list3.Add((int)value);
			}
			if (!IsTerrainTypeValidForNavigationType(value, MobileParty.NavigationType.Default))
			{
				list2.Add((int)value);
			}
		}
		_invalidTypesIntegerCache.Add(MobileParty.NavigationType.All, list.ToArray());
		_invalidTypesIntegerCache.Add(MobileParty.NavigationType.Default, list2.ToArray());
		_invalidTypesIntegerCache.Add(MobileParty.NavigationType.Naval, list3.ToArray());
	}

	public override bool IsTerrainTypeValidForNavigationType(TerrainType terrainType, MobileParty.NavigationType navigationType)
	{
		switch (navigationType)
		{
		case MobileParty.NavigationType.Naval:
			return IsTerrainTypeValidForNaval(terrainType);
		case MobileParty.NavigationType.All:
			if (!IsTerrainTypeValidForNaval(terrainType))
			{
				return _baseModel.IsTerrainTypeValidForNavigationType(terrainType, navigationType);
			}
			return true;
		default:
			return _baseModel.IsTerrainTypeValidForNavigationType(terrainType, navigationType);
		}
	}

	public override int[] GetInvalidTerrainTypesForNavigationType(MobileParty.NavigationType navigationType)
	{
		if (_invalidTypesIntegerCache.ContainsKey(navigationType))
		{
			return _invalidTypesIntegerCache[navigationType];
		}
		return new int[0];
	}

	public override bool HasNavalNavigationCapability(MobileParty mobileParty)
	{
		if (mobileParty.Ships.Count <= 0)
		{
			if (!mobileParty.IsMainParty)
			{
				if (mobileParty.AttachedTo == null || !mobileParty.AttachedTo.HasNavalNavigationCapability)
				{
					if (mobileParty.AttachedParties.Count > 0)
					{
						return mobileParty.AttachedParties.Any((MobileParty x) => x.Ships.Count > 0);
					}
					return false;
				}
				return true;
			}
			return false;
		}
		return true;
	}

	public override bool CanPlayerNavigateToPosition(CampaignVec2 vec2, out MobileParty.NavigationType navigationType)
	{
		navigationType = MobileParty.NavigationType.None;
		if (!vec2.Face.IsValid())
		{
			return false;
		}
		if (!MobileParty.MainParty.IsCurrentlyAtSea && NavigationHelper.IsPositionValidForNavigationType(vec2, MobileParty.NavigationType.Naval))
		{
			return false;
		}
		if (MobileParty.MainParty.IsCurrentlyAtSea)
		{
			if (MobileParty.MainParty.HasNavalNavigationCapability && NavigationHelper.IsPositionValidForNavigationType(vec2, MobileParty.NavigationType.Naval))
			{
				navigationType = MobileParty.NavigationType.Naval;
			}
			else
			{
				navigationType = MobileParty.MainParty.NavigationCapability;
			}
		}
		else
		{
			navigationType = MobileParty.NavigationType.Default;
		}
		int[] invalidTerrainTypesForNavigationType = Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(navigationType);
		if (invalidTerrainTypesForNavigationType.Contains(vec2.Face.FaceGroupIndex))
		{
			return false;
		}
		if (!vec2.IsOnLand && MobileParty.MainParty.IsCurrentlyAtSea)
		{
			return true;
		}
		float distance;
		return Campaign.Current.MapSceneWrapper.GetPathDistanceBetweenAIFaces(MobileParty.MainParty.CurrentNavigationFace, vec2.Face, MobileParty.MainParty.Position.ToVec2(), vec2.ToVec2(), 0.3f, Campaign.PathFindingMaxCostLimit, out distance, invalidTerrainTypesForNavigationType, MobileParty.MainParty.GetRegionSwitchCostFromLandToSea(), MobileParty.MainParty.GetRegionSwitchCostFromSeaToLand());
	}
}
