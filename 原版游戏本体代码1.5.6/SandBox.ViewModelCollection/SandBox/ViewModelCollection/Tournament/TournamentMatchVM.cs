using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Tournament;

public class TournamentMatchVM : ViewModel
{
	public enum TournamentMatchState
	{
		Unfinished,
		Current,
		Over,
		Active
	}

	private TournamentTeamVM _team1;

	private TournamentTeamVM _team2;

	private TournamentTeamVM _team3;

	private TournamentTeamVM _team4;

	private int _count = -1;

	private int _state = -1;

	private bool _isValid;

	public TournamentMatch Match { get; private set; }

	public List<TournamentTeamVM> Teams { get; }

	[DataSourceProperty]
	public bool IsValid
	{
		get
		{
			return _isValid;
		}
		set
		{
			if (value != _isValid)
			{
				_isValid = value;
				OnPropertyChangedWithValue(value, "IsValid");
			}
		}
	}

	[DataSourceProperty]
	public int State
	{
		get
		{
			return _state;
		}
		set
		{
			if (value != _state)
			{
				_state = value;
				OnPropertyChangedWithValue(value, "State");
			}
		}
	}

	[DataSourceProperty]
	public int Count
	{
		get
		{
			return _count;
		}
		set
		{
			if (value != _count)
			{
				_count = value;
				OnPropertyChangedWithValue(value, "Count");
			}
		}
	}

	[DataSourceProperty]
	public TournamentTeamVM Team1
	{
		get
		{
			return _team1;
		}
		set
		{
			if (value != _team1)
			{
				_team1 = value;
				OnPropertyChangedWithValue(value, "Team1");
			}
		}
	}

	[DataSourceProperty]
	public TournamentTeamVM Team2
	{
		get
		{
			return _team2;
		}
		set
		{
			if (value != _team2)
			{
				_team2 = value;
				OnPropertyChangedWithValue(value, "Team2");
			}
		}
	}

	[DataSourceProperty]
	public TournamentTeamVM Team3
	{
		get
		{
			return _team3;
		}
		set
		{
			if (value != _team3)
			{
				_team3 = value;
				OnPropertyChangedWithValue(value, "Team3");
			}
		}
	}

	[DataSourceProperty]
	public TournamentTeamVM Team4
	{
		get
		{
			return _team4;
		}
		set
		{
			if (value != _team4)
			{
				_team4 = value;
				OnPropertyChangedWithValue(value, "Team4");
			}
		}
	}

	public TournamentMatchVM()
	{
		Team1 = new TournamentTeamVM();
		Team2 = new TournamentTeamVM();
		Team3 = new TournamentTeamVM();
		Team4 = new TournamentTeamVM();
		Teams = new List<TournamentTeamVM> { Team1, Team2, Team3, Team4 };
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Teams.ForEach(delegate(TournamentTeamVM x)
		{
			x.RefreshValues();
		});
	}

	public void Initialize()
	{
		foreach (TournamentTeamVM team in Teams)
		{
			if (team.IsValid)
			{
				team.Initialize();
			}
		}
	}

	public void Initialize(TournamentMatch match)
	{
		int num = 0;
		Match = match;
		IsValid = Match != null;
		Count = match.Teams.Count();
		foreach (TournamentTeam team in match.Teams)
		{
			Teams[num].Initialize(team);
			num++;
		}
		State = 0;
	}

	public void Refresh(bool forceRefresh)
	{
		if (forceRefresh)
		{
			OnPropertyChanged("Count");
		}
		for (int i = 0; i < Count; i++)
		{
			TournamentTeamVM tournamentTeamVM = Teams[i];
			if (forceRefresh)
			{
				OnPropertyChanged("Team" + i + 1);
			}
			tournamentTeamVM.Refresh();
			for (int j = 0; j < tournamentTeamVM.Count; j++)
			{
				TournamentParticipantVM tournamentParticipantVM = tournamentTeamVM.Participants[j];
				tournamentParticipantVM.Score = tournamentParticipantVM.Participant.Score.ToString();
				tournamentParticipantVM.IsQualifiedForNextRound = Match.Winners.Contains(tournamentParticipantVM.Participant);
			}
		}
	}

	public void RefreshActiveMatch()
	{
		for (int i = 0; i < Count; i++)
		{
			TournamentTeamVM tournamentTeamVM = Teams[i];
			for (int j = 0; j < tournamentTeamVM.Count; j++)
			{
				TournamentParticipantVM tournamentParticipantVM = tournamentTeamVM.Participants[j];
				tournamentParticipantVM.Score = tournamentParticipantVM.Participant.Score.ToString();
			}
		}
	}

	public void Refresh(TournamentMatchVM target)
	{
		OnPropertyChanged("Count");
		int num = 0;
		foreach (TournamentTeamVM item in Teams.Where((TournamentTeamVM t) => t.IsValid))
		{
			OnPropertyChanged("Team" + num + 1);
			item.Refresh();
			num++;
		}
	}

	public IEnumerable<TournamentParticipantVM> GetParticipants()
	{
		List<TournamentParticipantVM> list = new List<TournamentParticipantVM>();
		if (Team1.IsValid)
		{
			list.AddRange(Team1.GetParticipants());
		}
		if (Team2.IsValid)
		{
			list.AddRange(Team2.GetParticipants());
		}
		if (Team3.IsValid)
		{
			list.AddRange(Team3.GetParticipants());
		}
		if (Team4.IsValid)
		{
			list.AddRange(Team4.GetParticipants());
		}
		return list;
	}
}
