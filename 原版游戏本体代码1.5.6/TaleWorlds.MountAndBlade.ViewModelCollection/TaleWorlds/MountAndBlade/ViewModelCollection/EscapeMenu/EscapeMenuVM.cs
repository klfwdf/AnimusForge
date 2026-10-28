using System.Collections.Generic;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;

public class EscapeMenuVM : ViewModel
{
	private const float CopiedTextDisappearTime = 2f;

	private readonly TextObject _titleObj;

	private uint _startSeed;

	private string _title;

	private string _startingOptionsTitle;

	private MBBindingList<EscapeMenuItemVM> _menuItems;

	private GameTipsVM _tips;

	private bool _showCampaignInfo;

	private float _copiedTextTimer;

	private string _copiedText;

	private string _startScenarioText;

	private string _startSeedText;

	[DataSourceProperty]
	public bool ShowCampaignInfo
	{
		get
		{
			return _showCampaignInfo;
		}
		set
		{
			if (value != _showCampaignInfo)
			{
				_showCampaignInfo = value;
				OnPropertyChangedWithValue(value, "ShowCampaignInfo");
			}
		}
	}

	[DataSourceProperty]
	public float CopiedTextTimer
	{
		get
		{
			return _copiedTextTimer;
		}
		set
		{
			if (value != _copiedTextTimer)
			{
				_copiedTextTimer = value;
				OnPropertyChangedWithValue(value, "CopiedTextTimer");
			}
		}
	}

	[DataSourceProperty]
	public string CopiedText
	{
		get
		{
			return _copiedText;
		}
		set
		{
			if (value != _copiedText)
			{
				_copiedText = value;
				OnPropertyChangedWithValue(value, "CopiedText");
			}
		}
	}

	[DataSourceProperty]
	public string StartScenarioText
	{
		get
		{
			return _startScenarioText;
		}
		set
		{
			if (value != _startScenarioText)
			{
				_startScenarioText = value;
				OnPropertyChangedWithValue(value, "StartScenarioText");
			}
		}
	}

	[DataSourceProperty]
	public string StartSeedText
	{
		get
		{
			return _startSeedText;
		}
		set
		{
			if (value != _startSeedText)
			{
				_startSeedText = value;
				OnPropertyChangedWithValue(value, "StartSeedText");
			}
		}
	}

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
	public string StartingOptionsTitle
	{
		get
		{
			return _startingOptionsTitle;
		}
		set
		{
			if (value != _startingOptionsTitle)
			{
				_startingOptionsTitle = value;
				OnPropertyChangedWithValue(value, "StartingOptionsTitle");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EscapeMenuItemVM> MenuItems
	{
		get
		{
			return _menuItems;
		}
		set
		{
			if (value != _menuItems)
			{
				_menuItems = value;
				OnPropertyChangedWithValue(value, "MenuItems");
			}
		}
	}

	[DataSourceProperty]
	public GameTipsVM Tips
	{
		get
		{
			return _tips;
		}
		set
		{
			if (value != _tips)
			{
				_tips = value;
				OnPropertyChangedWithValue(value, "Tips");
			}
		}
	}

	public EscapeMenuVM(IEnumerable<EscapeMenuItemVM> items, TextObject title = null)
	{
		_titleObj = title;
		MenuItems = new MBBindingList<EscapeMenuItemVM>();
		if (items != null)
		{
			foreach (EscapeMenuItemVM item in items)
			{
				MenuItems.Add(item);
			}
		}
		Tips = new GameTipsVM(isAutoChangeEnabled: true, navigationButtonsEnabled: true);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Title = _titleObj?.ToString() ?? "";
		StartingOptionsTitle = new TextObject("{=rVCb112K}Starting Options").ToString();
		MenuItems.ApplyActionOnAllItems(delegate(EscapeMenuItemVM x)
		{
			x.RefreshValues();
		});
		Tips.RefreshValues();
	}

	public void InitializeCampaignStartingOptionsInfo(string startScenario, uint startSeed)
	{
		TextObject textObject = new TextObject("{=5FVxxNxG}Scenario: {SCENARIO}");
		textObject.SetTextVariable("SCENARIO", startScenario);
		TextObject textObject2 = new TextObject("{=hDaYab60}Seed: {SEED}");
		textObject2.SetTextVariable("SEED", startSeed.ToString());
		_startSeed = startSeed;
		StartScenarioText = textObject.ToString();
		StartSeedText = textObject2.ToString();
		ShowCampaignInfo = !string.IsNullOrEmpty(startScenario);
	}

	public virtual void Tick(float dt)
	{
		if (CopiedTextTimer > 0f)
		{
			CopiedTextTimer -= dt;
		}
		else if (!string.IsNullOrEmpty(CopiedText))
		{
			CopiedText = string.Empty;
		}
	}

	public void RefreshItems(IEnumerable<EscapeMenuItemVM> items)
	{
		MenuItems.Clear();
		foreach (EscapeMenuItemVM item in items)
		{
			MenuItems.Add(item);
		}
	}

	public void ExecuteCopyGameSeed()
	{
		TaleWorlds.InputSystem.Input.SetClipboardText(StartSeedText);
		CopiedText = new TextObject("{=e8AmrZQ9}Copied").ToString();
		CopiedTextTimer = 2f;
	}
}
