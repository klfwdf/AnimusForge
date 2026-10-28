using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetMissionObjectVisibility : GameNetworkMessage
{
	public MissionObjectId MissionObjectId { get; private set; }

	public bool Visible { get; private set; }

	public SetMissionObjectVisibility(MissionObjectId missionObjectId, bool visible)
	{
		MissionObjectId = missionObjectId;
		Visible = visible;
	}

	public SetMissionObjectVisibility()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MissionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		Visible = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObjectId);
		GameNetworkMessage.WriteBoolToPacket(Visible);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionObjects;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Set Visibility of MissionObject with ID: ", MissionObjectId, " to: ", Visible ? "True" : "False");
	}
}
