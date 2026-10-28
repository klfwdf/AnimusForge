using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class GetClanLeaderboardResult : FunctionResult
{
	[JsonProperty]
	public ClanLeaderboardInfo ClanLeaderboardInfo { get; private set; }

	public GetClanLeaderboardResult()
	{
	}

	public GetClanLeaderboardResult(ClanLeaderboardInfo info)
	{
		ClanLeaderboardInfo = info;
	}
}
