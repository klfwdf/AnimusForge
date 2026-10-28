using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class BarkSelected : GameNetworkMessage
{
	public int IndexOfBark { get; private set; }

	public BarkSelected(int indexOfBark)
	{
		IndexOfBark = indexOfBark;
	}

	public BarkSelected()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		IndexOfBark = GameNetworkMessage.ReadIntFromPacket(CompressionMission.BarkIndexCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(IndexOfBark, CompressionMission.BarkIndexCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.None;
	}

	protected override string OnGetLogFormat()
	{
		return "FromClient.BarkSelected: " + IndexOfBark;
	}
}
