using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetSiegeMachineMovementDistance : GameNetworkMessage
{
	public MissionObjectId UsableMachineId { get; private set; }

	public float Distance { get; private set; }

	public SetSiegeMachineMovementDistance(MissionObjectId usableMachineId, float distance)
	{
		UsableMachineId = usableMachineId;
		Distance = distance;
	}

	public SetSiegeMachineMovementDistance()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		UsableMachineId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		Distance = GameNetworkMessage.ReadFloatFromPacket(CompressionBasic.PositionCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(UsableMachineId);
		GameNetworkMessage.WriteFloatToPacket(Distance, CompressionBasic.PositionCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.SiegeWeaponsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Set Movement Distance: " + Distance + " of SiegeMachine with ID: " + UsableMachineId;
	}
}
