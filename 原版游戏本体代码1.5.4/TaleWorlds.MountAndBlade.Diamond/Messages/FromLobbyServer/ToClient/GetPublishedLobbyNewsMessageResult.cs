using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class GetPublishedLobbyNewsMessageResult : FunctionResult
{
	[JsonProperty]
	public PublishedLobbyNewsArticle[] Content { get; private set; }

	public GetPublishedLobbyNewsMessageResult()
	{
	}

	public GetPublishedLobbyNewsMessageResult(PublishedLobbyNewsArticle[] content)
	{
		Content = content;
	}
}
