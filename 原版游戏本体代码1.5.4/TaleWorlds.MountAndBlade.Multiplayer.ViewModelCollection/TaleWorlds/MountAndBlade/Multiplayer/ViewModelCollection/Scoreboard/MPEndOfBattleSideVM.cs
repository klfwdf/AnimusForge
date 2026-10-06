using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Missions.Multiplayer;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Scoreboard;

public class MPEndOfBattleSideVM : ViewModel
{
	private MissionScoreboardComponent _missionScoreboardComponent;

	private BasicCultureObject _culture;

	private string _factionName;

	private string _cultureId;

	private int _score;

	private bool _isRoundWinner;

	private Color _cultureColor1;

	private Color _cultureColor2;

	public MissionScoreboardComponent.MissionScoreboardSide Side { get; private set; }

	[DataSourceProperty]
	public string FactionName
	{
		get
		{
			return _factionName;
		}
		set
		{
			if (value != _factionName)
			{
				_factionName = value;
				OnPropertyChangedWithValue(value, "FactionName");
			}
		}
	}

	[DataSourceProperty]
	public string CultureId
	{
		get
		{
			return _cultureId;
		}
		set
		{
			if (value != _cultureId)
			{
				_cultureId = value;
				OnPropertyChangedWithValue(value, "CultureId");
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
	public bool IsRoundWinner
	{
		get
		{
			return _isRoundWinner;
		}
		set
		{
			if (value != _isRoundWinner)
			{
				_isRoundWinner = value;
				OnPropertyChangedWithValue(value, "IsRoundWinner");
			}
		}
	}

	[DataSourceProperty]
	public Color CultureColor1
	{
		get
		{
			return _cultureColor1;
		}
		set
		{
			if (value != _cultureColor1)
			{
				_cultureColor1 = value;
				OnPropertyChangedWithValue(value, "CultureColor1");
			}
		}
	}

	[DataSourceProperty]
	public Color CultureColor2
	{
		get
		{
			return _cultureColor2;
		}
		set
		{
			if (value != _cultureColor2)
			{
				_cultureColor2 = value;
				OnPropertyChangedWithValue(value, "CultureColor2");
			}
		}
	}

	public MPEndOfBattleSideVM(MissionScoreboardComponent missionScoreboardComponent, MissionScoreboardComponent.MissionScoreboardSide side, MultiplayerBattleColors.MultiplayerCultureColorInfo cultureColorInfo)
	{
		_missionScoreboardComponent = missionScoreboardComponent;
		Side = side;
		_culture = cultureColorInfo.Culture;
		if (Side != null)
		{
			CultureId = _culture.StringId;
			Score = Side.SideScore;
			IsRoundWinner = _missionScoreboardComponent.RoundWinner == side.Side || _missionScoreboardComponent.RoundWinner == BattleSideEnum.None;
		}
		CultureColor1 = cultureColorInfo.Color1;
		CultureColor2 = cultureColorInfo.Color2;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (Side != null)
		{
			CultureId = _culture.StringId;
		}
	}
}
