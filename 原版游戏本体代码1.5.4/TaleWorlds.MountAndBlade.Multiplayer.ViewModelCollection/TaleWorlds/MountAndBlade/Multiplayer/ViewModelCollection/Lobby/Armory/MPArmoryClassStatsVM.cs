using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory;

public class MPArmoryClassStatsVM : ViewModel
{
	private readonly List<IReadOnlyPerkObject> _dummyPerkList;

	private string _factionDescription;

	private string _factionName;

	private string _flavorText;

	private int _cost;

	private HintViewModel _costHint;

	private HeroInformationVM _heroInformation;

	[DataSourceProperty]
	public string FactionDescription
	{
		get
		{
			return _factionDescription;
		}
		set
		{
			if (value != _factionDescription)
			{
				_factionDescription = value;
				OnPropertyChangedWithValue(value, "FactionDescription");
			}
		}
	}

	[DataSourceProperty]
	public string FactionName
	{
		get
		{
			return _factionName;
		}
		set
		{
			if (value != _factionName)
			{
				_factionName = value;
				OnPropertyChangedWithValue(value, "FactionName");
			}
		}
	}

	[DataSourceProperty]
	public string FlavorText
	{
		get
		{
			return _flavorText;
		}
		set
		{
			if (value != _flavorText)
			{
				_flavorText = value;
				OnPropertyChangedWithValue(value, "FlavorText");
			}
		}
	}

	[DataSourceProperty]
	public int Cost
	{
		get
		{
			return _cost;
		}
		set
		{
			if (value != _cost)
			{
				_cost = value;
				OnPropertyChangedWithValue(value, "Cost");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel CostHint
	{
		get
		{
			return _costHint;
		}
		set
		{
			if (value != _costHint)
			{
				_costHint = value;
				OnPropertyChangedWithValue(value, "CostHint");
			}
		}
	}

	[DataSourceProperty]
	public HeroInformationVM HeroInformation
	{
		get
		{
			return _heroInformation;
		}
		set
		{
			if (value != _heroInformation)
			{
				_heroInformation = value;
				OnPropertyChangedWithValue(value, "HeroInformation");
			}
		}
	}

	public MPArmoryClassStatsVM()
	{
		_dummyPerkList = new List<IReadOnlyPerkObject>();
		FactionDescription = new TextObject("{=5Pea977J}Faction: ").ToString();
		HeroInformation = new HeroInformationVM();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		CostHint = new HintViewModel(GameTexts.FindText("str_armory_troop_cost"));
		HeroInformation.RefreshValues();
	}

	public void RefreshWith(MultiplayerClassDivisions.MPHeroClass heroClass)
	{
		FactionName = heroClass.Culture.Name.ToString();
		FlavorText = GameTexts.FindText("str_troop_description", heroClass.StringId).ToString();
		HeroInformation.RefreshWith(heroClass, _dummyPerkList);
		Cost = heroClass.TroopCost;
	}
}
