using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

public class CheckClanTagValidResult : FunctionResult
{
	[JsonProperty]
	public bool TagExists { get; private set; }

	public CheckClanTagValidResult()
	{
	}

	public CheckClanTagValidResult(bool tagExists)
	{
		TagExists = tagExists;
	}
}
