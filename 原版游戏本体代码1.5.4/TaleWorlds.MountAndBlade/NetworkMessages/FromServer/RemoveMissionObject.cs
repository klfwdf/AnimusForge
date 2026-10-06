using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class RemoveMissionObject : GameNetworkMessage
{
	public MissionObjectId ObjectId { get; private set; }

	public RemoveMissionObject(MissionObjectId objectId)
	{
		ObjectId = objectId;
	}

	public RemoveMissionObject()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		ObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(ObjectId);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionObjects;
	}

	protected override string OnGetLogFormat()
	{
		return "Remove MissionObject with ID: " + ObjectId;
	}
}
