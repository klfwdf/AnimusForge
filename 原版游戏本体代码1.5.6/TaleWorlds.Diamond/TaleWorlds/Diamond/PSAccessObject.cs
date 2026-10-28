using System;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond;

[Serializable]
public class PSAccessObject : AccessObject
{
	[JsonProperty]
	public int IssuerId { get; private set; }

	[JsonProperty]
	public string AuthCode { get; private set; }

	public PSAccessObject()
	{
	}

	public PSAccessObject(int issuerId, string authCode)
	{
		base.Type = "PS";
		IssuerId = issuerId;
		AuthCode = authCode;
	}
}
