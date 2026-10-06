using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class TeamInitialPerkInfoMessage : GameNetworkMessage
{
	public int[] Perks { get; private set; }

	public TeamInitialPerkInfoMessage(int[] perks)
	{
		Perks = perks;
	}

	public TeamInitialPerkInfoMessage()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Perks = new int[3];
		for (int i = 0; i < 3; i++)
		{
			Perks[i] = GameNetworkMessage.ReadIntFromPacket(CompressionMission.PerkIndexCompressionInfo, ref bufferReadValid);
		}
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		for (int i = 0; i < 3; i++)
		{
			GameNetworkMessage.WriteIntToPacket(Perks[i], CompressionMission.PerkIndexCompressionInfo);
		}
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Equipment;
	}

	protected override string OnGetLogFormat()
	{
		return "TeamInitialPerkInfoMessage";
	}
}
