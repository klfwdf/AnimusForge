using System;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond;

[Serializable]
public class GDKAccessObject : AccessObject
{
	[JsonProperty]
	public string Id;

	[JsonProperty]
	public string Token;

	public GDKAccessObject()
	{
		base.Type = "GDK";
	}
}
