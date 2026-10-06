using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class AddMissionObjectBodyFlags : GameNetworkMessage
{
	public MissionObjectId MissionObjectId { get; private set; }

	public BodyFlags BodyFlags { get; private set; }

	public bool ApplyToChildren { get; private set; }

	public AddMissionObjectBodyFlags(MissionObjectId missionObjectId, BodyFlags bodyFlags, bool applyToChildren)
	{
		MissionObjectId = missionObjectId;
		BodyFlags = bodyFlags;
		ApplyToChildren = applyToChildren;
	}

	public AddMissionObjectBodyFlags()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MissionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		BodyFlags = (BodyFlags)GameNetworkMessage.ReadIntFromPacket(CompressionBasic.FlagsCompressionInfo, ref bufferReadValid);
		ApplyToChildren = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObjectId);
		GameNetworkMessage.WriteIntToPacket((int)BodyFlags, CompressionBasic.FlagsCompressionInfo);
		GameNetworkMessage.WriteBoolToPacket(ApplyToChildren);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionObjectsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Add bodyflags: ", BodyFlags, " to MissionObject with ID: ", MissionObjectId, ApplyToChildren ? "" : " and to all of its children.");
	}
}
