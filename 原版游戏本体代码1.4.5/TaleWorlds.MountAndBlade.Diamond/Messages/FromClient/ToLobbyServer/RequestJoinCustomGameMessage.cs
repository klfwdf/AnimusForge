using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", false)]
public class RequestJoinCustomGameMessage : Message
{
	[JsonProperty]
	public CustomBattleId CustomBattleId { get; private set; }

	[JsonProperty]
	public string Password { get; private set; }

	[JsonProperty]
	public bool IsJoinAsAdminOnly { get; private set; }

	public RequestJoinCustomGameMessage()
	{
	}

	public RequestJoinCustomGameMessage(CustomBattleId customBattleId, string password = "", bool isJoinAsAdminOnly = false)
	{
		CustomBattleId = customBattleId;
		Password = password;
		IsJoinAsAdminOnly = isJoinAsAdminOnly;
	}
}
