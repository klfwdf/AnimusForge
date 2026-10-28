using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Missions.Multiplayer;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.TeamSelection;

public class MultiplayerTeamSelectVM : ViewModel
{
	private readonly Action _onClose;

	private readonly Action _onAutoAssign;

	private readonly MissionMultiplayerGameModeBaseClient _gameMode;

	private readonly MissionPeer _missionPeer;

	private readonly string _gamemodeStr;

	private string _teamSelectTitle;

	private bool _isRoundCountdownAvailable;

	private string _remainingRoundTime;

	private string _gamemodeLbl;

	private string _autoassignLbl;

	private bool _isCancelDisabled;

	private TeamSelectTeamInstanceVM _team1;

	private TeamSelectTeamInstanceVM _team2;

	private TeamSelectTeamInstanceVM _teamSpectators;

	private MissionRepresentativeBase missionRep => GameNetwork.MyPeer?.VirtualPlayer?.GetComponent<MissionRepresentativeBase>();

	[DataSourceProperty]
	public TeamSelectTeamInstanceVM Team1
	{
		get
		{
			return _team1;
		}
		set
		{
			if (value != _team1)
			{
				_team1 = value;
				OnPropertyChangedWithValue(value, "Team1");
			}
		}
	}

	[DataSourceProperty]
	public TeamSelectTeamInstanceVM Team2
	{
		get
		{
			return _team2;
		}
		set
		{
			if (value != _team2)
			{
				_team2 = value;
				OnPropertyChangedWithValue(value, "Team2");
			}
		}
	}

	[DataSourceProperty]
	public TeamSelectTeamInstanceVM TeamSpectators
	{
		get
		{
			return _teamSpectators;
		}
		set
		{
			if (value != _teamSpectators)
			{
				_teamSpectators = value;
				OnPropertyChangedWithValue(value, "TeamSpectators");
			}
		}
	}

	[DataSourceProperty]
	public string TeamSelectTitle
	{
		get
		{
			return _teamSelectTitle;
		}
		set
		{
			_teamSelectTitle = value;
			OnPropertyChangedWithValue(value, "TeamSelectTitle");
		}
	}

	[DataSourceProperty]
	public bool IsRoundCountdownAvailable
	{
		get
		{
			return _isRoundCountdownAvailable;
		}
		set
		{
			if (value != _isRoundCountdownAvailable)
			{
				_isRoundCountdownAvailable = value;
				OnPropertyChangedWithValue(value, "IsRoundCountdownAvailable");
			}
		}
	}

	[DataSourceProperty]
	public string RemainingRoundTime
	{
		get
		{
			return _remainingRoundTime;
		}
		set
		{
			if (value != _remainingRoundTime)
			{
				_remainingRoundTime = value;
				OnPropertyChangedWithValue(value, "RemainingRoundTime");
			}
		}
	}

	[DataSourceProperty]
	public string GamemodeLbl
	{
		get
		{
			return _gamemodeLbl;
		}
		set
		{
			_gamemodeLbl = value;
			OnPropertyChangedWithValue(value, "GamemodeLbl");
		}
	}

	[DataSourceProperty]
	public string AutoassignLbl
	{
		get
		{
			return _autoassignLbl;
		}
		set
		{
			_autoassignLbl = value;
			OnPropertyChangedWithValue(value, "AutoassignLbl");
		}
	}

	[DataSourceProperty]
	public bool IsCancelDisabled
	{
		get
		{
			return _isCancelDisabled;
		}
		set
		{
			_isCancelDisabled = value;
			OnPropertyChangedWithValue(value, "IsCancelDisabled");
		}
	}

	public MultiplayerTeamSelectVM(Mission mission, Action<Team> onChangeTeamTo, Action onAutoAssign, Action onClose, IEnumerable<Team> teams, string gamemode)
	{
		_onClose = onClose;
		_onAutoAssign = onAutoAssign;
		_gamemodeStr = gamemode;
		Debug.Print("MultiplayerTeamSelectVM 1", 0, Debug.DebugColor.White, 17179869184uL);
		_gameMode = mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
		MissionScoreboardComponent missionBehavior = mission.GetMissionBehavior<MissionScoreboardComponent>();
		Debug.Print("MultiplayerTeamSelectVM 2", 0, Debug.DebugColor.White, 17179869184uL);
		IsRoundCountdownAvailable = _gameMode.IsGameModeUsingRoundCountdown;
		Debug.Print("MultiplayerTeamSelectVM 3", 0, Debug.DebugColor.White, 17179869184uL);
		Team team = teams.FirstOrDefault((Team t) => t.Side == BattleSideEnum.None);
		TeamSpectators = new TeamSelectTeamInstanceVM(missionBehavior, team, null, null, onChangeTeamTo, new MultiplayerBattleColors.MultiplayerCultureColorInfo(null, swapColors: false));
		Debug.Print("MultiplayerTeamSelectVM 4", 0, Debug.DebugColor.White, 17179869184uL);
		Team team2 = teams.FirstOrDefault((Team t) => t.Side == BattleSideEnum.Attacker);
		BasicCultureObject basicCultureObject = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
		Debug.Print("MultiplayerTeamSelectVM 5", 0, Debug.DebugColor.White, 17179869184uL);
		Team team3 = teams.FirstOrDefault((Team t) => t.Side == BattleSideEnum.Defender);
		BasicCultureObject basicCultureObject2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
		MultiplayerBattleColors multiplayerBattleColors = MultiplayerBattleColors.CreateWith(basicCultureObject, basicCultureObject2);
		Team1 = new TeamSelectTeamInstanceVM(missionBehavior, team2, basicCultureObject, team2.Banner, onChangeTeamTo, multiplayerBattleColors.AttackerColors);
		Team2 = new TeamSelectTeamInstanceVM(missionBehavior, team3, basicCultureObject2, team3.Banner, onChangeTeamTo, multiplayerBattleColors.DefenderColors);
		Debug.Print("MultiplayerTeamSelectVM 6", 0, Debug.DebugColor.White, 17179869184uL);
		if (GameNetwork.IsMyPeerReady)
		{
			_missionPeer = GameNetwork.MyPeer.GetComponent<MissionPeer>();
			IsCancelDisabled = _missionPeer.Team == null;
		}
		Debug.Print("MultiplayerTeamSelectVM 7", 0, Debug.DebugColor.White, 17179869184uL);
		RefreshValues();
		Debug.Print("MultiplayerTeamSelectVM 8", 0, Debug.DebugColor.White, 17179869184uL);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		AutoassignLbl = new TextObject("{=bON4Kn6B}Auto Assign").ToString();
		TeamSelectTitle = new TextObject("{=aVixswW5}Team Selection").ToString();
		GamemodeLbl = GameTexts.FindText("str_multiplayer_official_game_type_name", _gamemodeStr).ToString();
		Team1.RefreshValues();
		Team2.RefreshValues();
		TeamSpectators.RefreshValues();
	}

	public void Tick(float dt)
	{
		RemainingRoundTime = TimeSpan.FromSeconds(TaleWorlds.Library.MathF.Ceiling(_gameMode.RemainingTime)).ToString("mm':'ss");
	}

	public void RefreshDisabledTeams(List<Team> disabledTeams)
	{
		if (disabledTeams == null)
		{
			TeamSpectators?.SetIsDisabled(isCurrentTeam: false, disabledForBalance: false);
			Team1?.SetIsDisabled(isCurrentTeam: false, disabledForBalance: false);
			Team2?.SetIsDisabled(isCurrentTeam: false, disabledForBalance: false);
		}
		else
		{
			TeamSpectators?.SetIsDisabled(isCurrentTeam: false, disabledTeams?.Contains(TeamSpectators?.Team) ?? false);
			Team1?.SetIsDisabled(Team1?.Team == _missionPeer?.Team, disabledTeams?.Contains(Team1?.Team) ?? false);
			Team2?.SetIsDisabled(Team2?.Team == _missionPeer?.Team, disabledTeams?.Contains(Team2?.Team) ?? false);
		}
	}

	public void RefreshPlayerAndBotCount(int playersCountOne, int playersCountTwo, int botsCountOne, int botsCountTwo)
	{
		MBTextManager.SetTextVariable("PLAYER_COUNT", playersCountOne.ToString());
		Team1.DisplayedSecondary = new TextObject("{=Etjqamlh}{PLAYER_COUNT} Players").ToString();
		MBTextManager.SetTextVariable("BOT_COUNT", botsCountOne.ToString());
		Team1.DisplayedSecondarySub = new TextObject("{=eCOJSSUH}({BOT_COUNT} Bots)").ToString();
		MBTextManager.SetTextVariable("PLAYER_COUNT", playersCountTwo.ToString());
		Team2.DisplayedSecondary = new TextObject("{=Etjqamlh}{PLAYER_COUNT} Players").ToString();
		MBTextManager.SetTextVariable("BOT_COUNT", botsCountTwo.ToString());
		Team2.DisplayedSecondarySub = new TextObject("{=eCOJSSUH}({BOT_COUNT} Bots)").ToString();
	}

	public void RefreshFriendsPerTeam(IEnumerable<MissionPeer> friendsTeamOne, IEnumerable<MissionPeer> friendsTeamTwo)
	{
		Team1.RefreshFriends(friendsTeamOne);
		Team2.RefreshFriends(friendsTeamTwo);
	}

	[UsedImplicitly]
	public void ExecuteCancel()
	{
		_onClose();
	}

	[UsedImplicitly]
	public void ExecuteAutoAssign()
	{
		_onAutoAssign();
	}
}
