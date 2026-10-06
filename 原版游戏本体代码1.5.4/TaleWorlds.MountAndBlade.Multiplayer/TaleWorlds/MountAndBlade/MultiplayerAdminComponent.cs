using System;
using System.Collections.Generic;
using NetworkMessages.FromClient;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerAdminComponent : MissionNetwork
{
	public delegate void OnSelectPlayerToKickDelegate(bool banPlayer);

	public delegate void OnSetAdminMenuActiveStateDelegate(bool showMenu);

	private MissionLobbyComponent _missionLobbyComponent;

	private static MultiplayerAdminComponent _multiplayerAdminComponent;

	public event OnSetAdminMenuActiveStateDelegate OnSetAdminMenuActiveState;

	public MultiplayerAdminComponent()
	{
		if (string.IsNullOrEmpty(MultiplayerIntermissionVotingManager.Instance.InitialGameType))
		{
			MultiplayerIntermissionVotingManager.Instance.InitialGameType = MultiplayerOptions.OptionType.GameType.GetStrValue();
		}
	}

	public override void OnMissionStateActivated()
	{
		base.OnMissionStateActivated();
		MultiplayerIntermissionVotingManager.Instance.IsMapVoteEnabled = !MultiplayerIntermissionVotingManager.Instance.IsDisableMapVoteOverride;
		MultiplayerIntermissionVotingManager.Instance.IsCultureVoteEnabled = !MultiplayerIntermissionVotingManager.Instance.IsDisableCultureVoteOverride;
	}

	public void ChangeAdminMenuActiveState(bool isActive)
	{
		this.OnSetAdminMenuActiveState?.Invoke(isActive);
	}

	public void KickPlayer(NetworkCommunicator peerToKick, bool banPlayer)
	{
		if (GameNetwork.IsServer)
		{
			MissionPeer component = peerToKick.GetComponent<MissionPeer>();
			if (!peerToKick.IsMine && component != null && !peerToKick.IsAdmin)
			{
				DisconnectInfo disconnectInfo = peerToKick.PlayerConnectionInfo.GetParameter<DisconnectInfo>("DisconnectInfo") ?? new DisconnectInfo();
				disconnectInfo.Type = DisconnectType.KickedByHost;
				peerToKick.PlayerConnectionInfo.AddParameter("DisconnectInfo", disconnectInfo);
				GameNetwork.AddNetworkPeerToDisconnectAsServer(peerToKick);
				if (banPlayer)
				{
					CustomGameBannedPlayerManager.AddBannedPlayer(peerToKick.VirtualPlayer.Id, int.MaxValue);
				}
			}
		}
		else if (GameNetwork.IsClient && !peerToKick.IsMine)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new KickPlayer(peerToKick, banPlayer));
			GameNetwork.EndModuleEventAsClient();
		}
	}

	public void GlobalMuteUnmutePlayer(NetworkCommunicator peerToMute, bool unmute)
	{
		if (GameNetwork.IsServer)
		{
			MissionPeer component = peerToMute.GetComponent<MissionPeer>();
			if (peerToMute.IsMine || component == null || peerToMute.IsAdmin)
			{
				return;
			}
			PlayerId id = peerToMute.VirtualPlayer.Id;
			if (MultiplayerGlobalMutedPlayersManager.IsUserMuted(id) == unmute)
			{
				if (unmute)
				{
					MultiplayerGlobalMutedPlayersManager.UnmutePlayer(peerToMute.VirtualPlayer.Id);
				}
				else
				{
					MultiplayerGlobalMutedPlayersManager.MutePlayer(peerToMute.VirtualPlayer.Id);
				}
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SyncPlayerMuteState(id, !unmute));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}
		}
		else if (GameNetwork.IsClient && !peerToMute.IsMine)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new AdminMuteUnmutePlayer(peerToMute, unmute));
			GameNetwork.EndModuleEventAsClient();
		}
	}

	public void EndWarmup()
	{
		if (GameNetwork.IsServer)
		{
			if (Mission.Current != null)
			{
				Mission.Current.GetMissionBehavior<MultiplayerWarmupComponent>()?.EndWarmupProgress();
			}
		}
		else
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new AdminRequestEndWarmup());
			GameNetwork.EndModuleEventAsClient();
		}
	}

	public void ChangeWelcomeMessage(string newWelcomeMessage)
	{
		if (GameNetwork.IsServer)
		{
			MultiplayerOptions.OptionType.WelcomeMessage.SetValue(newWelcomeMessage);
			SyncImmediateOptions();
		}
		else if (MultiplayerOptions.OptionType.WelcomeMessage.GetStrValue() != newWelcomeMessage)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new ChangeWelcomeMessage(newWelcomeMessage));
			GameNetwork.EndModuleEventAsClient();
		}
	}

	public void AdminAnnouncement(string message, bool isBroadcast)
	{
		if (GameNetwork.IsServer)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new ServerAdminMessage(message, isBroadcast));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
		else
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new AdminRequestAnnouncement(message, isBroadcast));
			GameNetwork.EndModuleEventAsClient();
		}
	}

	public void ChangeClassRestriction(FormationClass classToChangeRestriction, bool newValue)
	{
		if (GameNetwork.IsServer)
		{
			_missionLobbyComponent.ChangeClassRestriction(classToChangeRestriction, newValue);
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new ChangeClassRestrictions(classToChangeRestriction, newValue));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
		else if (!_missionLobbyComponent.IsClassAvailable(classToChangeRestriction) != newValue)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new AdminRequestClassRestrictionChange(classToChangeRestriction, newValue));
			GameNetwork.EndModuleEventAsClient();
		}
	}

	public void AdminEndMission()
	{
		if (GameNetwork.IsServer)
		{
			_missionLobbyComponent.SetStateEndingAsServer();
			return;
		}
		GameNetwork.BeginModuleEventAsClient();
		GameNetwork.WriteMessage(new AdminRequestEndMission());
		GameNetwork.EndModuleEventAsClient();
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_missionLobbyComponent = Mission.Current.GetMissionBehavior<MissionLobbyComponent>();
		_missionLobbyComponent.OnAdminMessageRequested += AdminAnnouncement;
	}

	protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
	{
		if (GameNetwork.IsServer)
		{
			registerer.RegisterBaseHandler<KickPlayer>(HandleClientEventKickPlayer);
			registerer.RegisterBaseHandler<ChangeWelcomeMessage>(HandleClientEventChangeWelcomeMessage);
			registerer.RegisterBaseHandler<AdminRequestAnnouncement>(HandleClientEventAdminRequestAnnouncement);
			registerer.RegisterBaseHandler<AdminRequestClassRestrictionChange>(HandleClientEventAdminRequestClassRestrictionChange);
			registerer.RegisterBaseHandler<AdminRequestEndMission>(HandleClientEventAdminRequestEndMission);
			registerer.RegisterBaseHandler<AdminUpdateMultiplayerOptions>(HandleAdminUpdateMultiplayerOptions);
			registerer.RegisterBaseHandler<AdminMuteUnmutePlayer>(HandleClientEventMuteUnmutePlayer);
			registerer.RegisterBaseHandler<AdminRequestEndWarmup>(HandleClientEventAdminRequestEndWarmup);
		}
	}

	private bool HandleAdminUpdateMultiplayerOptions(NetworkCommunicator peer, GameNetworkMessage baseMessage)
	{
		AdminUpdateMultiplayerOptions adminUpdateMultiplayerOptions = (AdminUpdateMultiplayerOptions)baseMessage;
		if (peer.IsAdmin && adminUpdateMultiplayerOptions.Options != null)
		{
			bool flag = false;
			bool flag2 = false;
			string text = MultiplayerOptions.OptionType.GameType.GetStrValue();
			string text2 = null;
			string text3 = null;
			bool flag3 = false;
			bool flag4 = false;
			for (int i = 0; i < adminUpdateMultiplayerOptions.Options.Count; i++)
			{
				AdminUpdateMultiplayerOptions.AdminMultiplayerOptionInfo adminMultiplayerOptionInfo = adminUpdateMultiplayerOptions.Options[i];
				bool flag5 = true;
				if (adminMultiplayerOptionInfo.OptionType == MultiplayerOptions.OptionType.GameType)
				{
					flag5 = !string.IsNullOrEmpty(adminMultiplayerOptionInfo.StringValue);
					if (adminMultiplayerOptionInfo.StringValue == MultiplayerIntermissionVotingManager.Instance.InitialGameType || (!flag5 && MultiplayerOptions.OptionType.GameType.GetStrValue() == MultiplayerIntermissionVotingManager.Instance.InitialGameType))
					{
						flag = true;
					}
					if (flag5)
					{
						text = adminMultiplayerOptionInfo.StringValue;
					}
				}
				if (adminMultiplayerOptionInfo.OptionType == MultiplayerOptions.OptionType.Map)
				{
					flag5 = !string.IsNullOrEmpty(adminMultiplayerOptionInfo.StringValue);
					flag2 = !flag5;
				}
				if (adminMultiplayerOptionInfo.OptionType == MultiplayerOptions.OptionType.CultureTeam1)
				{
					flag3 = !string.IsNullOrEmpty(adminMultiplayerOptionInfo.StringValue);
					text2 = (flag3 ? adminMultiplayerOptionInfo.StringValue : null);
				}
				else if (adminMultiplayerOptionInfo.OptionType == MultiplayerOptions.OptionType.CultureTeam2)
				{
					flag4 = !string.IsNullOrEmpty(adminMultiplayerOptionInfo.StringValue);
					text3 = (flag4 ? adminMultiplayerOptionInfo.StringValue : null);
				}
				else if (flag5)
				{
					switch (adminMultiplayerOptionInfo.OptionType.GetOptionProperty().OptionValueType)
					{
					case MultiplayerOptions.OptionValueType.Bool:
						adminMultiplayerOptionInfo.OptionType.SetValue(adminMultiplayerOptionInfo.BoolValue, adminMultiplayerOptionInfo.AccessMode);
						break;
					case MultiplayerOptions.OptionValueType.Integer:
					case MultiplayerOptions.OptionValueType.Enum:
						adminMultiplayerOptionInfo.OptionType.SetValue(adminMultiplayerOptionInfo.IntValue, adminMultiplayerOptionInfo.AccessMode);
						break;
					case MultiplayerOptions.OptionValueType.String:
						adminMultiplayerOptionInfo.OptionType.SetValue(adminMultiplayerOptionInfo.StringValue, adminMultiplayerOptionInfo.AccessMode);
						break;
					}
				}
			}
			if (flag2)
			{
				MultiplayerIntermissionVotingManager.Instance.IsMapSelectedByAdmin = false;
				if (flag)
				{
					if (MultiplayerIntermissionVotingManager.Instance.IsDisableMapVoteOverride)
					{
						MultiplayerIntermissionVotingManager.Instance.IsMapVoteEnabled = false;
						string id = MultiplayerIntermissionVotingManager.Instance.MapVoteItems.GetRandomElement().Id;
						MultiplayerOptions.OptionType.Map.SetValue(id, MultiplayerOptions.MultiplayerOptionsAccessMode.NextMapOptions);
						Debug.Print("[Admin] game type was default and map was undecided. Voting was disabled. Selected map randomly from automated map pool: " + id);
					}
					else
					{
						MultiplayerIntermissionVotingManager.Instance.IsMapVoteEnabled = true;
						Debug.Print("[Admin] game type was default and map was undecided. Maps will be voted from automated map pool");
					}
				}
				else
				{
					MultiplayerIntermissionVotingManager.Instance.IsMapVoteEnabled = false;
					string randomElement = MultiplayerIntermissionVotingManager.Instance.GetUsableMaps(text).GetRandomElement();
					MultiplayerOptions.OptionType.Map.SetValue(randomElement, MultiplayerOptions.MultiplayerOptionsAccessMode.NextMapOptions);
					Debug.Print("[Admin] game type wasn't default and map was undecided. Selected map randomly from usable maps: " + randomElement + ".");
				}
			}
			else
			{
				MultiplayerIntermissionVotingManager.Instance.IsMapVoteEnabled = false;
				MultiplayerIntermissionVotingManager.Instance.IsMapSelectedByAdmin = true;
				Debug.Print("[Admin] next game type: " + text + " next map: " + MultiplayerOptions.OptionType.Map.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.NextMapOptions));
			}
			if (flag3 && flag4)
			{
				MultiplayerIntermissionVotingManager.Instance.IsCultureVoteEnabled = false;
				MultiplayerOptions.OptionType.CultureTeam1.SetValue(text2, MultiplayerOptions.MultiplayerOptionsAccessMode.NextMapOptions);
				MultiplayerOptions.OptionType.CultureTeam2.SetValue(text3, MultiplayerOptions.MultiplayerOptionsAccessMode.NextMapOptions);
				Debug.Print("[Admin] Both cultures were valid. Setting " + text2 + " vs " + text3 + " for next game.");
			}
			else if (MultiplayerIntermissionVotingManager.Instance.IsDisableCultureVoteOverride)
			{
				MultiplayerIntermissionVotingManager.Instance.IsCultureVoteEnabled = false;
				MultiplayerIntermissionVotingManager.Instance.SelectRandomCultures(MultiplayerOptions.MultiplayerOptionsAccessMode.NextMapOptions);
				string strValue = MultiplayerOptions.OptionType.CultureTeam1.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.NextMapOptions);
				string strValue2 = MultiplayerOptions.OptionType.CultureTeam2.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.NextMapOptions);
				Debug.Print("[Admin] Cultures weren't valid. Randomly setting " + strValue + " vs " + strValue2 + " for next game.");
			}
			else
			{
				MultiplayerIntermissionVotingManager.Instance.IsCultureVoteEnabled = true;
				Debug.Print("[Admin] Cultures weren't valid. Culture voting is enabled");
			}
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new MultiplayerOptionsImmediate());
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			GameNetwork.BeginModuleEventAsServer(peer);
			GameNetwork.WriteMessage(new UpdateIntermissionVotingManagerValues());
			GameNetwork.EndModuleEventAsServer();
		}
		return true;
	}

	private bool HandleClientEventKickPlayer(NetworkCommunicator peer, GameNetworkMessage baseMessage)
	{
		KickPlayer kickPlayer = (KickPlayer)baseMessage;
		if (peer.IsAdmin)
		{
			KickPlayer(kickPlayer.PlayerPeer, kickPlayer.BanPlayer);
		}
		return true;
	}

	private bool HandleClientEventMuteUnmutePlayer(NetworkCommunicator peer, GameNetworkMessage baseMessage)
	{
		AdminMuteUnmutePlayer adminMuteUnmutePlayer = (AdminMuteUnmutePlayer)baseMessage;
		if (peer.IsAdmin)
		{
			GlobalMuteUnmutePlayer(adminMuteUnmutePlayer.PlayerPeer, adminMuteUnmutePlayer.Unmute);
		}
		return true;
	}

	private bool HandleClientEventChangeWelcomeMessage(NetworkCommunicator peer, GameNetworkMessage baseMessage)
	{
		ChangeWelcomeMessage changeWelcomeMessage = (ChangeWelcomeMessage)baseMessage;
		if (peer.IsAdmin)
		{
			ChangeWelcomeMessage(changeWelcomeMessage.NewWelcomeMessage);
		}
		return true;
	}

	private bool HandleClientEventAdminRequestClassRestrictionChange(NetworkCommunicator peer, GameNetworkMessage baseMessage)
	{
		AdminRequestClassRestrictionChange adminRequestClassRestrictionChange = (AdminRequestClassRestrictionChange)baseMessage;
		if (peer.IsAdmin)
		{
			ChangeClassRestriction(adminRequestClassRestrictionChange.ClassToChangeRestriction, adminRequestClassRestrictionChange.NewValue);
		}
		return true;
	}

	private bool HandleClientEventAdminRequestAnnouncement(NetworkCommunicator peer, GameNetworkMessage baseMessage)
	{
		AdminRequestAnnouncement adminRequestAnnouncement = (AdminRequestAnnouncement)baseMessage;
		if (peer.IsAdmin)
		{
			AdminAnnouncement(adminRequestAnnouncement.Message, adminRequestAnnouncement.IsAdminBroadcast);
		}
		return true;
	}

	private bool HandleClientEventAdminRequestEndMission(NetworkCommunicator peer, GameNetworkMessage baseMessage)
	{
		_ = (AdminRequestEndMission)baseMessage;
		if (peer.IsAdmin)
		{
			AdminEndMission();
		}
		return true;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("announcement", "mp_admin")]
	public static string MPAdminAnnouncement(List<string> strings)
	{
		if (strings.Count == 0)
		{
			return "Wrong format! Usage: mp_admin.announcement {TEXT}";
		}
		if (Mission.Current == null)
		{
			return "Mission is not running!";
		}
		MultiplayerAdminComponent missionBehavior = Mission.Current.GetMissionBehavior<MultiplayerAdminComponent>();
		if (missionBehavior == null)
		{
			return "Admin component could not be found!";
		}
		missionBehavior.AdminAnnouncement(string.Join(" ", strings), isBroadcast: true);
		return "Success";
	}

	private bool HandleClientEventAdminRequestEndWarmup(NetworkCommunicator peer, GameNetworkMessage baseMessage)
	{
		_ = (AdminRequestEndWarmup)baseMessage;
		if (peer.IsAdmin)
		{
			EndWarmup();
		}
		return true;
	}

	public override void OnRemoveBehavior()
	{
		_multiplayerAdminComponent = null;
		base.OnRemoveBehavior();
	}

	private void SyncImmediateOptions()
	{
		GameNetwork.BeginBroadcastModuleEvent();
		GameNetwork.WriteMessage(new MultiplayerOptionsImmediate());
		GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("kick_player", "mp_admin")]
	public static string MPAdminKickPlayer(List<string> strings)
	{
		if (_multiplayerAdminComponent == null)
		{
			return "Failed: MultiplayerAdminComponent has not been created.";
		}
		NetworkCommunicator myPeer = GameNetwork.MyPeer;
		if (myPeer == null || !myPeer.IsAdmin)
		{
			return "Failed: Only admins can use mp_admin commands.";
		}
		if (strings.Count != 1)
		{
			return "Failed: Input is incorrect.";
		}
		string text = strings[0];
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			if (networkPeer.UserName == text)
			{
				_multiplayerAdminComponent.KickPlayer(networkPeer, banPlayer: false);
				return "Player " + text + " has been kicked from the server.";
			}
		}
		return text + " could not be found.";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("ban_player", "mp_admin")]
	public static string MPAdminBanPlayer(List<string> strings)
	{
		if (_multiplayerAdminComponent == null)
		{
			return "Failed: MultiplayerAdminComponent has not been created.";
		}
		NetworkCommunicator myPeer = GameNetwork.MyPeer;
		if (myPeer == null || !myPeer.IsAdmin)
		{
			return "Failed: Only admins can use mp_admin commands.";
		}
		if (strings.Count != 1)
		{
			return "Failed: Input is incorrect.";
		}
		string text = strings[0];
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			if (networkPeer.UserName == text)
			{
				_multiplayerAdminComponent.KickPlayer(networkPeer, banPlayer: true);
				return "Player " + text + " has been banned from the server.";
			}
		}
		return text + " could not be found.";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("change_welcome_message", "mp_admin")]
	public static string MPAdminChangeWelcomeMessage(List<string> strings)
	{
		if (_multiplayerAdminComponent == null)
		{
			return "Failed: MultiplayerAdminComponent has not been created.";
		}
		NetworkCommunicator myPeer = GameNetwork.MyPeer;
		if (myPeer == null || !myPeer.IsAdmin)
		{
			return "Failed: Only admins can use mp_host commands.";
		}
		string text = "";
		foreach (string @string in strings)
		{
			text = text + @string + " ";
		}
		_multiplayerAdminComponent.ChangeWelcomeMessage(text);
		return "Changed welcome message to: " + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("change_class_restriction", "mp_admin")]
	public static string MPAdminChangeClassRestriction(List<string> strings)
	{
		if (strings.Count != 2 || !Enum.TryParse<FormationClass>(strings[0], out var result) || !bool.TryParse(strings[1], out var result2))
		{
			return "Wrong format! Usage: mp_admin.change_class_restriction {FormationClass} {true/false}";
		}
		if (Mission.Current == null)
		{
			return "Mission is not running!";
		}
		MultiplayerAdminComponent missionBehavior = Mission.Current.GetMissionBehavior<MultiplayerAdminComponent>();
		if (missionBehavior == null)
		{
			return "Admin component could not be found!";
		}
		missionBehavior.ChangeClassRestriction(result, result2);
		return "Success";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("restart_game", "mp_admin")]
	public static string MPHostRestartGame(List<string> strings)
	{
		if (Mission.Current == null)
		{
			return "Mission is not running!";
		}
		MultiplayerAdminComponent missionBehavior = Mission.Current.GetMissionBehavior<MultiplayerAdminComponent>();
		if (missionBehavior == null)
		{
			return "Admin component could not be found!";
		}
		missionBehavior.AdminEndMission();
		return "Success";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("change_server_slots", "mp_admin")]
	public static string MPAdminChangeServerSlots(List<string> strings)
	{
		if (Mission.Current == null)
		{
			return "Mission is not running!";
		}
		if (Mission.Current.GetMissionBehavior<MultiplayerAdminComponent>() == null)
		{
			return "Admin component could not be found!";
		}
		if (strings.Count != 1)
		{
			return "Wrong format! Usage: mp_admin.change_server_slots {NUMBER}";
		}
		if (int.TryParse(strings[0], out var result))
		{
			MultiplayerOptions.OptionType.MaxNumberOfPlayers.SetValue(result, MultiplayerOptions.MultiplayerOptionsAccessMode.NextMapOptions);
			return "Success";
		}
		return "Wrong format! Usage: mp_admin.change_server_slots {NUMBER}";
	}
}
