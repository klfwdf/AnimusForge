using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Missions.Multiplayer;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.EndOfRound;

public class MultiplayerEndOfRoundSideVM : ViewModel
{
	private BasicCultureObject _culture;

	private bool _isWinner;

	private string _cultureID;

	private Color _cultureColor1;

	private Color _cultureColor2;

	private string _cultureName;

	private int _score;

	[DataSourceProperty]
	public bool IsWinner
	{
		get
		{
			return _isWinner;
		}
		set
		{
			if (value != _isWinner)
			{
				_isWinner = value;
				OnPropertyChangedWithValue(value, "IsWinner");
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

	[DataSourceProperty]
	public string CultureID
	{
		get
		{
			return _cultureID;
		}
		set
		{
			if (value != _cultureID)
			{
				_cultureID = value;
				OnPropertyChangedWithValue(value, "CultureID");
			}
		}
	}

	[DataSourceProperty]
	public string CultureName
	{
		get
		{
			return _cultureName;
		}
		set
		{
			if (value != _cultureName)
			{
				_cultureName = value;
				OnPropertyChangedWithValue(value, "CultureName");
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

	public void SetData(BasicCultureObject culture, int score, bool isWinner, MultiplayerBattleColors.MultiplayerCultureColorInfo cultureColors)
	{
		_culture = culture;
		CultureID = culture.StringId;
		Score = score;
		IsWinner = isWinner;
		CultureColor1 = cultureColors.Color1;
		CultureColor2 = cultureColors.Color2;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		CultureName = _culture.Name.ToString();
	}
}
