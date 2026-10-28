using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class UnselectFormation : GameNetworkMessage
{
	public int FormationIndex { get; private set; }

	public UnselectFormation(int formationIndex)
	{
		FormationIndex = formationIndex;
	}

	public UnselectFormation()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		FormationIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.FormationClassCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(FormationIndex, CompressionMission.FormationClassCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Formations;
	}

	protected override string OnGetLogFormat()
	{
		return "Deselect Formation with index: " + FormationIndex;
	}
}
