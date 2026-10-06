using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class CreateMissionObject : GameNetworkMessage
{
	public MissionObjectId ObjectId { get; private set; }

	public string Prefab { get; private set; }

	public MatrixFrame Frame { get; private set; }

	public List<MissionObjectId> ChildObjectIds { get; private set; }

	public CreateMissionObject(MissionObjectId objectId, string prefab, MatrixFrame frame, List<MissionObjectId> childObjectIds)
	{
		ObjectId = objectId;
		Prefab = prefab;
		Frame = frame;
		ChildObjectIds = childObjectIds;
	}

	public CreateMissionObject()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		ObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		Prefab = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		Frame = GameNetworkMessage.ReadMatrixFrameFromPacket(ref bufferReadValid);
		int num = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.EntityChildCountCompressionInfo, ref bufferReadValid);
		if (bufferReadValid)
		{
			ChildObjectIds = new List<MissionObjectId>(num);
			for (int i = 0; i < num; i++)
			{
				if (bufferReadValid)
				{
					ChildObjectIds.Add(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid));
				}
			}
		}
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(ObjectId);
		GameNetworkMessage.WriteStringToPacket(Prefab);
		GameNetworkMessage.WriteMatrixFrameToPacket(Frame);
		GameNetworkMessage.WriteIntToPacket(ChildObjectIds.Count, CompressionBasic.EntityChildCountCompressionInfo);
		foreach (MissionObjectId childObjectId in ChildObjectIds)
		{
			GameNetworkMessage.WriteMissionObjectIdToPacket(childObjectId);
		}
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionObjects;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Create a MissionObject with index: ", ObjectId, " from prefab: ", Prefab, " at frame: ", Frame);
	}
}
