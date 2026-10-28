using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class GameLog
{
	public int Id { get; set; }

	public GameLogType Type { get; set; }

	public PlayerId Player { get; set; }

	public float GameTime { get; set; }

	public Dictionary<string, string> Data { get; set; }

	public GameLog()
	{
	}

	public GameLog(GameLogType type, PlayerId player, float gameTime)
	{
		Type = type;
		Player = player;
		GameTime = gameTime;
		Data = new Dictionary<string, string>();
	}

	public string GetDataAsString()
	{
		string result = "{}";
		try
		{
			result = JsonConvert.SerializeObject(Data, Formatting.None);
		}
		catch (Exception)
		{
		}
		return result;
	}
}
