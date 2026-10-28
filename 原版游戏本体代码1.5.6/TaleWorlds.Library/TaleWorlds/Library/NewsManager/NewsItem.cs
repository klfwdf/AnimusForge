using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TaleWorlds.Library.NewsManager;

public struct NewsItem
{
	[JsonConverter(typeof(StringEnumConverter))]
	public enum NewsTypes
	{
		LauncherSingleplayer,
		LauncherMultiplayer,
		MultiplayerLobby
	}

	public string Title { get; set; }

	public string Description { get; set; }

	public string ImageSourcePath { get; set; }

	public List<NewsType> Feeds { get; set; }

	public string NewsLink { get; set; }
}
