using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetUsableMissionObjectIsDeactivated : GameNetworkMessage
{
	public MissionObjectId UsableGameObjectId { get; private set; }

	public bool IsDeactivated { get; private set; }

	public SetUsableMissionObjectIsDeactivated(MissionObjectId usableGameObjectId, bool isDeactivated)
	{
		UsableGameObjectId = usableGameObjectId;
		IsDeactivated = isDeactivated;
	}

	public SetUsableMissionObjectIsDeactivated()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		UsableGameObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		IsDeactivated = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(UsableGameObjectId);
		GameNetworkMessage.WriteBoolToPacket(IsDeactivated);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionObjects;
	}

	protected override string OnGetLogFormat()
	{
		return "Set IsDeactivated: " + (IsDeactivated ? "True" : "False") + " on UsableMissionObject with ID: " + UsableGameObjectId;
	}
}
