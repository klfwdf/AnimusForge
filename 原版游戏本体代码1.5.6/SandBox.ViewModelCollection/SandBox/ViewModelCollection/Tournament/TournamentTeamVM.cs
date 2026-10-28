using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Tournament;

public class TournamentTeamVM : ViewModel
{
	private TournamentTeam _team;

	private int _count = -1;

	private TournamentParticipantVM _participant1;

	private TournamentParticipantVM _participant2;

	private TournamentParticipantVM _participant3;

	private TournamentParticipantVM _participant4;

	private TournamentParticipantVM _participant5;

	private TournamentParticipantVM _participant6;

	private TournamentParticipantVM _participant7;

	private TournamentParticipantVM _participant8;

	private int _score;

	private bool _isValid;

	public List<TournamentParticipantVM> Participants { get; }

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
	public int Score
	{
		get
		{
			return _score;
		}
		set
		{
			if (value != _score)
			{
				_score = value;
				OnPropertyChangedWithValue(value, "Score");
			}
		}
	}

	[DataSourceProperty]
	public TournamentParticipantVM Participant1
	{
		get
		{
			return _participant1;
		}
		set
		{
			if (value != _participant1)
			{
				_participant1 = value;
				OnPropertyChangedWithValue(value, "Participant1");
			}
		}
	}

	[DataSourceProperty]
	public TournamentParticipantVM Participant2
	{
		get
		{
			return _participant2;
		}
		set
		{
			if (value != _participant2)
			{
				_participant2 = value;
				OnPropertyChangedWithValue(value, "Participant2");
			}
		}
	}

	[DataSourceProperty]
	public TournamentParticipantVM Participant3
	{
		get
		{
			return _participant3;
		}
		set
		{
			if (value != _participant3)
			{
				_participant3 = value;
				OnPropertyChangedWithValue(value, "Participant3");
			}
		}
	}

	[DataSourceProperty]
	public TournamentParticipantVM Participant4
	{
		get
		{
			return _participant4;
		}
		set
		{
			if (value != _participant4)
			{
				_participant4 = value;
				OnPropertyChangedWithValue(value, "Participant4");
			}
		}
	}

	[DataSourceProperty]
	public TournamentParticipantVM Participant5
	{
		get
		{
			return _participant5;
		}
		set
		{
			if (value != _participant5)
			{
				_participant5 = value;
				OnPropertyChangedWithValue(value, "Participant5");
			}
		}
	}

	[DataSourceProperty]
	public TournamentParticipantVM Participant6
	{
		get
		{
			return _participant6;
		}
		set
		{
			if (value != _participant6)
			{
				_participant6 = value;
				OnPropertyChangedWithValue(value, "Participant6");
			}
		}
	}

	[DataSourceProperty]
	public TournamentParticipantVM Participant7
	{
		get
		{
			return _participant7;
		}
		set
		{
			if (value != _participant7)
			{
				_participant7 = value;
				OnPropertyChangedWithValue(value, "Participant7");
			}
		}
	}

	[DataSourceProperty]
	public TournamentParticipantVM Participant8
	{
		get
		{
			return _participant8;
		}
		set
		{
			if (value != _participant8)
			{
				_participant8 = value;
				OnPropertyChangedWithValue(value, "Participant8");
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

	public TournamentTeamVM()
	{
		Participant1 = new TournamentParticipantVM();
		Participant2 = new TournamentParticipantVM();
		Participant3 = new TournamentParticipantVM();
		Participant4 = new TournamentParticipantVM();
		Participant5 = new TournamentParticipantVM();
		Participant6 = new TournamentParticipantVM();
		Participant7 = new TournamentParticipantVM();
		Participant8 = new TournamentParticipantVM();
		Participants = new List<TournamentParticipantVM> { Participant1, Participant2, Participant3, Participant4, Participant5, Participant6, Participant7, Participant8 };
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Participants.ForEach(delegate(TournamentParticipantVM x)
		{
			x.RefreshValues();
		});
	}

	public void Initialize()
	{
		IsValid = _team != null;
		for (int i = 0; i < Count; i++)
		{
			TournamentParticipant participant = _team.Participants.ElementAtOrDefault(i);
			Participants[i].Refresh(participant, Color.FromUint(_team.TeamColor));
		}
	}

	public void Initialize(TournamentTeam team)
	{
		_team = team;
		Count = team.TeamSize;
		IsValid = _team != null;
		Initialize();
	}

	public void Refresh()
	{
		IsValid = _team != null;
		OnPropertyChanged("Count");
		int num = 0;
		foreach (TournamentParticipantVM item in Participants.Where((TournamentParticipantVM p) => p.IsValid))
		{
			OnPropertyChanged("Participant" + num);
			item.Refresh();
			num++;
		}
	}

	public IEnumerable<TournamentParticipantVM> GetParticipants()
	{
		if (Participant1.IsValid)
		{
			yield return Participant1;
		}
		if (Participant2.IsValid)
		{
			yield return Participant2;
		}
		if (Participant3.IsValid)
		{
			yield return Participant3;
		}
		if (Participant4.IsValid)
		{
			yield return Participant4;
		}
		if (Participant5.IsValid)
		{
			yield return Participant5;
		}
		if (Participant6.IsValid)
		{
			yield return Participant6;
		}
		if (Participant7.IsValid)
		{
			yield return Participant7;
		}
		if (Participant8.IsValid)
		{
			yield return Participant8;
		}
	}
}
