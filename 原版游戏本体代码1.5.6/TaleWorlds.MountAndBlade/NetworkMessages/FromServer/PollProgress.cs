using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class PollProgress : GameNetworkMessage
{
	public int VotesAccepted { get; private set; }

	public int VotesRejected { get; private set; }

	public PollProgress(int votesAccepted, int votesRejected)
	{
		VotesAccepted = votesAccepted;
		VotesRejected = votesRejected;
	}

	public PollProgress()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		VotesAccepted = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.PlayerCompressionInfo, ref bufferReadValid);
		VotesRejected = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.PlayerCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(VotesAccepted, CompressionBasic.PlayerCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(VotesRejected, CompressionBasic.PlayerCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return "Update on the voting progress.";
	}
}
