using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class GetRankedLeaderboardMessage : Message
{
	[JsonProperty]
	public string GameType { get; private set; }

	[JsonProperty]
	public int StartIndex { get; private set; }

	[JsonProperty]
	public int Count { get; private set; }

	public GetRankedLeaderboardMessage()
	{
	}

	public GetRankedLeaderboardMessage(string gameType, int startIndex, int count)
	{
		GameType = gameType;
		StartIndex = startIndex;
		Count = count;
	}
}
