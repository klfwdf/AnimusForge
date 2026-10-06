using System.Collections.Generic;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Tournament;

public class TournamentRoundVM : ViewModel
{
	private TournamentMatchVM _match1;

	private TournamentMatchVM _match2;

	private TournamentMatchVM _match3;

	private TournamentMatchVM _match4;

	private TournamentMatchVM _match5;

	private TournamentMatchVM _match6;

	private TournamentMatchVM _match7;

	private TournamentMatchVM _match8;

	private int _count = -1;

	private string _name;

	private bool _isValid;

	public TournamentRound Round { get; private set; }

	public List<TournamentMatchVM> Matches { get; }

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
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
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
	public TournamentMatchVM Match1
	{
		get
		{
			return _match1;
		}
		set
		{
			if (value != _match1)
			{
				_match1 = value;
				OnPropertyChangedWithValue(value, "Match1");
			}
		}
	}

	[DataSourceProperty]
	public TournamentMatchVM Match2
	{
		get
		{
			return _match2;
		}
		set
		{
			if (value != _match2)
			{
				_match2 = value;
				OnPropertyChangedWithValue(value, "Match2");
			}
		}
	}

	[DataSourceProperty]
	public TournamentMatchVM Match3
	{
		get
		{
			return _match3;
		}
		set
		{
			if (value != _match3)
			{
				_match3 = value;
				OnPropertyChangedWithValue(value, "Match3");
			}
		}
	}

	[DataSourceProperty]
	public TournamentMatchVM Match4
	{
		get
		{
			return _match4;
		}
		set
		{
			if (value != _match4)
			{
				_match4 = value;
				OnPropertyChangedWithValue(value, "Match4");
			}
		}
	}

	[DataSourceProperty]
	public TournamentMatchVM Match5
	{
		get
		{
			return _match5;
		}
		set
		{
			if (value != _match5)
			{
				_match5 = value;
				OnPropertyChangedWithValue(value, "Match5");
			}
		}
	}

	[DataSourceProperty]
	public TournamentMatchVM Match6
	{
		get
		{
			return _match6;
		}
		set
		{
			if (value != _match6)
			{
				_match6 = value;
				OnPropertyChangedWithValue(value, "Match6");
			}
		}
	}

	[DataSourceProperty]
	public TournamentMatchVM Match7
	{
		get
		{
			return _match7;
		}
		set
		{
			if (value != _match7)
			{
				_match7 = value;
				OnPropertyChangedWithValue(value, "Match7");
			}
		}
	}

	[DataSourceProperty]
	public TournamentMatchVM Match8
	{
		get
		{
			return _match8;
		}
		set
		{
			if (value != _match8)
			{
				_match8 = value;
				OnPropertyChangedWithValue(value, "Match8");
			}
		}
	}

	public TournamentRoundVM()
	{
		Match1 = new TournamentMatchVM();
		Match2 = new TournamentMatchVM();
		Match3 = new TournamentMatchVM();
		Match4 = new TournamentMatchVM();
		Match5 = new TournamentMatchVM();
		Match6 = new TournamentMatchVM();
		Match7 = new TournamentMatchVM();
		Match8 = new TournamentMatchVM();
		Matches = new List<TournamentMatchVM> { Match1, Match2, Match3, Match4, Match5, Match6, Match7, Match8 };
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Matches.ForEach(delegate(TournamentMatchVM x)
		{
			x.RefreshValues();
		});
	}

	public void Initialize()
	{
		for (int i = 0; i < Count; i++)
		{
			Matches[i].Initialize();
		}
	}

	public void Initialize(TournamentRound round, TextObject name)
	{
		IsValid = true;
		Round = round;
		Count = round.Matches.Length;
		for (int i = 0; i < round.Matches.Length; i++)
		{
			Matches[i].Initialize(round.Matches[i]);
		}
		Name = name.ToString();
	}

	public IEnumerable<TournamentParticipantVM> GetParticipants()
	{
		foreach (TournamentMatchVM match in Matches)
		{
			if (!match.IsValid)
			{
				continue;
			}
			foreach (TournamentParticipantVM participant in match.GetParticipants())
			{
				yield return participant;
			}
		}
	}
}
