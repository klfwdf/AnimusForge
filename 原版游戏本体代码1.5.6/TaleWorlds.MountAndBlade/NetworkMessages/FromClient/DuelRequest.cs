using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class DuelRequest : GameNetworkMessage
{
	public int RequestedAgentIndex { get; private set; }

	public DuelRequest(int requestedAgentIndex)
	{
		RequestedAgentIndex = requestedAgentIndex;
	}

	public DuelRequest()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		RequestedAgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(RequestedAgentIndex);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.GameMode;
	}

	protected override string OnGetLogFormat()
	{
		return "Duel requested from agent with index: " + RequestedAgentIndex;
	}
}
