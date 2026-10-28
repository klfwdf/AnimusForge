using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class MultiplayerIntermissionUsableMapAdded : GameNetworkMessage
{
	public bool IsCompatibleWithAllGameTypes;

	public int CompatibleGameTypeCount;

	public List<string> CompatibleGameTypes;

	public string MapId { get; private set; }

	public MultiplayerIntermissionUsableMapAdded()
	{
		CompatibleGameTypes = new List<string>();
	}

	public MultiplayerIntermissionUsableMapAdded(string mapId, bool isCompatibleWithAllGameTypes, int compatibleGameTypeCount, List<string> compatibleGameTypes)
	{
		MapId = mapId;
		IsCompatibleWithAllGameTypes = isCompatibleWithAllGameTypes;
		CompatibleGameTypeCount = compatibleGameTypeCount;
		CompatibleGameTypes = compatibleGameTypes;
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MapId = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		IsCompatibleWithAllGameTypes = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		CompatibleGameTypeCount = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.IntermissionMapVoteItemCountCompressionInfo, ref bufferReadValid);
		for (int i = 0; i < CompatibleGameTypeCount; i++)
		{
			CompatibleGameTypes.Add(GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid));
		}
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteStringToPacket(MapId);
		GameNetworkMessage.WriteBoolToPacket(IsCompatibleWithAllGameTypes);
		GameNetworkMessage.WriteIntToPacket(CompatibleGameTypeCount, CompressionBasic.IntermissionMapVoteItemCountCompressionInfo);
		for (int i = 0; i < CompatibleGameTypeCount; i++)
		{
			GameNetworkMessage.WriteStringToPacket(CompatibleGameTypes[i]);
		}
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return "Adding usable map with id: " + MapId + ".";
	}
}
