using System;
using NavalDLC.CustomBattle.CustomBattle.SelectionItem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NavalDLC.CustomBattle.CustomBattle;

public class NavalCustomBattleFactionSelectionVM : ViewModel
{
	private Action<BasicCultureObject> _onSelectionChanged;

	private MBBindingList<NavalCustomBattleFactionItemVM> _factions;

	private string _selectedFactionName;

	private NavalCustomBattleFactionItemVM _selectedItem;

	[DataSourceProperty]
	public MBBindingList<NavalCustomBattleFactionItemVM> Factions
	{
		get
		{
			return _factions;
		}
		set
		{
			if (value != _factions)
			{
				_factions = value;
				OnPropertyChangedWithValue(value, "Factions");
			}
		}
	}

	[DataSourceProperty]
	public string SelectedFactionName
	{
		get
		{
			return _selectedFactionName;
		}
		set
		{
			if (value != _selectedFactionName)
			{
				_selectedFactionName = value;
				OnPropertyChangedWithValue(value, "SelectedFactionName");
			}
		}
	}

	[DataSourceProperty]
	public NavalCustomBattleFactionItemVM SelectedItem
	{
		get
		{
			return _selectedItem;
		}
		set
		{
			if (value != _selectedItem)
			{
				if (_selectedItem != null)
				{
					_selectedItem.IsSelected = false;
				}
				_selectedItem = value;
				OnPropertyChangedWithValue(value, "SelectedItem");
				if (_selectedItem != null)
				{
					_selectedItem.IsSelected = true;
				}
			}
		}
	}

	public NavalCustomBattleFactionSelectionVM(Action<BasicCultureObject> onSelectionChanged)
	{
		_onSelectionChanged = onSelectionChanged;
		Factions = new MBBindingList<NavalCustomBattleFactionItemVM>();
		foreach (BasicCultureObject faction in NavalCustomBattleData.Factions)
		{
			Factions.Add(new NavalCustomBattleFactionItemVM(faction, OnFactionSelected));
		}
		SelectFaction(0);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		SelectedFactionName = SelectedItem?.Faction.Name.ToString();
		Factions.ApplyActionOnAllItems(delegate(NavalCustomBattleFactionItemVM x)
		{
			x.RefreshValues();
		});
	}

	public void SelectFaction(int index)
	{
		if (index >= 0 && index < Factions.Count)
		{
			SelectedItem = Factions[index];
		}
	}

	public void ExecuteRandomize()
	{
		int index = MBRandom.RandomInt(Factions.Count);
		SelectFaction(index);
	}

	private void OnFactionSelected(NavalCustomBattleFactionItemVM faction)
	{
		SelectedItem = faction;
		_onSelectionChanged(faction.Faction);
		SelectedFactionName = SelectedItem.Faction.Name.ToString();
	}
}
