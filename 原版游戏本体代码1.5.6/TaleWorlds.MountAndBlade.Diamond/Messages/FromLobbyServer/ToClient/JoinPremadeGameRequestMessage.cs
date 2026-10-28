using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class JoinPremadeGameRequestMessage : Message
{
	[JsonProperty]
	public Guid ChallengerPartyId { get; private set; }

	[JsonProperty]
	public string ClanName { get; private set; }

	[JsonProperty]
	public string Sigil { get; private set; }

	[JsonProperty]
	public PlayerId[] ChallengerPlayers { get; private set; }

	[JsonProperty]
	public PlayerId ChallengerPartyLeaderId { get; private set; }

	[JsonProperty]
	public PremadeGameType PremadeGameType { get; private set; }

	public JoinPremadeGameRequestMessage()
	{
	}

	public JoinPremadeGameRequestMessage(Guid challengerPartyId, string clanName, string sigil, PlayerId[] challengerPlayers, PlayerId challengerPartyLeaderId, PremadeGameType premadeGameType)
	{
		ChallengerPartyId = challengerPartyId;
		ClanName = clanName;
		Sigil = sigil;
		ChallengerPlayers = challengerPlayers;
		ChallengerPartyLeaderId = challengerPartyLeaderId;
		PremadeGameType = premadeGameType;
	}

	public static JoinPremadeGameRequestMessage CreateClanGameRequest(Guid challengerPartyId, string clanName, string sigil, PlayerId[] challengerPlayers)
	{
		return new JoinPremadeGameRequestMessage(challengerPartyId, clanName, sigil, challengerPlayers, PlayerId.Empty, PremadeGameType.Clan);
	}

	public static JoinPremadeGameRequestMessage CreatePracticeGameRequest(Guid challengerPartyId, PlayerId leaderId, PlayerId[] challengerPlayers)
	{
		return new JoinPremadeGameRequestMessage(challengerPartyId, null, null, challengerPlayers, leaderId, PremadeGameType.Practice);
	}
}
