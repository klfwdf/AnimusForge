using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetUsableMissionObjectIsDisabledForPlayers : GameNetworkMessage
{
	public MissionObjectId UsableGameObjectId { get; private set; }

	public bool IsDisabledForPlayers { get; private set; }

	public SetUsableMissionObjectIsDisabledForPlayers(MissionObjectId usableGameObjectId, bool isDisabledForPlayers)
	{
		UsableGameObjectId = usableGameObjectId;
		IsDisabledForPlayers = isDisabledForPlayers;
	}

	public SetUsableMissionObjectIsDisabledForPlayers()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		UsableGameObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		IsDisabledForPlayers = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(UsableGameObjectId);
		GameNetworkMessage.WriteBoolToPacket(IsDisabledForPlayers);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionObjects;
	}

	protected override string OnGetLogFormat()
	{
		return "Set IsDisabled for player: " + (IsDisabledForPlayers ? "True" : "False") + " on UsableMissionObject with ID: " + UsableGameObjectId;
	}
}
