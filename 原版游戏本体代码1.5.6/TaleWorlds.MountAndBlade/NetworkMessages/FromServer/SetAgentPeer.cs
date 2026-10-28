using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetAgentPeer : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public NetworkCommunicator Peer { get; private set; }

	public SetAgentPeer(int agentIndex, NetworkCommunicator peer)
	{
		AgentIndex = agentIndex;
		Peer = peer;
	}

	public SetAgentPeer()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid, canReturnNull: true);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Peer);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Peers | MultiplayerMessageFilter.Agents;
	}

	protected override string OnGetLogFormat()
	{
		if (AgentIndex < 0)
		{
			return "Ignoring the message for invalid agent.";
		}
		return "Set NetworkPeer " + ((Peer != null) ? "" : "(to NULL) ") + "on Agent with agent-index: " + AgentIndex;
	}
}
