using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper.PerkSelection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;

public class CharacterDeveloperHeroItemVM : ViewModel
{
	private readonly PropertyOwner<CharacterAttribute> _characterAttributes;

	private MBBindingList<SkillVM> _skills;

	private PerkSelectionVM _perkSelection;

	private HeroViewModel _heroCharacter;

	private int _xpRequiredForNextLevel;

	private int _currentTotalXp;

	private int _levelProgressPercentage;

	private int _unspentCharacterPoints;

	private int _unspentAttributePoints;

	private string _currentCharacterLevelLbl;

	private string _levelProgressText;

	private string _heroNameText;

	private string _heroInfoText;

	private bool _isInspectingAnAttribute;

	private HintViewModel _levelHint;

	private SkillVM _currentSkill;

	private CharacterAttributeItemVM _currentInspectedAttribute;

	private string _heroNextLevelText;

	private string _focusPointsText;

	private MBBindingList<StringPairItemVM> _characterStats;

	private MBBindingList<CharacterAttributeItemVM> _attributes;

	private MBBindingList<EncyclopediaTraitItemVM> _traits;

	private bool _hasExtraSkills;

	public HeroDeveloper HeroDeveloper => Hero.HeroDeveloper;

	public Hero Hero { get; private set; }

	public int OrgUnspentFocusPoints { get; private set; }

	public int OrgUnspentAttributePoints { get; private set; }

	public IReadOnlyPropertyOwner<CharacterAttribute> CharacterAttributes => _characterAttributes;

	[DataSourceProperty]
	public MBBindingList<SkillVM> Skills
	{
		get
		{
			return _skills;
		}
		set
		{
			if (value != _skills)
			{
				_skills = value;
				OnPropertyChangedWithValue(value, "Skills");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<StringPairItemVM> CharacterStats
	{
		get
		{
			return _characterStats;
		}
		set
		{
			if (value != _characterStats)
			{
				_characterStats = value;
				OnPropertyChangedWithValue(value, "CharacterStats");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CharacterAttributeItemVM> Attributes
	{
		get
		{
			return _attributes;
		}
		set
		{
			if (value != _attributes)
			{
				_attributes = value;
				OnPropertyChangedWithValue(value, "Attributes");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EncyclopediaTraitItemVM> Traits
	{
		get
		{
			return _traits;
		}
		set
		{
			if (value != _traits)
			{
				_traits = value;
				OnPropertyChangedWithValue(value, "Traits");
			}
		}
	}

	[DataSourceProperty]
	public PerkSelectionVM PerkSelection
	{
		get
		{
			return _perkSelection;
		}
		set
		{
			if (value != _perkSelection)
			{
				_perkSelection = value;
				OnPropertyChangedWithValue(value, "PerkSelection");
			}
		}
	}

	[DataSourceProperty]
	public SkillVM CurrentSkill
	{
		get
		{
			return _currentSkill;
		}
		set
		{
			if (value != _currentSkill)
			{
				_currentSkill = value;
				OnPropertyChangedWithValue(value, "CurrentSkill");
			}
		}
	}

	[DataSourceProperty]
	public CharacterAttributeItemVM CurrentInspectedAttribute
	{
		get
		{
			return _currentInspectedAttribute;
		}
		set
		{
			if (value != _currentInspectedAttribute)
			{
				_currentInspectedAttribute = value;
				OnPropertyChangedWithValue(value, "CurrentInspectedAttribute");
			}
		}
	}

	[DataSourceProperty]
	public string FocusPointsText
	{
		get
		{
			return _focusPointsText;
		}
		set
		{
			if (value != _focusPointsText)
			{
				_focusPointsText = value;
				OnPropertyChangedWithValue(value, "FocusPointsText");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentCharacterLevelLbl
	{
		get
		{
			return _currentCharacterLevelLbl;
		}
		set
		{
			if (value != _currentCharacterLevelLbl)
			{
				_currentCharacterLevelLbl = value;
				OnPropertyChangedWithValue(value, "CurrentCharacterLevelLbl");
			}
		}
	}

	[DataSourceProperty]
	public string LevelProgressText
	{
		get
		{
			return _levelProgressText;
		}
		set
		{
			if (value != _levelProgressText)
			{
				_levelProgressText = value;
				OnPropertyChangedWithValue(value, "LevelProgressText");
			}
		}
	}

	[DataSourceProperty]
	public HeroViewModel HeroCharacter
	{
		get
		{
			return _heroCharacter;
		}
		set
		{
			if (value != _heroCharacter)
			{
				_heroCharacter = value;
				OnPropertyChangedWithValue(value, "HeroCharacter");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInspectingAnAttribute
	{
		get
		{
			return _isInspectingAnAttribute;
		}
		set
		{
			if (value != _isInspectingAnAttribute)
			{
				_isInspectingAnAttribute = value;
				OnPropertyChangedWithValue(value, "IsInspectingAnAttribute");
			}
		}
	}

	[DataSourceProperty]
	public int LevelProgressPercentage
	{
		get
		{
			return _levelProgressPercentage;
		}
		set
		{
			if (value != _levelProgressPercentage)
			{
				_levelProgressPercentage = value;
				OnPropertyChangedWithValue(value, "LevelProgressPercentage");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentTotalXp
	{
		get
		{
			return _currentTotalXp;
		}
		set
		{
			if (value != _currentTotalXp)
			{
				_currentTotalXp = value;
				OnPropertyChangedWithValue(value, "CurrentTotalXp");
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
	public int UnspentCharacterPoints
	{
		get
		{
			return _unspentCharacterPoints;
		}
		set
		{
			if (value != _unspentCharacterPoints)
			{
				_unspentCharacterPoints = value;
				OnPropertyChangedWithValue(value, "UnspentCharacterPoints");
			}
		}
	}

	[DataSourceProperty]
	public int UnspentAttributePoints
	{
		get
		{
			return _unspentAttributePoints;
		}
		set
		{
			if (value != _unspentAttributePoints)
			{
				_unspentAttributePoints = value;
				OnPropertyChangedWithValue(value, "UnspentAttributePoints");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel LevelHint
	{
		get
		{
			return _levelHint;
		}
		set
		{
			if (value != _levelHint)
			{
				_levelHint = value;
				OnPropertyChangedWithValue(value, "LevelHint");
			}
		}
	}

	[DataSourceProperty]
	public string HeroNameText
	{
		get
		{
			return _heroNameText;
		}
		set
		{
			if (value != _heroNameText)
			{
				_heroNameText = value;
				OnPropertyChangedWithValue(value, "HeroNameText");
			}
		}
	}

	[DataSourceProperty]
	public string HeroInfoText
	{
		get
		{
			return _heroInfoText;
		}
		set
		{
			if (value != _heroInfoText)
			{
				_heroInfoText = value;
				OnPropertyChangedWithValue(value, "HeroInfoText");
			}
		}
	}

	[DataSourceProperty]
	public string HeroNextLevelText
	{
		get
		{
			return _heroNextLevelText;
		}
		set
		{
			if (value != _heroNextLevelText)
			{
				_heroNextLevelText = value;
				OnPropertyChangedWithValue(value, "HeroNextLevelText");
			}
		}
	}

	[DataSourceProperty]
	public bool HasExtraSkills
	{
		get
		{
			return _hasExtraSkills;
		}
		set
		{
			if (value != _hasExtraSkills)
			{
				_hasExtraSkills = value;
				OnPropertyChangedWithValue(value, "HasExtraSkills");
			}
		}
	}

	public CharacterDeveloperHeroItemVM(Hero hero, Action onPerkSelection)
	{
		LevelHint = new HintViewModel();
		Hero = hero;
		OrgUnspentFocusPoints = HeroDeveloper.UnspentFocusPoints;
		UnspentCharacterPoints = OrgUnspentFocusPoints;
		OrgUnspentAttributePoints = HeroDeveloper.UnspentAttributePoints;
		UnspentAttributePoints = OrgUnspentAttributePoints;
		Attributes = new MBBindingList<CharacterAttributeItemVM>();
		_characterAttributes = new PropertyOwner<CharacterAttribute>();
		PerkSelection = new PerkSelectionVM(HeroDeveloper, RefreshPerksOfSkill, onPerkSelection);
		InitializeCharacter();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		StringHelpers.SetCharacterProperties("HERO", Hero.CharacterObject);
		HeroNameText = Hero.CharacterObject.Name.ToString();
		MBTextManager.SetTextVariable("LEVEL", Hero.CharacterObject.Level + 1);
		HeroNextLevelText = GameTexts.FindText("str_level_with_value").ToString();
		HeroInfoText = GameTexts.FindText("str_hero_name_level").ToString();
		FocusPointsText = GameTexts.FindText("str_focus_points").ToString();
		InitializeCharacter();
		Skills.ApplyActionOnAllItems(delegate(SkillVM x)
		{
			x.RefreshValues();
		});
		CurrentSkill.RefreshValues();
	}

	private void InitializeCharacter()
	{
		HeroCharacter = new HeroViewModel();
		Skills = new MBBindingList<SkillVM>();
		Traits = new MBBindingList<EncyclopediaTraitItemVM>();
		Attributes.Clear();
		HeroCharacter.FillFrom(Hero);
		HeroCharacter.SetEquipment(EquipmentIndex.ArmorItemEndSlot, default(EquipmentElement));
		HeroCharacter.SetEquipment(EquipmentIndex.HorseHarness, default(EquipmentElement));
		HeroCharacter.SetEquipment(EquipmentIndex.NumAllWeaponSlots, default(EquipmentElement));
		List<CharacterAttribute> list = TaleWorlds.CampaignSystem.Extensions.Attributes.All.ToList();
		list.Sort(CampaignUIHelper.CharacterAttributeComparerInstance);
		foreach (CharacterAttribute item in list)
		{
			_characterAttributes.SetPropertyValue(item, Hero.GetAttributeValue(item));
			Attributes.Add(new CharacterAttributeItemVM(Hero, item, this, OnInspectAttribute, OnAddAttributePoint));
		}
		List<SkillObject> list2 = TaleWorlds.CampaignSystem.Extensions.Skills.All.ToList();
		list2.Sort(CampaignUIHelper.SkillObjectComparerInstance);
		foreach (SkillObject item2 in list2)
		{
			Skills.Add(new SkillVM(item2, this, OnStartPerkSelection));
		}
		HasExtraSkills = Skills.Count > 18;
		foreach (SkillVM skill in Skills)
		{
			skill.RefreshWithCurrentValues();
		}
		foreach (CharacterAttributeItemVM attribute in Attributes)
		{
			attribute.RefreshWithCurrentValues();
		}
		SetCurrentSkill(Skills[0]);
		RefreshCharacterValues();
		CharacterStats = new MBBindingList<StringPairItemVM>();
		if (Hero.GovernorOf != null)
		{
			GameTexts.SetVariable("SETTLEMENT_NAME", Hero.GovernorOf.Name.ToString());
			CharacterStats.Add(new StringPairItemVM(GameTexts.FindText("str_governor_of_label").ToString(), ""));
		}
		if (MobileParty.MainParty.GetHeroPartyRoles(Hero).Count > 0)
		{
			CharacterStats.Add(new StringPairItemVM(CampaignUIHelper.GetHeroClanRoleText(Hero, Clan.PlayerClan), ""));
		}
		foreach (TraitObject heroTrait in CampaignUIHelper.GetHeroTraits())
		{
			if (Hero.GetTraitLevel(heroTrait) != 0)
			{
				Traits.Add(new EncyclopediaTraitItemVM(heroTrait, Hero));
			}
		}
	}

	private void OnInspectAttribute(CharacterAttributeItemVM att)
	{
		CurrentInspectedAttribute = att;
		IsInspectingAnAttribute = true;
	}

	private void OnAddAttributePoint(CharacterAttributeItemVM att)
	{
		UnspentAttributePoints--;
		_characterAttributes.SetPropertyValue(att.AttributeType, _characterAttributes.GetPropertyValue(att.AttributeType) + 1);
		RefreshCharacterValues();
	}

	public void ExecuteStopInspectingCurrentAttribute()
	{
		IsInspectingAnAttribute = false;
		CurrentInspectedAttribute = null;
	}

	public void RefreshCharacterValues()
	{
		CurrentCharacterLevelLbl = Hero.Level.ToString();
		CurrentTotalXp = HeroDeveloper.TotalXp;
		XpRequiredForNextLevel = Campaign.Current.Models.CharacterDevelopmentModel.SkillsRequiredForLevel(Hero.Level + 1);
		GameTexts.SetVariable("CURRENTAMOUNT", CurrentTotalXp);
		GameTexts.SetVariable("TARGETAMOUNT", XpRequiredForNextLevel);
		LevelProgressText = GameTexts.FindText("str_character_skillpoint_progress").ToString();
		GameTexts.SetVariable("newline", "\n");
		GameTexts.SetVariable("CURRENT_SKILL_POINTS", CurrentTotalXp);
		GameTexts.SetVariable("STR1", GameTexts.FindText("str_total_skill_points"));
		GameTexts.SetVariable("NEXT_SKILL_POINTS", XpRequiredForNextLevel);
		GameTexts.SetVariable("STR2", GameTexts.FindText("str_next_level_at"));
		string content = GameTexts.FindText("str_string_newline_string").ToString();
		GameTexts.SetVariable("SKILL_LEVEL_FOR_LEVEL_UP", XpRequiredForNextLevel - CurrentTotalXp);
		GameTexts.SetVariable("STR1", content);
		GameTexts.SetVariable("STR2", GameTexts.FindText("str_how_to_level_up_character"));
		string text = GameTexts.FindText("str_string_newline_string").ToString();
		LevelHint.HintText = new TextObject("{=!}" + text);
		foreach (SkillVM skill in Skills)
		{
			skill.RefreshWithCurrentValues();
		}
		foreach (CharacterAttributeItemVM attribute in Attributes)
		{
			attribute.RefreshWithCurrentValues();
		}
	}

	public void RefreshPerksOfSkill(SkillObject skill)
	{
		Skills.SingleOrDefault((SkillVM s) => s.Skill == skill)?.RefreshLists();
	}

	public void ResetChanges(bool isCancel)
	{
		PerkSelection.ResetSelectedPerks();
		foreach (CharacterAttribute item in TaleWorlds.CampaignSystem.Extensions.Attributes.All)
		{
			_characterAttributes.SetPropertyValue(item, Hero.GetAttributeValue(item));
		}
		if (!isCancel)
		{
			UnspentCharacterPoints = OrgUnspentFocusPoints;
			UnspentAttributePoints = OrgUnspentAttributePoints;
		}
		foreach (CharacterAttributeItemVM attribute in Attributes)
		{
			attribute.Reset();
		}
		if (!isCancel)
		{
			foreach (CharacterAttributeItemVM attribute2 in Attributes)
			{
				attribute2.RefreshWithCurrentValues();
			}
		}
		foreach (SkillVM skill in Skills)
		{
			skill.ResetChanges();
		}
		if (isCancel)
		{
			return;
		}
		foreach (SkillVM skill2 in Skills)
		{
			skill2.RefreshWithCurrentValues();
		}
	}

	public void ApplyChanges()
	{
		PerkSelection.ApplySelectedPerks();
		foreach (CharacterAttributeItemVM attribute in Attributes)
		{
			attribute.Commit();
		}
		foreach (SkillVM skill in Skills)
		{
			skill.ApplyChanges();
		}
	}

	public void SetCurrentSkill(SkillVM skill)
	{
		if (CurrentSkill != null)
		{
			CurrentSkill.IsInspected = false;
		}
		CurrentSkill = skill;
		CurrentSkill.IsInspected = true;
	}

	public bool IsThereAnyChanges()
	{
		bool flag = Skills.Any((SkillVM s) => s.IsThereAnyChanges());
		return UnspentCharacterPoints != OrgUnspentFocusPoints || UnspentAttributePoints != OrgUnspentAttributePoints || PerkSelection.IsAnyPerkSelected() || flag;
	}

	public int GetRequiredFocusPointsToAddFocusWithCurrentFocus(SkillObject skill)
	{
		return Hero.HeroDeveloper.GetRequiredFocusPointsToAddFocus(skill);
	}

	public bool CanAddFocusToSkillWithFocusAmount(int currentFocusAmount)
	{
		if (currentFocusAmount < Campaign.Current.Models.CharacterDevelopmentModel.MaxFocusPerSkill)
		{
			return UnspentCharacterPoints > 0;
		}
		return false;
	}

	public bool IsSkillMaxAmongOtherSkills(SkillVM skill)
	{
		if (Skills.Count > 0)
		{
			int currentFocusLevel = skill.CurrentFocusLevel;
			return Skills.Max((SkillVM s) => s.CurrentFocusLevel) <= currentFocusLevel;
		}
		return false;
	}

	public string GetNameWithNumOfUnopenedPerks()
	{
		if (Skills.Sum((SkillVM s) => s.NumOfUnopenedPerks) == 0)
		{
			return HeroNameText;
		}
		GameTexts.SetVariable("STR1", HeroNameText);
		GameTexts.SetVariable("STR2", "{=!}<img src=\"CharacterDeveloper\\UnselectedPerksIcon\" extend=\"2\">");
		return GameTexts.FindText("str_STR1_space_STR2").ToString();
	}

	private void OnStartPerkSelection(PerkVM perk)
	{
		PerkSelection.SetCurrentSelectionPerk(perk);
	}

	public int GetNumberOfUnselectedPerks()
	{
		return Skills.Sum((SkillVM s) => s.NumOfUnopenedPerks);
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		HeroCharacter.OnFinalize();
	}
}
