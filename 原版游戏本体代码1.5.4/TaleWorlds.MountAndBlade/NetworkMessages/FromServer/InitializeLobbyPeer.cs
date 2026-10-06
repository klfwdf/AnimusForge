using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.PlayerServices;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class InitializeLobbyPeer : GameNetworkMessage
{
	public NetworkCommunicator Peer { get; private set; }

	public PlayerId ProvidedId { get; private set; }

	public string BannerCode { get; private set; }

	public BodyProperties BodyProperties { get; private set; }

	public int ChosenBadgeIndex { get; private set; }

	public int ForcedAvatarIndex { get; private set; }

	public bool IsFemale { get; private set; }

	public InitializeLobbyPeer(NetworkCommunicator peer, VirtualPlayer virtualPlayer, int forcedAvatarIndex)
	{
		Peer = peer;
		ProvidedId = virtualPlayer.Id;
		BannerCode = ((virtualPlayer.BannerCode != null) ? virtualPlayer.BannerCode : string.Empty);
		BodyProperties = virtualPlayer.BodyProperties;
		ChosenBadgeIndex = virtualPlayer.ChosenBadgeIndex;
		IsFemale = virtualPlayer.IsFemale;
		ForcedAvatarIndex = forcedAvatarIndex;
	}

	public InitializeLobbyPeer()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		ulong part = GameNetworkMessage.ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
		ulong part2 = GameNetworkMessage.ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
		ulong part3 = GameNetworkMessage.ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
		ulong part4 = GameNetworkMessage.ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
		BannerCode = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		string keyValue = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		if (bufferReadValid)
		{
			ProvidedId = new PlayerId(part, part2, part3, part4);
			if (BodyProperties.FromString(keyValue, out var bodyProperties))
			{
				BodyProperties = bodyProperties;
			}
			else
			{
				bufferReadValid = false;
			}
		}
		ChosenBadgeIndex = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.PlayerChosenBadgeCompressionInfo, ref bufferReadValid);
		ForcedAvatarIndex = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.ForcedAvatarIndexCompressionInfo, ref bufferReadValid);
		IsFemale = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Peer);
		GameNetworkMessage.WriteUlongToPacket(ProvidedId.Part1, CompressionBasic.DebugULongNonCompressionInfo);
		GameNetworkMessage.WriteUlongToPacket(ProvidedId.Part2, CompressionBasic.DebugULongNonCompressionInfo);
		GameNetworkMessage.WriteUlongToPacket(ProvidedId.Part3, CompressionBasic.DebugULongNonCompressionInfo);
		GameNetworkMessage.WriteUlongToPacket(ProvidedId.Part4, CompressionBasic.DebugULongNonCompressionInfo);
		GameNetworkMessage.WriteStringToPacket(BannerCode);
		GameNetworkMessage.WriteStringToPacket(BodyProperties.ToString());
		GameNetworkMessage.WriteIntToPacket(ChosenBadgeIndex, CompressionBasic.PlayerChosenBadgeCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(ForcedAvatarIndex, CompressionBasic.ForcedAvatarIndexCompressionInfo);
		GameNetworkMessage.WriteBoolToPacket(IsFemale);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Peers;
	}

	protected override string OnGetLogFormat()
	{
		return "Initialize LobbyPeer from Peer: " + Peer.UserName;
	}
}
