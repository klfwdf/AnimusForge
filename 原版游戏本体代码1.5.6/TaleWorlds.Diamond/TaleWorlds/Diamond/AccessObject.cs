using System;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond;

[Serializable]
[JsonConverter(typeof(AccessObjectJsonConverter))]
public abstract class AccessObject
{
	public string Type { get; set; }
}
