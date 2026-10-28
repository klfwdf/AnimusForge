using System;
using Newtonsoft.Json;
using TaleWorlds.Core;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class UpdateCharacterMessage : Message
{
	[JsonProperty]
	public BodyProperties BodyProperties { get; private set; }

	[JsonProperty]
	public bool IsFemale { get; private set; }

	public UpdateCharacterMessage()
	{
	}

	public UpdateCharacterMessage(BodyProperties bodyProperties, bool isFemale)
	{
		BodyProperties = bodyProperties;
		IsFemale = isFemale;
	}
}
