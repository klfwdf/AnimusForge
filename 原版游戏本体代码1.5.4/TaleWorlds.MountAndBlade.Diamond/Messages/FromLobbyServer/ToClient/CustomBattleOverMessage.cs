using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class CustomBattleOverMessage : Message
{
	[JsonProperty]
	public int OldExperience { get; set; }

	[JsonProperty]
	public int NewExperience { get; set; }

	[JsonProperty]
	public int GoldGain { get; set; }

	public CustomBattleOverMessage()
	{
	}

	public CustomBattleOverMessage(int oldExperience, int newExperience, int goldGain)
	{
		OldExperience = oldExperience;
		NewExperience = newExperience;
		GoldGain = goldGain;
	}
}
