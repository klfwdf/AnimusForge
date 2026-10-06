using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class UpdateUsedCosmeticItemsMessage : Message
{
	[JsonProperty]
	public List<CosmeticItemInfo> UsedCosmetics { get; private set; }

	public UpdateUsedCosmeticItemsMessage()
	{
	}

	public UpdateUsedCosmeticItemsMessage(List<CosmeticItemInfo> usedCosmetics)
	{
		UsedCosmetics = usedCosmetics;
	}
}
