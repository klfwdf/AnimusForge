using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class MultiplayerIntermissionUpdate : GameNetworkMessage
{
	public MultiplayerIntermissionState IntermissionState { get; private set; }

	public float IntermissionTimer { get; private set; }

	public MultiplayerIntermissionUpdate()
	{
	}

	public MultiplayerIntermissionUpdate(MultiplayerIntermissionState intermissionState, float intermissionTimer)
	{
		IntermissionState = intermissionState;
		IntermissionTimer = intermissionTimer;
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		int intermissionState = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.IntermissionStateCompressionInfo, ref bufferReadValid);
		IntermissionState = (MultiplayerIntermissionState)intermissionState;
		IntermissionTimer = GameNetworkMessage.ReadFloatFromPacket(CompressionBasic.IntermissionTimerCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket((int)IntermissionState, CompressionBasic.IntermissionStateCompressionInfo);
		GameNetworkMessage.WriteFloatToPacket(IntermissionTimer, CompressionBasic.IntermissionTimerCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return "Receiving runtime intermission state.";
	}
}
