using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetSpawnedFormationCount : GameNetworkMessage
{
	public int NumOfFormationsTeamOne { get; private set; }

	public int NumOfFormationsTeamTwo { get; private set; }

	public SetSpawnedFormationCount(int numFormationsTeamOne, int numFormationsTeamTwo)
	{
		NumOfFormationsTeamOne = numFormationsTeamOne;
		NumOfFormationsTeamTwo = numFormationsTeamTwo;
	}

	public SetSpawnedFormationCount()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		NumOfFormationsTeamOne = GameNetworkMessage.ReadIntFromPacket(CompressionMission.FormationClassCompressionInfo, ref bufferReadValid);
		NumOfFormationsTeamTwo = GameNetworkMessage.ReadIntFromPacket(CompressionMission.FormationClassCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(NumOfFormationsTeamOne, CompressionMission.FormationClassCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(NumOfFormationsTeamTwo, CompressionMission.FormationClassCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Peers;
	}

	protected override string OnGetLogFormat()
	{
		return "Syncing formation count";
	}
}
