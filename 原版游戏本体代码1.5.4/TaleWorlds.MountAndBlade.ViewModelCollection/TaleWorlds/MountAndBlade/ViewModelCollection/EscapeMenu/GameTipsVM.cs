using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;

public class GameTipsVM : ViewModel
{
	private MBList<string> _allTips;

	private readonly float _tipTimeInterval = 5f;

	private readonly bool _isAutoChangeEnabled;

	private int _currentTipIndex;

	private float _totalDt;

	private string _currentTip;

	private string _gameTipTitle;

	private bool _navigationButtonsEnabled;

	[DataSourceProperty]
	public string CurrentTip
	{
		get
		{
			return _currentTip;
		}
		set
		{
			if (value != _currentTip)
			{
				_currentTip = value;
				OnPropertyChangedWithValue(value, "CurrentTip");
			}
		}
	}

	[DataSourceProperty]
	public string GameTipTitle
	{
		get
		{
			return _gameTipTitle;
		}
		set
		{
			if (value != _gameTipTitle)
			{
				_gameTipTitle = value;
				OnPropertyChangedWithValue(value, "GameTipTitle");
			}
		}
	}

	[DataSourceProperty]
	public bool NavigationButtonsEnabled
	{
		get
		{
			return _navigationButtonsEnabled;
		}
		set
		{
			if (value != _navigationButtonsEnabled)
			{
				_navigationButtonsEnabled = value;
				OnPropertyChangedWithValue(value, "NavigationButtonsEnabled");
			}
		}
	}

	public GameTipsVM(bool isAutoChangeEnabled, bool navigationButtonsEnabled)
	{
		_navigationButtonsEnabled = navigationButtonsEnabled;
		_isAutoChangeEnabled = isAutoChangeEnabled;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		_allTips = new MBList<string>();
		GameTipTitle = GameTexts.FindText("str_game_tip_title").ToString();
		float overrideExtendScale = 0.8f;
		string keyHyperlinkText = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("Generic", 4), overrideExtendScale);
		GameTexts.SetVariable("LEAVE_AREA_KEY", keyHyperlinkText);
		string keyHyperlinkText2 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("Generic", 5), overrideExtendScale);
		GameTexts.SetVariable("MISSION_INDICATORS_KEY", keyHyperlinkText2);
		GameTexts.SetVariable("EXTEND_KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MapHotKeyCategory", "MapFollowModifier"), overrideExtendScale));
		GameTexts.SetVariable("ENCYCLOPEDIA_SHORTCUT", HyperlinkTexts.GetKeyHyperlinkText("RightMouseButton", overrideExtendScale));
		if (TaleWorlds.InputSystem.Input.IsMouseActive)
		{
			foreach (TextObject item in GameTexts.FindAllTextVariations("str_game_tip_pc"))
			{
				_allTips.Add(item.ToString());
			}
		}
		foreach (TextObject item2 in GameTexts.FindAllTextVariations("str_game_tip"))
		{
			_allTips.Add(item2.ToString());
		}
		NavigationButtonsEnabled = _allTips.Count > 1;
		CurrentTip = ((_allTips.Count == 0) ? string.Empty : _allTips.GetRandomElement());
	}

	public void ExecutePreviousTip()
	{
		_currentTipIndex--;
		if (_currentTipIndex < 0)
		{
			_currentTipIndex = _allTips.Count - 1;
		}
		CurrentTip = _allTips[_currentTipIndex];
	}

	public void ExecuteNextTip()
	{
		_currentTipIndex = (_currentTipIndex + 1) % _allTips.Count;
		CurrentTip = _allTips[_currentTipIndex];
	}

	public void OnTick(float dt)
	{
		if (_isAutoChangeEnabled)
		{
			_totalDt += dt;
			if (_totalDt > _tipTimeInterval)
			{
				ExecuteNextTip();
				_totalDt = 0f;
			}
		}
	}
}
