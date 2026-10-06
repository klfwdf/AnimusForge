using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class BurstAllHeavyHitParticles : GameNetworkMessage
{
	public MissionObjectId MissionObjectId { get; private set; }

	public BurstAllHeavyHitParticles(MissionObjectId missionObjectId)
	{
		MissionObjectId = missionObjectId;
	}

	public BurstAllHeavyHitParticles()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MissionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObjectId);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionObjectsDetailed | MultiplayerMessageFilter.SiegeWeaponsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Bursting all heavy-hit particles for the DestructableComponent of MissionObject with Id: " + MissionObjectId;
	}
}
