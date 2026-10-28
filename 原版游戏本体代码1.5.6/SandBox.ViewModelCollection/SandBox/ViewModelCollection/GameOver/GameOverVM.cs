using System;
using SandBox.ViewModelCollection.Input;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.GameOver;

public class GameOverVM : ViewModel
{
	private readonly Action _onClose;

	private readonly GameOverStatsProvider _statsProvider;

	private readonly GameOverState.GameOverReason _reason;

	private GameOverStatCategoryVM _currentCategory;

	private string _closeText;

	private string _titleText;

	private string _reasonAsString;

	private string _statisticsTitle;

	private bool _isPositiveGameOver;

	private InputKeyItemVM _closeInputKey;

	private BannerImageIdentifierVM _clanBanner;

	private MBBindingList<GameOverStatCategoryVM> _categories;

	[DataSourceProperty]
	public string CloseText
	{
		get
		{
			return _closeText;
		}
		set
		{
			if (value != _closeText)
			{
				_closeText = value;
				OnPropertyChangedWithValue(value, "CloseText");
			}
		}
	}

	[DataSourceProperty]
	public string StatisticsTitle
	{
		get
		{
			return _statisticsTitle;
		}
		set
		{
			if (value != _statisticsTitle)
			{
				_statisticsTitle = value;
				OnPropertyChangedWithValue(value, "StatisticsTitle");
			}
		}
	}

	[DataSourceProperty]
	public string ReasonAsString
	{
		get
		{
			return _reasonAsString;
		}
		set
		{
			if (value != _reasonAsString)
			{
				_reasonAsString = value;
				OnPropertyChangedWithValue(value, "ReasonAsString");
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
	public BannerImageIdentifierVM ClanBanner
	{
		get
		{
			return _clanBanner;
		}
		set
		{
			if (value != _clanBanner)
			{
				_clanBanner = value;
				OnPropertyChangedWithValue(value, "ClanBanner");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPositiveGameOver
	{
		get
		{
			return _isPositiveGameOver;
		}
		set
		{
			if (value != _isPositiveGameOver)
			{
				_isPositiveGameOver = value;
				OnPropertyChangedWithValue(value, "IsPositiveGameOver");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM CloseInputKey
	{
		get
		{
			return _closeInputKey;
		}
		set
		{
			if (value != _closeInputKey)
			{
				_closeInputKey = value;
				OnPropertyChangedWithValue(value, "CloseInputKey");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<GameOverStatCategoryVM> Categories
	{
		get
		{
			return _categories;
		}
		set
		{
			if (value != _categories)
			{
				_categories = value;
				OnPropertyChangedWithValue(value, "Categories");
			}
		}
	}

	public GameOverVM(GameOverState.GameOverReason reason, Action onClose)
	{
		_onClose = onClose;
		_reason = reason;
		_statsProvider = new GameOverStatsProvider();
		Categories = new MBBindingList<GameOverStatCategoryVM>();
		IsPositiveGameOver = _reason == GameOverState.GameOverReason.Victory;
		ClanBanner = new BannerImageIdentifierVM(Hero.MainHero.ClanBanner, nineGrid: true);
		ReasonAsString = Enum.GetName(typeof(GameOverState.GameOverReason), _reason);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		CloseText = (IsPositiveGameOver ? new TextObject("{=AdgAJbAP}Return To The Map").ToString() : GameTexts.FindText("str_main_menu").ToString());
		TitleText = GameTexts.FindText("str_game_over_title", ReasonAsString).ToString();
		StatisticsTitle = GameTexts.FindText("str_statistics").ToString();
		Categories.Clear();
		foreach (StatCategory gameOverStat in _statsProvider.GetGameOverStats())
		{
			Categories.Add(new GameOverStatCategoryVM(gameOverStat, OnCategorySelection));
		}
		OnCategorySelection(Categories[0]);
	}

	private void OnCategorySelection(GameOverStatCategoryVM newCategory)
	{
		if (_currentCategory != null)
		{
			_currentCategory.IsSelected = false;
		}
		_currentCategory = newCategory;
		if (_currentCategory != null)
		{
			_currentCategory.IsSelected = true;
		}
	}

	public void ExecuteClose()
	{
		_onClose?.DynamicInvokeWithLog();
	}

	public void SetCloseInputKey(HotKey hotKey)
	{
		CloseInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CloseInputKey?.OnFinalize();
	}
}
