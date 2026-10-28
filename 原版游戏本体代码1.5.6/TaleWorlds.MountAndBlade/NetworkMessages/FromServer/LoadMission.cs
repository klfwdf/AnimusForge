using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class LoadMission : GameNetworkMessage
{
	public string GameType { get; private set; }

	public string Map { get; private set; }

	public int BattleIndex { get; private set; }

	public LoadMission(string gameType, string map, int battleIndex)
	{
		GameType = gameType;
		Map = map;
		BattleIndex = battleIndex;
	}

	public LoadMission()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		GameType = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		Map = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		BattleIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.AutomatedBattleIndexCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
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
		return "Load Mission";
	}
}
