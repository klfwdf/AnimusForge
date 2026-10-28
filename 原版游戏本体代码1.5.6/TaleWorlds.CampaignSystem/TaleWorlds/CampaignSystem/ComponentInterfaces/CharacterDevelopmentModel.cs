using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class CharacterDevelopmentModel : MBGameModel<CharacterDevelopmentModel>
{
	public abstract int MaxAttribute { get; }

	public abstract int MaxFocusPerSkill { get; }

	public abstract int MaxSkillRequiredForEpicPerkBonus { get; }

	public abstract int MinSkillRequiredForEpicPerkBonus { get; }

	public abstract int FocusPointsPerLevel { get; }

	public abstract int FocusPointsAtStart { get; }

	public abstract int AttributePointsAtStart { get; }

	public abstract int LevelsPerAttributePoint { get; }

	public abstract int SkillsRequiredForLevel(int level);

	public abstract int GetMaxSkillPoint();

	public abstract int GetXpRequiredForSkillLevel(int skillLevel);

	public abstract int GetSkillLevelChange(Hero hero, SkillObject skill, float skillXp);

	public abstract int GetXpAmountForSkillLevelChange(Hero hero, SkillObject skill, int skillLevelChange);

	public abstract void GetTraitLevelForTraitXp(Hero hero, TraitObject trait, int newValue, out int traitLevel, out int traitXp);

	public abstract int GetTraitXpRequiredForTraitLevel(TraitObject trait, int traitLevel);

	public abstract ExplainedNumber CalculateLearningLimit(IReadOnlyPropertyOwner<CharacterAttribute> characterAttributes, int focusValue, SkillObject skill, bool includeDescriptions = false);

	public abstract ExplainedNumber CalculateLearningRate(IReadOnlyPropertyOwner<CharacterAttribute> characterAttributes, int focusValue, int skillValue, SkillObject skill, bool includeDescriptions = false);

	public abstract SkillObject GetNextSkillToAddFocus(Hero hero);

	public abstract CharacterAttribute GetNextAttributeToUpgrade(Hero hero);

	public abstract PerkObject GetNextPerkToChoose(Hero hero, PerkObject perk);
}
