using System;
using System.Collections.Generic;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.ViewModelCollection.OrderOfBattle;

public class NavalOrderOfBattleShipItemVM : ViewModel
{
	public readonly IShipOrigin ShipOrigin;

	public MissionShip MissionShip;

	private readonly Action<NavalOrderOfBattleShipItemVM, bool> _onSelected;

	private readonly Func<NavalOrderOfBattleShipItemVM, NavalOrderOfBattleFormationItemVM> _findFormationOfShip;

	private List<TooltipProperty> _cachedTooltipProperties;

	private bool _isDisabled;

	private bool _isSelected;

	private bool _isFlagship;

	private string _prefabId;

	private string _shipName;

	private float _healthRatio;

	private string _healthPercentageAsString;

	private int _mainDeckCrewCount;

	private int _reserveCrewCount;

	private int _mainDeckCrewCapacity;

	private string _crewCountAsString;

	private string _reserveCrewCountAsString;

	private float _mainDeckCrewRatio;

	private float _totalCrewRatio;

	private BasicTooltipViewModel _tooltip;

	[DataSourceProperty]
	public bool IsDisabled
	{
		get
		{
			return _isDisabled;
		}
		set
		{
			if (value != _isDisabled)
			{
				_isDisabled = value;
				OnPropertyChangedWithValue(value, "IsDisabled");
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
	public bool IsFlagship
	{
		get
		{
			return _isFlagship;
		}
		set
		{
			if (value != _isFlagship)
			{
				_isFlagship = value;
				OnPropertyChangedWithValue(value, "IsFlagship");
			}
		}
	}

	[DataSourceProperty]
	public string PrefabId
	{
		get
		{
			return _prefabId;
		}
		set
		{
			if (value != _prefabId)
			{
				_prefabId = value;
				OnPropertyChangedWithValue(value, "PrefabId");
			}
		}
	}

	[DataSourceProperty]
	public string ShipName
	{
		get
		{
			return _shipName;
		}
		set
		{
			if (value != _shipName)
			{
				_shipName = value;
				OnPropertyChangedWithValue(value, "ShipName");
			}
		}
	}

	[DataSourceProperty]
	public float HealthRatio
	{
		get
		{
			return _healthRatio;
		}
		set
		{
			if (value != _healthRatio)
			{
				_healthRatio = value;
				OnPropertyChangedWithValue(value, "HealthRatio");
			}
		}
	}

	[DataSourceProperty]
	public string HealthPercentageAsString
	{
		get
		{
			return _healthPercentageAsString;
		}
		set
		{
			if (value != _healthPercentageAsString)
			{
				_healthPercentageAsString = value;
				OnPropertyChangedWithValue(value, "HealthPercentageAsString");
			}
		}
	}

	[DataSourceProperty]
	public int MainDeckCrewCount
	{
		get
		{
			return _mainDeckCrewCount;
		}
		set
		{
			if (value != _mainDeckCrewCount)
			{
				_mainDeckCrewCount = value;
				OnPropertyChangedWithValue(value, "MainDeckCrewCount");
			}
		}
	}

	[DataSourceProperty]
	public int ReserveCrewCount
	{
		get
		{
			return _reserveCrewCount;
		}
		set
		{
			if (value != _reserveCrewCount)
			{
				_reserveCrewCount = value;
				OnPropertyChangedWithValue(value, "ReserveCrewCount");
			}
		}
	}

	[DataSourceProperty]
	public int MainDeckCrewCapacity
	{
		get
		{
			return _mainDeckCrewCapacity;
		}
		set
		{
			if (value != _mainDeckCrewCapacity)
			{
				_mainDeckCrewCapacity = value;
				OnPropertyChangedWithValue(value, "MainDeckCrewCapacity");
			}
		}
	}

	[DataSourceProperty]
	public string CrewCountAsString
	{
		get
		{
			return _crewCountAsString;
		}
		set
		{
			if (value != _crewCountAsString)
			{
				_crewCountAsString = value;
				OnPropertyChangedWithValue(value, "CrewCountAsString");
			}
		}
	}

	[DataSourceProperty]
	public string ReserveCrewCountAsString
	{
		get
		{
			return _reserveCrewCountAsString;
		}
		set
		{
			if (value != _reserveCrewCountAsString)
			{
				_reserveCrewCountAsString = value;
				OnPropertyChangedWithValue(value, "ReserveCrewCountAsString");
			}
		}
	}

	[DataSourceProperty]
	public float MainDeckCrewRatio
	{
		get
		{
			return _mainDeckCrewRatio;
		}
		set
		{
			if (value != _mainDeckCrewRatio)
			{
				_mainDeckCrewRatio = value;
				OnPropertyChangedWithValue(value, "MainDeckCrewRatio");
			}
		}
	}

	[DataSourceProperty]
	public float TotalCrewRatio
	{
		get
		{
			return _totalCrewRatio;
		}
		set
		{
			if (value != _totalCrewRatio)
			{
				_totalCrewRatio = value;
				OnPropertyChangedWithValue(value, "TotalCrewRatio");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel Tooltip
	{
		get
		{
			return _tooltip;
		}
		set
		{
			if (value != _tooltip)
			{
				_tooltip = value;
				OnPropertyChangedWithValue(value, "Tooltip");
			}
		}
	}

	public NavalOrderOfBattleShipItemVM(IShipOrigin shipOrigin, Action<NavalOrderOfBattleShipItemVM, bool> onSelected, Func<NavalOrderOfBattleShipItemVM, NavalOrderOfBattleFormationItemVM> findFormationOfShip)
	{
		_onSelected = onSelected;
		_findFormationOfShip = findFormationOfShip;
		ShipOrigin = shipOrigin;
		PrefabId = NavalUIHelper.GetPrefabIdOfShipHull(shipOrigin.Hull);
		IsFlagship = ShipOrigin is Ship ship && ship == NavalUIHelper.GetFlagship(ship.Owner);
		Tooltip = new BasicTooltipViewModel(() => _cachedTooltipProperties);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ShipName = ShipOrigin.Name.ToString();
		if (MissionShip != null)
		{
			HealthRatio = MissionShip.HitPoints / MissionShip.MaxHealth;
			MainDeckCrewCount = MissionShip.Formation.CountOfUnits;
			ReserveCrewCount = Mission.Current.GetMissionBehavior<NavalAgentsLogic>().GetReservedTroopsCountOfShip(MissionShip);
			MainDeckCrewCapacity = MissionShip.CrewSizeOnMainDeck;
			MainDeckCrewRatio = (float)MainDeckCrewCount / (float)(MainDeckCrewCapacity + ReserveCrewCount);
			TotalCrewRatio = (float)(MainDeckCrewCount + ReserveCrewCount) / (float)(MainDeckCrewCapacity + ReserveCrewCount);
		}
		else
		{
			HealthRatio = ShipOrigin.HitPoints / ShipOrigin.MaxHitPoints;
			MainDeckCrewCount = 0;
			ReserveCrewCount = 0;
			MainDeckCrewCapacity = ShipOrigin.MainDeckCrewCapacity;
			MainDeckCrewRatio = 0f;
			TotalCrewRatio = 0f;
		}
		HealthPercentageAsString = new TextObject("{=gYATKZJp}{NUMBER}%").SetTextVariable("NUMBER", ((int)(HealthRatio * 100f)).ToString()).ToString();
		CrewCountAsString = GameTexts.FindText("str_LEFT_over_RIGHT_no_space").SetTextVariable("LEFT", MainDeckCrewCount).SetTextVariable("RIGHT", MainDeckCrewCapacity)
			.ToString();
		if (ReserveCrewCount > 0)
		{
			string variable = GameTexts.FindText("str_plus_with_number").SetTextVariable("NUMBER", ReserveCrewCount).ToString();
			ReserveCrewCountAsString = GameTexts.FindText("str_STR_in_parentheses").SetTextVariable("STR", variable).ToString();
		}
		else
		{
			ReserveCrewCountAsString = string.Empty;
		}
		_cachedTooltipProperties = GetTooltip();
	}

	public void ExecuteSelect()
	{
		if (!IsDisabled)
		{
			_onSelected?.Invoke(this, arg2: true);
		}
	}

	public void ExecuteToggleSelect()
	{
		if (!IsDisabled)
		{
			_onSelected?.Invoke(this, !IsSelected);
		}
	}

	public void ExecuteDeselect()
	{
		if (!IsDisabled)
		{
			_onSelected?.Invoke(this, arg2: false);
		}
	}

	private List<TooltipProperty> GetTooltip()
	{
		List<TooltipProperty> list = new List<TooltipProperty>
		{
			new TooltipProperty(ShipName, string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title)
		};
		if (IsDisabled)
		{
			list.Add(new TooltipProperty(string.Empty, new TextObject("{=cIpPMkry}You can only change your formation's ship when you are not the general.").ToString(), 0));
			list.Add(new TooltipProperty(string.Empty, string.Empty, 0));
		}
		if (ShipOrigin is Ship ship)
		{
			list.Add(new TooltipProperty(GameTexts.FindText("str_owner").ToString(), ship.Owner.Name.ToString(), 0));
			list.Add(new TooltipProperty(new TextObject("{=wEmx6fZi}Hull").ToString(), ship.ShipHull.Name.ToString(), 0));
		}
		list.Add(new TooltipProperty(new TextObject("{=sqdzHOPe}Class").ToString(), GameTexts.FindText("str_ship_type", ShipOrigin.Hull.Type.ToString().ToLowerInvariant()).ToString(), 0));
		if (MissionShip == null)
		{
			string value = GameTexts.FindText("str_LEFT_over_RIGHT_no_space").SetTextVariable("LEFT", (int)ShipOrigin.HitPoints).SetTextVariable("RIGHT", (int)ShipOrigin.MaxHitPoints)
				.ToString();
			list.Add(new TooltipProperty(new TextObject("{=oBbiVeKE}Hit Points").ToString(), value, 0));
			list.Add(new TooltipProperty(new TextObject("{=TrbfOCyF}Main Deck Crew Capacity").ToString(), ShipOrigin.MainDeckCrewCapacity.ToString(), 0));
			int num = ShipOrigin.TotalCrewCapacity - ShipOrigin.MainDeckCrewCapacity;
			if (num > 0)
			{
				list.Add(new TooltipProperty(new TextObject("{=saS6Sub2}Reserve Crew Capacity").ToString(), num.ToString(), 0));
			}
		}
		else
		{
			string value2 = GameTexts.FindText("str_LEFT_over_RIGHT_no_space").SetTextVariable("LEFT", (int)MissionShip.HitPoints).SetTextVariable("RIGHT", (int)MissionShip.MaxHealth)
				.ToString();
			list.Add(new TooltipProperty(new TextObject("{=oBbiVeKE}Hit Points").ToString(), value2, 0));
			list.Add(new TooltipProperty(new TextObject("{=LfOIa8eh}Troops On Deck").ToString(), CrewCountAsString, 0));
			if (ReserveCrewCount > 0)
			{
				string value3 = GameTexts.FindText("str_LEFT_over_RIGHT_no_space").SetTextVariable("LEFT", ReserveCrewCount).SetTextVariable("RIGHT", MissionShip.CrewSizeOnLowerDeck)
					.ToString();
				list.Add(new TooltipProperty(new TextObject("{=25fleLuY}Troops In Reserve").ToString(), value3, 0));
			}
		}
		List<ShipSlotAndPieceName> shipSlotAndPieceNames = ShipOrigin.GetShipSlotAndPieceNames();
		if (shipSlotAndPieceNames.Count > 0)
		{
			list.Add(new TooltipProperty(string.Empty, string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator)
			{
				OnlyShowWhenExtended = true
			});
			list.Add(new TooltipProperty(string.Empty, new TextObject("{=zMvUzdKR}Ship Upgrades").ToString(), -1)
			{
				OnlyShowWhenExtended = true
			});
			foreach (ShipSlotAndPieceName item in shipSlotAndPieceNames)
			{
				list.Add(new TooltipProperty(item.SlotName, item.PieceName, 0)
				{
					OnlyShowWhenExtended = true
				});
			}
		}
		if (shipSlotAndPieceNames.Count > 0)
		{
			if (Input.IsGamepadActive)
			{
				GameTexts.SetVariable("EXTEND_KEY", Game.Current.GameTextManager.GetHotKeyGameText("MapHotKeyCategory", "MapFollowModifier").ToString());
			}
			else
			{
				GameTexts.SetVariable("EXTEND_KEY", Game.Current.GameTextManager.FindText("str_game_key_text", "anyalt").ToString());
			}
			list.Add(new TooltipProperty(string.Empty, string.Empty, 0)
			{
				OnlyShowWhenNotExtended = true
			});
			list.Add(new TooltipProperty(string.Empty, GameTexts.FindText("str_map_tooltip_info").ToString(), -1)
			{
				OnlyShowWhenNotExtended = true
			});
		}
		return list;
	}

	public bool GetCanBeUnassignedOrMoved()
	{
		if (!IsDisabled)
		{
			Func<NavalOrderOfBattleShipItemVM, NavalOrderOfBattleFormationItemVM> findFormationOfShip = _findFormationOfShip;
			if (findFormationOfShip == null)
			{
				return true;
			}
			return findFormationOfShip(this)?.Captain?.IsMainHero != true;
		}
		return false;
	}
}
