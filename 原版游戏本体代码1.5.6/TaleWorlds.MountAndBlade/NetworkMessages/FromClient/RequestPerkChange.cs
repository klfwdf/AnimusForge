using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class RequestPerkChange : GameNetworkMessage
{
	public int PerkListIndex { get; private set; }

	public int PerkIndex { get; private set; }

	public RequestPerkChange(int perkListIndex, int perkIndex)
	{
		PerkListIndex = perkListIndex;
		PerkIndex = perkIndex;
	}

	public RequestPerkChange()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		PerkListIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.PerkListIndexCompressionInfo, ref bufferReadValid);
		PerkIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.PerkIndexCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(PerkListIndex, CompressionMission.PerkListIndexCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(PerkIndex, CompressionMission.PerkIndexCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Equipment;
	}

	protected override string OnGetLogFormat()
	{
		return "Requesting perk selection in list " + PerkListIndex + " change to " + PerkIndex;
	}
}
