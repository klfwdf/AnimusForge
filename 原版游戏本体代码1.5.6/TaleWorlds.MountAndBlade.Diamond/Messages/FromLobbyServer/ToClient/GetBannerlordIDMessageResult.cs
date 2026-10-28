using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class GetBannerlordIDMessageResult : FunctionResult
{
	[JsonProperty]
	public string BannerlordID { get; private set; }

	public GetBannerlordIDMessageResult()
	{
	}

	public GetBannerlordIDMessageResult(string bannerlordID)
	{
		BannerlordID = bannerlordID;
	}
}
