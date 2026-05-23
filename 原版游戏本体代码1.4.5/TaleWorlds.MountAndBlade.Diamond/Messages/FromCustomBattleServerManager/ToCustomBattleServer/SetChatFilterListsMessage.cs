using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromCustomBattleServerManager.ToCustomBattleServer;

[Serializable]
[MessageDescription("CustomBattleServerManager", "CustomBattleServer", true)]
[DataContract]
public class SetChatFilterListsMessage : Message
{
	[JsonProperty]
	public string[] ProfanityList { get; private set; }

	[JsonProperty]
	public string[] AllowList { get; private set; }

	public SetChatFilterListsMessage()
	{
	}

	public SetChatFilterListsMessage(string[] profanityList, string[] allowList)
	{
		ProfanityList = profanityList;
		AllowList = allowList;
	}
}
