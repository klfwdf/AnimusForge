using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class InitializeSessionResponse : LoginResultObject
{
	[JsonProperty]
	public PlayerData PlayerData { get; private set; }

	[JsonProperty]
	public ServerStatus ServerStatus { get; private set; }

	[JsonProperty]
	public AvailableScenes AvailableScenes { get; private set; }

	[JsonProperty]
	public SupportedFeatures SupportedFeatures { get; private set; }

	[JsonProperty]
	public bool HasPendingRejoin { get; private set; }

	public InitializeSessionResponse()
	{
	}

	public InitializeSessionResponse(PlayerData playerData, ServerStatus serverStatus, AvailableScenes availableScenes, SupportedFeatures supportedFeatures, bool hasPendingRejoin)
	{
		PlayerData = playerData;
		ServerStatus = serverStatus;
		AvailableScenes = availableScenes;
		SupportedFeatures = supportedFeatures;
		HasPendingRejoin = hasPendingRejoin;
	}
}
