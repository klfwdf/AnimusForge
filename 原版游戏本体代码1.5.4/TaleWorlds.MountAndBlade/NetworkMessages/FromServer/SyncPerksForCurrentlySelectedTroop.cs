using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SyncPerksForCurrentlySelectedTroop : GameNetworkMessage
{
	public NetworkCommunicator Peer { get; private set; }

	public int[] PerkIndices { get; private set; }

	public SyncPerksForCurrentlySelectedTroop()
	{
	}

	public SyncPerksForCurrentlySelectedTroop(NetworkCommunicator peer, int[] perkIndices)
	{
		Peer = peer;
		PerkIndices = perkIndices;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Peer);
		for (int i = 0; i < 3; i++)
		{
			GameNetworkMessage.WriteIntToPacket(PerkIndices[i], CompressionMission.PerkIndexCompressionInfo);
		}
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		PerkIndices = new int[3];
		for (int i = 0; i < 3; i++)
		{
			PerkIndices[i] = GameNetworkMessage.ReadIntFromPacket(CompressionMission.PerkIndexCompressionInfo, ref bufferReadValid);
		}
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.GameMode;
	}

	protected override string OnGetLogFormat()
	{
		string text = "";
		for (int i = 0; i < 3; i++)
		{
			text += $"[{PerkIndices[i]}]";
		}
		return "Selected perks for " + Peer.UserName + " has been updated as " + text + ".";
	}
}
