using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.PlayerServices;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SyncPlayerMuteState : GameNetworkMessage
{
	public PlayerId PlayerId { get; private set; }

	public bool IsMuted { get; private set; }

	public SyncPlayerMuteState()
	{
	}

	public SyncPlayerMuteState(PlayerId playerId, bool isMuted)
	{
		PlayerId = playerId;
		IsMuted = isMuted;
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		ulong part = GameNetworkMessage.ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
		ulong part2 = GameNetworkMessage.ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
		ulong part3 = GameNetworkMessage.ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
		ulong part4 = GameNetworkMessage.ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
		if (bufferReadValid)
		{
			PlayerId = new PlayerId(part, part2, part3, part4);
		}
		IsMuted = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteUlongToPacket(PlayerId.Part1, CompressionBasic.DebugULongNonCompressionInfo);
		GameNetworkMessage.WriteUlongToPacket(PlayerId.Part2, CompressionBasic.DebugULongNonCompressionInfo);
		GameNetworkMessage.WriteUlongToPacket(PlayerId.Part3, CompressionBasic.DebugULongNonCompressionInfo);
		GameNetworkMessage.WriteUlongToPacket(PlayerId.Part4, CompressionBasic.DebugULongNonCompressionInfo);
		GameNetworkMessage.WriteBoolToPacket(IsMuted);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return $"SyncPlayerMuteState Player:{PlayerId}, IsMuted:{IsMuted}";
	}
}
