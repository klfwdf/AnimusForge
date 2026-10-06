using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class SetMachineRotation : GameNetworkMessage
{
	public MissionObjectId UsableMachineId { get; private set; }

	public float HorizontalRotation { get; private set; }

	public float VerticalRotation { get; private set; }

	public SetMachineRotation(MissionObjectId missionObjectId, float horizontalRotation, float verticalRotation)
	{
		UsableMachineId = missionObjectId;
		HorizontalRotation = horizontalRotation;
		VerticalRotation = verticalRotation;
	}

	public SetMachineRotation()
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
		return "Set rotation of UsableMachine with ID: " + UsableMachineId;
	}
}
