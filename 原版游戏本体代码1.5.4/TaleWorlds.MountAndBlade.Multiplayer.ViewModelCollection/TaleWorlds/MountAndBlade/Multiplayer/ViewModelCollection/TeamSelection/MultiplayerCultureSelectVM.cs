using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Missions.Multiplayer;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.TeamSelection;

public class MultiplayerCultureSelectVM : ViewModel
{
	private BasicCultureObject _firstCulture;

	private BasicCultureObject _secondCulture;

	private Action<BasicCultureObject> _onCultureSelected;

	private Action _onClose;

	private string _gameModeText;

	private string _cultureSelectionText;

	private string _firstCultureName;

	private string _secondCultureName;

	private Color _firstCultureColor1;

	private Color _firstCultureColor2;

	private Color _secondCultureColor1;

	private Color _secondCultureColor2;

	private string _firstCultureCode;

	private string _secondCultureCode;

	[DataSourceProperty]
	public string GameModeText
	{
		get
		{
			return _gameModeText;
		}
		set
		{
			if (value != _gameModeText)
			{
				_gameModeText = value;
				OnPropertyChangedWithValue(value, "GameModeText");
			}
		}
	}

	[DataSourceProperty]
	public string CultureSelectionText
	{
		get
		{
			return _cultureSelectionText;
		}
		set
		{
			if (value != _cultureSelectionText)
			{
				_cultureSelectionText = value;
				OnPropertyChangedWithValue(value, "CultureSelectionText");
			}
		}
	}

	[DataSourceProperty]
	public string FirstCultureName
	{
		get
		{
			return _firstCultureName;
		}
		set
		{
			if (value != _firstCultureName)
			{
				_firstCultureName = value;
				OnPropertyChangedWithValue(value, "FirstCultureName");
			}
		}
	}

	[DataSourceProperty]
	public string SecondCultureName
	{
		get
		{
			return _secondCultureName;
		}
		set
		{
			if (value != _secondCultureName)
			{
				_secondCultureName = value;
				OnPropertyChangedWithValue(value, "SecondCultureName");
			}
		}
	}

	[DataSourceProperty]
	public string FirstCultureCode
	{
		get
		{
			return _firstCultureCode;
		}
		set
		{
			if (value != _firstCultureCode)
			{
				_firstCultureCode = value;
				OnPropertyChangedWithValue(value, "FirstCultureCode");
			}
		}
	}

	[DataSourceProperty]
	public string SecondCultureCode
	{
		get
		{
			return _secondCultureCode;
		}
		set
		{
			if (value != _secondCultureCode)
			{
				_secondCultureCode = value;
				OnPropertyChangedWithValue(value, "SecondCultureCode");
			}
		}
	}

	[DataSourceProperty]
	public Color FirstCultureColor1
	{
		get
		{
			return _firstCultureColor1;
		}
		set
		{
			if (value != _firstCultureColor1)
			{
				_firstCultureColor1 = value;
				OnPropertyChangedWithValue(value, "FirstCultureColor1");
			}
		}
	}

	[DataSourceProperty]
	public Color FirstCultureColor2
	{
		get
		{
			return _firstCultureColor2;
		}
		set
		{
			if (value != _firstCultureColor2)
			{
				_firstCultureColor2 = value;
				OnPropertyChangedWithValue(value, "FirstCultureColor2");
			}
		}
	}

	[DataSourceProperty]
	public Color SecondCultureColor1
	{
		get
		{
			return _secondCultureColor1;
		}
		set
		{
			if (value != _secondCultureColor1)
			{
				_secondCultureColor1 = value;
				OnPropertyChangedWithValue(value, "SecondCultureColor1");
			}
		}
	}

	[DataSourceProperty]
	public Color SecondCultureColor2
	{
		get
		{
			return _secondCultureColor2;
		}
		set
		{
			if (value != _secondCultureColor2)
			{
				_secondCultureColor2 = value;
				OnPropertyChangedWithValue(value, "SecondCultureColor2");
			}
		}
	}

	public MultiplayerCultureSelectVM(Action<BasicCultureObject> onCultureSelected, Action onClose)
	{
		_onCultureSelected = onCultureSelected;
		_onClose = onClose;
		_firstCulture = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
		_secondCulture = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
		FirstCultureCode = _firstCulture.StringId;
		SecondCultureCode = _secondCulture.StringId;
		MultiplayerBattleColors multiplayerBattleColors = MultiplayerBattleColors.CreateWith(_firstCulture, _secondCulture);
		FirstCultureColor1 = multiplayerBattleColors.AttackerColors.Color1;
		FirstCultureColor2 = multiplayerBattleColors.AttackerColors.Color2;
		SecondCultureColor1 = multiplayerBattleColors.DefenderColors.Color1;
		SecondCultureColor2 = multiplayerBattleColors.DefenderColors.Color2;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		GameModeText = GameTexts.FindText("str_multiplayer_official_game_type_name", MultiplayerOptions.OptionType.GameType.GetStrValue()).ToString();
		CultureSelectionText = new TextObject("{=yQ0p8Glo}Select Culture").ToString();
		FirstCultureName = _firstCulture.Name.ToString();
		SecondCultureName = _secondCulture.Name.ToString();
	}

	public void ExecuteSelectCulture(int cultureIndex)
	{
		switch (cultureIndex)
		{
		case 0:
			_onCultureSelected?.Invoke(_firstCulture);
			break;
		case 1:
			_onCultureSelected?.Invoke(_secondCulture);
			break;
		default:
			Debug.FailedAssert("Invalid Culture Index!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\TeamSelection\\MultiplayerCultureSelectVM.cs", "ExecuteSelectCulture", 65);
			break;
		}
	}

	public void ExecuteClose()
	{
		_onClose?.Invoke();
	}
}
