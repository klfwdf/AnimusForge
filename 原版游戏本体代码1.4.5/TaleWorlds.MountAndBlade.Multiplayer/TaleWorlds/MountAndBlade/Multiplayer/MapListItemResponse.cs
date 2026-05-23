using System;
using Newtonsoft.Json;

namespace TaleWorlds.MountAndBlade.Multiplayer;

[Serializable]
public class MapListItemResponse
{
	public string Name { get; private set; }

	public string UniqueToken { get; private set; }

	public string Revision { get; private set; }

	[JsonConstructor]
	public MapListItemResponse(string name, string uniqueToken, string revision)
	{
		Name = name;
		UniqueToken = uniqueToken;
		Revision = revision;
	}
}
