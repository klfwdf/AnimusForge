using System;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;

public class PerkVM : ViewModel
{
	public enum PerkStates
	{
		None = -1,
		NotEarned,
		EarnedButNotSelected,
		EarnedAndActive,
		EarnedAndNotActive,
		EarnedPreviousPerkNotSelected
	}

	public enum PerkAlternativeType
	{
		NoAlternative,
		FirstAlternative,
		SecondAlternative
	}

	public readonly PerkObject Perk;

	private readonly Action<PerkVM> _onStartSelection;

	private readonly Action<PerkVM> _onSelectionOver;

	private readonly Func<PerkObject, bool> _getIsPerkSelected;

	private readonly Func<PerkObject, bool> _getIsPreviousPerkSelected;

	private readonly bool _isAvailable;

	private readonly Concept _perkConceptObj;

	private PerkStates _currentState = PerkStates.None;

	private string _levelText;

	private string _perkId;

	private string _backgroundImage;

	private BasicTooltipViewModel _hint;

	private int _level;

	private int _alternativeType;

	private int _perkState = -1;

	private bool _isTutorialHighlightEnabled;

	private bool _hasAlternativeAndSelected
	{
		get
		{
			if (AlternativeType != 0)
			{
				return _getIsPerkSelected(Perk.AlternativePerk);
			}
			return false;
		}
	}

	public PerkStates CurrentState
	{
		get
		{
			return _currentState;
		}
		private set
		{
			if (value != _currentState)
			{
				_currentState = value;
				PerkState = (int)value;
			}
		}
	}

	[DataSourceProperty]
	public bool IsTutorialHighlightEnabled
	{
		get
		{
			return _isTutorialHighlightEnabled;
		}
		set
		{
			if (value != _isTutorialHighlightEnabled)
			{
				_isTutorialHighlightEnabled = value;
				OnPropertyChangedWithValue(value, "IsTutorialHighlightEnabled");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel Hint
	{
		get
		{
			return _hint;
		}
		set
		{
			if (value != _hint)
			{
				_hint = value;
				OnPropertyChangedWithValue(value, "Hint");
			}
		}
	}

	[DataSourceProperty]
	public int Level
	{
		get
		{
			return _level;
		}
		set
		{
			if (value != _level)
			{
				_level = value;
				OnPropertyChangedWithValue(value, "Level");
			}
		}
	}

	[DataSourceProperty]
	public int PerkState
	{
		get
		{
			return _perkState;
		}
		set
		{
			if (value != _perkState)
			{
				_perkState = value;
				OnPropertyChangedWithValue(value, "PerkState");
			}
		}
	}

	[DataSourceProperty]
	public int AlternativeType
	{
		get
		{
			return _alternativeType;
		}
		set
		{
			if (value != _alternativeType)
			{
				_alternativeType = value;
				OnPropertyChangedWithValue(value, "AlternativeType");
			}
		}
	}

	[DataSourceProperty]
	public string LevelText
	{
		get
		{
			return _levelText;
		}
		set
		{
			if (value != _levelText)
			{
				_levelText = value;
				OnPropertyChangedWithValue(value, "LevelText");
			}
		}
	}

	[DataSourceProperty]
	public string BackgroundImage
	{
		get
		{
			return _backgroundImage;
		}
		set
		{
			if (value != _backgroundImage)
			{
				_backgroundImage = value;
				OnPropertyChangedWithValue(value, "BackgroundImage");
			}
		}
	}

	[DataSourceProperty]
	public string PerkId
	{
		get
		{
			return _perkId;
		}
		set
		{
			if (value != _perkId)
			{
				_perkId = value;
				OnPropertyChangedWithValue(value, "PerkId");
			}
		}
	}

	public PerkVM(PerkObject perk, bool isAvailable, PerkAlternativeType alternativeType, Action<PerkVM> onStartSelection, Action<PerkVM> onSelectionOver, Func<PerkObject, bool> getIsPerkSelected, Func<PerkObject, bool> getIsPreviousPerkSelected)
	{
		PerkVM perkVM = this;
		AlternativeType = (int)alternativeType;
		Perk = perk;
		_onStartSelection = onStartSelection;
		_onSelectionOver = onSelectionOver;
		_getIsPerkSelected = getIsPerkSelected;
		_getIsPreviousPerkSelected = getIsPreviousPerkSelected;
		_isAvailable = isAvailable;
		PerkId = "SPPerks\\" + perk.StringId;
		Level = (int)perk.RequiredSkillValue;
		LevelText = ((int)perk.RequiredSkillValue).ToString();
		Hint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPerkEffectText(perk, perkVM._getIsPerkSelected(perkVM.Perk)));
		_perkConceptObj = Concept.All.SingleOrDefault((Concept c) => c.StringId == "str_game_objects_perks");
		RefreshState();
	}

	public void RefreshState()
	{
		bool flag = _getIsPerkSelected(Perk);
		if (!_isAvailable)
		{
			CurrentState = PerkStates.NotEarned;
		}
		else if (flag)
		{
			CurrentState = PerkStates.EarnedAndActive;
		}
		else if (Perk.AlternativePerk != null && _getIsPerkSelected(Perk.AlternativePerk))
		{
			CurrentState = PerkStates.EarnedAndNotActive;
		}
		else if (_getIsPreviousPerkSelected(Perk))
		{
			CurrentState = PerkStates.EarnedButNotSelected;
		}
		else
		{
			CurrentState = PerkStates.EarnedPreviousPerkNotSelected;
		}
	}

	public void ExecuteShowPerkConcept()
	{
		if (_perkConceptObj != null)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(_perkConceptObj.EncyclopediaLink);
		}
		else
		{
			Debug.FailedAssert("Couldn't find Perks encyclopedia page", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\CharacterDeveloper\\PerkVM.cs", "ExecuteShowPerkConcept", 127);
		}
	}

	public void ExecuteStartSelection()
	{
		if (_isAvailable && !_getIsPerkSelected(Perk) && !_hasAlternativeAndSelected && _getIsPreviousPerkSelected(Perk))
		{
			_onStartSelection(this);
		}
	}
}
