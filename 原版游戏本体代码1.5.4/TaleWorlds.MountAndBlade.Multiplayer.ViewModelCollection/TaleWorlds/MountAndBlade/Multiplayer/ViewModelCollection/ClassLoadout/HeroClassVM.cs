using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Missions.Multiplayer;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;

public class HeroClassVM : ViewModel
{
	private readonly MissionMultiplayerGameModeBaseClient _gameMode;

	public readonly MultiplayerClassDivisions.MPHeroClass HeroClass;

	private readonly Action<HeroClassVM> _onSelect;

	private Action<HeroPerkVM, MPPerkVM> _onPerkSelect;

	private bool _isSelected;

	private string _name;

	private string _iconType;

	private int _gold;

	private int _numOfTroops;

	private bool _isEnabled;

	private bool _isGoldEnabled;

	private bool _isNumOfTroopsEnabled;

	private string _cultureId;

	private string _troopTypeId;

	private Color _cultureColor;

	private MBBindingList<HeroPerkVM> _perks;

	public List<IReadOnlyPerkObject> SelectedPerks { get; private set; }

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<HeroPerkVM> Perks
	{
		get
		{
			return _perks;
		}
		set
		{
			if (value != _perks)
			{
				_perks = value;
				OnPropertyChangedWithValue(value, "Perks");
			}
		}
	}

	[DataSourceProperty]
	public string CultureId
	{
		get
		{
			return _cultureId;
		}
		set
		{
			if (value != _cultureId)
			{
				_cultureId = value;
				OnPropertyChangedWithValue(value, "CultureId");
			}
		}
	}

	[DataSourceProperty]
	public string TroopTypeId
	{
		get
		{
			return _troopTypeId;
		}
		set
		{
			if (value != _troopTypeId)
			{
				_troopTypeId = value;
				OnPropertyChangedWithValue(value, "TroopTypeId");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

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
	public string IconType
	{
		get
		{
			return _iconType;
		}
		set
		{
			if (value != _iconType)
			{
				_iconType = value;
				OnPropertyChangedWithValue(value, "IconType");
			}
		}
	}

	[DataSourceProperty]
	public int Gold
	{
		get
		{
			return _gold;
		}
		set
		{
			if (value != _gold)
			{
				_gold = value;
				OnPropertyChangedWithValue(value, "Gold");
			}
		}
	}

	[DataSourceProperty]
	public int NumOfTroops
	{
		get
		{
			return _numOfTroops;
		}
		set
		{
			if (value != _numOfTroops)
			{
				_numOfTroops = value;
				OnPropertyChangedWithValue(value, "NumOfTroops");
			}
		}
	}

	[DataSourceProperty]
	public bool IsGoldEnabled
	{
		get
		{
			return _isGoldEnabled;
		}
		set
		{
			if (value != _isGoldEnabled)
			{
				_isGoldEnabled = value;
				OnPropertyChangedWithValue(value, "IsGoldEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsNumOfTroopsEnabled
	{
		get
		{
			return _isNumOfTroopsEnabled;
		}
		set
		{
			if (value != _isNumOfTroopsEnabled)
			{
				_isNumOfTroopsEnabled = value;
				OnPropertyChangedWithValue(value, "IsNumOfTroopsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public Color CultureColor
	{
		get
		{
			return _cultureColor;
		}
		set
		{
			if (value != _cultureColor)
			{
				_cultureColor = value;
				OnPropertyChangedWithValue(value, "CultureColor");
			}
		}
	}

	[DataSourceProperty]
	public HeroPerkVM FirstPerk => Perks.ElementAtOrDefault(0);

	[DataSourceProperty]
	public HeroPerkVM SecondPerk => Perks.ElementAtOrDefault(1);

	[DataSourceProperty]
	public HeroPerkVM ThirdPerk => Perks.ElementAtOrDefault(2);

	public HeroClassVM(Action<HeroClassVM> onSelect, Action<HeroPerkVM, MPPerkVM> onPerkSelect, MultiplayerClassDivisions.MPHeroClass heroClass, MultiplayerBattleColors.MultiplayerCultureColorInfo colorInfo)
	{
		HeroClass = heroClass;
		_onSelect = onSelect;
		_onPerkSelect = onPerkSelect;
		CultureId = heroClass.Culture.StringId;
		IconType = heroClass.IconType.ToString();
		TroopTypeId = heroClass.ClassGroup.StringId;
		CultureColor = colorInfo.Color1;
		_gameMode = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
		Gold = (_gameMode.IsGameModeUsingCasualGold ? HeroClass.TroopCasualCost : ((_gameMode.GameType == MultiplayerGameType.Battle) ? HeroClass.TroopBattleCost : HeroClass.TroopCost));
		InitPerksList();
		int intValue = MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue();
		IsNumOfTroopsEnabled = !_gameMode.IsInWarmup && intValue > 0;
		if (IsNumOfTroopsEnabled)
		{
			NumOfTroops = MPPerkObject.GetTroopCount(heroClass, intValue, MPPerkObject.GetOnSpawnPerkHandler(_perks.Select((HeroPerkVM p) => p.SelectedPerk)));
		}
		UpdateEnabled();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = HeroClass.HeroName.ToString();
		Perks.ApplyActionOnAllItems(delegate(HeroPerkVM x)
		{
			x.RefreshValues();
		});
	}

	private void InitPerksList()
	{
		List<List<IReadOnlyPerkObject>> allPerksForHeroClass = MultiplayerClassDivisions.GetAllPerksForHeroClass(HeroClass);
		if (SelectedPerks == null)
		{
			SelectedPerks = new List<IReadOnlyPerkObject>();
		}
		else
		{
			SelectedPerks.Clear();
		}
		for (int i = 0; i < allPerksForHeroClass.Count; i++)
		{
			if (allPerksForHeroClass[i].Count > 0)
			{
				SelectedPerks.Add(allPerksForHeroClass[i][0]);
			}
			else
			{
				SelectedPerks.Add(null);
			}
		}
		if (GameNetwork.IsMyPeerReady)
		{
			MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
			int troopIndex = (component.NextSelectedTroopIndex = MultiplayerClassDivisions.GetMPHeroClasses(HeroClass.Culture).ToList().IndexOf(HeroClass));
			for (int j = 0; j < allPerksForHeroClass.Count; j++)
			{
				if (allPerksForHeroClass[j].Count > 0)
				{
					int num2 = component.GetSelectedPerkIndexWithPerkListIndex(troopIndex, j);
					if (num2 >= allPerksForHeroClass[j].Count)
					{
						num2 = 0;
					}
					IReadOnlyPerkObject value = allPerksForHeroClass[j][num2];
					SelectedPerks[j] = value;
				}
			}
		}
		MBBindingList<HeroPerkVM> mBBindingList = new MBBindingList<HeroPerkVM>();
		for (int k = 0; k < allPerksForHeroClass.Count; k++)
		{
			if (allPerksForHeroClass[k].Count > 0)
			{
				mBBindingList.Add(new HeroPerkVM(_onPerkSelect, SelectedPerks[k], allPerksForHeroClass[k], k));
			}
		}
		Perks = mBBindingList;
	}

	public void UpdateEnabled()
	{
		IsEnabled = _gameMode.IsClassAvailable(HeroClass) && (_gameMode.IsInWarmup || !_gameMode.IsGameModeUsingGold || _gameMode.GetGoldAmount() >= Gold);
	}

	[UsedImplicitly]
	public void OnSelect()
	{
		_onSelect(this);
	}
}
