using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;

public class CharacterAttributeItemVM : ViewModel
{
	private readonly Hero _hero;

	private readonly HeroDeveloper _developer;

	private readonly int _initialAttValue;

	private readonly Action<CharacterAttributeItemVM> _onInpectAttribute;

	private readonly Action<CharacterAttributeItemVM> _onAddAttributePoint;

	private readonly CharacterDeveloperHeroItemVM _characterVM;

	private int _atttributeValue;

	private int _unspentAttributePoints;

	private string _unspentAttributePointsText;

	private bool _canAddPoint;

	private bool _isInspecting;

	private bool _isAttributeAtMax;

	private string _name;

	private string _nameExtended;

	private string _description;

	private string _increaseHelpText;

	private MBBindingList<AttributeBoundSkillItemVM> _boundSkills;

	public CharacterAttribute AttributeType { get; private set; }

	[DataSourceProperty]
	public MBBindingList<AttributeBoundSkillItemVM> BoundSkills
	{
		get
		{
			return _boundSkills;
		}
		set
		{
			if (value != _boundSkills)
			{
				_boundSkills = value;
				OnPropertyChangedWithValue(value, "BoundSkills");
			}
		}
	}

	[DataSourceProperty]
	public int AttributeValue
	{
		get
		{
			return _atttributeValue;
		}
		set
		{
			if (value != _atttributeValue)
			{
				_atttributeValue = value;
				OnPropertyChangedWithValue(value, "AttributeValue");
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
				GameTexts.SetVariable("NUMBER", value);
				UnspentAttributePointsText = GameTexts.FindText("str_free_attribute_points").ToString();
			}
		}
	}

	[DataSourceProperty]
	public string UnspentAttributePointsText
	{
		get
		{
			return _unspentAttributePointsText;
		}
		set
		{
			if (value != _unspentAttributePointsText)
			{
				_unspentAttributePointsText = value;
				OnPropertyChangedWithValue(value, "UnspentAttributePointsText");
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
	public string NameExtended
	{
		get
		{
			return _nameExtended;
		}
		set
		{
			if (value != _nameExtended)
			{
				_nameExtended = value;
				OnPropertyChangedWithValue(value, "NameExtended");
			}
		}
	}

	[DataSourceProperty]
	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			if (value != _description)
			{
				_description = value;
				OnPropertyChangedWithValue(value, "Description");
			}
		}
	}

	[DataSourceProperty]
	public string IncreaseHelpText
	{
		get
		{
			return _increaseHelpText;
		}
		set
		{
			if (value != _increaseHelpText)
			{
				_increaseHelpText = value;
				OnPropertyChangedWithValue(value, "IncreaseHelpText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInspecting
	{
		get
		{
			return _isInspecting;
		}
		set
		{
			if (value != _isInspecting)
			{
				_isInspecting = value;
				OnPropertyChangedWithValue(value, "IsInspecting");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAttributeAtMax
	{
		get
		{
			return _isAttributeAtMax;
		}
		set
		{
			if (value != _isAttributeAtMax)
			{
				_isAttributeAtMax = value;
				OnPropertyChangedWithValue(value, "IsAttributeAtMax");
			}
		}
	}

	[DataSourceProperty]
	public bool CanAddPoint
	{
		get
		{
			return _canAddPoint;
		}
		set
		{
			if (value != _canAddPoint)
			{
				_canAddPoint = value;
				OnPropertyChangedWithValue(value, "CanAddPoint");
			}
		}
	}

	public CharacterAttributeItemVM(Hero hero, CharacterAttribute currAtt, CharacterDeveloperHeroItemVM developerVM, Action<CharacterAttributeItemVM> onInpectAttribute, Action<CharacterAttributeItemVM> onAddAttributePoint)
	{
		_hero = hero;
		_developer = _hero.HeroDeveloper;
		_characterVM = developerVM;
		AttributeType = currAtt;
		_onInpectAttribute = onInpectAttribute;
		_onAddAttributePoint = onAddAttributePoint;
		_initialAttValue = _characterVM.CharacterAttributes.GetPropertyValue(currAtt);
		AttributeValue = _initialAttValue;
		BoundSkills = new MBBindingList<AttributeBoundSkillItemVM>();
		RefreshWithCurrentValues();
		RefreshValues();
		UnspentAttributePoints = _characterVM.UnspentAttributePoints;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = AttributeType.Abbreviation.ToString();
		string content = AttributeType.Description.ToString();
		GameTexts.SetVariable("STR1", content);
		GameTexts.SetVariable("ATTRIBUTE_NAME", AttributeType.Name);
		TextObject textObject = GameTexts.FindText("str_skill_attribute_bound_skills");
		textObject.SetTextVariable("IS_SOCIAL", (AttributeType == DefaultCharacterAttributes.Social) ? 1 : 0);
		GameTexts.SetVariable("STR2", textObject);
		Description = GameTexts.FindText("str_STR1_space_STR2").ToString();
		TextObject textObject2 = GameTexts.FindText("str_skill_attribute_increase_description");
		textObject2.SetTextVariable("IS_SOCIAL", (AttributeType == DefaultCharacterAttributes.Social) ? 1 : 0);
		GameTexts.SetVariable("NUMBER", UnspentAttributePoints);
		UnspentAttributePointsText = GameTexts.FindText("str_free_attribute_points").ToString();
		IncreaseHelpText = textObject2.ToString();
		BoundSkills.Clear();
		List<SkillObject> list = Skills.All.ToList();
		list.Sort(CampaignUIHelper.SkillObjectComparerInstance);
		foreach (SkillObject skill in list)
		{
			if (skill.Attributes.Contains(AttributeType) && !BoundSkills.Any((AttributeBoundSkillItemVM s) => s.SkillId == skill.StringId))
			{
				BoundSkills.Add(new AttributeBoundSkillItemVM(skill));
			}
		}
	}

	public void ExecuteInspectAttribute()
	{
		_onInpectAttribute?.Invoke(this);
	}

	public void ExecuteAddAttributePoint()
	{
		_onAddAttributePoint?.Invoke(this);
		UnspentAttributePoints = _characterVM.UnspentAttributePoints;
		RefreshWithCurrentValues();
	}

	public void Reset()
	{
		RefreshWithCurrentValues();
	}

	public void RefreshWithCurrentValues()
	{
		UnspentAttributePoints = _characterVM.UnspentAttributePoints;
		AttributeValue = _characterVM.CharacterAttributes.GetPropertyValue(AttributeType);
		CanAddPoint = AttributeValue < Campaign.Current.Models.CharacterDevelopmentModel.MaxAttribute && _characterVM.UnspentAttributePoints > 0;
		IsAttributeAtMax = AttributeValue >= Campaign.Current.Models.CharacterDevelopmentModel.MaxAttribute;
	}

	public void Commit()
	{
		for (int i = 0; i < AttributeValue - _initialAttValue; i++)
		{
			_developer.AddAttribute(AttributeType, 1);
		}
	}
}
