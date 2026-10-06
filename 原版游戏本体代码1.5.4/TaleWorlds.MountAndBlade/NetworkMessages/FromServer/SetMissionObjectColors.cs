using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetMissionObjectColors : GameNetworkMessage
{
	public MissionObjectId MissionObjectId { get; private set; }

	public uint Color { get; private set; }

	public uint Color2 { get; private set; }

	public SetMissionObjectColors(MissionObjectId missionObjectId, uint color, uint color2)
	{
		MissionObjectId = missionObjectId;
		Color = color;
		Color2 = color2;
	}

	public SetMissionObjectColors()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MissionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		Color = GameNetworkMessage.ReadUintFromPacket(CompressionBasic.ColorCompressionInfo, ref bufferReadValid);
		Color2 = GameNetworkMessage.ReadUintFromPacket(CompressionBasic.ColorCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObjectId);
		GameNetworkMessage.WriteUintToPacket(Color, CompressionBasic.ColorCompressionInfo);
		GameNetworkMessage.WriteUintToPacket(Color2, CompressionBasic.ColorCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionObjects;
	}

	protected override string OnGetLogFormat()
	{
		return "Set Colors of MissionObject with ID: " + MissionObjectId;
	}
}
