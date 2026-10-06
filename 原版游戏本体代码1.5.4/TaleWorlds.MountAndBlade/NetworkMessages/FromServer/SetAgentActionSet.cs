using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetAgentActionSet : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public MBActionSet ActionSet { get; private set; }

	public int NumPaces { get; private set; }

	public int MonsterUsageSetIndex { get; private set; }

	public float WalkingSpeedLimit { get; private set; }

	public float CrouchWalkingSpeedLimit { get; private set; }

	public float StepSize { get; private set; }

	public SetAgentActionSet(int agentIndex, AnimationSystemData animationSystemData)
	{
		AgentIndex = agentIndex;
		ActionSet = animationSystemData.ActionSet;
		NumPaces = animationSystemData.NumPaces;
		MonsterUsageSetIndex = animationSystemData.MonsterUsageSetIndex;
		WalkingSpeedLimit = animationSystemData.WalkingSpeedLimit;
		CrouchWalkingSpeedLimit = animationSystemData.CrouchWalkingSpeedLimit;
		StepSize = animationSystemData.StepSize;
	}

	public SetAgentActionSet()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		ActionSet = GameNetworkMessage.ReadActionSetReferenceFromPacket(CompressionMission.ActionSetCompressionInfo, ref bufferReadValid);
		NumPaces = GameNetworkMessage.ReadIntFromPacket(CompressionMission.NumberOfPacesCompressionInfo, ref bufferReadValid);
		MonsterUsageSetIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.MonsterUsageSetCompressionInfo, ref bufferReadValid);
		WalkingSpeedLimit = GameNetworkMessage.ReadFloatFromPacket(CompressionMission.WalkingSpeedLimitCompressionInfo, ref bufferReadValid);
		CrouchWalkingSpeedLimit = GameNetworkMessage.ReadFloatFromPacket(CompressionMission.WalkingSpeedLimitCompressionInfo, ref bufferReadValid);
		StepSize = GameNetworkMessage.ReadFloatFromPacket(CompressionMission.StepSizeCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteActionSetReferenceToPacket(ActionSet, CompressionMission.ActionSetCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(NumPaces, CompressionMission.NumberOfPacesCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(MonsterUsageSetIndex, CompressionMission.MonsterUsageSetCompressionInfo);
		GameNetworkMessage.WriteFloatToPacket(WalkingSpeedLimit, CompressionMission.WalkingSpeedLimitCompressionInfo);
		GameNetworkMessage.WriteFloatToPacket(CrouchWalkingSpeedLimit, CompressionMission.WalkingSpeedLimitCompressionInfo);
		GameNetworkMessage.WriteFloatToPacket(StepSize, CompressionMission.StepSizeCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.AgentAnimations;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Set ActionSet: ", ActionSet, " on agent with agent-index: ", AgentIndex);
	}
}
