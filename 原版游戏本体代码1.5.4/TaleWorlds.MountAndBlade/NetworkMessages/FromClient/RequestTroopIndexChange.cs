using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class RequestTroopIndexChange : GameNetworkMessage
{
	public int SelectedTroopIndex { get; private set; }

	public RequestTroopIndexChange(int selectedTroopIndex)
	{
		SelectedTroopIndex = selectedTroopIndex;
	}

	public RequestTroopIndexChange()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		SelectedTroopIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.SelectedTroopIndexCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(SelectedTroopIndex, CompressionMission.SelectedTroopIndexCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Equipment;
	}

	protected override string OnGetLogFormat()
	{
		return "Requesting selected troop change to " + SelectedTroopIndex;
	}
}
