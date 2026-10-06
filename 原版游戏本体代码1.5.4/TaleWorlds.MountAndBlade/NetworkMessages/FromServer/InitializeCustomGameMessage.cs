using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class InitializeCustomGameMessage : GameNetworkMessage
{
	public bool InMission { get; private set; }

	public string GameType { get; private set; }

	public string Map { get; private set; }

	public int BattleIndex { get; private set; }

	public InitializeCustomGameMessage(bool inMission, string gameType, string map, int battleIndex)
	{
		InMission = inMission;
		GameType = gameType;
		Map = map;
		BattleIndex = battleIndex;
	}

	public InitializeCustomGameMessage()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		InMission = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		GameType = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		Map = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		BattleIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.AutomatedBattleIndexCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteBoolToPacket(InMission);
		GameNetworkMessage.WriteStringToPacket(GameType);
		GameNetworkMessage.WriteStringToPacket(Map);
		GameNetworkMessage.WriteIntToPacket(BattleIndex, CompressionMission.AutomatedBattleIndexCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Mission;
	}

	protected override string OnGetLogFormat()
	{
		return "Initialize Custom Game";
	}
}
