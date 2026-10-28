using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class BotData : GameNetworkMessage
{
	public BattleSideEnum Side { get; private set; }

	public int KillCount { get; private set; }

	public int AssistCount { get; private set; }

	public int DeathCount { get; private set; }

	public int AliveBotCount { get; private set; }

	public BotData(BattleSideEnum side, int kill, int assist, int death, int alive)
	{
		Side = side;
		KillCount = kill;
		AssistCount = assist;
		DeathCount = death;
		AliveBotCount = alive;
	}

	public BotData()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Side = (BattleSideEnum)GameNetworkMessage.ReadIntFromPacket(CompressionMission.TeamSideCompressionInfo, ref bufferReadValid);
		KillCount = GameNetworkMessage.ReadIntFromPacket(CompressionMatchmaker.KillDeathAssistCountCompressionInfo, ref bufferReadValid);
		AssistCount = GameNetworkMessage.ReadIntFromPacket(CompressionMatchmaker.KillDeathAssistCountCompressionInfo, ref bufferReadValid);
		DeathCount = GameNetworkMessage.ReadIntFromPacket(CompressionMatchmaker.KillDeathAssistCountCompressionInfo, ref bufferReadValid);
		AliveBotCount = GameNetworkMessage.ReadIntFromPacket(CompressionMission.AgentCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket((int)Side, CompressionMission.TeamSideCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(KillCount, CompressionMatchmaker.KillDeathAssistCountCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(AssistCount, CompressionMatchmaker.KillDeathAssistCountCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(DeathCount, CompressionMatchmaker.KillDeathAssistCountCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(AliveBotCount, CompressionMission.AgentCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.General;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("BOTS for side: ", Side, ", Kill: ", KillCount, " Death: ", DeathCount, " Assist: ", AssistCount, ", Alive: ", AliveBotCount);
	}
}
