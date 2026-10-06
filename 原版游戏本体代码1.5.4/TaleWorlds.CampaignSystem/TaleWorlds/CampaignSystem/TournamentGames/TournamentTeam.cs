using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.TournamentGames;

public class TournamentTeam
{
	private List<TournamentParticipant> _participants;

	public int TeamSize { get; private set; }

	public uint TeamColor { get; private set; }

	public Banner TeamBanner { get; private set; }

	public bool IsPlayerTeam { get; private set; }

	public IEnumerable<TournamentParticipant> Participants => _participants.AsEnumerable();

	public int Score
	{
		get
		{
			int num = 0;
			foreach (TournamentParticipant participant in _participants)
			{
				num += participant.Score;
			}
			return num;
		}
	}

	public TournamentTeam(int teamSize, uint teamColor, Banner teamBanner)
	{
		TeamColor = teamColor;
		TeamBanner = teamBanner;
		TeamSize = teamSize;
		_participants = new List<TournamentParticipant>();
	}

	public bool IsParticipantRequired()
	{
		return _participants.Count < TeamSize;
	}

	public void AddParticipant(TournamentParticipant participant)
	{
		participant.IsAssigned = true;
		_participants.Add(participant);
		participant.SetTeam(this);
		if (participant.IsPlayer)
		{
			IsPlayerTeam = true;
		}
	}
}
