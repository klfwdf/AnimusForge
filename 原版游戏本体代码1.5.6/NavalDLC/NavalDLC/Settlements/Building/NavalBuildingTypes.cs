using System;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace NavalDLC.Settlements.Building;

public class NavalBuildingTypes
{
	private BuildingType _buildingShipyard;

	public static BuildingType SettlementShipyard => Instance._buildingShipyard;

	private static NavalBuildingTypes Instance => NavalDLCManager.Instance.NavalBuildingTypes;

	public NavalBuildingTypes()
	{
		RegisterAll();
		InitializeAll();
	}

	private void RegisterAll()
	{
		_buildingShipyard = Create("building_shipyard");
	}

	private BuildingType Create(string stringId)
	{
		return Game.Current.ObjectManager.RegisterPresumedObject(new BuildingType(stringId));
	}

	private void InitializeAll()
	{
		_buildingShipyard.Initialize(GameTexts.FindText("str_shipyard"), new TextObject("{=bDDtGsyv}Allows ship production. Enables repair, trading, and upgrades of ships."), new int[3] { 0, 4800, 6000 }, new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>[2]
		{
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.ShipProduction, BuildingEffectIncrementType.Add, 1f, 2f, 3f),
			new Tuple<BuildingEffectEnum, BuildingEffectIncrementType, float, float, float>(BuildingEffectEnum.MaximumShipCount, BuildingEffectIncrementType.Add, 9f, 12f, 15f)
		}, isMilitaryProject: false, 0f, 1);
	}
}
