using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetSiegeLadderState : GameNetworkMessage
{
	public MissionObjectId SiegeLadderId { get; private set; }

	public SiegeLadder.LadderState State { get; private set; }

	public SetSiegeLadderState(MissionObjectId siegeLadderId, SiegeLadder.LadderState state)
	{
		SiegeLadderId = siegeLadderId;
		State = state;
	}

	public SetSiegeLadderState()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		SiegeLadderId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		State = (SiegeLadder.LadderState)GameNetworkMessage.ReadIntFromPacket(CompressionMission.SiegeLadderStateCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(SiegeLadderId);
		GameNetworkMessage.WriteIntToPacket((int)State, CompressionMission.SiegeLadderStateCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.SiegeWeaponsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Set SiegeLadder State to: ", State, " on SiegeLadderState with ID: ", SiegeLadderId);
	}
}
