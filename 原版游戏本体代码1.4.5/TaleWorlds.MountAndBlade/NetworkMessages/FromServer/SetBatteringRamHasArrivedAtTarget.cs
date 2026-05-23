using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetBatteringRamHasArrivedAtTarget : GameNetworkMessage
{
	public MissionObjectId BatteringRamId { get; private set; }

	public SetBatteringRamHasArrivedAtTarget(MissionObjectId batteringRamId)
	{
		BatteringRamId = batteringRamId;
	}

	public SetBatteringRamHasArrivedAtTarget()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		BatteringRamId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(BatteringRamId);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.SiegeWeaponsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Battering Ram with ID: ", BatteringRamId, " has arrived at its target.");
	}
}
