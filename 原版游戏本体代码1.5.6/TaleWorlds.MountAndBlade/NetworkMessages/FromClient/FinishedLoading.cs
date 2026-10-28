using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class FinishedLoading : GameNetworkMessage
{
	public int BattleIndex { get; private set; }

	public FinishedLoading()
	{
	}

	public FinishedLoading(int battleIndex)
	{
		BattleIndex = battleIndex;
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		BattleIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.AutomatedBattleIndexCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(BattleIndex, CompressionMission.AutomatedBattleIndexCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.General;
	}

	protected override string OnGetLogFormat()
	{
		return "Finished Loading";
	}
}
