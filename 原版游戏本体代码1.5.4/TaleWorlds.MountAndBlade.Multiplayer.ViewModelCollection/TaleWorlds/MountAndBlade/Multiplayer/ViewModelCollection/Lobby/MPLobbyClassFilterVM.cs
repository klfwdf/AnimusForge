using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.ClassFilter;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby;

public class MPLobbyClassFilterVM : ViewModel
{
	private Action<MPLobbyClassFilterClassItemVM, bool> _onSelectionChange;

	private string _titleText;

	private MBBindingList<MPLobbyClassFilterFactionItemVM> _factions;

	private MBBindingList<MPLobbyClassFilterClassGroupItemVM> _activeClassGroups;

	public MPLobbyClassFilterClassItemVM SelectedClassItem { get; private set; }

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPLobbyClassFilterFactionItemVM> Factions
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
	public MBBindingList<MPLobbyClassFilterClassGroupItemVM> ActiveClassGroups
	{
		get
		{
			return _activeClassGroups;
		}
		set
		{
			if (value != _activeClassGroups)
			{
				_activeClassGroups = value;
				OnPropertyChangedWithValue(value, "ActiveClassGroups");
			}
		}
	}

	public MPLobbyClassFilterVM(Action<MPLobbyClassFilterClassItemVM, bool> onSelectionChange)
	{
		_onSelectionChange = onSelectionChange;
		Factions = new MBBindingList<MPLobbyClassFilterFactionItemVM>();
		Factions.Add(new MPLobbyClassFilterFactionItemVM("empire", isEnabled: true, OnFactionFilterChanged, OnSelectionChange));
		Factions.Add(new MPLobbyClassFilterFactionItemVM("vlandia", isEnabled: true, OnFactionFilterChanged, OnSelectionChange));
		Factions.Add(new MPLobbyClassFilterFactionItemVM("battania", isEnabled: true, OnFactionFilterChanged, OnSelectionChange));
		Factions.Add(new MPLobbyClassFilterFactionItemVM("sturgia", isEnabled: true, OnFactionFilterChanged, OnSelectionChange));
		Factions.Add(new MPLobbyClassFilterFactionItemVM("khuzait", isEnabled: true, OnFactionFilterChanged, OnSelectionChange));
		Factions.Add(new MPLobbyClassFilterFactionItemVM("aserai", isEnabled: true, OnFactionFilterChanged, OnSelectionChange));
		ActiveClassGroups = new MBBindingList<MPLobbyClassFilterClassGroupItemVM>();
		Factions[0].IsActive = true;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = new TextObject("{=Q50X65NB}Classes").ToString();
		Factions.ApplyActionOnAllItems(delegate(MPLobbyClassFilterFactionItemVM x)
		{
			x.RefreshValues();
		});
		ActiveClassGroups.ApplyActionOnAllItems(delegate(MPLobbyClassFilterClassGroupItemVM x)
		{
			x.RefreshValues();
		});
	}

	private void OnFactionFilterChanged(MPLobbyClassFilterFactionItemVM factionItemVm)
	{
		ActiveClassGroups = factionItemVm.ClassGroups;
		OnSelectionChange(factionItemVm.SelectedClassItem);
	}

	private void OnSelectionChange(MPLobbyClassFilterClassItemVM selectedItemVm)
	{
		SelectedClassItem = selectedItemVm;
		_onSelectionChange?.Invoke(selectedItemVm, arg2: false);
	}
}
