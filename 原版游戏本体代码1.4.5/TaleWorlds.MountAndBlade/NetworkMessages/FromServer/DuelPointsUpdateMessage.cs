using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.MissionRepresentatives;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class DuelPointsUpdateMessage : GameNetworkMessage
{
	public int Bounty { get; private set; }

	public int Score { get; private set; }

	public int NumberOfWins { get; private set; }

	public NetworkCommunicator NetworkCommunicator { get; private set; }

	public DuelPointsUpdateMessage()
	{
	}

	public DuelPointsUpdateMessage(DuelMissionRepresentative representative)
	{
		Bounty = representative.Bounty;
		Score = representative.Score;
		NumberOfWins = representative.NumberOfWins;
		NetworkCommunicator = representative.GetNetworkPeer();
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(Bounty, CompressionMatchmaker.ScoreCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(Score, CompressionMatchmaker.ScoreCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(NumberOfWins, CompressionMatchmaker.KillDeathAssistCountCompressionInfo);
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(NetworkCommunicator);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Bounty = GameNetworkMessage.ReadIntFromPacket(CompressionMatchmaker.ScoreCompressionInfo, ref bufferReadValid);
		Score = GameNetworkMessage.ReadIntFromPacket(CompressionMatchmaker.ScoreCompressionInfo, ref bufferReadValid);
		NumberOfWins = GameNetworkMessage.ReadIntFromPacket(CompressionMatchmaker.KillDeathAssistCountCompressionInfo, ref bufferReadValid);
		NetworkCommunicator = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.GameMode;
	}

	protected override string OnGetLogFormat()
	{
		return "PointUpdateMessage";
	}
}
