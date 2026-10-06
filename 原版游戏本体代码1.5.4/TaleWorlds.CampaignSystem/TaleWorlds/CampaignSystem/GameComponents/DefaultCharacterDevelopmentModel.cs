using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultCharacterDevelopmentModel : CharacterDevelopmentModel
{
	private const int MaxCharacterLevels = 62;

	private const int SkillPointsAtLevel1 = 1;

	private const int SkillPointsGainNeededInitialValue = 1000;

	private const int SkillPointsGainNeededIncreasePerLevel = 1000;

	private readonly int[] _skillsRequiredForLevel = new int[63];

	private const int FocusPointsPerLevelConst = 1;

	private const int LevelsPerAttributePointConst = 4;

	private const int FocusPointsAtStartConst = 5;

	private const int AttributePointsAtStartConst = 15;

	private const int MaxSkillLevels = 1024;

	private readonly int[] _xpRequiredForSkillLevel = new int[1024];

	private const int XpRequirementForFirstLevel = 30;

	private const int MaxSkillPoint = int.MaxValue;

	private const float BaseLearningRate = 1.25f;

	private const int TraitThreshold2 = 4000;

	private const int TraitMaxValue1 = 2500;

	private const int TraitThreshold1 = 1000;

	private const int TraitMaxValue2 = 6000;

	private const int SkillLevelVariant = 10;

	private static readonly TextObject _attributeEffectText = new TextObject("{=jlrvzwFb}Attribute Effect");

	private static readonly TextObject _skillFocusText = new TextObject("{=MRktqZwu}Skill Focus");

	private static readonly TextObject _overLimitText = new TextObject("{=bcA7ZuyO}Learning Limit Exceeded");

	public override int MaxFocusPerSkill => 5;

	public override int MaxAttribute => 10;

	public override int AttributePointsAtStart => 15;

	public override int LevelsPerAttributePoint => 4;

	public override int FocusPointsPerLevel => 1;

	public override int FocusPointsAtStart => 5;

	public override int MaxSkillRequiredForEpicPerkBonus => 250;

	public override int MinSkillRequiredForEpicPerkBonus => 200;

	public DefaultCharacterDevelopmentModel()
	{
		InitializeSkillsRequiredForLevel();
		InitializeXpRequiredForSkillLevel();
	}

	public void InitializeSkillsRequiredForLevel()
	{
		int num = 1000;
		int num2 = 1;
		_skillsRequiredForLevel[0] = 0;
		_skillsRequiredForLevel[1] = 1;
		for (int i = 2; i < _skillsRequiredForLevel.Length; i++)
		{
			num2 += num;
			_skillsRequiredForLevel[i] = num2;
			num += 1000 + num / 5;
		}
	}

	public void InitializeXpRequiredForSkillLevel()
	{
		int num = 30;
		_xpRequiredForSkillLevel[0] = num;
		for (int i = 1; i < 1024; i++)
		{
			num += 10 + i;
			_xpRequiredForSkillLevel[i] = _xpRequiredForSkillLevel[i - 1] + num;
		}
		if (Campaign.Current.Options.AccelerationMode == GameAccelerationMode.Fast)
		{
			for (int j = 0; j < _xpRequiredForSkillLevel.Length; j++)
			{
				_xpRequiredForSkillLevel[j] = (int)((float)_xpRequiredForSkillLevel[j] * 0.3f);
			}
		}
	}

	public override int SkillsRequiredForLevel(int level)
	{
		if (level > 62)
		{
			return Campaign.Current.Models.CharacterDevelopmentModel.GetMaxSkillPoint();
		}
		return _skillsRequiredForLevel[level];
	}

	public override int GetMaxSkillPoint()
	{
		return int.MaxValue;
	}

	public override int GetXpRequiredForSkillLevel(int skillLevel)
	{
		if (skillLevel > 1024)
		{
			skillLevel = 1024;
		}
		if (skillLevel <= 0)
		{
			return 0;
		}
		return _xpRequiredForSkillLevel[skillLevel - 1];
	}

	public override int GetSkillLevelChange(Hero hero, SkillObject skill, float skillXp)
	{
		CharacterDevelopmentModel characterDevelopmentModel = Campaign.Current.Models.CharacterDevelopmentModel;
		int num = 0;
		int skillValue = hero.GetSkillValue(skill);
		for (int i = 0; i < 1024 - skillValue; i++)
		{
			int num2 = skillValue + i;
			if (num2 < 1023)
			{
				if (!(skillXp >= (float)characterDevelopmentModel.GetXpRequiredForSkillLevel(num2 + 1)))
				{
					break;
				}
				num++;
			}
		}
		return num;
	}

	public override int GetXpAmountForSkillLevelChange(Hero hero, SkillObject skill, int skillLevelChange)
	{
		CharacterDevelopmentModel characterDevelopmentModel = Campaign.Current.Models.CharacterDevelopmentModel;
		int skillValue = hero.GetSkillValue(skill);
		return characterDevelopmentModel.GetXpRequiredForSkillLevel(skillValue + skillLevelChange + 1) - characterDevelopmentModel.GetXpRequiredForSkillLevel(skillValue + 1);
	}

	public override void GetTraitLevelForTraitXp(Hero hero, TraitObject trait, int xpValue, out int traitLevel, out int clampedTraitXp)
	{
		clampedTraitXp = xpValue;
		int num = ((trait.MinValue < -1) ? (-6000) : ((trait.MinValue == -1) ? (-2500) : 0));
		int num2 = ((trait.MaxValue > 1) ? 6000 : ((trait.MaxValue == 1) ? 2500 : 0));
		if (xpValue > num2)
		{
			clampedTraitXp = num2;
		}
		else if (xpValue < num)
		{
			clampedTraitXp = num;
		}
		traitLevel = ((clampedTraitXp <= -4000) ? (-2) : ((clampedTraitXp <= -1000) ? (-1) : ((clampedTraitXp >= 1000) ? ((clampedTraitXp < 4000) ? 1 : 2) : 0)));
		if (traitLevel < trait.MinValue)
		{
			traitLevel = trait.MinValue;
		}
		else if (traitLevel > trait.MaxValue)
		{
			traitLevel = trait.MaxValue;
		}
	}

	public override int GetTraitXpRequiredForTraitLevel(TraitObject trait, int traitLevel)
	{
		if (traitLevel >= -1)
		{
			return traitLevel switch
			{
				1 => 1000, 
				0 => 0, 
				-1 => -1000, 
				_ => 4000, 
			};
		}
		return -4000;
	}

	public override ExplainedNumber CalculateLearningLimit(IReadOnlyPropertyOwner<CharacterAttribute> characterAttributes, int focusValue, SkillObject skill, bool includeDescriptions = false)
	{
		float num = 0f;
		float num2 = 0f;
		ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions);
		CharacterAttribute[] attributes = skill.Attributes;
		foreach (CharacterAttribute attribute in attributes)
		{
			num2 += (float)characterAttributes.GetPropertyValue(attribute);
		}
		num = num2 / (float)skill.Attributes.Length;
		result.Add(Math.Max(0f, (num - 1f) * 10f), _attributeEffectText);
		result.Add(focusValue * 30, _skillFocusText);
		result.LimitMin(0f);
		return result;
	}

	public override ExplainedNumber CalculateLearningRate(IReadOnlyPropertyOwner<CharacterAttribute> characterAttributes, int focusValue, int skillValue, SkillObject skill, bool includeDescriptions = false)
	{
		ExplainedNumber result = new ExplainedNumber(1.25f, includeDescriptions);
		float num = 0f;
		float num2 = 0f;
		CharacterAttribute[] attributes = skill.Attributes;
		foreach (CharacterAttribute attribute in attributes)
		{
			num2 += (float)characterAttributes.GetPropertyValue(attribute);
		}
		num = num2 / (float)skill.Attributes.Length;
		result.AddFactor(0.4f * num, _attributeEffectText);
		int num3 = TaleWorlds.Library.MathF.Round(Campaign.Current.Models.CharacterDevelopmentModel.CalculateLearningLimit(characterAttributes, focusValue, skill).ResultNumber);
		result.AddFactor((float)focusValue * 1f, _skillFocusText);
		if (skillValue > num3)
		{
			int num4 = skillValue - num3;
			result.AddFactor(-1f - 0.1f * (float)num4, _overLimitText);
		}
		result.LimitMin(0f);
		return result;
	}

	public override SkillObject GetNextSkillToAddFocus(Hero hero)
	{
		SkillObject result = null;
		float num = float.MinValue;
		foreach (SkillObject item in Skills.All)
		{
			if (hero.HeroDeveloper.CanAddFocusToSkill(item))
			{
				int focus = hero.HeroDeveloper.GetFocus(item);
				float num2 = (float)hero.GetSkillValue(item) - Campaign.Current.Models.CharacterDevelopmentModel.CalculateLearningLimit(hero.CharacterAttributes, focus, item).ResultNumber;
				if (num2 > num)
				{
					num = num2;
					result = item;
				}
			}
		}
		return result;
	}

	public override CharacterAttribute GetNextAttributeToUpgrade(Hero hero)
	{
		CharacterAttribute result = null;
		float num = float.MinValue;
		foreach (CharacterAttribute currentAttribute in Attributes.All)
		{
			int attributeValue = hero.GetAttributeValue(currentAttribute);
			if (attributeValue >= Campaign.Current.Models.CharacterDevelopmentModel.MaxAttribute)
			{
				continue;
			}
			float num2 = 0f;
			if (attributeValue == 0)
			{
				num2 = float.MaxValue;
			}
			else
			{
				float num3 = 0f;
				List<SkillObject> list = Skills.All.Where((SkillObject skill) => skill.Attributes.Contains(currentAttribute)).ToList();
				foreach (SkillObject item in list)
				{
					num3 += TaleWorlds.Library.MathF.Max(0f, (float)(75 + hero.GetSkillValue(item)) - Campaign.Current.Models.CharacterDevelopmentModel.CalculateLearningLimit(hero.CharacterAttributes, hero.HeroDeveloper.GetFocus(item), item).ResultNumber);
				}
				num2 += num3 / (float)list.Count;
				int num4 = 1;
				foreach (CharacterAttribute item2 in Attributes.All)
				{
					if (item2 != currentAttribute)
					{
						int attributeValue2 = hero.GetAttributeValue(item2);
						if (num4 < attributeValue2)
						{
							num4 = attributeValue2;
						}
					}
				}
				float num5 = TaleWorlds.Library.MathF.Sqrt((float)num4 / (float)attributeValue);
				num2 *= num5;
			}
			if (num2 > num)
			{
				num = num2;
				result = currentAttribute;
			}
		}
		return result;
	}

	public override PerkObject GetNextPerkToChoose(Hero hero, PerkObject perk)
	{
		PerkObject result = perk;
		if (perk.AlternativePerk != null && MBRandom.RandomFloat < 0.5f)
		{
			result = perk.AlternativePerk;
		}
		return result;
	}
}
