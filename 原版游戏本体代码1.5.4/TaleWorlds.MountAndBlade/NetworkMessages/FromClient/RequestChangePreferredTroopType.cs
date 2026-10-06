using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class RequestChangePreferredTroopType : GameNetworkMessage
{
	public TroopType TroopType { get; private set; }

	public RequestChangePreferredTroopType(TroopType troopType)
	{
		TroopType = troopType;
	}

	public RequestChangePreferredTroopType()
	{
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket((int)TroopType, CompressionBasic.TroopTypeCompressionInfo);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		TroopType = (TroopType)GameNetworkMessage.ReadIntFromPacket(CompressionBasic.TroopTypeCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Peer requesting preferred troop type change to " + TroopType;
	}
}
