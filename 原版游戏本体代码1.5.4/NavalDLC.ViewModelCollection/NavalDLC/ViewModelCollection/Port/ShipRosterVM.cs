using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.ViewModelCollection.Port;

public class ShipRosterVM : ViewModel
{
	private class PortShipVMComparer : IComparer<ShipItemVM>
	{
		private readonly MBReadOnlyList<Ship> _orderedShipsList;

		public PortShipVMComparer(MBReadOnlyList<Ship> orderedShipsList)
		{
			_orderedShipsList = orderedShipsList;
		}

		public int Compare(ShipItemVM x, ShipItemVM y)
		{
			int num = _orderedShipsList.IndexOf(x.Ship);
			int value = _orderedShipsList.IndexOf(y.Ship);
			return num.CompareTo(value);
		}
	}

	private TextObject _rosterName;

	private readonly Action _onSelected;

	private bool _hasAnyShips;

	private bool _hasMultipleShips;

	private bool _hasOwnerCharacter;

	private bool _isSelected;

	private bool _isTownShipyard;

	private int _townShipyardLevel;

	private string _name;

	private string _hasNoShipsText;

	private string _shipCountText;

	private string _weightText;

	private string _troopCountText;

	private bool _isWeightDangerous;

	private bool _isTroopCountDangerous;

	private MBBindingList<ShipItemVM> _ships;

	private CharacterImageIdentifierVM _ownerVisual;

	private HintViewModel _tooltip;

	public PartyBase Owner { get; private set; }

	[DataSourceProperty]
	public bool HasAnyShips
	{
		get
		{
			return _hasAnyShips;
		}
		set
		{
			if (value != _hasAnyShips)
			{
				_hasAnyShips = value;
				OnPropertyChangedWithValue(value, "HasAnyShips");
			}
		}
	}

	[DataSourceProperty]
	public bool HasMultipleShips
	{
		get
		{
			return _hasMultipleShips;
		}
		set
		{
			if (value != _hasMultipleShips)
			{
				_hasMultipleShips = value;
				OnPropertyChangedWithValue(value, "HasMultipleShips");
			}
		}
	}

	[DataSourceProperty]
	public bool HasOwnerCharacter
	{
		get
		{
			return _hasOwnerCharacter;
		}
		set
		{
			if (value != _hasOwnerCharacter)
			{
				_hasOwnerCharacter = value;
				OnPropertyChangedWithValue(value, "HasOwnerCharacter");
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
	public bool IsTownShipyard
	{
		get
		{
			return _isTownShipyard;
		}
		set
		{
			if (value != _isTownShipyard)
			{
				_isTownShipyard = value;
				OnPropertyChangedWithValue(value, "IsTownShipyard");
			}
		}
	}

	[DataSourceProperty]
	public int TownShipyardLevel
	{
		get
		{
			return _townShipyardLevel;
		}
		set
		{
			if (value != _townShipyardLevel)
			{
				_townShipyardLevel = value;
				OnPropertyChangedWithValue(value, "TownShipyardLevel");
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
	public string HasNoShipsText
	{
		get
		{
			return _hasNoShipsText;
		}
		set
		{
			if (value != _hasNoShipsText)
			{
				_hasNoShipsText = value;
				OnPropertyChangedWithValue(value, "HasNoShipsText");
			}
		}
	}

	[DataSourceProperty]
	public string ShipCountText
	{
		get
		{
			return _shipCountText;
		}
		set
		{
			if (value != _shipCountText)
			{
				_shipCountText = value;
				OnPropertyChangedWithValue(value, "ShipCountText");
			}
		}
	}

	[DataSourceProperty]
	public string WeightText
	{
		get
		{
			return _weightText;
		}
		set
		{
			if (value != _weightText)
			{
				_weightText = value;
				OnPropertyChangedWithValue(value, "WeightText");
			}
		}
	}

	[DataSourceProperty]
	public string TroopCountText
	{
		get
		{
			return _troopCountText;
		}
		set
		{
			if (value != _troopCountText)
			{
				_troopCountText = value;
				OnPropertyChangedWithValue(value, "TroopCountText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsWeightDangerous
	{
		get
		{
			return _isWeightDangerous;
		}
		set
		{
			if (value != _isWeightDangerous)
			{
				_isWeightDangerous = value;
				OnPropertyChangedWithValue(value, "IsWeightDangerous");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTroopCountDangerous
	{
		get
		{
			return _isTroopCountDangerous;
		}
		set
		{
			if (value != _isTroopCountDangerous)
			{
				_isTroopCountDangerous = value;
				OnPropertyChangedWithValue(value, "IsTroopCountDangerous");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ShipItemVM> Ships
	{
		get
		{
			return _ships;
		}
		set
		{
			if (value != _ships)
			{
				_ships = value;
				OnPropertyChangedWithValue(value, "Ships");
			}
		}
	}

	[DataSourceProperty]
	public CharacterImageIdentifierVM OwnerCharacterVisual
	{
		get
		{
			return _ownerVisual;
		}
		set
		{
			if (value != _ownerVisual)
			{
				_ownerVisual = value;
				OnPropertyChangedWithValue(value, "OwnerCharacterVisual");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel Tooltip
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

	public ShipRosterVM(Action onSelected)
	{
		_onSelected = onSelected;
		Ships = new MBBindingList<ShipItemVM>();
		Tooltip = new HintViewModel();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = _rosterName?.ToString();
		HasNoShipsText = new TextObject("{=vfXHD89T}No ships available").ToString();
		ShipCountText = new TextObject("{=nx9Pk1ca}{AMOUNT} {?AMOUNT==1}ship{?}ships{\\?}").SetTextVariable("AMOUNT", Ships.Count).ToString();
		if (HasOwnerCharacter)
		{
			float num = (Owner.IsMobile ? Campaign.Current.Models.InventoryCapacityModel.CalculateTotalWeightCarried(Owner.MobileParty, isCurrentlyAtSea: true).ResultNumber : 0f);
			float num2 = ((Owner.IsMobile && HasAnyShips) ? Campaign.Current.Models.InventoryCapacityModel.CalculateInventoryCapacity(Owner.MobileParty, isCurrentlyAtSea: true).ResultNumber : _ships.Sum((ShipItemVM x) => x.Ship.InventoryCapacity));
			WeightText = GameTexts.FindText("str_LEFT_over_RIGHT_no_space").SetTextVariable("LEFT", (int)num).SetTextVariable("RIGHT", (int)num2)
				.ToString();
			IsWeightDangerous = num > num2;
			int numberOfAllMembers = Owner.NumberOfAllMembers;
			int num3 = _ships.Sum((ShipItemVM x) => x.Ship.TotalCrewCapacity);
			TroopCountText = GameTexts.FindText("str_LEFT_over_RIGHT_no_space").SetTextVariable("LEFT", numberOfAllMembers).SetTextVariable("RIGHT", num3)
				.ToString();
			IsTroopCountDangerous = numberOfAllMembers > num3;
		}
		else
		{
			WeightText = string.Empty;
			TroopCountText = string.Empty;
			IsWeightDangerous = false;
			IsTroopCountDangerous = false;
		}
		if (!HasAnyShips)
		{
			Tooltip.HintText = new TextObject("{=vfXHD89T}No ships available");
		}
		else if (IsTroopCountDangerous)
		{
			Tooltip.HintText = new TextObject("{=LPUWr7J1}Over the troop limit, sailing speed will be negatively affected!");
		}
		else if (IsWeightDangerous)
		{
			Tooltip.HintText = new TextObject("{=qSRbt9qc}Over the carrying limit, sailing speed will be negatively affected!");
		}
		else
		{
			Tooltip.HintText = null;
		}
		Ships.ApplyActionOnAllItems(delegate(ShipItemVM s)
		{
			s.RefreshValues();
		});
	}

	public void SetRosterName(TextObject rosterName)
	{
		_rosterName = rosterName;
		RefreshValues();
	}

	public void SetRosterOwner(PartyBase owner)
	{
		Owner = owner;
		HasOwnerCharacter = Owner != null && Owner.LeaderHero != null;
		PartyBase owner2 = Owner;
		IsTownShipyard = owner2 != null && owner2.IsSettlement && Owner.Settlement.HasPort;
		TownShipyardLevel = (IsTownShipyard ? (Owner.Settlement.Town?.GetShipyard()?.CurrentLevel ?? 0) : 0);
		OwnerCharacterVisual?.OnFinalize();
		if (HasOwnerCharacter)
		{
			OwnerCharacterVisual = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(Owner.LeaderHero.CharacterObject));
		}
		else
		{
			OwnerCharacterVisual = null;
		}
		RefreshValues();
	}

	public void RefreshShips(MBReadOnlyList<ShipItemVM> removedShips, MBReadOnlyList<ShipItemVM> addedShips, MBReadOnlyList<Ship> orderedShipsList)
	{
		for (int i = 0; i < removedShips.Count; i++)
		{
			Ships.Remove(removedShips[i]);
		}
		for (int j = 0; j < addedShips.Count; j++)
		{
			Ships.Add(addedShips[j]);
		}
		Ships.Sort(new PortShipVMComparer(orderedShipsList));
		HasAnyShips = Ships.Count > 0;
		HasMultipleShips = Ships.Count > 1;
		RefreshValues();
	}

	public void ExecuteSelectRoster()
	{
		_onSelected?.Invoke();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		foreach (ShipItemVM ship in Ships)
		{
			ship.OnFinalize();
		}
		Ships.Clear();
		OwnerCharacterVisual?.OnFinalize();
		OwnerCharacterVisual = null;
	}
}
