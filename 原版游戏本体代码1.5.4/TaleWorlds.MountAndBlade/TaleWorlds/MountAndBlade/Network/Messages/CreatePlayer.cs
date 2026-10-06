namespace TaleWorlds.MountAndBlade.Network.Messages;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class CreatePlayer : GameNetworkMessage
{
	public int PlayerIndex { get; private set; }

	public string PlayerName { get; private set; }

	public int DisconnectedPeerIndex { get; private set; }

	public bool IsNonExistingDisconnectedPeer { get; private set; }

	public bool IsReceiverPeer { get; private set; }

	public bool IsSpectator { get; private set; }

	public bool IsAdmin { get; private set; }

	public CreatePlayer(int playerIndex, string playerName, int disconnectedPeerIndex, bool isNonExistingDisconnectedPeer = false, bool isReceiverPeer = false, bool isSpectator = false, bool isAdmin = false)
	{
		PlayerIndex = playerIndex;
		PlayerName = playerName;
		DisconnectedPeerIndex = disconnectedPeerIndex;
		IsNonExistingDisconnectedPeer = isNonExistingDisconnectedPeer;
		IsReceiverPeer = isReceiverPeer;
		IsSpectator = isSpectator;
		IsAdmin = isAdmin;
	}

	public CreatePlayer()
	{
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(PlayerIndex, CompressionBasic.PlayerCompressionInfo);
		GameNetworkMessage.WriteStringToPacket(PlayerName);
		GameNetworkMessage.WriteIntToPacket(DisconnectedPeerIndex, CompressionBasic.PlayerCompressionInfo);
		GameNetworkMessage.WriteBoolToPacket(IsNonExistingDisconnectedPeer);
		GameNetworkMessage.WriteBoolToPacket(IsReceiverPeer);
		GameNetworkMessage.WriteBoolToPacket(IsSpectator);
		GameNetworkMessage.WriteBoolToPacket(IsAdmin);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		PlayerIndex = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.PlayerCompressionInfo, ref bufferReadValid);
		PlayerName = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		DisconnectedPeerIndex = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.PlayerCompressionInfo, ref bufferReadValid);
		IsNonExistingDisconnectedPeer = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		IsReceiverPeer = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		IsSpectator = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		IsAdmin = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Peers;
	}

	protected override string OnGetLogFormat()
	{
		return "Create a new player with name: " + PlayerName + " and index: " + PlayerIndex + " and dcedIndex: " + DisconnectedPeerIndex + " which is " + ((!IsNonExistingDisconnectedPeer) ? "not" : "") + " a NonExistingDisconnectedPeer, isSpectator: " + IsSpectator.ToString() + ", isAdmin: " + IsAdmin.ToString();
	}
}
