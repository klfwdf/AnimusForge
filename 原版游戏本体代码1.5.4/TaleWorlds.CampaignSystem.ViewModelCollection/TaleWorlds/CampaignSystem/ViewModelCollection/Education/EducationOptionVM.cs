using System;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Education;

public class EducationOptionVM : StringItemWithActionVM
{
	private readonly TextObject _optionTextObject;

	private readonly TextObject _optionDescriptionObject;

	private readonly TextObject _optionEffectObject;

	private bool _isSelected;

	public string OptionEffect { get; private set; }

	public string OptionDescription { get; private set; }

	public EducationCampaignBehavior.EducationCharacterProperties[] CharacterProperties { get; private set; }

	public string ActionID { get; private set; }

	public (CharacterAttribute, int)[] OptionAttributes { get; private set; }

	public (SkillObject, int)[] OptionSkills { get; private set; }

	public (SkillObject, int)[] OptionFocusPoints { get; private set; }

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

	public EducationOptionVM(Action<object> onExecute, string optionId, TextObject optionText, TextObject optionDescription, TextObject optionEffect, bool isSelected, (CharacterAttribute, int)[] optionAttributes, (SkillObject, int)[] optionSkills, (SkillObject, int)[] optionFocusPoints, EducationCampaignBehavior.EducationCharacterProperties[] characterProperties)
		: base(onExecute, optionText.ToString(), optionId)
	{
		IsSelected = isSelected;
		CharacterProperties = characterProperties;
		_optionTextObject = optionText;
		_optionDescriptionObject = optionDescription;
		_optionEffectObject = optionEffect;
		OptionAttributes = optionAttributes;
		OptionSkills = optionSkills;
		OptionFocusPoints = optionFocusPoints;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		OptionEffect = _optionEffectObject.ToString();
		OptionDescription = _optionDescriptionObject.ToString();
		base.ActionText = _optionTextObject.ToString();
	}
}
