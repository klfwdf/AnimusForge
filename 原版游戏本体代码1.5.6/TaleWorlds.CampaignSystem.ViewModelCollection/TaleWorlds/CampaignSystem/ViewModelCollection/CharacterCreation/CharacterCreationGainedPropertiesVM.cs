using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;

public class CharacterCreationGainedPropertiesVM : ViewModel
{
	private readonly CharacterCreationManager _characterCreationManager;

	private readonly Dictionary<CharacterAttribute, Tuple<int, int>> _affectedAttributesMap;

	private readonly Dictionary<SkillObject, Tuple<int, int>> _affectedSkillMap;

	private MBBindingList<CharacterCreationGainGroupItemVM> _gainGroups;

	private MBBindingList<EncyclopediaTraitItemVM> _gainedTraits;

	private MBBindingList<CharacterCreationGainedSkillItemVM> _otherSkills;

	[DataSourceProperty]
	public MBBindingList<CharacterCreationGainGroupItemVM> GainGroups
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
	public MBBindingList<EncyclopediaTraitItemVM> GainedTraits
	{
		get
		{
			return _gainedTraits;
		}
		set
		{
			if (value != _gainedTraits)
			{
				_gainedTraits = value;
				OnPropertyChangedWithValue(value, "GainedTraits");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CharacterCreationGainedSkillItemVM> OtherSkills
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

	public CharacterCreationGainedPropertiesVM(CharacterCreationManager characterCreationManager)
	{
		_characterCreationManager = characterCreationManager;
		_affectedAttributesMap = new Dictionary<CharacterAttribute, Tuple<int, int>>();
		_affectedSkillMap = new Dictionary<SkillObject, Tuple<int, int>>();
		GainGroups = new MBBindingList<CharacterCreationGainGroupItemVM>();
		OtherSkills = new MBBindingList<CharacterCreationGainedSkillItemVM>();
		List<CharacterAttribute> list = Attributes.All.ToList();
		list.Sort(CampaignUIHelper.CharacterAttributeComparerInstance);
		foreach (CharacterAttribute item in list)
		{
			GainGroups.Add(new CharacterCreationGainGroupItemVM(item));
		}
		List<SkillObject> list2 = Skills.All.ToList();
		list2.Sort(CampaignUIHelper.SkillObjectComparerInstance);
		foreach (SkillObject skill in list2)
		{
			if (!GainGroups.Any((CharacterCreationGainGroupItemVM attribute) => attribute.Skills.Any((CharacterCreationGainedSkillItemVM attributeSkill) => attributeSkill.SkillId == skill.StringId)))
			{
				OtherSkills.Add(new CharacterCreationGainedSkillItemVM(skill));
			}
		}
		GainedTraits = new MBBindingList<EncyclopediaTraitItemVM>();
		UpdateValues();
	}

	public void UpdateValues()
	{
		_affectedAttributesMap.Clear();
		_affectedSkillMap.Clear();
		GainGroups.ApplyActionOnAllItems(delegate(CharacterCreationGainGroupItemVM g)
		{
			g.ResetValues();
		});
		OtherSkills.ApplyActionOnAllItems(delegate(CharacterCreationGainedSkillItemVM s)
		{
			s.ResetValues();
		});
		PopulateInitialValues();
		PopulateGainedAttributeValues();
		PopulateGainedTraitValues();
		foreach (KeyValuePair<CharacterAttribute, Tuple<int, int>> item in _affectedAttributesMap)
		{
			GetItemFromAttribute(item.Key).SetValue(item.Value.Item1, item.Value.Item2);
		}
		foreach (KeyValuePair<SkillObject, Tuple<int, int>> item2 in _affectedSkillMap)
		{
			GetItemFromSkill(item2.Key).SetValue(item2.Value.Item1, item2.Value.Item2);
		}
	}

	private void PopulateInitialValues()
	{
		foreach (SkillObject item in Skills.All)
		{
			int focus = Hero.MainHero.HeroDeveloper.GetFocus(item);
			if (_affectedSkillMap.ContainsKey(item))
			{
				Tuple<int, int> tuple = _affectedSkillMap[item];
				_affectedSkillMap[item] = new Tuple<int, int>(tuple.Item1 + focus, 0);
			}
			else
			{
				_affectedSkillMap.Add(item, new Tuple<int, int>(focus, 0));
			}
		}
		foreach (CharacterAttribute item2 in Attributes.All)
		{
			int attributeValue = Hero.MainHero.GetAttributeValue(item2);
			if (_affectedAttributesMap.ContainsKey(item2))
			{
				Tuple<int, int> tuple2 = _affectedAttributesMap[item2];
				_affectedAttributesMap[item2] = new Tuple<int, int>(tuple2.Item1 + attributeValue, 0);
			}
			else
			{
				_affectedAttributesMap.Add(item2, new Tuple<int, int>(attributeValue, 0));
			}
		}
	}

	private void PopulateGainedAttributeValues()
	{
		foreach (KeyValuePair<NarrativeMenu, NarrativeMenuOption> selectedOption in _characterCreationManager.SelectedOptions)
		{
			NarrativeMenu key = selectedOption.Key;
			NarrativeMenuOption value = selectedOption.Value;
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			if (key == _characterCreationManager.CurrentMenu)
			{
				num = value.Args.AttributeLevelToAdd;
			}
			else
			{
				num2 += value.Args.AttributeLevelToAdd;
			}
			if (value.Args.EffectedAttribute != null)
			{
				if (_affectedAttributesMap.ContainsKey(value.Args.EffectedAttribute))
				{
					Tuple<int, int> tuple = _affectedAttributesMap[value.Args.EffectedAttribute];
					_affectedAttributesMap[value.Args.EffectedAttribute] = new Tuple<int, int>(tuple.Item1 + num2, tuple.Item2 + num);
				}
				else
				{
					_affectedAttributesMap.Add(value.Args.EffectedAttribute, new Tuple<int, int>(num2, num));
				}
			}
			if (key == _characterCreationManager.CurrentMenu)
			{
				num3 = value.Args.FocusToAdd;
			}
			else
			{
				num4 += value.Args.FocusToAdd;
			}
			foreach (SkillObject affectedSkill in value.Args.AffectedSkills)
			{
				if (_affectedSkillMap.ContainsKey(affectedSkill))
				{
					Tuple<int, int> tuple2 = _affectedSkillMap[affectedSkill];
					_affectedSkillMap[affectedSkill] = new Tuple<int, int>(tuple2.Item1 + num4, tuple2.Item2 + num3);
				}
				else
				{
					_affectedSkillMap.Add(affectedSkill, new Tuple<int, int>(num4, num3));
				}
			}
		}
	}

	private void PopulateGainedTraitValues()
	{
		GainedTraits.Clear();
		foreach (KeyValuePair<NarrativeMenu, NarrativeMenuOption> selectedOption in _characterCreationManager.SelectedOptions)
		{
			NarrativeMenuOption value = selectedOption.Value;
			if (value.Args.AffectedTraits == null || value.Args.AffectedTraits.Count <= 0)
			{
				continue;
			}
			foreach (TraitObject effectedTrait in value.Args.AffectedTraits)
			{
				if (GainedTraits.FirstOrDefault((EncyclopediaTraitItemVM t) => t.TraitId == effectedTrait.StringId) == null)
				{
					GainedTraits.Add(new EncyclopediaTraitItemVM(effectedTrait, 1));
				}
			}
		}
	}

	private CharacterCreationGainedAttributeItemVM GetItemFromAttribute(CharacterAttribute attribute)
	{
		return GainGroups.SingleOrDefault((CharacterCreationGainGroupItemVM g) => g.AttributeObj == attribute)?.Attribute;
	}

	private CharacterCreationGainedSkillItemVM GetItemFromSkill(SkillObject skill)
	{
		foreach (CharacterCreationGainGroupItemVM gainGroup in GainGroups)
		{
			foreach (CharacterCreationGainedSkillItemVM skill2 in gainGroup.Skills)
			{
				if (skill2.SkillObj == skill)
				{
					return skill2;
				}
			}
		}
		foreach (CharacterCreationGainedSkillItemVM otherSkill in OtherSkills)
		{
			if (otherSkill.SkillObj == skill)
			{
				return otherSkill;
			}
		}
		return null;
	}
}
