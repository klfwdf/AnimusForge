using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class PremadeGameEligibilityStatusMessage : Message
{
	[JsonProperty]
	public PremadeGameType[] EligibleGameTypes { get; private set; }

	public PremadeGameEligibilityStatusMessage()
	{
	}

	public PremadeGameEligibilityStatusMessage(PremadeGameType[] eligibleGameTypes)
	{
		EligibleGameTypes = eligibleGameTypes;
	}
}
