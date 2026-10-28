using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Education;

public class EducationGainedPropertiesVM : ViewModel
{
	private readonly Hero _child;

	private readonly int _pageCount;

	private readonly IEducationLogic _educationBehavior;

	private readonly Dictionary<CharacterAttribute, Tuple<int, int>> _affectedAttributesMap;

	private readonly Dictionary<SkillObject, Tuple<int, int>> _affectedSkillFocusMap;

	private readonly Dictionary<SkillObject, Tuple<int, int>> _affectedSkillValueMap;

	private MBBindingList<EducationGainGroupItemVM> _gainGroups;

	private MBBindingList<EducationGainedSkillItemVM> _otherSkills;

	[DataSourceProperty]
	public MBBindingList<EducationGainGroupItemVM> GainGroups
	{
		get
		{
			return _gainGroups;
		}
		set
		{
			if (value != _gainGroups)
			{
				_gainGroups = value;
				OnPropertyChangedWithValue(value, "GainGroups");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EducationGainedSkillItemVM> OtherSkills
	{
		get
		{
			return _otherSkills;
		}
		set
		{
			if (value != _otherSkills)
			{
				_otherSkills = value;
				OnPropertyChangedWithValue(value, "OtherSkills");
			}
		}
	}

	public EducationGainedPropertiesVM(Hero child, int pageCount)
	{
		_child = child;
		_pageCount = pageCount;
		_educationBehavior = Campaign.Current.GetCampaignBehavior<IEducationLogic>();
		_affectedSkillFocusMap = new Dictionary<SkillObject, Tuple<int, int>>();
		_affectedSkillValueMap = new Dictionary<SkillObject, Tuple<int, int>>();
		_affectedAttributesMap = new Dictionary<CharacterAttribute, Tuple<int, int>>();
		GainGroups = new MBBindingList<EducationGainGroupItemVM>();
		OtherSkills = new MBBindingList<EducationGainedSkillItemVM>();
		List<CharacterAttribute> list = Attributes.All.ToList();
		list.Sort(CampaignUIHelper.CharacterAttributeComparerInstance);
		foreach (CharacterAttribute item in list)
		{
			GainGroups.Add(new EducationGainGroupItemVM(item));
		}
		List<SkillObject> list2 = Skills.All.ToList();
		list2.Sort(CampaignUIHelper.SkillObjectComparerInstance);
		foreach (SkillObject skill in list2)
		{
			if (!GainGroups.Any((EducationGainGroupItemVM attribute) => attribute.Skills.Any((EducationGainedSkillItemVM attributeSkill) => attributeSkill.SkillId == skill.StringId)))
			{
				OtherSkills.Add(new EducationGainedSkillItemVM(skill));
			}
		}
		UpdateWithSelections(new List<string>(), -1);
	}

	internal void UpdateWithSelections(List<string> selectedOptions, int currentPageIndex)
	{
		_affectedAttributesMap.Clear();
		_affectedSkillFocusMap.Clear();
		_affectedSkillValueMap.Clear();
		GainGroups.ApplyActionOnAllItems(delegate(EducationGainGroupItemVM g)
		{
			g.ResetValues();
		});
		OtherSkills.ApplyActionOnAllItems(delegate(EducationGainedSkillItemVM s)
		{
			s.ResetValues();
		});
		PopulateInitialValues();
		PopulateGainedAttributeValues(selectedOptions, currentPageIndex);
		foreach (KeyValuePair<CharacterAttribute, Tuple<int, int>> item in _affectedAttributesMap)
		{
			GetItemFromAttribute(item.Key).SetValue(item.Value.Item1, item.Value.Item2);
		}
		foreach (KeyValuePair<SkillObject, Tuple<int, int>> item2 in _affectedSkillFocusMap)
		{
			GetItemFromSkill(item2.Key).SetFocusValue(item2.Value.Item1, item2.Value.Item2);
		}
		foreach (KeyValuePair<SkillObject, Tuple<int, int>> item3 in _affectedSkillValueMap)
		{
			GetItemFromSkill(item3.Key).SetSkillValue(item3.Value.Item1, item3.Value.Item2);
		}
	}

	private void PopulateInitialValues()
	{
		foreach (SkillObject item in Skills.All)
		{
			int focus = _child.HeroDeveloper.GetFocus(item);
			if (_affectedSkillFocusMap.ContainsKey(item))
			{
				Tuple<int, int> tuple = _affectedSkillFocusMap[item];
				_affectedSkillFocusMap[item] = new Tuple<int, int>(tuple.Item1 + focus, 0);
			}
			else
			{
				_affectedSkillFocusMap.Add(item, new Tuple<int, int>(focus, 0));
			}
			int skillValue = _child.GetSkillValue(item);
			if (_affectedSkillValueMap.ContainsKey(item))
			{
				Tuple<int, int> tuple2 = _affectedSkillValueMap[item];
				_affectedSkillValueMap[item] = new Tuple<int, int>(tuple2.Item1 + skillValue, 0);
			}
			else
			{
				_affectedSkillValueMap.Add(item, new Tuple<int, int>(skillValue, 0));
			}
		}
		foreach (CharacterAttribute item2 in Attributes.All)
		{
			int attributeValue = _child.GetAttributeValue(item2);
			if (_affectedAttributesMap.ContainsKey(item2))
			{
				Tuple<int, int> tuple3 = _affectedAttributesMap[item2];
				_affectedAttributesMap[item2] = new Tuple<int, int>(tuple3.Item1 + attributeValue, 0);
			}
			else
			{
				_affectedAttributesMap.Add(item2, new Tuple<int, int>(attributeValue, 0));
			}
		}
	}

	private void PopulateGainedAttributeValues(List<string> selectedOptions, int currentPageIndex)
	{
		bool flag = currentPageIndex == _pageCount - 1;
		for (int i = 0; i < selectedOptions.Count; i++)
		{
			string optionKey = selectedOptions[i];
			_educationBehavior.GetOptionProperties(_child, optionKey, selectedOptions, out var _, out var _, out var _, out var attributes, out var skills, out var focusPoints, out var _);
			bool flag2 = i == currentPageIndex;
			if (attributes != null)
			{
				(CharacterAttribute, int)[] array = attributes;
				for (int j = 0; j < array.Length; j++)
				{
					(CharacterAttribute, int) tuple = array[j];
					Tuple<int, int> tuple2 = _affectedAttributesMap[tuple.Item1];
					int item = (flag2 ? tuple.Item2 : (flag ? (tuple2.Item2 + tuple.Item2) : 0));
					int item2 = (flag2 ? tuple2.Item1 : (flag ? tuple2.Item1 : (tuple2.Item1 + tuple.Item2)));
					_affectedAttributesMap[tuple.Item1] = new Tuple<int, int>(item2, item);
				}
			}
			if (skills != null)
			{
				(SkillObject, int)[] array2 = skills;
				for (int j = 0; j < array2.Length; j++)
				{
					(SkillObject, int) tuple3 = array2[j];
					Tuple<int, int> tuple4 = _affectedSkillValueMap[tuple3.Item1];
					int item3 = (flag2 ? tuple3.Item2 : (flag ? (tuple4.Item2 + tuple3.Item2) : 0));
					int item4 = (flag2 ? tuple4.Item1 : (flag ? tuple4.Item1 : (tuple4.Item1 + tuple3.Item2)));
					_affectedSkillValueMap[tuple3.Item1] = new Tuple<int, int>(item4, item3);
				}
			}
			if (focusPoints != null)
			{
				(SkillObject, int)[] array2 = focusPoints;
				for (int j = 0; j < array2.Length; j++)
				{
					(SkillObject, int) tuple5 = array2[j];
					Tuple<int, int> tuple6 = _affectedSkillFocusMap[tuple5.Item1];
					int val = (flag2 ? tuple5.Item2 : (flag ? (tuple6.Item2 + tuple5.Item2) : 0));
					int val2 = (flag2 ? tuple6.Item1 : (flag ? tuple6.Item1 : (tuple6.Item1 + tuple5.Item2)));
					val2 = Math.Min(val2, 5);
					val = Math.Min(val, 5 - val2);
					_affectedSkillFocusMap[tuple5.Item1] = new Tuple<int, int>(val2, val);
				}
			}
		}
	}

	private EducationGainedAttributeItemVM GetItemFromAttribute(CharacterAttribute attribute)
	{
		return GainGroups.SingleOrDefault((EducationGainGroupItemVM g) => g.AttributeObj == attribute)?.Attribute;
	}

	private EducationGainedSkillItemVM GetItemFromSkill(SkillObject skill)
	{
		foreach (EducationGainGroupItemVM gainGroup in GainGroups)
		{
			foreach (EducationGainedSkillItemVM skill2 in gainGroup.Skills)
			{
				if (skill2.SkillObj == skill)
				{
					return skill2;
				}
			}
		}
		foreach (EducationGainedSkillItemVM otherSkill in OtherSkills)
		{
			if (otherSkill.SkillObj == skill)
			{
				return otherSkill;
			}
		}
		return null;
	}
}
