using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class GetPlayerBadgesMessageResult : FunctionResult
{
	[JsonProperty]
	public string[] Badges { get; private set; }

	public GetPlayerBadgesMessageResult()
	{
	}

	public GetPlayerBadgesMessageResult(string[] badges)
	{
		Badges = badges;
	}
}
