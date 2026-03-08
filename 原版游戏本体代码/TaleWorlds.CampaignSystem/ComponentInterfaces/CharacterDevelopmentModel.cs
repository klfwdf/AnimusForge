using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x02000187 RID: 391
	public abstract class CharacterDevelopmentModel : MBGameModel<CharacterDevelopmentModel>
	{
		// Token: 0x06001BBF RID: 7103
		public abstract int SkillsRequiredForLevel(int level);

		// Token: 0x06001BC0 RID: 7104
		public abstract int GetMaxSkillPoint();

		// Token: 0x06001BC1 RID: 7105
		public abstract int GetXpRequiredForSkillLevel(int skillLevel);

		// Token: 0x06001BC2 RID: 7106
		public abstract int GetSkillLevelChange(Hero hero, SkillObject skill, float skillXp);

		// Token: 0x06001BC3 RID: 7107
		public abstract int GetXpAmountForSkillLevelChange(Hero hero, SkillObject skill, int skillLevelChange);

		// Token: 0x170006F9 RID: 1785
		// (get) Token: 0x06001BC4 RID: 7108
		public abstract int MaxAttribute { get; }

		// Token: 0x170006FA RID: 1786
		// (get) Token: 0x06001BC5 RID: 7109
		public abstract int MaxFocusPerSkill { get; }

		// Token: 0x170006FB RID: 1787
		// (get) Token: 0x06001BC6 RID: 7110
		public abstract int MaxSkillRequiredForEpicPerkBonus { get; }

		// Token: 0x170006FC RID: 1788
		// (get) Token: 0x06001BC7 RID: 7111
		public abstract int MinSkillRequiredForEpicPerkBonus { get; }

		// Token: 0x06001BC8 RID: 7112
		public abstract void GetTraitLevelForTraitXp(Hero hero, TraitObject trait, int newValue, out int traitLevel, out int traitXp);

		// Token: 0x06001BC9 RID: 7113
		public abstract int GetTraitXpRequiredForTraitLevel(TraitObject trait, int traitLevel);

		// Token: 0x170006FD RID: 1789
		// (get) Token: 0x06001BCA RID: 7114
		public abstract int FocusPointsPerLevel { get; }

		// Token: 0x170006FE RID: 1790
		// (get) Token: 0x06001BCB RID: 7115
		public abstract int FocusPointsAtStart { get; }

		// Token: 0x170006FF RID: 1791
		// (get) Token: 0x06001BCC RID: 7116
		public abstract int AttributePointsAtStart { get; }

		// Token: 0x17000700 RID: 1792
		// (get) Token: 0x06001BCD RID: 7117
		public abstract int LevelsPerAttributePoint { get; }

		// Token: 0x06001BCE RID: 7118
		public abstract ExplainedNumber CalculateLearningLimit(IReadOnlyPropertyOwner<CharacterAttribute> characterAttributes, int focusValue, SkillObject skill, bool includeDescriptions = false);

		// Token: 0x06001BCF RID: 7119
		public abstract ExplainedNumber CalculateLearningRate(IReadOnlyPropertyOwner<CharacterAttribute> characterAttributes, int focusValue, int skillValue, SkillObject skill, bool includeDescriptions = false);

		// Token: 0x06001BD0 RID: 7120
		public abstract SkillObject GetNextSkillToAddFocus(Hero hero);

		// Token: 0x06001BD1 RID: 7121
		public abstract CharacterAttribute GetNextAttributeToUpgrade(Hero hero);

		// Token: 0x06001BD2 RID: 7122
		public abstract PerkObject GetNextPerkToChoose(Hero hero, PerkObject perk);
	}
}
