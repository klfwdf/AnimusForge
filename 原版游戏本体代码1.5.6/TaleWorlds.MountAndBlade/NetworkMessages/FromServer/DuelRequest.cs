using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class DuelRequest : GameNetworkMessage
{
	public int RequesterAgentIndex { get; private set; }

	public int RequestedAgentIndex { get; private set; }

	public TroopType SelectedAreaTroopType { get; private set; }

	public DuelRequest(int requesterAgentIndex, int requestedAgentIndex, TroopType selectedAreaTroopType)
	{
		RequesterAgentIndex = requesterAgentIndex;
		RequestedAgentIndex = requestedAgentIndex;
		SelectedAreaTroopType = selectedAreaTroopType;
	}

	public DuelRequest()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		RequesterAgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		RequestedAgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		SelectedAreaTroopType = (TroopType)GameNetworkMessage.ReadIntFromPacket(CompressionBasic.TroopTypeCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(RequesterAgentIndex);
		GameNetworkMessage.WriteAgentIndexToPacket(RequestedAgentIndex);
		GameNetworkMessage.WriteIntToPacket((int)SelectedAreaTroopType, CompressionBasic.TroopTypeCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.GameMode;
	}

	protected override string OnGetLogFormat()
	{
		return "Request duel from agent with index: " + RequestedAgentIndex;
	}
}
