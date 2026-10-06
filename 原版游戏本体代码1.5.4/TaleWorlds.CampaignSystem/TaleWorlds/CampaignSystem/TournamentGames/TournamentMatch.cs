using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.TournamentGames;

public class TournamentMatch
{
	public enum MatchState
	{
		Ready,
		Started,
		Finished
	}

	private readonly int _numberOfWinnerParticipants;

	public readonly TournamentGame.QualificationMode QualificationMode;

	private readonly TournamentTeam[] _teams;

	private readonly List<TournamentParticipant> _participants;

	private List<TournamentParticipant> _winners;

	private readonly int _participantCount;

	private int _teamSize;

	public IEnumerable<TournamentTeam> Teams => _teams.AsEnumerable();

	public IEnumerable<TournamentParticipant> Participants => _participants.AsEnumerable();

	public MatchState State { get; private set; }

	public IEnumerable<TournamentParticipant> Winners => _winners.AsEnumerable();

	public bool IsReady => State == MatchState.Ready;

	public TournamentMatch(int participantCount, int numberOfTeamsPerMatch, int numberOfWinnerParticipants, TournamentGame.QualificationMode qualificationMode)
	{
		_participants = new List<TournamentParticipant>();
		_participantCount = participantCount;
		_teams = new TournamentTeam[numberOfTeamsPerMatch];
		_winners = new List<TournamentParticipant>();
		_numberOfWinnerParticipants = numberOfWinnerParticipants;
		QualificationMode = qualificationMode;
		_teamSize = participantCount / numberOfTeamsPerMatch;
		int[] array = new int[4] { 119, 118, 120, 121 };
		int num = 0;
		for (int i = 0; i < numberOfTeamsPerMatch; i++)
		{
			_teams[i] = new TournamentTeam(_teamSize, BannerManager.GetColor(array[num]), Banner.CreateOneColoredEmptyBanner(array[num]));
			num++;
			num %= 4;
		}
		State = MatchState.Ready;
	}

	public void End()
	{
		State = MatchState.Finished;
		_winners = GetWinners();
	}

	public void Start()
	{
		if (State == MatchState.Started)
		{
			return;
		}
		State = MatchState.Started;
		foreach (TournamentParticipant participant in Participants)
		{
			participant.ResetScore();
		}
	}

	public TournamentParticipant GetParticipant(int uniqueSeed)
	{
		return _participants.FirstOrDefault((TournamentParticipant p) => p.Descriptor.CompareTo(uniqueSeed) == 0);
	}

	public bool IsParticipantRequired()
	{
		return _participants.Count < _participantCount;
	}

	public void AddParticipant(TournamentParticipant participant, bool firstTime)
	{
		_participants.Add(participant);
		foreach (TournamentTeam team in Teams)
		{
			if (team.IsParticipantRequired() && ((participant.Team != null && participant.Team.TeamColor == team.TeamColor) || firstTime))
			{
				team.AddParticipant(participant);
				return;
			}
		}
		foreach (TournamentTeam team2 in Teams)
		{
			if (team2.IsParticipantRequired())
			{
				team2.AddParticipant(participant);
				break;
			}
		}
	}

	public bool IsPlayerParticipating()
	{
		return Participants.Any((TournamentParticipant x) => x.Character == CharacterObject.PlayerCharacter);
	}

	public bool IsPlayerWinner()
	{
		if (IsPlayerParticipating())
		{
			return GetWinners().Any((TournamentParticipant x) => x.Character == CharacterObject.PlayerCharacter);
		}
		return false;
	}

	private List<TournamentParticipant> GetWinners()
	{
		List<TournamentParticipant> list = new List<TournamentParticipant>();
		if (QualificationMode == TournamentGame.QualificationMode.IndividualScore)
		{
			List<TournamentParticipant> list2 = _participants.OrderByDescending((TournamentParticipant x) => x.Score).Take(_numberOfWinnerParticipants).ToList();
			foreach (TournamentParticipant participant in _participants)
			{
				if (list2.Contains(participant))
				{
					participant.IsAssigned = false;
					list.Add(participant);
				}
			}
		}
		else if (QualificationMode == TournamentGame.QualificationMode.TeamScore)
		{
			IOrderedEnumerable<TournamentTeam> orderedEnumerable = _teams.OrderByDescending((TournamentTeam x) => x.Score);
			List<TournamentTeam> list3 = orderedEnumerable.Take(_numberOfWinnerParticipants / _teamSize).ToList();
			TournamentTeam[] teams = _teams;
			foreach (TournamentTeam tournamentTeam in teams)
			{
				if (!list3.Contains(tournamentTeam))
				{
					continue;
				}
				foreach (TournamentParticipant participant2 in tournamentTeam.Participants)
				{
					participant2.IsAssigned = false;
					list.Add(participant2);
				}
			}
			foreach (TournamentTeam item in orderedEnumerable)
			{
				int num2 = _numberOfWinnerParticipants - list.Count;
				if (item.Participants.Count() >= num2)
				{
					IOrderedEnumerable<TournamentParticipant> source = item.Participants.OrderByDescending((TournamentParticipant x) => x.Score);
					list.AddRange(source.Take(num2));
					break;
				}
				list.AddRange(item.Participants);
			}
		}
		return list;
	}
}
