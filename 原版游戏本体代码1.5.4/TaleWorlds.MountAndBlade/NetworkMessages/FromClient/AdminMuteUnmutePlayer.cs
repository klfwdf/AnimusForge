using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class AdminMuteUnmutePlayer : GameNetworkMessage
{
	public NetworkCommunicator PlayerPeer { get; private set; }

	public bool Unmute { get; private set; }

	public AdminMuteUnmutePlayer(NetworkCommunicator playerPeer, bool unmute)
	{
		PlayerPeer = playerPeer;
		Unmute = unmute;
	}

	public AdminMuteUnmutePlayer()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		PlayerPeer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		Unmute = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(PlayerPeer);
		GameNetworkMessage.WriteBoolToPacket(Unmute);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return "Requested to " + (Unmute ? " unmute" : "mute") + " player: " + PlayerPeer.UserName;
	}
}
