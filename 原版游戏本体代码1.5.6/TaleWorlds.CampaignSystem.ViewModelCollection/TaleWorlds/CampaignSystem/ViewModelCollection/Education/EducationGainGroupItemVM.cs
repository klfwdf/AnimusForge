using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Education;

public class EducationGainGroupItemVM : ViewModel
{
	private MBBindingList<EducationGainedSkillItemVM> _skills;

	private EducationGainedAttributeItemVM _attribute;

	public CharacterAttribute AttributeObj { get; private set; }

	[DataSourceProperty]
	public MBBindingList<EducationGainedSkillItemVM> Skills
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
	public EducationGainedAttributeItemVM Attribute
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

	public EducationGainGroupItemVM(CharacterAttribute attributeObj)
	{
		AttributeObj = attributeObj;
		Skills = new MBBindingList<EducationGainedSkillItemVM>();
		Attribute = new EducationGainedAttributeItemVM(AttributeObj);
		List<SkillObject> list = TaleWorlds.CampaignSystem.Extensions.Skills.All.ToList();
		list.Sort(CampaignUIHelper.SkillObjectComparerInstance);
		foreach (SkillObject skill in list)
		{
			if (!CampaignUIHelper.GetIsNavalSkill(skill) && skill.Attributes.FirstOrDefault() == AttributeObj && !Skills.Any((EducationGainedSkillItemVM s) => s.SkillObj == skill))
			{
				Skills.Add(new EducationGainedSkillItemVM(skill));
			}
		}
	}

	public void ResetValues()
	{
		Attribute.ResetValues();
		Skills.ApplyActionOnAllItems(delegate(EducationGainedSkillItemVM s)
		{
			s.ResetValues();
		});
	}
}
