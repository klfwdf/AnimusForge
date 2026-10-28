using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromCustomBattleServer.ToCustomBattleServerManager;

[Serializable]
[MessageDescription("CustomBattleServer", "CustomBattleServerManager", false)]
public class UpdateCustomGameData : Message
{
	[JsonProperty]
	public string NewGameType { get; private set; }

	[JsonProperty]
	public string NewMap { get; private set; }

	[JsonProperty]
	public int NewMaxNumberOfPlayers { get; private set; }

	public UpdateCustomGameData()
	{
	}

	public UpdateCustomGameData(string newGameType, string newMap, int newMaxNumberOfPlayers)
	{
		NewGameType = newGameType;
		NewMap = newMap;
		NewMaxNumberOfPlayers = newMaxNumberOfPlayers;
	}
}
