using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement;

public class TownManagementVillageItemVM : ViewModel
{
	private enum VillageTypes
	{
		None,
		EuropeHorseRanch,
		BattanianHorseRanch,
		SteppeHorseRanch,
		DesertHorseRanch,
		WheatFarm,
		Lumberjack,
		ClayMine,
		SaltMine,
		IronMine,
		Fisherman,
		CattleRange,
		SheepFarm,
		VineYard,
		FlaxPlant,
		DateFarm,
		OliveTrees,
		SilkPlant,
		SilverMine
	}

	private readonly Village _village;

	private string _name;

	private string _background;

	private string _productionName;

	private int _villageType;

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public string ProductionName
	{
		get
		{
			return _productionName;
		}
		set
		{
			if (value != _productionName)
			{
				_productionName = value;
				OnPropertyChangedWithValue(value, "ProductionName");
			}
		}
	}

	[DataSourceProperty]
	public string Background
	{
		get
		{
			return _background;
		}
		set
		{
			if (value != _background)
			{
				_background = value;
				OnPropertyChangedWithValue(value, "Background");
			}
		}
	}

	[DataSourceProperty]
	public int VillageType
	{
		get
		{
			return _villageType;
		}
		set
		{
			if (value != _villageType)
			{
				_villageType = value;
				OnPropertyChangedWithValue(value, "VillageType");
			}
		}
	}

	public TownManagementVillageItemVM(Village village)
	{
		_village = village;
		Background = village.Settlement.SettlementComponent.BackgroundMeshName + "_t";
		VillageType = (int)DetermineVillageType(village.VillageType);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = _village.Name.ToString();
		ProductionName = _village.VillageType.PrimaryProduction.Name.ToString();
	}

	public void ExecuteShowTooltip()
	{
		InformationManager.ShowTooltip(typeof(Settlement), _village.Settlement);
	}

	public void ExecuteHideTooltip()
	{
		MBInformationManager.HideInformations();
	}

	private VillageTypes DetermineVillageType(VillageType village)
	{
		if (village == DefaultVillageTypes.EuropeHorseRanch)
		{
			return VillageTypes.EuropeHorseRanch;
		}
		if (village == DefaultVillageTypes.BattanianHorseRanch)
		{
			return VillageTypes.BattanianHorseRanch;
		}
		if (village == DefaultVillageTypes.SteppeHorseRanch)
		{
			return VillageTypes.SteppeHorseRanch;
		}
		if (village == DefaultVillageTypes.DesertHorseRanch)
		{
			return VillageTypes.DesertHorseRanch;
		}
		if (village == DefaultVillageTypes.WheatFarm)
		{
			return VillageTypes.WheatFarm;
		}
		if (village == DefaultVillageTypes.Lumberjack)
		{
			return VillageTypes.Lumberjack;
		}
		if (village == DefaultVillageTypes.ClayMine)
		{
			return VillageTypes.ClayMine;
		}
		if (village == DefaultVillageTypes.SaltMine)
		{
			return VillageTypes.SaltMine;
		}
		if (village == DefaultVillageTypes.IronMine)
		{
			return VillageTypes.IronMine;
		}
		if (village == DefaultVillageTypes.Fisherman)
		{
			return VillageTypes.Fisherman;
		}
		if (village == DefaultVillageTypes.CattleRange)
		{
			return VillageTypes.CattleRange;
		}
		if (village == DefaultVillageTypes.SheepFarm)
		{
			return VillageTypes.SheepFarm;
		}
		if (village == DefaultVillageTypes.VineYard)
		{
			return VillageTypes.VineYard;
		}
		if (village == DefaultVillageTypes.FlaxPlant)
		{
			return VillageTypes.FlaxPlant;
		}
		if (village == DefaultVillageTypes.DateFarm)
		{
			return VillageTypes.DateFarm;
		}
		if (village == DefaultVillageTypes.OliveTrees)
		{
			return VillageTypes.OliveTrees;
		}
		if (village == DefaultVillageTypes.SilkPlant)
		{
			return VillageTypes.SilkPlant;
		}
		if (village == DefaultVillageTypes.SilverMine)
		{
			return VillageTypes.SilverMine;
		}
		return VillageTypes.None;
	}
}
