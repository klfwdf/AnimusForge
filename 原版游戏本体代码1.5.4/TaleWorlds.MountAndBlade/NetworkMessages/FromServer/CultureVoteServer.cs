using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class CultureVoteServer : GameNetworkMessage
{
	public NetworkCommunicator Peer { get; private set; }

	public BasicCultureObject VotedCulture { get; private set; }

	public CultureVoteTypes VotedType { get; private set; }

	public CultureVoteServer()
	{
	}

	public CultureVoteServer(NetworkCommunicator peer, CultureVoteTypes type, BasicCultureObject culture)
	{
		Peer = peer;
		VotedType = type;
		VotedCulture = culture;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Peer);
		GameNetworkMessage.WriteIntToPacket((int)VotedType, CompressionMission.TeamSideCompressionInfo);
		MBReadOnlyList<BasicCultureObject> objectTypeList = MBObjectManager.Instance.GetObjectTypeList<BasicCultureObject>();
		GameNetworkMessage.WriteIntToPacket((VotedCulture == null) ? (-1) : objectTypeList.IndexOf(VotedCulture), CompressionBasic.CultureIndexCompressionInfo);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		VotedType = (CultureVoteTypes)GameNetworkMessage.ReadIntFromPacket(CompressionMission.TeamSideCompressionInfo, ref bufferReadValid);
		int num = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.CultureIndexCompressionInfo, ref bufferReadValid);
		if (bufferReadValid)
		{
			MBReadOnlyList<BasicCultureObject> objectTypeList = MBObjectManager.Instance.GetObjectTypeList<BasicCultureObject>();
			VotedCulture = ((num < 0) ? null : objectTypeList[num]);
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
