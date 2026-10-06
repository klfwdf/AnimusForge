using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;

namespace NavalDLC.GameComponents;

public class NavalCharacterDevelopmentModel : DefaultCharacterDevelopmentModel
{
	public const int AdditionalFocusPointsAtStart = 6;

	public override int MaxAttribute => base.BaseModel.MaxAttribute;

	public override int MaxFocusPerSkill => base.BaseModel.MaxFocusPerSkill;

	public override int MaxSkillRequiredForEpicPerkBonus => base.BaseModel.MaxSkillRequiredForEpicPerkBonus;

	public override int MinSkillRequiredForEpicPerkBonus => base.BaseModel.MinSkillRequiredForEpicPerkBonus;

	public override int FocusPointsPerLevel => base.BaseModel.FocusPointsPerLevel;

	public override int FocusPointsAtStart => base.BaseModel.FocusPointsAtStart + 6;

	public override int AttributePointsAtStart => base.BaseModel.AttributePointsAtStart;

	public override int LevelsPerAttributePoint => base.BaseModel.LevelsPerAttributePoint;

	public override ExplainedNumber CalculateLearningLimit(IReadOnlyPropertyOwner<CharacterAttribute> characterAttributes, int focusValue, SkillObject skill, bool includeDescriptions = false)
	{
		return base.BaseModel.CalculateLearningLimit(characterAttributes, focusValue, skill, includeDescriptions);
	}

	public override ExplainedNumber CalculateLearningRate(IReadOnlyPropertyOwner<CharacterAttribute> characterAttributes, int focusValue, int skillValue, SkillObject skill, bool includeDescriptions = false)
	{
		return base.BaseModel.CalculateLearningRate(characterAttributes, focusValue, skillValue, skill, includeDescriptions);
	}

	public override int GetMaxSkillPoint()
	{
		return base.BaseModel.GetMaxSkillPoint();
	}

	public override CharacterAttribute GetNextAttributeToUpgrade(Hero hero)
	{
		return base.BaseModel.GetNextAttributeToUpgrade(hero);
	}

	public override PerkObject GetNextPerkToChoose(Hero hero, PerkObject perk)
	{
		return base.BaseModel.GetNextPerkToChoose(hero, perk);
	}

	public override SkillObject GetNextSkillToAddFocus(Hero hero)
	{
		return base.BaseModel.GetNextSkillToAddFocus(hero);
	}

	public override int GetSkillLevelChange(Hero hero, SkillObject skill, float skillXp)
	{
		return base.BaseModel.GetSkillLevelChange(hero, skill, skillXp);
	}

	public override void GetTraitLevelForTraitXp(Hero hero, TraitObject trait, int newValue, out int traitLevel, out int traitXp)
	{
		base.BaseModel.GetTraitLevelForTraitXp(hero, trait, newValue, out traitLevel, out traitXp);
	}

	public override int GetTraitXpRequiredForTraitLevel(TraitObject trait, int traitLevel)
	{
		return base.BaseModel.GetTraitXpRequiredForTraitLevel(trait, traitLevel);
	}

	public override int GetXpAmountForSkillLevelChange(Hero hero, SkillObject skill, int skillLevelChange)
	{
		return base.BaseModel.GetXpAmountForSkillLevelChange(hero, skill, skillLevelChange);
	}

	public override int GetXpRequiredForSkillLevel(int skillLevel)
	{
		return base.BaseModel.GetXpRequiredForSkillLevel(skillLevel);
	}

	public override int SkillsRequiredForLevel(int level)
	{
		return base.BaseModel.SkillsRequiredForLevel(level);
	}
}
