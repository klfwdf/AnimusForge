using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class CultureVoteClient : GameNetworkMessage
{
	public BasicCultureObject VotedCulture { get; private set; }

	public CultureVoteTypes VotedType { get; private set; }

	public CultureVoteClient()
	{
	}

	public CultureVoteClient(CultureVoteTypes type, BasicCultureObject culture)
	{
		VotedType = type;
		VotedCulture = culture;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket((int)VotedType, CompressionMission.TeamSideCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(MBObjectManager.Instance.GetObjectTypeList<BasicCultureObject>().IndexOf(VotedCulture), CompressionBasic.CultureIndexCompressionInfo);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		VotedType = (CultureVoteTypes)GameNetworkMessage.ReadIntFromPacket(CompressionMission.TeamSideCompressionInfo, ref bufferReadValid);
		int index = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.CultureIndexCompressionInfo, ref bufferReadValid);
		if (bufferReadValid)
		{
			VotedCulture = MBObjectManager.Instance.GetObjectTypeList<BasicCultureObject>()[index];
		}
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Mission;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Culture ", VotedCulture.Name, " has been ", VotedType.ToString().ToLower(), (VotedType == CultureVoteTypes.Ban) ? "ned." : "ed.");
	}
}
