using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TaleWorlds.MountAndBlade.Multiplayer;

[Serializable]
public class MapListResponse
{
	public string CurrentlyPlaying { get; private set; }

	public List<MapListItemResponse> Maps { get; private set; }

	[JsonConstructor]
	public MapListResponse(string currentlyPlaying, List<MapListItemResponse> maps)
	{
		CurrentlyPlaying = currentlyPlaying;
		Maps = maps;
	}
}
