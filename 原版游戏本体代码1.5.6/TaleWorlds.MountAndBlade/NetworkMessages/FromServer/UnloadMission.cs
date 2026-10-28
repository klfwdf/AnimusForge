using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class UnloadMission : GameNetworkMessage
{
	public bool UnloadingForBattleIndexMismatch { get; private set; }

	public UnloadMission()
	{
	}

	public UnloadMission(bool unloadingForBattleIndexMismatch)
	{
		UnloadingForBattleIndexMismatch = unloadingForBattleIndexMismatch;
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		UnloadingForBattleIndexMismatch = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteBoolToPacket(UnloadingForBattleIndexMismatch);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Mission;
	}

	protected override string OnGetLogFormat()
	{
		return "Unload Mission";
	}
}
