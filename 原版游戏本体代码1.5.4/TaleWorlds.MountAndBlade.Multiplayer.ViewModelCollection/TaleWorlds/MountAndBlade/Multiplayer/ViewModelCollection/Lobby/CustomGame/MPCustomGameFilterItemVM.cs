using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.CustomGame;

public class MPCustomGameFilterItemVM : ViewModel
{
	public readonly Func<GameServerEntry, bool> GetIsApplicaple;

	public readonly Action _onSelectionChange;

	private readonly TextObject _descriptionObj;

	private MPCustomGameFiltersVM.CustomGameFilterType _filterType;

	private bool _isSelected;

	private string _description;

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
				OnToggled();
			}
		}
	}

	[DataSourceProperty]
	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			if (value != _description)
			{
				_description = value;
				OnPropertyChangedWithValue(value, "Description");
			}
		}
	}

	public MPCustomGameFilterItemVM(MPCustomGameFiltersVM.CustomGameFilterType filterType, TextObject description, Func<GameServerEntry, bool> getFilterApplicaple, Action onSelectionChange)
	{
		_filterType = filterType;
		_descriptionObj = description;
		GetIsApplicaple = getFilterApplicaple;
		_onSelectionChange = onSelectionChange;
		SetInitialState();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Description = _descriptionObj.ToString();
	}

	private void SetInitialState()
	{
		switch (_filterType)
		{
		case MPCustomGameFiltersVM.CustomGameFilterType.NotFull:
			IsSelected = BannerlordConfig.HideFullServers;
			break;
		case MPCustomGameFiltersVM.CustomGameFilterType.HasPlayers:
			IsSelected = BannerlordConfig.HideEmptyServers;
			break;
		case MPCustomGameFiltersVM.CustomGameFilterType.HasPasswordProtection:
			IsSelected = BannerlordConfig.HidePasswordProtectedServers;
			break;
		case MPCustomGameFiltersVM.CustomGameFilterType.IsOfficial:
			IsSelected = BannerlordConfig.HideUnofficialServers;
			break;
		case MPCustomGameFiltersVM.CustomGameFilterType.ModuleCompatible:
			IsSelected = BannerlordConfig.HideModuleIncompatibleServers;
			break;
		case MPCustomGameFiltersVM.CustomGameFilterType.Favorite:
			IsSelected = BannerlordConfig.ShowOnlyFavoriteServers;
			break;
		}
	}

	private void OnToggled()
	{
		_onSelectionChange();
		switch (_filterType)
		{
		case MPCustomGameFiltersVM.CustomGameFilterType.NotFull:
			BannerlordConfig.HideFullServers = IsSelected;
			break;
		case MPCustomGameFiltersVM.CustomGameFilterType.HasPlayers:
			BannerlordConfig.HideEmptyServers = IsSelected;
			break;
		case MPCustomGameFiltersVM.CustomGameFilterType.HasPasswordProtection:
			BannerlordConfig.HidePasswordProtectedServers = IsSelected;
			break;
		case MPCustomGameFiltersVM.CustomGameFilterType.IsOfficial:
			BannerlordConfig.HideUnofficialServers = IsSelected;
			break;
		case MPCustomGameFiltersVM.CustomGameFilterType.ModuleCompatible:
			BannerlordConfig.HideModuleIncompatibleServers = IsSelected;
			break;
		}
	}
}
