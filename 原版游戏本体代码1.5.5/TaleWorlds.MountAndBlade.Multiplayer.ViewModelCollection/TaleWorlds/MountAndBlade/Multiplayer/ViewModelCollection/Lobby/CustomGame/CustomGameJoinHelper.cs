using System;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.CustomGame;

public static class CustomGameJoinHelper
{
	public static event Action<bool> OnMapCheckingStateChanged;

	public static async Task JoinAsync(LobbyState lobbyState, GameServerEntry serverEntry, CustomGameJoinType joinType, string password)
	{
		if (NetworkMain.GameClient.CurrentState != LobbyClient.State.AtLobby)
		{
			ShowCouldntJoinMessage(new TextObject("{=KO2adj2I}You need to be in Lobby to join a custom game").ToString());
			return;
		}
		OnMapCheckingStateChanged?.Invoke(obj: true);
		(bool, string) tuple = await MapCheckHelpers.CheckMaps(serverEntry);
		OnMapCheckingStateChanged?.Invoke(obj: false);
		if (tuple.Item1)
		{
			lobbyState.OnClientRefusedToJoinCustomServer(serverEntry);
			string text = new TextObject("{=sVVaMyvb}You don't have at least one map ({MAP_NAME}) being played on the server or the local map is not identical. Download all missing maps from the server if you would like to join it.").SetTextVariable("MAP_NAME", tuple.Item2).ToString();
			InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_couldnt_join_server").ToString(), text, isAffirmativeOptionShown: false, isNegativeOptionShown: true, "", GameTexts.FindText("str_dismiss").ToString(), null, null));
		}
		else if (NetworkMain.GameClient.CurrentState != LobbyClient.State.AtLobby)
		{
			ShowCouldntJoinMessage(new TextObject("{=KO2adj2I}You need to be in Lobby to join a custom game").ToString());
		}
		else
		{
			await NetworkMain.GameClient.RequestJoinCustomGame(serverEntry.Id, joinType, password);
		}
	}

	public static async Task JoinByIdAsync(LobbyState lobbyState, CustomBattleId customBattleId, CustomGameJoinType joinType, string password)
	{
		if (NetworkMain.GameClient.CurrentState != LobbyClient.State.AtLobby)
		{
			ShowCouldntJoinMessage(new TextObject("{=KO2adj2I}You need to be in Lobby to join a custom game").ToString());
			return;
		}
		GameServerEntry gameServerEntry = (await NetworkMain.GameClient.GetCustomGameServerList())?.CustomGameServerInfos.Find((GameServerEntry entry) => entry.Id == customBattleId);
		if (gameServerEntry == null)
		{
			ShowCouldntJoinMessage(new TextObject("{=T8IniCKU}Game server is not available").ToString());
		}
		else
		{
			await JoinAsync(lobbyState, gameServerEntry, joinType, password);
		}
	}

	private static void ShowCouldntJoinMessage(string reason)
	{
		InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_couldnt_join_server").ToString(), reason, isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_ok").ToString(), "", null, null));
	}
}
