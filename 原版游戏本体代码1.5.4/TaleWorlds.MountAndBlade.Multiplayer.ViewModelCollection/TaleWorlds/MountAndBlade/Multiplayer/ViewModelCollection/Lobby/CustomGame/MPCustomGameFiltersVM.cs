using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Diamond.Lobby;
using TaleWorlds.MountAndBlade.Diamond.Lobby.LocalData;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.CustomGame;

public class MPCustomGameFiltersVM : ViewModel
{
	public enum CustomGameFilterType
	{
		Name,
		NotFull,
		HasPlayers,
		HasPasswordProtection,
		IsOfficial,
		ModuleCompatible,
		Favorite
	}

	public Action OnFiltersApplied;

	private string _titleText;

	private string _searchInitialText;

	private string _searchText;

	private MBBindingList<MPCustomGameFilterItemVM> _items;

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
	public string SearchInitialText
	{
		get
		{
			return _searchInitialText;
		}
		set
		{
			if (value != _searchInitialText)
			{
				_searchInitialText = value;
				OnPropertyChangedWithValue(value, "SearchInitialText");
			}
		}
	}

	[DataSourceProperty]
	public string SearchText
	{
		get
		{
			return _searchText;
		}
		set
		{
			if (value != _searchText)
			{
				_searchText = value;
				OnPropertyChangedWithValue(value, "SearchText");
				OnAnyFilterChange();
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPCustomGameFilterItemVM> Items
	{
		get
		{
			return _items;
		}
		set
		{
			if (value != _items)
			{
				_items = value;
				OnPropertyChangedWithValue(value, "Items");
			}
		}
	}

	public MPCustomGameFiltersVM()
	{
		SearchText = string.Empty;
		Items = new MBBindingList<MPCustomGameFilterItemVM>
		{
			new MPCustomGameFilterItemVM(CustomGameFilterType.IsOfficial, new TextObject("{=Tlc2buKG}Is Official"), (GameServerEntry x) => x.IsOfficial, OnAnyFilterChange),
			new MPCustomGameFilterItemVM(CustomGameFilterType.HasPlayers, new TextObject("{=aB4Md0if}Has players"), (GameServerEntry x) => x.PlayerCount > 0, OnAnyFilterChange),
			new MPCustomGameFilterItemVM(CustomGameFilterType.HasPasswordProtection, new TextObject("{=v6J8ILV3}No password"), (GameServerEntry x) => !x.PasswordProtected, OnAnyFilterChange),
			new MPCustomGameFilterItemVM(CustomGameFilterType.NotFull, new TextObject("{=W4DLzPSb}Server not full"), (GameServerEntry x) => x.MaxPlayerCount - x.PlayerCount > 0, OnAnyFilterChange),
			new MPCustomGameFilterItemVM(CustomGameFilterType.ModuleCompatible, new TextObject("{=CNR4cZwZ}Modules compatible"), FilterByCompatibleModules, OnAnyFilterChange),
			new MPCustomGameFilterItemVM(CustomGameFilterType.Favorite, new TextObject("{=BDdVhfuJ}Favorite"), FilterByFavorites, OnAnyFilterChange)
		};
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = new TextObject("{=OwqFpPwa}Filters").ToString();
		SearchInitialText = new TextObject("{=NLKmdNbt}Search").ToString();
		Items.ApplyActionOnAllItems(delegate(MPCustomGameFilterItemVM x)
		{
			x.RefreshValues();
		});
	}

	public List<GameServerEntry> GetFilteredServerList(IEnumerable<GameServerEntry> unfilteredList)
	{
		List<GameServerEntry> list = unfilteredList.ToList();
		IEnumerable<MPCustomGameFilterItemVM> enabledFilterItems = Items.Where((MPCustomGameFilterItemVM filterItem) => filterItem.IsSelected);
		if (enabledFilterItems.Any())
		{
			list.RemoveAll((GameServerEntry s) => enabledFilterItems.Any((MPCustomGameFilterItemVM fi) => !fi.GetIsApplicaple(s)));
		}
		if (!string.IsNullOrEmpty(SearchText))
		{
			list = list.Where((GameServerEntry i) => i.ServerName.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
		}
		return list;
	}

	private bool FilterByCompatibleModules(GameServerEntry serverEntry)
	{
		return NetworkMain.GameClient.LoadedUnofficialModules.IsCompatibleWith(serverEntry.LoadedModules, serverEntry.AllowsOptionalModules);
	}

	private bool FilterByFavorites(GameServerEntry serverEntry)
	{
		FavoriteServerData favoriteServerData;
		return MultiplayerLocalDataManager.Instance.FavoriteServers.TryGetServerData(serverEntry, out favoriteServerData);
	}

	private void OnAnyFilterChange()
	{
		OnFiltersApplied?.Invoke();
	}
}
