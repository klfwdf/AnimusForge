using System;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond;

[Serializable]
public class GOGAccessObject : AccessObject
{
	[JsonProperty]
	public ulong GogId { get; set; }

	[JsonProperty]
	public ulong OldId { get; set; }

	[JsonProperty]
	public string UserName { get; set; }

	[JsonProperty]
	public string Ticket { get; set; }

	public GOGAccessObject()
	{
	}

	public GOGAccessObject(string userName, ulong gogId, ulong oldId, string ticket)
	{
		base.Type = "GOG";
		UserName = userName;
		GogId = gogId;
		Ticket = ticket;
		OldId = oldId;
	}
}
