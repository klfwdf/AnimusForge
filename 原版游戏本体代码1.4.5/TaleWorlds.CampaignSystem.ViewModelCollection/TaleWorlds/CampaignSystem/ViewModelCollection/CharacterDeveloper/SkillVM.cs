using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;

public class SkillVM : ViewModel
{
	private enum SkillType
	{
		Default,
		Party,
		Leader
	}

	public const int MAX_SKILL_LEVEL = 300;

	public readonly SkillObject Skill;

	private readonly CharacterDeveloperHeroItemVM _heroItem;

	private readonly Concept _focusConceptObj;

	private readonly Concept _skillConceptObj;

	private readonly Action<PerkVM> _onStartPerkSelection;

	private int _orgFocusAmount;

	private MBBindingList<BindingListStringItem> _skillEffects;

	private MBBindingList<PerkVM> _perks;

	private BasicTooltipViewModel _progressHint;

	private HintViewModel _addFocusHint;

	private BasicTooltipViewModel _skillXPHint;

	private BasicTooltipViewModel _learningLimitTooltip;

	private BasicTooltipViewModel _learningRateTooltip;

	private string _nameText;

	private string _skillId;

	private string _addFocusText;

	private string _focusCostText;

	private string _currentLearningRateText;

	private string _nextLevelLearningRateText;

	private string _nextLevelCostText;

	private string _howToLearnText;

	private string _howToLearnTitle;

	private string _progressText;

	private string _descriptionText;

	private string _attributesText;

	private int _level = -1;

	private int _maxLevel;

	private int _currentFocusLevel;

	private int _currentSkillXP;

	private int _xpRequiredForNextLevel;

	private int _nextLevel;

	private int _fullLearningRateLevel;

	private int _numOfUnopenedPerks;

	private bool _isInspected;

	private bool _canAddFocus;

	private bool _canLearnSkill;

	private float _learningRate;

	private double _progressPercentage;

	[DataSourceProperty]
	public string DescriptionText
	{
		get
		{
			return _descriptionText;
		}
		set
		{
			if (value != _descriptionText)
			{
				_descriptionText = value;
				OnPropertyChangedWithValue(value, "DescriptionText");
			}
		}
	}

	[DataSourceProperty]
	public string HowToLearnText
	{
		get
		{
			return _howToLearnText;
		}
		set
		{
			if (value != _howToLearnText)
			{
				_howToLearnText = value;
				OnPropertyChangedWithValue(value, "HowToLearnText");
			}
		}
	}

	[DataSourceProperty]
	public string HowToLearnTitle
	{
		get
		{
			return _howToLearnTitle;
		}
		set
		{
			if (value != _howToLearnTitle)
			{
				_howToLearnTitle = value;
				OnPropertyChangedWithValue(value, "HowToLearnTitle");
			}
		}
	}

	[DataSourceProperty]
	public string AttributesText
	{
		get
		{
			return _attributesText;
		}
		set
		{
			if (value != _attributesText)
			{
				_attributesText = value;
				OnPropertyChangedWithValue(value, "AttributesText");
			}
		}
	}

	[DataSourceProperty]
	public bool CanAddFocus
	{
		get
		{
			return _canAddFocus;
		}
		set
		{
			if (value != _canAddFocus)
			{
				_canAddFocus = value;
				OnPropertyChangedWithValue(value, "CanAddFocus");
			}
		}
	}

	[DataSourceProperty]
	public bool CanLearnSkill
	{
		get
		{
			return _canLearnSkill;
		}
		set
		{
			if (value != _canLearnSkill)
			{
				_canLearnSkill = value;
				OnPropertyChangedWithValue(value, "CanLearnSkill");
			}
		}
	}

	[DataSourceProperty]
	public string NextLevelLearningRateText
	{
		get
		{
			return _nextLevelLearningRateText;
		}
		set
		{
			if (value != _nextLevelLearningRateText)
			{
				_nextLevelLearningRateText = value;
				OnPropertyChangedWithValue(value, "NextLevelLearningRateText");
			}
		}
	}

	[DataSourceProperty]
	public string NextLevelCostText
	{
		get
		{
			return _nextLevelCostText;
		}
		set
		{
			if (value != _nextLevelCostText)
			{
				_nextLevelCostText = value;
				OnPropertyChangedWithValue(value, "NextLevelCostText");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel ProgressHint
	{
		get
		{
			return _progressHint;
		}
		set
		{
			if (value != _progressHint)
			{
				_progressHint = value;
				OnPropertyChangedWithValue(value, "ProgressHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel SkillXPHint
	{
		get
		{
			return _skillXPHint;
		}
		set
		{
			if (value != _skillXPHint)
			{
				_skillXPHint = value;
				OnPropertyChangedWithValue(value, "SkillXPHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel AddFocusHint
	{
		get
		{
			return _addFocusHint;
		}
		set
		{
			if (value != _addFocusHint)
			{
				_addFocusHint = value;
				OnPropertyChangedWithValue(value, "AddFocusHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel LearningLimitTooltip
	{
		get
		{
			return _learningLimitTooltip;
		}
		set
		{
			if (value != _learningLimitTooltip)
			{
				_learningLimitTooltip = value;
				OnPropertyChangedWithValue(value, "LearningLimitTooltip");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel LearningRateTooltip
	{
		get
		{
			return _learningRateTooltip;
		}
		set
		{
			if (value != _learningRateTooltip)
			{
				_learningRateTooltip = value;
				OnPropertyChangedWithValue(value, "LearningRateTooltip");
			}
		}
	}

	[DataSourceProperty]
	public double ProgressPercentage
	{
		get
		{
			return _progressPercentage;
		}
		set
		{
			if (value != _progressPercentage)
			{
				_progressPercentage = value;
				OnPropertyChangedWithValue(value, "ProgressPercentage");
			}
		}
	}

	[DataSourceProperty]
	public float LearningRate
	{
		get
		{
			return _learningRate;
		}
		set
		{
			if (value != _learningRate)
			{
				_learningRate = value;
				OnPropertyChangedWithValue(value, "LearningRate");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentSkillXP
	{
		get
		{
			return _currentSkillXP;
		}
		set
		{
			if (value != _currentSkillXP)
			{
				_currentSkillXP = value;
				OnPropertyChangedWithValue(value, "CurrentSkillXP");
			}
		}
	}

	[DataSourceProperty]
	public int NextLevel
	{
		get
		{
			return _nextLevel;
		}
		set
		{
			if (value != _nextLevel)
			{
				_nextLevel = value;
				OnPropertyChangedWithValue(value, "NextLevel");
			}
		}
	}

	[DataSourceProperty]
	public int FullLearningRateLevel
	{
		get
		{
			return _fullLearningRateLevel;
		}
		set
		{
			if (value != _fullLearningRateLevel)
			{
				_fullLearningRateLevel = value;
				OnPropertyChangedWithValue(value, "FullLearningRateLevel");
			}
		}
	}

	[DataSourceProperty]
	public int XpRequiredForNextLevel
	{
		get
		{
			return _xpRequiredForNextLevel;
		}
		set
		{
			if (value != _xpRequiredForNextLevel)
			{
				_xpRequiredForNextLevel = value;
				OnPropertyChangedWithValue(value, "XpRequiredForNextLevel");
			}
		}
	}

	[DataSourceProperty]
	public int NumOfUnopenedPerks
	{
		get
		{
			return _numOfUnopenedPerks;
		}
		set
		{
			if (value != _numOfUnopenedPerks)
			{
				_numOfUnopenedPerks = value;
				OnPropertyChangedWithValue(value, "NumOfUnopenedPerks");
			}
		}
	}

	[DataSourceProperty]
	public string ProgressText
	{
		get
		{
			return _progressText;
		}
		set
		{
			if (value != _progressText)
			{
				_progressText = value;
				OnPropertyChangedWithValue(value, "ProgressText");
			}
		}
	}

	[DataSourceProperty]
	public string FocusCostText
	{
		get
		{
			return _focusCostText;
		}
		set
		{
			if (value != _focusCostText)
			{
				_focusCostText = value;
				OnPropertyChangedWithValue(value, "FocusCostText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<PerkVM> Perks
	{
		get
		{
			return _perks;
		}
		set
		{
			if (value != _perks)
			{
				_perks = value;
				OnPropertyChangedWithValue(value, "Perks");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BindingListStringItem> SkillEffects
	{
		get
		{
			return _skillEffects;
		}
		set
		{
			if (value != _skillEffects)
			{
				_skillEffects = value;
				OnPropertyChangedWithValue(value, "SkillEffects");
			}
		}
	}

	[DataSourceProperty]
	public int MaxLevel
	{
		get
		{
			return _maxLevel;
		}
		set
		{
			if (value != _maxLevel)
			{
				_maxLevel = value;
				OnPropertyChangedWithValue(value, "MaxLevel");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentLearningRateText
	{
		get
		{
			return _currentLearningRateText;
		}
		set
		{
			if (value != _currentLearningRateText)
			{
				_currentLearningRateText = value;
				OnPropertyChangedWithValue(value, "CurrentLearningRateText");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentFocusLevel
	{
		get
		{
			return _currentFocusLevel;
		}
		set
		{
			if (value != _currentFocusLevel)
			{
				_currentFocusLevel = value;
				OnPropertyChangedWithValue(value, "CurrentFocusLevel");
			}
		}
	}

	[DataSourceProperty]
	public string AddFocusText
	{
		get
		{
			return _addFocusText;
		}
		set
		{
			if (value != _addFocusText)
			{
				_addFocusText = value;
				OnPropertyChangedWithValue(value, "AddFocusText");
			}
		}
	}

	[DataSourceProperty]
	public string SkillId
	{
		get
		{
			return _skillId;
		}
		set
		{
			if (value != _skillId)
			{
				_skillId = value;
				OnPropertyChangedWithValue(value, "SkillId");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInspected
	{
		get
		{
			return _isInspected;
		}
		set
		{
			if (value != _isInspected)
			{
				_isInspected = value;
				OnPropertyChangedWithValue(value, "IsInspected");
			}
		}
	}

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

	public SkillVM(SkillObject skill, CharacterDeveloperHeroItemVM heroItem, Action<PerkVM> onStartPerkSelection)
	{
		SkillVM skillVM = this;
		_heroItem = heroItem;
		Skill = skill;
		MaxLevel = 300;
		SkillId = skill.StringId;
		_onStartPerkSelection = onStartPerkSelection;
		IsInspected = false;
		SkillEffects = new MBBindingList<BindingListStringItem>();
		Perks = new MBBindingList<PerkVM>();
		AddFocusHint = new HintViewModel();
		LearningRateTooltip = new BasicTooltipViewModel(() => CampaignUIHelper.GetLearningRateTooltip(skillVM._heroItem.CharacterAttributes, skillVM.CurrentFocusLevel, heroItem.Hero.GetSkillValue(skill), skillVM.Skill));
		LearningLimitTooltip = new BasicTooltipViewModel(() => CampaignUIHelper.GetLearningLimitTooltip(skillVM._heroItem.CharacterAttributes, skillVM.CurrentFocusLevel, skillVM.Skill));
		InitializeValues();
		_focusConceptObj = Concept.All.SingleOrDefault((Concept c) => c.StringId == "str_game_objects_skill_focus");
		_skillConceptObj = Concept.All.SingleOrDefault((Concept c) => c.StringId == "str_game_objects_skills");
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		AddFocusText = GameTexts.FindText("str_add_focus").ToString();
		HowToLearnText = Skill.HowToLearnSkillText.ToString();
		HowToLearnTitle = GameTexts.FindText("str_how_to_learn").ToString();
		DescriptionText = Skill.Description.ToString();
		NameText = Skill.Name.ToString();
		AttributesText = GameTexts.GameTextHelper.MergeTextObjectsWithComma(Skill.Attributes.Select((CharacterAttribute x) => x.Abbreviation).ToList(), includeAnd: false).ToString();
		InitializeValues();
		RefreshWithCurrentValues();
		SkillEffects.ApplyActionOnAllItems(delegate(BindingListStringItem x)
		{
			x.RefreshValues();
		});
		Perks.ApplyActionOnAllItems(delegate(PerkVM x)
		{
			x.RefreshValues();
		});
	}

	public void InitializeValues()
	{
		if (_heroItem.HeroDeveloper == null)
		{
			Level = 0;
		}
		else
		{
			Level = _heroItem.HeroDeveloper.Hero.GetSkillValue(Skill);
			NextLevel = Level + 1;
			CurrentSkillXP = _heroItem.HeroDeveloper.GetSkillXpProgress(Skill);
			XpRequiredForNextLevel = Campaign.Current.Models.CharacterDevelopmentModel.GetXpRequiredForSkillLevel(Level + 1) - Campaign.Current.Models.CharacterDevelopmentModel.GetXpRequiredForSkillLevel(Level);
			ProgressPercentage = 100.0 * (double)_currentSkillXP / (double)XpRequiredForNextLevel;
			ProgressHint = new BasicTooltipViewModel(delegate
			{
				GameTexts.SetVariable("CURRENT_XP", CurrentSkillXP.ToString());
				GameTexts.SetVariable("LEVEL_MAX_XP", XpRequiredForNextLevel.ToString());
				return GameTexts.FindText("str_current_xp_over_max").ToString();
			});
			GameTexts.SetVariable("CURRENT_XP", CurrentSkillXP.ToString());
			GameTexts.SetVariable("LEVEL_MAX_XP", XpRequiredForNextLevel.ToString());
			ProgressText = GameTexts.FindText("str_current_xp_over_max").ToString();
			SkillXPHint = new BasicTooltipViewModel(delegate
			{
				GameTexts.SetVariable("REQUIRED_XP_FOR_NEXT_LEVEL", XpRequiredForNextLevel - CurrentSkillXP);
				return GameTexts.FindText("str_skill_xp_hint").ToString();
			});
		}
		_orgFocusAmount = _heroItem.HeroDeveloper.GetFocus(Skill);
		CurrentFocusLevel = _orgFocusAmount;
		CreateLists();
	}

	public void RefreshWithCurrentValues()
	{
		float resultNumber = Campaign.Current.Models.CharacterDevelopmentModel.CalculateLearningRate(_heroItem.CharacterAttributes, CurrentFocusLevel, _heroItem.Hero.GetSkillValue(Skill), Skill).ResultNumber;
		GameTexts.SetVariable("COUNT", resultNumber.ToString("0.00"));
		CurrentLearningRateText = GameTexts.FindText("str_learning_rate_COUNT").ToString();
		CanLearnSkill = Math.Round(resultNumber, 2) > 0.0;
		LearningRate = resultNumber;
		FullLearningRateLevel = TaleWorlds.Library.MathF.Round(Campaign.Current.Models.CharacterDevelopmentModel.CalculateLearningLimit(_heroItem.CharacterAttributes, CurrentFocusLevel, Skill).ResultNumber);
		int requiredFocusPointsToAddFocusWithCurrentFocus = _heroItem.GetRequiredFocusPointsToAddFocusWithCurrentFocus(Skill);
		GameTexts.SetVariable("COSTAMOUNT", requiredFocusPointsToAddFocusWithCurrentFocus);
		FocusCostText = requiredFocusPointsToAddFocusWithCurrentFocus.ToString();
		GameTexts.SetVariable("COUNT", requiredFocusPointsToAddFocusWithCurrentFocus);
		GameTexts.SetVariable("RIGHT", "");
		GameTexts.SetVariable("LEFT", GameTexts.FindText("str_cost_COUNT"));
		MBTextManager.SetTextVariable("FOCUS_ICON", "{=!}<img src=\"CharacterDeveloper\\cp_icon\">");
		NextLevelCostText = GameTexts.FindText("str_sf_text_with_focus_icon").ToString();
		RefreshCanAddFocus();
	}

	public void CreateLists()
	{
		SkillEffects.Clear();
		Perks.Clear();
		int skillValue = _heroItem.HeroDeveloper.Hero.GetSkillValue(Skill);
		foreach (SkillEffect item2 in SkillEffect.All.Where((SkillEffect x) => x.EffectedSkill == Skill))
		{
			SkillEffects.Add(new BindingListStringItem(CampaignUIHelper.GetSkillEffectText(item2, skillValue)));
		}
		foreach (PerkObject item3 in from p in PerkObject.All
			where p.Skill == Skill
			orderby p.RequiredSkillValue
			select p)
		{
			PerkVM.PerkAlternativeType alternativeType = ((item3.AlternativePerk != null) ? ((item3.StringId.CompareTo(item3.AlternativePerk.StringId) < 0) ? PerkVM.PerkAlternativeType.FirstAlternative : PerkVM.PerkAlternativeType.SecondAlternative) : PerkVM.PerkAlternativeType.NoAlternative);
			PerkVM item = new PerkVM(item3, IsPerkAvailable(item3), alternativeType, OnStartPerkSelection, OnPerkSelectionOver, IsPerkSelected, IsPreviousPerkSelected);
			Perks.Add(item);
		}
		RefreshNumOfUnopenedPerks();
	}

	public void RefreshLists(SkillObject skill = null)
	{
		if (skill != null && skill != Skill)
		{
			return;
		}
		foreach (PerkVM perk in Perks)
		{
			perk.RefreshState();
		}
		RefreshNumOfUnopenedPerks();
	}

	private void RefreshNumOfUnopenedPerks()
	{
		int num = 0;
		foreach (PerkVM perk in Perks)
		{
			if ((perk.CurrentState == PerkVM.PerkStates.EarnedButNotSelected || perk.CurrentState == PerkVM.PerkStates.EarnedPreviousPerkNotSelected) && (perk.AlternativeType == 1 || perk.AlternativeType == 0))
			{
				num++;
			}
		}
		NumOfUnopenedPerks = num;
	}

	private bool IsPerkSelected(PerkObject perk)
	{
		if (!_heroItem.HeroDeveloper.GetPerkValue(perk))
		{
			return _heroItem.PerkSelection.IsPerkSelected(perk);
		}
		return true;
	}

	private bool IsPreviousPerkSelected(PerkObject perk)
	{
		IEnumerable<PerkObject> source = PerkObject.All.Where((PerkObject p) => p.Skill == perk.Skill && p.RequiredSkillValue < perk.RequiredSkillValue);
		if (!source.Any())
		{
			return true;
		}
		PerkObject perkObject = TaleWorlds.Core.Extensions.MaxBy(source, (PerkObject p) => p.RequiredSkillValue - perk.RequiredSkillValue);
		if (!IsPerkSelected(perkObject))
		{
			if (perkObject.AlternativePerk != null)
			{
				return IsPerkSelected(perkObject.AlternativePerk);
			}
			return false;
		}
		return true;
	}

	private bool IsPerkAvailable(PerkObject perk)
	{
		return perk.RequiredSkillValue <= (float)Level;
	}

	public void RefreshCanAddFocus()
	{
		bool playerHasEnoughPoints = _heroItem.UnspentCharacterPoints >= _heroItem.GetRequiredFocusPointsToAddFocusWithCurrentFocus(Skill);
		bool isMaxedSkill = _currentFocusLevel >= Campaign.Current.Models.CharacterDevelopmentModel.MaxFocusPerSkill;
		string addFocusHintString = CampaignUIHelper.GetAddFocusHintString(playerHasEnoughPoints, isMaxedSkill, CurrentFocusLevel);
		AddFocusHint.HintText = (string.IsNullOrEmpty(addFocusHintString) ? TextObject.GetEmpty() : new TextObject("{=!}" + addFocusHintString));
		CanAddFocus = _heroItem.CanAddFocusToSkillWithFocusAmount(_currentFocusLevel);
	}

	public void ExecuteAddFocus()
	{
		if (CanAddFocus)
		{
			_heroItem.UnspentCharacterPoints -= _heroItem.GetRequiredFocusPointsToAddFocusWithCurrentFocus(Skill);
			CurrentFocusLevel++;
			_heroItem.RefreshCharacterValues();
			RefreshWithCurrentValues();
			MBInformationManager.HideInformations();
			Game.Current.EventManager.TriggerEvent(new FocusAddedByPlayerEvent(_heroItem.Hero, Skill));
		}
	}

	public void ExecuteShowFocusConcept()
	{
		if (_focusConceptObj != null)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(_focusConceptObj.EncyclopediaLink);
		}
		else
		{
			Debug.FailedAssert("Couldn't find Focus encyclopedia page", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\CharacterDeveloper\\SkillVM.cs", "ExecuteShowFocusConcept", 253);
		}
	}

	public void ExecuteShowSkillConcept()
	{
		if (_focusConceptObj != null)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(_skillConceptObj.EncyclopediaLink);
		}
		else
		{
			Debug.FailedAssert("Couldn't find Focus encyclopedia page", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\CharacterDeveloper\\SkillVM.cs", "ExecuteShowSkillConcept", 265);
		}
	}

	public void ExecuteInspect()
	{
		_heroItem.SetCurrentSkill(this);
		RefreshCanAddFocus();
	}

	public void ResetChanges()
	{
		CurrentFocusLevel = _orgFocusAmount;
		Perks.ApplyActionOnAllItems(delegate(PerkVM p)
		{
			p.RefreshState();
		});
		RefreshNumOfUnopenedPerks();
	}

	public bool IsThereAnyChanges()
	{
		return CurrentFocusLevel != _orgFocusAmount;
	}

	public void ApplyChanges()
	{
		for (int i = 0; i < CurrentFocusLevel - _orgFocusAmount; i++)
		{
			_heroItem.HeroDeveloper.AddFocus(Skill, 1);
		}
		_orgFocusAmount = CurrentFocusLevel;
	}

	private void OnStartPerkSelection(PerkVM perk)
	{
		_onStartPerkSelection(perk);
		if (perk.AlternativeType != 0)
		{
			Perks.SingleOrDefault((PerkVM p) => p.Perk == perk.Perk.AlternativePerk);
		}
	}

	private void OnPerkSelectionOver(PerkVM perk)
	{
		if (perk.AlternativeType != 0)
		{
			Perks.SingleOrDefault((PerkVM p) => p.Perk == perk.Perk.AlternativePerk);
		}
	}
}
