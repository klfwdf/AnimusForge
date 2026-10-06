using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;

public class CharacterCreationGainGroupItemVM : ViewModel
{
	private MBBindingList<CharacterCreationGainedSkillItemVM> _skills;

	private CharacterCreationGainedAttributeItemVM _attribute;

	public CharacterAttribute AttributeObj { get; private set; }

	[DataSourceProperty]
	public MBBindingList<CharacterCreationGainedSkillItemVM> Skills
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
	public CharacterCreationGainedAttributeItemVM Attribute
	{
		get
		{
			return _attribute;
		}
		set
		{
			if (value != _attribute)
			{
				_attribute = value;
				OnPropertyChangedWithValue(value, "Attribute");
			}
		}
	}

	public CharacterCreationGainGroupItemVM(CharacterAttribute attributeObj)
	{
		AttributeObj = attributeObj;
		Skills = new MBBindingList<CharacterCreationGainedSkillItemVM>();
		Attribute = new CharacterCreationGainedAttributeItemVM(AttributeObj);
		List<SkillObject> list = TaleWorlds.CampaignSystem.Extensions.Skills.All.ToList();
		list.Sort(CampaignUIHelper.SkillObjectComparerInstance);
		foreach (SkillObject skill in list)
		{
			if (!CampaignUIHelper.GetIsNavalSkill(skill) && skill.Attributes.FirstOrDefault() == attributeObj && !Skills.Any((CharacterCreationGainedSkillItemVM s) => s.SkillObj == skill))
			{
				Skills.Add(new CharacterCreationGainedSkillItemVM(skill));
			}
		}
	}

	public void ResetValues()
	{
		Attribute.ResetValues();
		Skills.ApplyActionOnAllItems(delegate(CharacterCreationGainedSkillItemVM s)
		{
			s.ResetValues();
		});
	}
}
