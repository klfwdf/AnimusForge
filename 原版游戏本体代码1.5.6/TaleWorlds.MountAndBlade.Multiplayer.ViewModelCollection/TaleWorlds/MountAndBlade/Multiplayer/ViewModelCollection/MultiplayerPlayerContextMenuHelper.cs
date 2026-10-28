using System;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Friends;
using TaleWorlds.PlatformService;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;

public static class MultiplayerPlayerContextMenuHelper
{
	public static void AddLobbyViewProfileOptions(MPLobbyPlayerBaseVM player, MBBindingList<StringPairItemWithActionVM> contextMenuOptions)
	{
		contextMenuOptions.Add(new StringPairItemWithActionVM(ExecuteViewProfile, new TextObject("{=bjJkW9dO}View Profile").ToString(), "ViewProfile", player));
		AddPlatformProfileCardOption(ExecuteViewPlatformProfileCardLobby, player, player.ProvidedID, contextMenuOptions);
	}

	public static void AddMissionViewProfileOptions(MPPlayerVM player, MBBindingList<StringPairItemWithActionVM> contextMenuOptions)
	{
		AddPlatformProfileCardOption(ExecuteViewPlatformProfileCardMission, player, player.Peer.Peer.Id, contextMenuOptions);
	}

	private static void AddPlatformProfileCardOption(Action<object> onExecuted, object target, PlayerId playerId, MBBindingList<StringPairItemWithActionVM> contextMenuOptions)
	{
		if (PlatformServices.Instance.IsPlayerProfileCardAvailable(NetworkMain.GameClient.PlayerID) && PlatformServices.Instance.IsPlayerProfileCardAvailable(playerId) && playerId.ProvidedType.SupportsPlayerCard())
		{
			TextObject empty = TextObject.GetEmpty();
			Debug.FailedAssert("Platform profile is supported but \"Show Profile\" text is not defined!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\MultiplayerPlayerContextMenuHelper.cs", "AddPlatformProfileCardOption", 51);
			if (!empty.IsEmpty())
			{
				contextMenuOptions.Add(new StringPairItemWithActionVM(onExecuted, empty.ToString(), "ViewProfile", target));
			}
		}
	}

	private static void ExecuteViewProfile(object playerObj)
	{
		(playerObj as MPLobbyPlayerBaseVM).ExecuteShowProfile();
	}

	private static void ExecuteViewPlatformProfileCardLobby(object playerObj)
	{
		PlatformServices.Instance.ShowPlayerProfileCard((playerObj as MPLobbyPlayerBaseVM).ProvidedID);
	}

	private static void ExecuteViewPlatformProfileCardMission(object playerObj)
	{
		PlatformServices.Instance.ShowPlayerProfileCard((playerObj as MPPlayerVM).Peer.Peer.Id);
	}
}
