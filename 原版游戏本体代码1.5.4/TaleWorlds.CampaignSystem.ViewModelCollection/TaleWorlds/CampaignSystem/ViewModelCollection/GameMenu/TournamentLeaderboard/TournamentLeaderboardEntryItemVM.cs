using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TournamentLeaderboard;

public class TournamentLeaderboardEntryItemVM : ViewModel
{
	private readonly Hero _heroObj;

	private int _placementOnLeaderboard;

	private int _victories;

	private bool _isMainHero;

	private bool _isChampion;

	private string _name;

	private string _rankText;

	private string _prizeStr;

	private HeroVM _hero;

	private BasicTooltipViewModel _championRewardsHint;

	public int Rank { get; private set; }

	public float PrizeValue { get; private set; }

	[DataSourceProperty]
	public BasicTooltipViewModel ChampionRewardsHint
	{
		get
		{
			return _championRewardsHint;
		}
		set
		{
			if (value != _championRewardsHint)
			{
				_championRewardsHint = value;
				OnPropertyChangedWithValue(value, "ChampionRewardsHint");
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
	public string RankText
	{
		get
		{
			return _rankText;
		}
		set
		{
			if (value != _rankText)
			{
				_rankText = value;
				OnPropertyChangedWithValue(value, "RankText");
			}
		}
	}

	[DataSourceProperty]
	public int Victories
	{
		get
		{
			return _victories;
		}
		set
		{
			if (value != _victories)
			{
				_victories = value;
				OnPropertyChangedWithValue(value, "Victories");
			}
		}
	}

	[DataSourceProperty]
	public bool IsChampion
	{
		get
		{
			return _isChampion;
		}
		set
		{
			if (value != _isChampion)
			{
				_isChampion = value;
				OnPropertyChangedWithValue(value, "IsChampion");
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
	public HeroVM Hero
	{
		get
		{
			return _hero;
		}
		set
		{
			if (value != _hero)
			{
				_hero = value;
				OnPropertyChangedWithValue(value, "Hero");
			}
		}
	}

	[DataSourceProperty]
	public string PrizeStr
	{
		get
		{
			return _prizeStr;
		}
		set
		{
			if (value != _prizeStr)
			{
				_prizeStr = value;
				OnPropertyChangedWithValue(value, "PrizeStr");
			}
		}
	}

	[DataSourceProperty]
	public int PlacementOnLeaderboard
	{
		get
		{
			return _placementOnLeaderboard;
		}
		set
		{
			if (value != _placementOnLeaderboard)
			{
				_placementOnLeaderboard = value;
				OnPropertyChangedWithValue(value, "PlacementOnLeaderboard");
			}
		}
	}

	public TournamentLeaderboardEntryItemVM(Hero hero, int victories, int placement)
	{
		_heroObj = hero;
		PrizeStr = "-";
		Rank = placement;
		PlacementOnLeaderboard = placement;
		IsChampion = placement == 1;
		Victories = victories;
		if (float.TryParse(PrizeStr, out var result))
		{
			PrizeValue = result;
		}
		IsMainHero = hero == TaleWorlds.CampaignSystem.Hero.MainHero;
		Hero = new HeroVM(hero);
		ChampionRewardsHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTournamentChampionRewardsTooltip(hero, null));
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = _heroObj.Name.ToString();
		GameTexts.SetVariable("RANK", Rank);
		RankText = GameTexts.FindText("str_leaderboard_rank").ToString();
		Hero?.RefreshValues();
	}
}
