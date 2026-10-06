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

	public UpdateGamePropertiesMessage()
	{
	}

	public UpdateGamePropertiesMessage(string gameType, string scene, string uniqueSceneId)
	{
		GameType = gameType;
		Scene = scene;
		UniqueSceneId = uniqueSceneId;
	}
}
