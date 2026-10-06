using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class GoldGain : GameNetworkMessage
{
	public List<KeyValuePair<ushort, int>> GoldChangeEventList { get; private set; }

	public GoldGain(List<KeyValuePair<ushort, int>> goldChangeEventList)
	{
		GoldChangeEventList = goldChangeEventList;
	}

	public GoldGain()
	{
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(GoldChangeEventList.Count - 1, CompressionMission.TdmGoldGainTypeCompressionInfo);
		foreach (KeyValuePair<ushort, int> goldChangeEvent in GoldChangeEventList)
		{
			GameNetworkMessage.WriteIntToPacket(goldChangeEvent.Key, CompressionMission.TdmGoldGainTypeCompressionInfo);
			GameNetworkMessage.WriteIntToPacket(goldChangeEvent.Value, CompressionMission.TdmGoldChangeCompressionInfo);
		}
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		GoldChangeEventList = new List<KeyValuePair<ushort, int>>();
		int num = GameNetworkMessage.ReadIntFromPacket(CompressionMission.TdmGoldGainTypeCompressionInfo, ref bufferReadValid) + 1;
		for (int i = 0; i < num; i++)
		{
			ushort key = (ushort)GameNetworkMessage.ReadIntFromPacket(CompressionMission.TdmGoldGainTypeCompressionInfo, ref bufferReadValid);
			int value = GameNetworkMessage.ReadIntFromPacket(CompressionMission.TdmGoldChangeCompressionInfo, ref bufferReadValid);
			GoldChangeEventList.Add(new KeyValuePair<ushort, int>(key, value));
		}
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.GameMode;
	}

	protected override string OnGetLogFormat()
	{
		return "Gold change events synced.";
	}
}
