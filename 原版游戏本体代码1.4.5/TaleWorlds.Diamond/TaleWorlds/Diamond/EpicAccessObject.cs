using System;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond;

[Serializable]
public class EpicAccessObject : AccessObject
{
	[JsonProperty]
	public string AccessToken;

	[JsonProperty]
	public string EpicId;

	public EpicAccessObject()
	{
		base.Type = "Epic";
	}
}
