using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Missions.Multiplayer;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Scoreboard;

public class MPEndOfBattleVM : ViewModel
{
	private MissionScoreboardComponent _missionScoreboardComponent;

	private MissionMultiplayerGameModeBaseClient _gameMode;

	private MissionLobbyComponent _lobbyComponent;

	private bool _isSingleTeam;

	private BattleSideEnum _allyBattleSide;

	private BattleSideEnum _enemyBattleSide;

	private bool _isAvailable;

	private string _countdownTitle;

	private int _countdown;

	private string _header;

	private int _battleResult;

	private string _resultText;

	private MPEndOfBattleSideVM _allySide;

	private MPEndOfBattleSideVM _enemySide;

	private MissionRepresentativeBase missionRep => GameNetwork.MyPeer?.VirtualPlayer?.GetComponent<MissionRepresentativeBase>();

	[DataSourceProperty]
	public bool IsAvailable
	{
		get
		{
			return _isAvailable;
		}
		set
		{
			if (value != _isAvailable)
			{
				_isAvailable = value;
				OnPropertyChangedWithValue(value, "IsAvailable");
			}
		}
	}

	[DataSourceProperty]
	public string CountdownTitle
	{
		get
		{
			return _countdownTitle;
		}
		set
		{
			if (value != _countdownTitle)
			{
				_countdownTitle = value;
				OnPropertyChangedWithValue(value, "CountdownTitle");
			}
		}
	}

	[DataSourceProperty]
	public int Countdown
	{
		get
		{
			return _countdown;
		}
		set
		{
			if (value != _countdown)
			{
				_countdown = value;
				OnPropertyChangedWithValue(value, "Countdown");
			}
		}
	}

	[DataSourceProperty]
	public string Header
	{
		get
		{
			return _header;
		}
		set
		{
			if (value != _header)
			{
				_header = value;
				OnPropertyChangedWithValue(value, "Header");
			}
		}
	}

	[DataSourceProperty]
	public int BattleResult
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
	public string ResultText
	{
		get
		{
			return _resultText;
		}
		set
		{
			if (value != _resultText)
			{
				_resultText = value;
				OnPropertyChangedWithValue(value, "ResultText");
			}
		}
	}

	[DataSourceProperty]
	public MPEndOfBattleSideVM AllySide
	{
		get
		{
			return _allySide;
		}
		set
		{
			if (value != _allySide)
			{
				_allySide = value;
				OnPropertyChangedWithValue(value, "AllySide");
			}
		}
	}

	[DataSourceProperty]
	public MPEndOfBattleSideVM EnemySide
	{
		get
		{
			return _enemySide;
		}
		set
		{
			if (value != _enemySide)
			{
				_enemySide = value;
				OnPropertyChangedWithValue(value, "EnemySide");
			}
		}
	}

	public MPEndOfBattleVM(Mission mission, MissionScoreboardComponent missionScoreboardComponent, bool isSingleTeam)
	{
		_missionScoreboardComponent = missionScoreboardComponent;
		_gameMode = mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
		_lobbyComponent = mission.GetMissionBehavior<MissionLobbyComponent>();
		_lobbyComponent.OnPostMatchEnded += OnPostMatchEnded;
		_isSingleTeam = isSingleTeam;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		CountdownTitle = new TextObject("{=wGjQgQlY}Next Game begins in:").ToString();
		Header = new TextObject("{=HXxNfncd}End of Battle").ToString();
		AllySide?.RefreshValues();
		EnemySide?.RefreshValues();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		_lobbyComponent.OnPostMatchEnded -= OnPostMatchEnded;
	}

	public void Tick(float dt)
	{
		Countdown = MathF.Ceiling(_gameMode.RemainingTime);
	}

	private void OnPostMatchEnded()
	{
		OnFinalRoundEnded();
	}

	private void OnFinalRoundEnded()
	{
		if (!_isSingleTeam)
		{
			IsAvailable = true;
			InitSides();
			BattleSideEnum battleSideEnum = _missionScoreboardComponent?.GetMatchWinnerSide() ?? BattleSideEnum.None;
			if (battleSideEnum == _enemyBattleSide)
			{
				BattleResult = 0;
				ResultText = GameTexts.FindText("str_defeat").ToString();
			}
			else if (battleSideEnum == _allyBattleSide)
			{
				BattleResult = 1;
				ResultText = GameTexts.FindText("str_victory").ToString();
			}
			else
			{
				BattleResult = 2;
				ResultText = GameTexts.FindText("str_draw").ToString();
			}
		}
	}

	private void InitSides()
	{
		_allyBattleSide = BattleSideEnum.Attacker;
		_enemyBattleSide = BattleSideEnum.Defender;
		MissionPeer missionPeer = GameNetwork.MyPeer?.GetComponent<MissionPeer>();
		if (missionPeer != null)
		{
			Team team = missionPeer.Team;
			if (team != null && team.Side == BattleSideEnum.Defender)
			{
				_allyBattleSide = BattleSideEnum.Defender;
				_enemyBattleSide = BattleSideEnum.Attacker;
			}
		}
		MissionScoreboardComponent.MissionScoreboardSide missionScoreboardSide = _missionScoreboardComponent.Sides.FirstOrDefault((MissionScoreboardComponent.MissionScoreboardSide s) => s != null && s.Side == _allyBattleSide);
		MissionScoreboardComponent.MissionScoreboardSide missionScoreboardSide2 = _missionScoreboardComponent.Sides.FirstOrDefault((MissionScoreboardComponent.MissionScoreboardSide s) => s != null && s.Side == _enemyBattleSide);
		string text = ((missionScoreboardSide != null && missionScoreboardSide.Side == BattleSideEnum.Attacker) ? MultiplayerOptions.OptionType.CultureTeam1.GetStrValue() : MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
		string text2 = ((missionScoreboardSide2 != null && missionScoreboardSide2.Side == BattleSideEnum.Attacker) ? MultiplayerOptions.OptionType.CultureTeam1.GetStrValue() : MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
		BasicCultureObject attackerCulture = (string.IsNullOrEmpty(text) ? null : MBObjectManager.Instance.GetObject<BasicCultureObject>(text));
		BasicCultureObject defenderCulture = (string.IsNullOrEmpty(text2) ? null : MBObjectManager.Instance.GetObject<BasicCultureObject>(text2));
		MultiplayerBattleColors multiplayerBattleColors = MultiplayerBattleColors.CreateWith(attackerCulture, defenderCulture);
		if (missionScoreboardSide != null)
		{
			AllySide = new MPEndOfBattleSideVM(_missionScoreboardComponent, missionScoreboardSide, multiplayerBattleColors.AttackerColors);
		}
		if (missionScoreboardSide2 != null)
		{
			EnemySide = new MPEndOfBattleSideVM(_missionScoreboardComponent, missionScoreboardSide2, multiplayerBattleColors.DefenderColors);
		}
	}
}
