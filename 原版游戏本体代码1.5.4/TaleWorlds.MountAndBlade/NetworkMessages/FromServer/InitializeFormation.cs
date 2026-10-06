using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class InitializeFormation : GameNetworkMessage
{
	public int FormationIndex { get; private set; }

	public int TeamIndex { get; private set; }

	public string BannerCode { get; private set; }

	public InitializeFormation(Formation formation, int teamIndex, string bannerCode)
	{
		FormationIndex = (int)formation.FormationIndex;
		TeamIndex = teamIndex;
		BannerCode = ((!string.IsNullOrEmpty(bannerCode)) ? bannerCode : string.Empty);
	}

	public InitializeFormation()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		FormationIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.FormationClassCompressionInfo, ref bufferReadValid);
		TeamIndex = GameNetworkMessage.ReadTeamIndexFromPacket(ref bufferReadValid);
		BannerCode = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(FormationIndex, CompressionMission.FormationClassCompressionInfo);
		GameNetworkMessage.WriteTeamIndexToPacket(TeamIndex);
		GameNetworkMessage.WriteStringToPacket(BannerCode);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Peers;
	}

	protected override string OnGetLogFormat()
	{
		return "Initialize formation with index: " + FormationIndex + ", for team: " + TeamIndex;
	}
}
