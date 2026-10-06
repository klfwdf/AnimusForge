using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class ReplaceBotWithPlayer : GameNetworkMessage
{
	public int BotAgentIndex { get; private set; }

	public NetworkCommunicator Peer { get; private set; }

	public int Health { get; private set; }

	public int MountHealth { get; private set; }

	public ReplaceBotWithPlayer(NetworkCommunicator peer, int botAgentIndex, float botAgentHealth, float botAgentMountHealth = -1f)
	{
		Peer = peer;
		BotAgentIndex = botAgentIndex;
		Health = MathF.Ceiling(botAgentHealth);
		MountHealth = MathF.Ceiling(botAgentMountHealth);
	}

	public ReplaceBotWithPlayer()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		BotAgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		Health = GameNetworkMessage.ReadIntFromPacket(CompressionMission.AgentHealthCompressionInfo, ref bufferReadValid);
		MountHealth = GameNetworkMessage.ReadIntFromPacket(CompressionMission.AgentHealthCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Peer);
		GameNetworkMessage.WriteAgentIndexToPacket(BotAgentIndex);
		GameNetworkMessage.WriteIntToPacket(Health, CompressionMission.AgentHealthCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(MountHealth, CompressionMission.AgentHealthCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Agents;
	}

	protected override string OnGetLogFormat()
	{
		return "Replace a bot with a player";
	}
}
