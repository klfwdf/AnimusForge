using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.CustomBattle.CustomBattle.SelectionItem;

public class CustomBattleFactionSelectionVM : ViewModel
{
	private Action<BasicCultureObject> _onSelectionChanged;

	private MBBindingList<FactionItemVM> _factions;

	private string _selectedFactionName;

	private FactionItemVM _selectedItem;

	[DataSourceProperty]
	public MBBindingList<FactionItemVM> Factions
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
	public FactionItemVM SelectedItem
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

	public CustomBattleFactionSelectionVM(Action<BasicCultureObject> onSelectionChanged)
	{
		_onSelectionChanged = onSelectionChanged;
		Factions = new MBBindingList<FactionItemVM>();
		foreach (BasicCultureObject faction in CustomBattleData.Factions)
		{
			Factions.Add(new FactionItemVM(faction, OnFactionSelected));
		}
		SelectFaction(0);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		SelectedFactionName = SelectedItem?.Faction.Name.ToString();
		Factions.ApplyActionOnAllItems(delegate(FactionItemVM x)
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

	private void OnFactionSelected(FactionItemVM faction)
	{
		SelectedItem = faction;
		_onSelectionChanged(faction.Faction);
		SelectedFactionName = SelectedItem.Faction.Name.ToString();
	}
}
