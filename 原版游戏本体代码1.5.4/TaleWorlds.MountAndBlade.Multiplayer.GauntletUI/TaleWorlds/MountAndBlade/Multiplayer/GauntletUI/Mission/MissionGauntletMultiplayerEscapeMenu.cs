using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.GauntletUI.Mission;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;

[OverrideView(typeof(MissionMultiplayerEscapeMenu))]
public class MissionGauntletMultiplayerEscapeMenu : MissionGauntletEscapeMenuBase
{
	private MissionOptionsComponent _missionOptionsComponent;

	private MissionLobbyComponent _missionLobbyComponent;

	private MultiplayerAdminComponent _missionAdminComponent;

	private MultiplayerTeamSelectComponent _missionTeamSelectComponent;

	private MissionMultiplayerGameModeBaseClient _gameModeClient;

	private readonly string _gameType;

	private EscapeMenuItemVM _changeTroopItem;

	private EscapeMenuItemVM _changeCultureItem;

	public MissionGauntletMultiplayerEscapeMenu(string gameType)
		: base("MultiplayerEscapeMenu")
	{
		_gameType = gameType;
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_missionOptionsComponent = base.Mission.GetMissionBehavior<MissionOptionsComponent>();
		_missionLobbyComponent = base.Mission.GetMissionBehavior<MissionLobbyComponent>();
		_missionAdminComponent = base.Mission.GetMissionBehavior<MultiplayerAdminComponent>();
		_missionTeamSelectComponent = base.Mission.GetMissionBehavior<MultiplayerTeamSelectComponent>();
		_gameModeClient = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
		TextObject title = GameTexts.FindText("str_multiplayer_game_type", _gameType);
		DataSource = new MPEscapeMenuVM(null, title);
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		DataSource.Tick(dt);
	}

	public override bool OnEscape()
	{
		bool result = base.OnEscape();
		if (base.IsActive)
		{
			if (_gameModeClient.IsGameModeUsingAllowTroopChange)
			{
				_changeTroopItem.IsDisabled = !_gameModeClient.CanRequestTroopChange();
			}
			if (_gameModeClient.IsGameModeUsingAllowCultureChange)
			{
				_changeCultureItem.IsDisabled = !_gameModeClient.CanRequestCultureChange();
			}
		}
		return result;
	}

	protected override List<EscapeMenuItemVM> GetEscapeMenuItems()
	{
		List<EscapeMenuItemVM> list = new List<EscapeMenuItemVM>();
		list.Add(new EscapeMenuItemVM(new TextObject("{=e139gKZc}Return to the Game"), delegate
		{
			OnEscapeMenuToggled(isOpened: false);
		}, null, () => new Tuple<bool, TextObject>(item1: false, null)));
		list.Add(new EscapeMenuItemVM(new TextObject("{=NqarFr4P}Options"), delegate
		{
			OnEscapeMenuToggled(isOpened: false);
			_missionOptionsComponent?.OnAddOptionsUIHandler();
		}, null, () => new Tuple<bool, TextObject>(item1: false, null)));
		MultiplayerTeamSelectComponent missionTeamSelectComponent = _missionTeamSelectComponent;
		if (missionTeamSelectComponent != null && missionTeamSelectComponent.TeamSelectionEnabled && !GameNetwork.MyPeer.IsSpectator)
		{
			list.Add(new EscapeMenuItemVM(new TextObject("{=2SEofGth}Change Team"), delegate
			{
				OnEscapeMenuToggled(isOpened: false);
				if (_missionTeamSelectComponent != null)
				{
					_missionTeamSelectComponent.SelectTeam();
				}
			}, null, () => new Tuple<bool, TextObject>(item1: false, null)));
		}
		if (_gameModeClient.IsGameModeUsingAllowCultureChange)
		{
			_changeCultureItem = new EscapeMenuItemVM(new TextObject("{=aGGq9lJT}Change Culture"), delegate
			{
				OnEscapeMenuToggled(isOpened: false);
				_missionLobbyComponent.RequestCultureSelection();
			}, null, () => new Tuple<bool, TextObject>(item1: false, null));
			list.Add(_changeCultureItem);
		}
		if (_gameModeClient.IsGameModeUsingAllowTroopChange)
		{
			_changeTroopItem = new EscapeMenuItemVM(new TextObject("{=Yza0JYJt}Change Troop"), delegate
			{
				OnEscapeMenuToggled(isOpened: false);
				_missionLobbyComponent.RequestTroopSelection();
			}, null, () => new Tuple<bool, TextObject>(item1: false, null));
			list.Add(_changeTroopItem);
		}
		if (base.Mission.CurrentState == TaleWorlds.MountAndBlade.Mission.State.Continuing && base.Mission.GetMissionEndTimerValue() < 0f && ((GameNetwork.IsMyPeerReady && GameNetwork.MyPeer.IsAdmin) || GameNetwork.IsServer))
		{
			EscapeMenuItemVM item = new EscapeMenuItemVM(new TextObject("{=xILeUbY3}Admin Panel"), delegate
			{
				OnEscapeMenuToggled(isOpened: false);
				if (_missionAdminComponent != null)
				{
					_missionAdminComponent.ChangeAdminMenuActiveState(isActive: true);
				}
			}, null, () => new Tuple<bool, TextObject>(item1: false, null));
			list.Add(item);
		}
		list.Add(new EscapeMenuItemVM(new TextObject("{=InGwtrWt}Quit"), delegate
		{
			InformationManager.ShowInquiry(new InquiryData(new TextObject("{=InGwtrWt}Quit").ToString(), new TextObject("{=lxq6SaQn}Are you sure want to quit?").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
			{
				LobbyClient gameClient = NetworkMain.GameClient;
				CommunityClient communityClient = NetworkMain.CommunityClient;
				if (communityClient.IsInGame)
				{
					communityClient.QuitFromGame();
				}
				else if (gameClient.CurrentState == LobbyClient.State.InCustomGame)
				{
					gameClient.QuitFromCustomGame();
				}
				else if (gameClient.CurrentState == LobbyClient.State.HostingCustomGame)
				{
					gameClient.EndCustomGame();
				}
				else
				{
					gameClient.QuitFromMatchmakerGame();
				}
			}, null));
		}, null, () => new Tuple<bool, TextObject>(item1: false, null)));
		return list;
	}
}
