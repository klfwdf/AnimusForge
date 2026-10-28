using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class TauntSelected : GameNetworkMessage
{
	public int IndexOfTaunt { get; private set; }

	public TauntSelected(int indexOfTaunt)
	{
		IndexOfTaunt = indexOfTaunt;
	}

	public TauntSelected()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		IndexOfTaunt = GameNetworkMessage.ReadIntFromPacket(CompressionMission.TauntIndexCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(IndexOfTaunt, CompressionMission.TauntIndexCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.None;
	}

	protected override string OnGetLogFormat()
	{
		return "FromClient.CheerSelected: " + IndexOfTaunt;
	}
}
