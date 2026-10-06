using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetSiegeTowerHasArrivedAtTarget : GameNetworkMessage
{
	public MissionObjectId SiegeTowerId { get; private set; }

	public SetSiegeTowerHasArrivedAtTarget(MissionObjectId siegeTowerId)
	{
		SiegeTowerId = siegeTowerId;
	}

	public SetSiegeTowerHasArrivedAtTarget()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		SiegeTowerId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(SiegeTowerId);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.SiegeWeapons;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("SiegeTower with ID: ", SiegeTowerId, " has arrived at its target.");
	}
}
