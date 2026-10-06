using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade;

public class MissionRecentPlayersComponent : MissionNetwork
{
	private PlayerId _myId;

	public override void AfterStart()
	{
		base.AfterStart();
		MissionPeer.OnTeamChanged += TeamChange;
		MissionPeer.OnPlayerKilled += OnPlayerKilled;
		_myId = NetworkMain.GameClient.PlayerID;
	}

	private void TeamChange(NetworkCommunicator player, Team oldTeam, Team nextTeam)
	{
		if (player.VirtualPlayer.Id != _myId)
		{
			RecentPlayersManager.AddOrUpdatePlayerEntry(player.VirtualPlayer.Id, player.UserName, InteractionType.InGameTogether, player.ForcedAvatarIndex);
		}
	}

	private void OnPlayerKilled(MissionPeer killerPeer, MissionPeer killedPeer)
	{
		if (killerPeer != null && killedPeer != null && killerPeer.Peer != null && killedPeer.Peer != null)
		{
			PlayerId id = killerPeer.Peer.Id;
			PlayerId id2 = killedPeer.Peer.Id;
			if (id == _myId && id2 != _myId)
			{
				RecentPlayersManager.AddOrUpdatePlayerEntry(id2, killedPeer.Name, InteractionType.Killed, killedPeer.GetNetworkPeer().ForcedAvatarIndex);
			}
			else if (id2 == _myId && id != _myId)
			{
				RecentPlayersManager.AddOrUpdatePlayerEntry(id, killerPeer.Name, InteractionType.KilledBy, killerPeer.GetNetworkPeer().ForcedAvatarIndex);
			}
		}
	}

	public override void OnRemoveBehavior()
	{
		MissionPeer.OnTeamChanged -= TeamChange;
		MissionPeer.OnPlayerKilled -= OnPlayerKilled;
		base.OnRemoveBehavior();
	}
}
