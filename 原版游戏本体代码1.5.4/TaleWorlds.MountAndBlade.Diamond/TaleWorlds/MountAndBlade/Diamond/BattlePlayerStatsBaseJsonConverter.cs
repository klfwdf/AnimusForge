using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TaleWorlds.MountAndBlade.Diamond;

public class BattlePlayerStatsBaseJsonConverter : JsonConverter
{
	public override bool CanWrite => false;

	public override bool CanConvert(Type objectType)
	{
		return typeof(BattlePlayerStatsBase).IsAssignableFrom(objectType);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null)
		{
			return null;
		}
		JObject jObject = JObject.Load(reader);
		BattlePlayerStatsBase battlePlayerStatsBase;
		switch ((string?)jObject["GameType"])
		{
		case "Skirmish":
			battlePlayerStatsBase = new BattlePlayerStatsSkirmish();
			break;
		case "Captain":
			battlePlayerStatsBase = new BattlePlayerStatsCaptain();
			break;
		case "Siege":
			battlePlayerStatsBase = new BattlePlayerStatsSiege();
			break;
		case "TeamDeathmatch":
			battlePlayerStatsBase = new BattlePlayerStatsTeamDeathmatch();
			break;
		case "Duel":
			battlePlayerStatsBase = new BattlePlayerStatsDuel();
			break;
		case "Battle":
			battlePlayerStatsBase = new BattlePlayerStatsBattle();
			break;
		default:
			return null;
		}
		serializer.Populate(jObject.CreateReader(), battlePlayerStatsBase);
		return battlePlayerStatsBase;
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
	}
}
