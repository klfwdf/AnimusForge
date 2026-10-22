using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.HostGame.HostGameOptions;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.HostGame;

public class HostGameOptionSectionVM : ViewModel
{
	private string _title;

	private bool _isExpanded;

	private bool _hasAnyVisibleOption;

	private MBBindingList<GenericHostGameOptionDataVM> _options;

	public HostGameOptionSection Section { get; }

	[DataSourceProperty]
	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			if (value != _title)
			{
				_title = value;
				OnPropertyChangedWithValue(value, "Title");
			}
		}
	}

	[DataSourceProperty]
	public bool IsExpanded
	{
		get
		{
			return _isExpanded;
		}
		set
		{
			if (value != _isExpanded)
			{
				_isExpanded = value;
				OnPropertyChangedWithValue(value, "IsExpanded");
			}
		}
	}

	[DataSourceProperty]
	public bool HasAnyVisibleOption
	{
		get
		{
			return _hasAnyVisibleOption;
		}
		set
		{
			if (value != _hasAnyVisibleOption)
			{
				_hasAnyVisibleOption = value;
				OnPropertyChangedWithValue(value, "HasAnyVisibleOption");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<GenericHostGameOptionDataVM> Options
	{
		get
		{
			return _options;
		}
		set
		{
			if (value != _options)
			{
				_options = value;
				OnPropertyChangedWithValue(value, "Options");
			}
		}
	}

	public HostGameOptionSectionVM(HostGameOptionSection section, bool isExpanded)
	{
		Section = section;
		Options = new MBBindingList<GenericHostGameOptionDataVM>();
		IsExpanded = isExpanded;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Title = GameTexts.FindText("str_multiplayer_host_game_section", Section.ToString()).ToString();
		Options.ApplyActionOnAllItems(delegate(GenericHostGameOptionDataVM x)
		{
			x.RefreshValues();
		});
	}

	public void ExecuteToggle()
	{
		IsExpanded = !IsExpanded;
	}

	public void RefreshHasAnyVisibleOption()
	{
		bool hasAnyVisibleOption = false;
		for (int i = 0; i < Options.Count; i++)
		{
			if (Options[i].IsVisible)
			{
				hasAnyVisibleOption = true;
				break;
			}
		}
		HasAnyVisibleOption = hasAnyVisibleOption;
	}
}
