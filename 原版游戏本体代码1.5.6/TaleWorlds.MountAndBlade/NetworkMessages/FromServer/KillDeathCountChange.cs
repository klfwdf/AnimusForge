using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class KillDeathCountChange : GameNetworkMessage
{
	public NetworkCommunicator VictimPeer { get; private set; }

	public NetworkCommunicator AttackerPeer { get; private set; }

	public int KillCount { get; private set; }

	public int AssistCount { get; private set; }

	public int DeathCount { get; private set; }

	public int Score { get; private set; }

	public KillDeathCountChange(NetworkCommunicator peer, NetworkCommunicator attackerPeer, int killCount, int assistCount, int deathCount, int score)
	{
		VictimPeer = peer;
		AttackerPeer = attackerPeer;
		KillCount = killCount;
		AssistCount = assistCount;
		DeathCount = deathCount;
		Score = score;
	}

	public KillDeathCountChange()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		VictimPeer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		AttackerPeer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid, canReturnNull: true);
		KillCount = GameNetworkMessage.ReadIntFromPacket(CompressionMatchmaker.KillDeathAssistCountCompressionInfo, ref bufferReadValid);
		AssistCount = GameNetworkMessage.ReadIntFromPacket(CompressionMatchmaker.KillDeathAssistCountCompressionInfo, ref bufferReadValid);
		DeathCount = GameNetworkMessage.ReadIntFromPacket(CompressionMatchmaker.KillDeathAssistCountCompressionInfo, ref bufferReadValid);
		Score = GameNetworkMessage.ReadIntFromPacket(CompressionMatchmaker.ScoreCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(VictimPeer);
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(AttackerPeer);
		GameNetworkMessage.WriteIntToPacket(KillCount, CompressionMatchmaker.KillDeathAssistCountCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(AssistCount, CompressionMatchmaker.KillDeathAssistCountCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(DeathCount, CompressionMatchmaker.KillDeathAssistCountCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(Score, CompressionMatchmaker.ScoreCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return "Kill-Death Count Changed. Peer: " + (VictimPeer?.UserName ?? "NULL") + " killed peer: " + (AttackerPeer?.UserName ?? "NULL") + " and now has " + KillCount + " kills, " + AssistCount + " assists, and " + DeathCount + " deaths.";
	}
}
