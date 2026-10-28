using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.Diamond;

namespace TaleWorlds.MountAndBlade.Diamond;

public class PlayerStatsBaseJsonConverter : JsonConverter
{
	public override bool CanWrite => false;

	public override bool CanConvert(Type objectType)
	{
		return typeof(AccessObject).IsAssignableFrom(objectType);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		JObject jObject = JObject.Load(reader);
		string text = (string?)jObject["gameType"];
		if (text == null)
		{
			text = (string?)jObject["GameType"];
		}
		PlayerStatsBase playerStatsBase;
		switch (text)
		{
		case "Skirmish":
			playerStatsBase = new PlayerStatsSkirmish();
			break;
		case "Captain":
			playerStatsBase = new PlayerStatsCaptain();
			break;
		case "TeamDeathmatch":
			playerStatsBase = new PlayerStatsTeamDeathmatch();
			break;
		case "Siege":
			playerStatsBase = new PlayerStatsSiege();
			break;
		case "Duel":
			playerStatsBase = new PlayerStatsDuel();
			break;
		case "Battle":
			playerStatsBase = new PlayerStatsBattle();
			break;
		default:
			return null;
		}
		serializer.Populate(jObject.CreateReader(), playerStatsBase);
		return playerStatsBase;
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
	}
}
