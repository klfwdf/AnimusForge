using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetMachineTargetRotation : GameNetworkMessage
{
	public MissionObjectId UsableMachineId { get; private set; }

	public float HorizontalRotation { get; private set; }

	public float VerticalRotation { get; private set; }

	public SetMachineTargetRotation(MissionObjectId usableMachineId, float horizontalRotaiton, float verticalRotation)
	{
		UsableMachineId = usableMachineId;
		HorizontalRotation = horizontalRotaiton;
		VerticalRotation = verticalRotation;
	}

	public SetMachineTargetRotation()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		UsableMachineId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		HorizontalRotation = GameNetworkMessage.ReadFloatFromPacket(CompressionBasic.HighResRadianCompressionInfo, ref bufferReadValid);
		VerticalRotation = GameNetworkMessage.ReadFloatFromPacket(CompressionBasic.HighResRadianCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(UsableMachineId);
		GameNetworkMessage.WriteFloatToPacket(HorizontalRotation, CompressionBasic.HighResRadianCompressionInfo);
		GameNetworkMessage.WriteFloatToPacket(VerticalRotation, CompressionBasic.HighResRadianCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.SiegeWeaponsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Set target rotation of UsableMachine with ID: " + UsableMachineId;
	}
}
