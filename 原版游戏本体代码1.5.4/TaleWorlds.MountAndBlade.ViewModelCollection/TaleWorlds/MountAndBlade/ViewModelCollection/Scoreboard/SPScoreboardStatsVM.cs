using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard;

public class SPScoreboardStatsVM : ViewModel
{
	private TextObject _nameTextObject;

	private string _nameText = "";

	private int _kill;

	private int _dead;

	private int _wounded;

	private int _routed;

	private int _remaining;

	private int _readyToUpgrade;

	private bool _isMainParty;

	private bool _isMainHero;

	[DataSourceProperty]
	public string NameText
	{
		get
		{
			return _nameText;
		}
		set
		{
			if (value != _nameText)
			{
				_nameText = value;
				OnPropertyChangedWithValue(value, "NameText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMainHero
	{
		get
		{
			return _isMainHero;
		}
		set
		{
			if (value != _isMainHero)
			{
				_isMainHero = value;
				OnPropertyChangedWithValue(value, "IsMainHero");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMainParty
	{
		get
		{
			return _isMainParty;
		}
		set
		{
			if (value != _isMainParty)
			{
				_isMainParty = value;
				OnPropertyChangedWithValue(value, "IsMainParty");
			}
		}
	}

	[DataSourceProperty]
	public int Kill
	{
		get
		{
			return _kill;
		}
		set
		{
			if (value != _kill)
			{
				_kill = value;
				OnPropertyChangedWithValue(value, "Kill");
			}
		}
	}

	[DataSourceProperty]
	public int Dead
	{
		get
		{
			return _dead;
		}
		set
		{
			if (value != _dead)
			{
				_dead = value;
				OnPropertyChangedWithValue(value, "Dead");
			}
		}
	}

	[DataSourceProperty]
	public int Wounded
	{
		get
		{
			return _wounded;
		}
		set
		{
			if (value != _wounded)
			{
				_wounded = value;
				OnPropertyChangedWithValue(value, "Wounded");
			}
		}
	}

	[DataSourceProperty]
	public int Routed
	{
		get
		{
			return _routed;
		}
		set
		{
			if (value != _routed)
			{
				_routed = value;
				OnPropertyChangedWithValue(value, "Routed");
			}
		}
	}

	[DataSourceProperty]
	public int Remaining
	{
		get
		{
			return _remaining;
		}
		set
		{
			if (value != _remaining)
			{
				_remaining = value;
				OnPropertyChangedWithValue(value, "Remaining");
			}
		}
	}

	[DataSourceProperty]
	public int ReadyToUpgrade
	{
		get
		{
			return _readyToUpgrade;
		}
		set
		{
			if (value != _readyToUpgrade)
			{
				_readyToUpgrade = value;
				OnPropertyChangedWithValue(value, "ReadyToUpgrade");
			}
		}
	}

	public SPScoreboardStatsVM(TextObject name)
	{
		_nameTextObject = name;
		Kill = 0;
		Dead = 0;
		Wounded = 0;
		Routed = 0;
		Remaining = 0;
		ReadyToUpgrade = 0;
		IsMainParty = false;
		IsMainHero = false;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		NameText = _nameTextObject?.ToString() ?? "";
	}

	public void UpdateScores(int numberRemaining, int numberDead, int numberWounded, int numberRouted, int numberKilled, int numberReadyToUpgrade)
	{
		Kill += numberKilled;
		Dead += numberDead;
		Wounded += numberWounded;
		Routed += numberRouted;
		Remaining += numberRemaining;
		ReadyToUpgrade += numberReadyToUpgrade;
	}

	public bool IsAnyStatRelevant()
	{
		if (Remaining < 1)
		{
			return Routed >= 1;
		}
		return true;
	}

	public SPScoreboardStatsVM GetScoreForOneAliveMember()
	{
		return new SPScoreboardStatsVM(TextObject.GetEmpty())
		{
			Remaining = MathF.Min(1, Remaining),
			Dead = 0,
			Wounded = 0,
			Routed = MathF.Min(1, Routed),
			Kill = 0,
			ReadyToUpgrade = 0
		};
	}
}
