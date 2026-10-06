using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.PlayerServices;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SyncMutedPlayers : GameNetworkMessage
{
	public int MutedPlayerCount { get; private set; }

	public List<PlayerId> MutedPlayerIds { get; private set; }

	public SyncMutedPlayers()
	{
	}

	public SyncMutedPlayers(List<PlayerId> mutedPlayerIds)
	{
		MutedPlayerIds = mutedPlayerIds;
		MutedPlayerCount = MutedPlayerIds?.Count ?? 0;
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MutedPlayerIds = new List<PlayerId>();
		MutedPlayerCount = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.IntermissionVoterCountCompressionInfo, ref bufferReadValid);
		for (int i = 0; i < MutedPlayerCount; i++)
		{
			ulong part = GameNetworkMessage.ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
			ulong part2 = GameNetworkMessage.ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
			ulong part3 = GameNetworkMessage.ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
			ulong part4 = GameNetworkMessage.ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
			MutedPlayerIds.Add(new PlayerId(part, part2, part3, part4));
		}
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(MutedPlayerCount, CompressionBasic.IntermissionVoterCountCompressionInfo);
		for (int i = 0; i < MutedPlayerCount; i++)
		{
			GameNetworkMessage.WriteUlongToPacket(MutedPlayerIds[i].Part1, CompressionBasic.DebugULongNonCompressionInfo);
			GameNetworkMessage.WriteUlongToPacket(MutedPlayerIds[i].Part2, CompressionBasic.DebugULongNonCompressionInfo);
			GameNetworkMessage.WriteUlongToPacket(MutedPlayerIds[i].Part3, CompressionBasic.DebugULongNonCompressionInfo);
			GameNetworkMessage.WriteUlongToPacket(MutedPlayerIds[i].Part4, CompressionBasic.DebugULongNonCompressionInfo);
		}
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return $"SyncMutedPlayers {MutedPlayerCount} muted players.";
	}
}
