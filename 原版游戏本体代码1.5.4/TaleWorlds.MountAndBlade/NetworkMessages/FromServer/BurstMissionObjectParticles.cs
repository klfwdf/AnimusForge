using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class BurstMissionObjectParticles : GameNetworkMessage
{
	public MissionObjectId MissionObjectId { get; private set; }

	public bool DoChildren { get; private set; }

	public BurstMissionObjectParticles(MissionObjectId missionObjectId, bool doChildren)
	{
		MissionObjectId = missionObjectId;
		DoChildren = doChildren;
	}

	public BurstMissionObjectParticles()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MissionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		DoChildren = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObjectId);
		GameNetworkMessage.WriteBoolToPacket(DoChildren);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionObjectsDetailed | MultiplayerMessageFilter.Particles;
	}

	protected override string OnGetLogFormat()
	{
		return "Burst MissionObject particles on MissionObject with ID: " + MissionObjectId;
	}
}
