using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetAgentOwningMissionPeer : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public VirtualPlayer Peer { get; private set; }

	public SetAgentOwningMissionPeer(int agentIndex, VirtualPlayer peer)
	{
		AgentIndex = agentIndex;
		Peer = peer;
	}

	public SetAgentOwningMissionPeer()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		Peer = GameNetworkMessage.ReadVirtualPlayerReferenceToPacket(ref bufferReadValid, canReturnNull: true);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteVirtualPlayerReferenceToPacket(Peer);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Peers | MultiplayerMessageFilter.Agents;
	}

	protected override string OnGetLogFormat()
	{
		return string.Format("SetAgentOwningMissionPeer for agent-index: {0} to {1}", AgentIndex, Peer?.UserName ?? "null");
	}
}
