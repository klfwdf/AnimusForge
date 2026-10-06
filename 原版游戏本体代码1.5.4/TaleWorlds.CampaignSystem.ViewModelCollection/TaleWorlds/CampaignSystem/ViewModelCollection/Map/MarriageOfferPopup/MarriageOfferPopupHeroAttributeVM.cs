using System.Linq;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MarriageOfferPopup;

public class MarriageOfferPopupHeroAttributeVM : ViewModel
{
	private readonly Hero _hero;

	private readonly CharacterAttribute _attribute;

	private string _attributeText;

	private MBBindingList<EncyclopediaSkillVM> _attributeSkills;

	[DataSourceProperty]
	public string AttributeText
	{
		get
		{
			return _attributeText;
		}
		set
		{
			if (value != _attributeText)
			{
				_attributeText = value;
				OnPropertyChangedWithValue(value, "AttributeText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EncyclopediaSkillVM> AttributeSkills
	{
		get
		{
			return _attributeSkills;
		}
		set
		{
			if (value != _attributeSkills)
			{
				_attributeSkills = value;
				OnPropertyChangedWithValue(value, "AttributeSkills");
			}
		}
	}

	public MarriageOfferPopupHeroAttributeVM(Hero hero, CharacterAttribute attribute)
	{
		_hero = hero;
		_attribute = attribute;
		FillSkillsList();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TextObject textObject = GameTexts.FindText("str_STR1_space_STR2");
		textObject.SetTextVariable("STR1", _attribute.Name);
		TextObject textObject2 = GameTexts.FindText("str_STR_in_parentheses");
		textObject2.SetTextVariable("STR", _hero.GetAttributeValue(_attribute));
		textObject.SetTextVariable("STR2", textObject2);
		_attributeText = textObject.ToString();
	}

	private void FillSkillsList()
	{
		_attributeSkills = new MBBindingList<EncyclopediaSkillVM>();
		foreach (SkillObject skill in Skills.All)
		{
			if (!CampaignUIHelper.GetIsNavalSkill(skill) && skill.Attributes.FirstOrDefault() == _attribute && !_attributeSkills.Any((EncyclopediaSkillVM s) => s.SkillId == skill.StringId))
			{
				_attributeSkills.Add(new EncyclopediaSkillVM(skill, _hero.GetSkillValue(skill)));
			}
		}
	}
}
