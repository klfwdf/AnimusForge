using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetSiegeTowerGateState : GameNetworkMessage
{
	public MissionObjectId SiegeTowerId { get; private set; }

	public SiegeTower.GateState State { get; private set; }

	public SetSiegeTowerGateState(MissionObjectId siegeTowerId, SiegeTower.GateState state)
	{
		SiegeTowerId = siegeTowerId;
		State = state;
	}

	public SetSiegeTowerGateState()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		SiegeTowerId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		State = (SiegeTower.GateState)GameNetworkMessage.ReadIntFromPacket(CompressionMission.SiegeTowerGateStateCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(SiegeTowerId);
		GameNetworkMessage.WriteIntToPacket((int)State, CompressionMission.SiegeTowerGateStateCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.SiegeWeaponsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Set SiegeTower State to: ", State, " on SiegeTower with ID: ", SiegeTowerId);
	}
}
