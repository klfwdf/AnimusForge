using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.CustomBattle;

public class CustomBattleTroopTypeVM : ViewModel
{
	public bool IsDefault;

	private readonly Action<CustomBattleTroopTypeVM> _onSelectionToggled;

	private readonly MBReadOnlyList<SkillObject> _allSkills;

	private CharacterImageIdentifierVM _visual;

	private BasicTooltipViewModel _troopSkillsHint;

	private HintViewModel _nameHint;

	private StringItemWithHintVM _tierIconData;

	private StringItemWithHintVM _typeIconData;

	private string _name;

	private bool _isSelected;

	public BasicCharacterObject Character { get; private set; }

	[DataSourceProperty]
	public CharacterImageIdentifierVM Visual
	{
		get
		{
			return _visual;
		}
		set
		{
			if (value != _visual)
			{
				_visual = value;
				OnPropertyChangedWithValue(value, "Visual");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel TroopSkillsHint
	{
		get
		{
			return _troopSkillsHint;
		}
		set
		{
			if (value != _troopSkillsHint)
			{
				_troopSkillsHint = value;
				OnPropertyChangedWithValue(value, "TroopSkillsHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel NameHint
	{
		get
		{
			return _nameHint;
		}
		set
		{
			if (value != _nameHint)
			{
				_nameHint = value;
				OnPropertyChangedWithValue(value, "NameHint");
			}
		}
	}

	[DataSourceProperty]
	public StringItemWithHintVM TierIconData
	{
		get
		{
			return _tierIconData;
		}
		set
		{
			if (value != _tierIconData)
			{
				_tierIconData = value;
				OnPropertyChangedWithValue(value, "TierIconData");
			}
		}
	}

	[DataSourceProperty]
	public StringItemWithHintVM TypeIconData
	{
		get
		{
			return _typeIconData;
		}
		set
		{
			if (value != _typeIconData)
			{
				_typeIconData = value;
				OnPropertyChangedWithValue(value, "TypeIconData");
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
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	public CustomBattleTroopTypeVM(BasicCharacterObject character, Action<CustomBattleTroopTypeVM> onSelectionToggled, StringItemWithHintVM typeIconData, MBReadOnlyList<SkillObject> allSkills, bool isDefault)
	{
		Character = character;
		IsDefault = isDefault;
		_onSelectionToggled = onSelectionToggled;
		_allSkills = allSkills;
		if (character != null)
		{
			Visual = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(character));
			NameHint = new HintViewModel(character.Name);
			TroopSkillsHint = new BasicTooltipViewModel(() => GetTroopSkillsTooltip(Character));
			TierIconData = GetCharacterTierData(Character);
			TypeIconData = typeIconData;
		}
		else
		{
			Debug.FailedAssert("Character shouldn't be null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.CustomBattle\\CustomBattle\\CustomBattleTroopTypeVM.cs", ".ctor", 40);
		}
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = Character?.Name.ToString();
	}

	public void ExecuteToggleSelection()
	{
		_onSelectionToggled?.Invoke(this);
	}

	public void ExecuteRandomize()
	{
		IsSelected = MBRandom.RandomInt(2) == 1;
	}

	private List<TooltipProperty> GetTroopSkillsTooltip(BasicCharacterObject character)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		list.Add(new TooltipProperty("", character.Name.ToString(), 1, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title));
		list.Add(new TooltipProperty(GameTexts.FindText("str_skills").ToString(), " ", 0));
		list.Add(new TooltipProperty("", "", 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.RundownSeperator));
		foreach (SkillObject allSkill in _allSkills)
		{
			int skillValue = character.GetSkillValue(allSkill);
			if (skillValue > 0)
			{
				list.Add(new TooltipProperty(allSkill.Name.ToString(), skillValue.ToString(), 0));
			}
		}
		return list;
	}

	public static StringItemWithHintVM GetCharacterTierData(BasicCharacterObject character, bool isBig = false)
	{
		int characterTier = GetCharacterTier(character);
		if (characterTier <= 0 || characterTier > 7)
		{
			return new StringItemWithHintVM("", null);
		}
		string text = (isBig ? (characterTier + "_big") : characterTier.ToString());
		string text2 = "General\\TroopTierIcons\\icon_tier_" + text;
		GameTexts.SetVariable("TIER_LEVEL", characterTier);
		TextObject hint = new TextObject("{=!}" + GameTexts.FindText("str_party_troop_tier").ToString());
		return new StringItemWithHintVM(text2, hint);
	}

	public static int GetCharacterTier(BasicCharacterObject character)
	{
		if (character.IsHero)
		{
			return 0;
		}
		return TaleWorlds.Library.MathF.Min(TaleWorlds.Library.MathF.Max(TaleWorlds.Library.MathF.Ceiling(((float)character.Level - 5f) / 5f), 0), 7);
	}
}
