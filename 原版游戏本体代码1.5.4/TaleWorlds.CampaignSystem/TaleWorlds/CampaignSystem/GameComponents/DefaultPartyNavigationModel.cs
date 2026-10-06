using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultPartyNavigationModel : PartyNavigationModel
{
	private int[] _invalidTerrainTypes;

	public override float GetEmbarkDisembarkThresholdDistance()
	{
		return 0f;
	}

	private static bool IsTerrainTypeValidForDefault(TerrainType t)
	{
		if (t != TerrainType.Plain && t != TerrainType.Desert && t != TerrainType.Snow && t != TerrainType.Forest && t != TerrainType.Steppe && t != TerrainType.Swamp && t != TerrainType.Dune && t != TerrainType.Bridge && t != TerrainType.Fording)
		{
			return t == TerrainType.Beach;
		}
		return true;
	}

	public DefaultPartyNavigationModel()
	{
		List<int> list = new List<int>();
		foreach (TerrainType value in Enum.GetValues(typeof(TerrainType)))
		{
			if (!IsTerrainTypeValidForDefault(value))
			{
				list.Add((int)value);
			}
		}
		_invalidTerrainTypes = list.ToArray();
	}

	public override int[] GetInvalidTerrainTypesForNavigationType(MobileParty.NavigationType navigationType)
	{
		if (navigationType == MobileParty.NavigationType.Default || navigationType == MobileParty.NavigationType.All)
		{
			return _invalidTerrainTypes;
		}
		return new int[0];
	}

	public override bool IsTerrainTypeValidForNavigationType(TerrainType terrainType, MobileParty.NavigationType navigationType)
	{
		if (navigationType == MobileParty.NavigationType.Default || navigationType == MobileParty.NavigationType.All)
		{
			return IsTerrainTypeValidForDefault(terrainType);
		}
		return false;
	}

	public override bool HasNavalNavigationCapability(MobileParty mobileParty)
	{
		return false;
	}

	public override bool CanPlayerNavigateToPosition(CampaignVec2 vec2, out MobileParty.NavigationType navigationType)
	{
		navigationType = MobileParty.NavigationType.Default;
		if (!vec2.Face.IsValid() || !MobileParty.MainParty.Position.IsOnLand || !vec2.IsOnLand)
		{
			return false;
		}
		return !Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(navigationType).Contains(vec2.Face.FaceGroupIndex);
	}
}
