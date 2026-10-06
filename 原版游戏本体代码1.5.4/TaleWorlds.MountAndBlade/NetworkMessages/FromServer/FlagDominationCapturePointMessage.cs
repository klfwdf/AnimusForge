using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class FlagDominationCapturePointMessage : GameNetworkMessage
{
	public int FlagIndex { get; private set; }

	public int OwnerTeamIndex { get; private set; }

	public FlagDominationCapturePointMessage()
	{
	}

	public FlagDominationCapturePointMessage(int flagIndex, int ownerTeamIndex)
	{
		FlagIndex = flagIndex;
		OwnerTeamIndex = ownerTeamIndex;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(FlagIndex, CompressionMission.FlagCapturePointIndexCompressionInfo);
		GameNetworkMessage.WriteTeamIndexToPacket(OwnerTeamIndex);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		FlagIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.FlagCapturePointIndexCompressionInfo, ref bufferReadValid);
		OwnerTeamIndex = GameNetworkMessage.ReadTeamIndexFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.GameMode;
	}

	protected override string OnGetLogFormat()
	{
		return "Flag owner changed.";
	}
}
