using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromCustomBattleServer.ToCustomBattleServerManager;

[Serializable]
[MessageDescription("CustomBattleServer", "CustomBattleServerManager", false)]
public class UpdateGamePropertiesMessage : Message
{
	[JsonProperty]
	public string GameType { get; private set; }

	[JsonProperty]
	public string Scene { get; private set; }

	[JsonProperty]
	public string UniqueSceneId { get; private set; }

	[JsonProperty]
	public bool HasStreamerSettings { get; private set; }

	[JsonProperty]
	public int MaxStreamerCount { get; private set; }

	[JsonProperty]
	public string StreamerPassword { get; private set; }

	public UpdateGamePropertiesMessage()
	{
	}

	public UpdateGamePropertiesMessage(string gameType, string scene, string uniqueSceneId, int maxStreamerCount, string streamerPassword)
	{
		GameType = gameType;
		Scene = scene;
		UniqueSceneId = uniqueSceneId;
		HasStreamerSettings = true;
		MaxStreamerCount = maxStreamerCount;
		StreamerPassword = streamerPassword;
	}
}
