using System.Collections.Generic;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TournamentLeaderboard;

public class TournamentLeaderboardVM : ViewModel
{
	private InputKeyItemVM _doneInputKey;

	private bool _isEnabled;

	private string _doneText;

	private string _heroText;

	private string _victoriesText;

	private string _rankText;

	private string _titleText;

	private MBBindingList<TournamentLeaderboardEntryItemVM> _entries;

	private TournamentLeaderboardSortControllerVM _sortController;

	[DataSourceProperty]
	public InputKeyItemVM DoneInputKey
	{
		get
		{
			return _doneInputKey;
		}
		set
		{
			if (value != _doneInputKey)
			{
				_doneInputKey = value;
				OnPropertyChangedWithValue(value, "DoneInputKey");
			}
		}
	}

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
	public TournamentLeaderboardSortControllerVM SortController
	{
		get
		{
			return _sortController;
		}
		set
		{
			if (value != _sortController)
			{
				_sortController = value;
				OnPropertyChangedWithValue(value, "SortController");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<TournamentLeaderboardEntryItemVM> Entries
	{
		get
		{
			return _entries;
		}
		set
		{
			if (value != _entries)
			{
				_entries = value;
				OnPropertyChangedWithValue(value, "Entries");
			}
		}
	}

	[DataSourceProperty]
	public string DoneText
	{
		get
		{
			return _doneText;
		}
		set
		{
			if (value != _doneText)
			{
				_doneText = value;
				OnPropertyChangedWithValue(value, "DoneText");
			}
		}
	}

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
	public string HeroText
	{
		get
		{
			return _heroText;
		}
		set
		{
			if (value != _heroText)
			{
				_heroText = value;
				OnPropertyChangedWithValue(value, "HeroText");
			}
		}
	}

	[DataSourceProperty]
	public string VictoriesText
	{
		get
		{
			return _victoriesText;
		}
		set
		{
			if (value != _victoriesText)
			{
				_victoriesText = value;
				OnPropertyChangedWithValue(value, "VictoriesText");
			}
		}
	}

	[DataSourceProperty]
	public string RankText
	{
		get
		{
			return _rankText;
		}
		set
		{
			if (value != _rankText)
			{
				_rankText = value;
				OnPropertyChangedWithValue(value, "RankText");
			}
		}
	}

	public TournamentLeaderboardVM()
	{
		Entries = new MBBindingList<TournamentLeaderboardEntryItemVM>();
		List<KeyValuePair<Hero, int>> leaderboard = Campaign.Current.TournamentManager.GetLeaderboard();
		for (int i = 0; i < leaderboard.Count; i++)
		{
			Entries.Add(new TournamentLeaderboardEntryItemVM(leaderboard[i].Key, leaderboard[i].Value, i + 1));
		}
		SortController = new TournamentLeaderboardSortControllerVM(ref _entries);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		DoneText = GameTexts.FindText("str_done").ToString();
		Entries.ApplyActionOnAllItems(delegate(TournamentLeaderboardEntryItemVM x)
		{
			x.RefreshValues();
		});
		HeroText = GameTexts.FindText("str_hero").ToString();
		VictoriesText = GameTexts.FindText("str_leaderboard_victories").ToString();
		RankText = GameTexts.FindText("str_rank_sign").ToString();
		TitleText = GameTexts.FindText("str_leaderboard_title").ToString();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		DoneInputKey?.OnFinalize();
	}

	public void ExecuteDone()
	{
		IsEnabled = false;
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
