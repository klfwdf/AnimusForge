using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Missions.BattleScore;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard;

public abstract class ScoreboardBaseVM : ViewModel
{
	internal enum MouseState
	{
		NotVisible,
		Visible
	}

	public enum Categories
	{
		Party,
		Tactical,
		NumOfCategories
	}

	protected enum BattleResultType
	{
		NotOver = -1,
		Defeat,
		Victory,
		Retreat
	}

	protected Action OnFastForwardIncreaseSpeed;

	protected Action OnFastForwardDecreaseSpeed;

	protected Action OnFastForwardResetSpeed;

	private static readonly TextObject _hourAbbrString = GameTexts.FindText("str_hour_abbr");

	private static readonly TextObject _minuteAbbrString = GameTexts.FindText("str_minute_abbr");

	private static readonly TextObject _secondAbbrString = GameTexts.FindText("str_second_abbr");

	protected BattleSideEnum PlayerSide;

	protected IMissionScreen _missionScreen;

	protected Mission _mission;

	protected BattleEndLogic _battleEndLogic;

	protected InquiryData _retreatInquiryData;

	protected Action _releaseSimulationSources;

	protected Action<bool> OnToggle;

	private MouseState _mouseState;

	protected const float MissionEndScoreboardDelayTime = 1.5f;

	protected BattleScoreContext ScoreboardContext;

	private string _quitText;

	private string _showScoreboardText;

	private string _fastForwardText;

	private string _moraleText;

	private bool _isFastForwarding;

	private bool _isPaused;

	private bool _isMainCharacterDead;

	private bool _showScoreboard;

	private bool _isSimulation = true;

	private bool _isNavalBattle;

	private bool _isMouseEnabled;

	private PowerLevelComparer _powerComparer;

	private bool _isOver;

	private string _battleResult;

	private int _battleResultIndex = -1;

	private HintViewModel _killHint;

	private HintViewModel _upgradeHint;

	private HintViewModel _deadHint;

	private HintViewModel _woundedHint;

	private HintViewModel _routedHint;

	private HintViewModel _remainingHint;

	private SPScoreboardSideVM _attackers;

	private SPScoreboardSideVM _defenders;

	private SPScoreboardSideVM _neutralTroops;

	private bool _isPowerComparerEnabled;

	private int _missionTimeInSeconds;

	private string _missionTimeStr;

	private string _simulationResult;

	private InputKeyItemVM _showMouseKey;

	private InputKeyItemVM _showScoreboardKey;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _fastForwardKey;

	private InputKeyItemVM _pauseInputKey;

	protected int MissionTimeInSeconds
	{
		get
		{
			return _missionTimeInSeconds;
		}
		set
		{
			if (value != _missionTimeInSeconds)
			{
				_missionTimeInSeconds = value;
				MissionTimeStr = GetFormattedTimeTextFromSeconds(_missionTimeInSeconds);
			}
		}
	}

	[DataSourceProperty]
	public string MissionTimeStr
	{
		get
		{
			return _missionTimeStr;
		}
		set
		{
			if (value != _missionTimeStr)
			{
				_missionTimeStr = value;
				OnPropertyChangedWithValue(value, "MissionTimeStr");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPowerComparerEnabled
	{
		get
		{
			return _isPowerComparerEnabled;
		}
		set
		{
			if (value != _isPowerComparerEnabled)
			{
				_isPowerComparerEnabled = value;
				OnPropertyChangedWithValue(value, "IsPowerComparerEnabled");
			}
		}
	}

	[DataSourceProperty]
	public string QuitText
	{
		get
		{
			return _quitText;
		}
		set
		{
			if (value != _quitText)
			{
				_quitText = value;
				OnPropertyChangedWithValue(value, "QuitText");
			}
		}
	}

	[DataSourceProperty]
	public string ShowScoreboardText
	{
		get
		{
			return _showScoreboardText;
		}
		set
		{
			if (value != _showScoreboardText)
			{
				_showScoreboardText = value;
				OnPropertyChangedWithValue(value, "ShowScoreboardText");
			}
		}
	}

	[DataSourceProperty]
	public string FastForwardText
	{
		get
		{
			return _fastForwardText;
		}
		set
		{
			if (value != _fastForwardText)
			{
				_fastForwardText = value;
				OnPropertyChangedWithValue(value, "FastForwardText");
			}
		}
	}

	[DataSourceProperty]
	public string MoraleText
	{
		get
		{
			return _moraleText;
		}
		set
		{
			if (value != _moraleText)
			{
				_moraleText = value;
				OnPropertyChangedWithValue(value, "MoraleText");
			}
		}
	}

	[DataSourceProperty]
	public SPScoreboardSideVM Attackers
	{
		get
		{
			return _attackers;
		}
		set
		{
			if (value != _attackers)
			{
				_attackers = value;
				OnPropertyChangedWithValue(value, "Attackers");
			}
		}
	}

	[DataSourceProperty]
	public SPScoreboardSideVM Defenders
	{
		get
		{
			return _defenders;
		}
		set
		{
			if (value != _defenders)
			{
				_defenders = value;
				OnPropertyChangedWithValue(value, "Defenders");
			}
		}
	}

	[DataSourceProperty]
	public SPScoreboardSideVM NeutralTroops
	{
		get
		{
			return _neutralTroops;
		}
		set
		{
			if (value != _neutralTroops)
			{
				_neutralTroops = value;
				OnPropertyChangedWithValue(value, "NeutralTroops");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel KillHint
	{
		get
		{
			return _killHint;
		}
		set
		{
			if (value != _killHint)
			{
				_killHint = value;
				OnPropertyChangedWithValue(value, "KillHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel DeadHint
	{
		get
		{
			return _deadHint;
		}
		set
		{
			if (value != _deadHint)
			{
				_deadHint = value;
				OnPropertyChangedWithValue(value, "DeadHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel UpgradeHint
	{
		get
		{
			return _upgradeHint;
		}
		set
		{
			if (value != _upgradeHint)
			{
				_upgradeHint = value;
				OnPropertyChangedWithValue(value, "UpgradeHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel WoundedHint
	{
		get
		{
			return _woundedHint;
		}
		set
		{
			if (value != _woundedHint)
			{
				_woundedHint = value;
				OnPropertyChangedWithValue(value, "WoundedHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel RoutedHint
	{
		get
		{
			return _routedHint;
		}
		set
		{
			if (value != _routedHint)
			{
				_routedHint = value;
				OnPropertyChangedWithValue(value, "RoutedHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel RemainingHint
	{
		get
		{
			return _remainingHint;
		}
		set
		{
			if (value != _remainingHint)
			{
				_remainingHint = value;
				OnPropertyChangedWithValue(value, "RemainingHint");
			}
		}
	}

	[DataSourceProperty]
	public int BattleResultIndex
	{
		get
		{
			return _battleResultIndex;
		}
		set
		{
			if (value != _battleResultIndex)
			{
				_battleResultIndex = value;
				OnPropertyChangedWithValue(value, "BattleResultIndex");
			}
		}
	}

	[DataSourceProperty]
	public string BattleResult
	{
		get
		{
			return _battleResult;
		}
		set
		{
			if (value != _battleResult)
			{
				_battleResult = value;
				OnPropertyChangedWithValue(value, "BattleResult");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMouseEnabled
	{
		get
		{
			return _isMouseEnabled;
		}
		set
		{
			if (value != _isMouseEnabled)
			{
				_isMouseEnabled = value;
				OnPropertyChangedWithValue(value, "IsMouseEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOver
	{
		get
		{
			return _isOver;
		}
		set
		{
			if (value != _isOver)
			{
				_isOver = value;
				OnPropertyChangedWithValue(value, "IsOver");
				UpdateQuitText();
			}
		}
	}

	[DataSourceProperty]
	public string SimulationResult
	{
		get
		{
			return _simulationResult;
		}
		set
		{
			if (value != _simulationResult)
			{
				_simulationResult = value;
				OnPropertyChangedWithValue(value, "SimulationResult");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMainCharacterDead
	{
		get
		{
			return _isMainCharacterDead;
		}
		set
		{
			if (value != _isMainCharacterDead)
			{
				_isMainCharacterDead = value;
				OnPropertyChangedWithValue(value, "IsMainCharacterDead");
				UpdateQuitText();
			}
		}
	}

	[DataSourceProperty]
	public bool ShowScoreboard
	{
		get
		{
			return _showScoreboard;
		}
		set
		{
			if (value != _showScoreboard)
			{
				_showScoreboard = value;
				OnPropertyChangedWithValue(value, "ShowScoreboard");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSimulation
	{
		get
		{
			return _isSimulation;
		}
		set
		{
			if (value != _isSimulation)
			{
				_isSimulation = value;
				OnPropertyChangedWithValue(value, "IsSimulation");
			}
		}
	}

	[DataSourceProperty]
	public bool IsNavalBattle
	{
		get
		{
			return _isNavalBattle;
		}
		set
		{
			if (value != _isNavalBattle)
			{
				_isNavalBattle = value;
				OnPropertyChangedWithValue(value, "IsNavalBattle");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFastForwarding
	{
		get
		{
			return _isFastForwarding;
		}
		set
		{
			if (value != _isFastForwarding)
			{
				_isFastForwarding = value;
				OnPropertyChangedWithValue(value, "IsFastForwarding");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPaused
	{
		get
		{
			return _isPaused;
		}
		set
		{
			if (value != _isPaused)
			{
				_isPaused = value;
				OnPropertyChangedWithValue(value, "IsPaused");
			}
		}
	}

	[DataSourceProperty]
	public PowerLevelComparer PowerComparer
	{
		get
		{
			return _powerComparer;
		}
		set
		{
			if (value != _powerComparer)
			{
				_powerComparer = value;
				OnPropertyChangedWithValue(value, "PowerComparer");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM ShowMouseKey
	{
		get
		{
			return _showMouseKey;
		}
		set
		{
			if (value != _showMouseKey)
			{
				_showMouseKey = value;
				OnPropertyChangedWithValue(value, "ShowMouseKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM ShowScoreboardKey
	{
		get
		{
			return _showScoreboardKey;
		}
		set
		{
			if (value != _showScoreboardKey)
			{
				_showScoreboardKey = value;
				OnPropertyChangedWithValue(value, "ShowScoreboardKey");
			}
		}
	}

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
	public InputKeyItemVM FastForwardKey
	{
		get
		{
			return _fastForwardKey;
		}
		set
		{
			if (value != _fastForwardKey)
			{
				_fastForwardKey = value;
				OnPropertyChangedWithValue(value, "FastForwardKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM PauseInputKey
	{
		get
		{
			return _pauseInputKey;
		}
		set
		{
			if (value != _pauseInputKey)
			{
				_pauseInputKey = value;
				OnPropertyChangedWithValue(value, "PauseInputKey");
			}
		}
	}

	[DataSourceProperty]
	public virtual MBBindingList<BattleResultVM> BattleResults
	{
		get
		{
			return null;
		}
		set
		{
		}
	}

	public ScoreboardBaseVM(BattleScoreContext scoreboardContext)
	{
		ScoreboardContext = scoreboardContext;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		KillHint = new HintViewModel(GameTexts.FindText("str_battle_result_score_sort_button", "0"));
		DeadHint = new HintViewModel(GameTexts.FindText("str_battle_result_score_sort_button", "1"));
		WoundedHint = new HintViewModel(GameTexts.FindText("str_battle_result_score_sort_button", "2"));
		RoutedHint = new HintViewModel(GameTexts.FindText("str_battle_result_score_sort_button", "3"));
		RemainingHint = new HintViewModel(GameTexts.FindText("str_battle_result_score_sort_button", "4"));
		UpgradeHint = new HintViewModel(GameTexts.FindText("str_battle_result_score_sort_button", "5"));
		UpdateQuitText();
		GameTexts.SetVariable("KEY", Game.Current.GameTextManager.GetHotKeyGameText("Generic", 4));
		_retreatInquiryData = new InquiryData("", GameTexts.FindText("str_can_not_retreat").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_ok").ToString(), "", null, null);
		Attackers?.RefreshValues();
		Defenders?.RefreshValues();
		ShowScoreboardText = new TextObject("{=5Ixsvn3s}Toggle scoreboard").ToString();
		FastForwardText = new TextObject("{=HH7LDwlK}Toggle Fast Forward").ToString();
		MoraleText = GameTexts.FindText("str_morale").ToString();
		ShowMouseKey?.RefreshValues();
		ShowScoreboardKey?.RefreshValues();
		DoneInputKey?.RefreshValues();
		FastForwardKey?.RefreshValues();
		PauseInputKey?.RefreshValues();
	}

	public void OnMainHeroDeath()
	{
		IsMainCharacterDead = true;
		_missionScreen?.SetOrderFlagVisibility(value: false);
	}

	public void OnTakenControlOfAnotherAgent()
	{
		IsMainCharacterDead = false;
		_missionScreen?.SetOrderFlagVisibility(value: false);
	}

	public virtual void Initialize(IMissionScreen missionScreen, Mission mission, Action releaseSimulationSources, Action<bool> onToggle)
	{
		OnToggle = onToggle;
		_missionScreen = missionScreen;
		_mission = mission;
		_releaseSimulationSources = releaseSimulationSources;
		BattleResult = "";
		BattleResultIndex = -1;
		IsOver = false;
		ShowScoreboard = false;
		OnToggle?.Invoke(obj: false);
		if (mission != null)
		{
			_battleEndLogic = _mission.GetMissionBehavior<BattleEndLogic>();
		}
		NeutralTroops = new SPScoreboardSideVM(null, null, isSimulation: false, isPlayerSide: false);
		PowerComparer = new PowerLevelComparer(1.0, 1.0);
		RefreshValues();
	}

	protected virtual void UpdateQuitText()
	{
		if (IsOver)
		{
			QuitText = GameTexts.FindText("str_done").ToString();
		}
		else if (IsMainCharacterDead && !IsSimulation)
		{
			QuitText = GameTexts.FindText("str_end_battle").ToString();
		}
		else
		{
			QuitText = GameTexts.FindText("str_retreat").ToString();
		}
	}

	public virtual void OnDeploymentFinished()
	{
	}

	public void Tick(float dt)
	{
		PowerComparer.IsEnabled = ScoreboardContext.IsPowerComparisonRelevant;
		IsPowerComparerEnabled = PowerComparer.IsEnabled && !BannerlordConfig.HideBattleUI && !MBCommon.IsPaused;
		OnTick(dt);
	}

	protected abstract void OnTick(float dt);

	protected SPScoreboardSideVM GetSide(BattleSideEnum side)
	{
		return side switch
		{
			BattleSideEnum.Defender => Defenders, 
			BattleSideEnum.Attacker => Attackers, 
			_ => NeutralTroops, 
		};
	}

	public void SetMouseState(bool visible)
	{
		_mouseState = (visible ? MouseState.Visible : MouseState.NotVisible);
		IsMouseEnabled = visible;
	}

	public static string GetFormattedTimeTextFromSeconds(int seconds)
	{
		TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
		string text = "";
		if (timeSpan.Hours > 0)
		{
			text += $"{timeSpan.Hours:D2}{_hourAbbrString}:";
		}
		text += $"{timeSpan.Minutes:D2}{_minuteAbbrString}:";
		return text + $"{timeSpan.Seconds:D2}{_secondAbbrString}";
	}

	protected float GetBattleMoraleOfSide(BattleSideEnum side)
	{
		if (Mission.Current == null)
		{
			return 0f;
		}
		float num = 0f;
		int num2 = 0;
		bool flag = false;
		for (int i = 0; i < Mission.Current.Teams.Count; i++)
		{
			Team team = Mission.Current.Teams[i];
			if (team.Side != side)
			{
				continue;
			}
			for (int j = 0; j < team.ActiveAgents.Count; j++)
			{
				Agent agent = team.ActiveAgents[j];
				if (agent.IsHuman)
				{
					if (agent.IsAIControlled)
					{
						num2++;
						num += agent.GetMorale();
					}
					else
					{
						flag = true;
					}
				}
			}
		}
		if (num2 > 0)
		{
			return MBMath.ClampFloat(num / (float)num2, 0f, 100f);
		}
		if (flag)
		{
			return 50f;
		}
		return 0f;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		ShowMouseKey?.OnFinalize();
		ShowScoreboardKey?.OnFinalize();
		DoneInputKey?.OnFinalize();
		FastForwardKey?.OnFinalize();
		PauseInputKey?.OnFinalize();
	}

	public virtual void ExecuteShowScoreboardAction()
	{
		ShowScoreboard = !ShowScoreboard;
	}

	public virtual void ExecutePlayAction()
	{
	}

	public virtual void ExecuteFastForwardAction()
	{
	}

	public virtual void ExecutePauseSimulationAction()
	{
	}

	public virtual void ExecuteEndSimulationAction()
	{
	}

	public virtual void ExecuteQuitAction()
	{
	}

	public virtual void SetShortcuts(ScoreboardHotkeys shortcuts)
	{
		ShowMouseKey = InputKeyItemVM.CreateFromGameKey(shortcuts.ShowMouseHotkey, isConsoleOnly: false);
		ShowScoreboardKey = InputKeyItemVM.CreateFromGameKey(shortcuts.ShowScoreboardHotkey, isConsoleOnly: false);
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(shortcuts.DoneInputKey, isConsoleOnly: true);
		FastForwardKey = InputKeyItemVM.CreateFromHotKey(shortcuts.FastForwardKey, isConsoleOnly: true);
		PauseInputKey = InputKeyItemVM.CreateFromHotKey(shortcuts.PauseInputKey, isConsoleOnly: true);
	}
}
