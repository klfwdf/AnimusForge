using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000100 RID: 256
	public class DefaultCharacterDevelopmentModel : CharacterDevelopmentModel
	{
		// Token: 0x060016AB RID: 5803 RVA: 0x00067673 File Offset: 0x00065873
		public DefaultCharacterDevelopmentModel()
		{
			this.InitializeSkillsRequiredForLevel();
			this.InitializeXpRequiredForSkillLevel();
		}

		// Token: 0x060016AC RID: 5804 RVA: 0x000676A4 File Offset: 0x000658A4
		public void InitializeSkillsRequiredForLevel()
		{
			int num = 1000;
			int num2 = 1;
			this._skillsRequiredForLevel[0] = 0;
			this._skillsRequiredForLevel[1] = 1;
			for (int i = 2; i < this._skillsRequiredForLevel.Length; i++)
			{
				num2 += num;
				this._skillsRequiredForLevel[i] = num2;
				num += 1000 + num / 5;
			}
		}

		// Token: 0x060016AD RID: 5805 RVA: 0x000676F8 File Offset: 0x000658F8
		public void InitializeXpRequiredForSkillLevel()
		{
			int num = 30;
			this._xpRequiredForSkillLevel[0] = num;
			for (int i = 1; i < 1024; i++)
			{
				num += 10 + i;
				this._xpRequiredForSkillLevel[i] = this._xpRequiredForSkillLevel[i - 1] + num;
			}
			if (Campaign.Current.Options.AccelerationMode == GameAccelerationMode.Fast)
			{
				for (int j = 0; j < this._xpRequiredForSkillLevel.Length; j++)
				{
					this._xpRequiredForSkillLevel[j] = (int)((float)this._xpRequiredForSkillLevel[j] * 0.3f);
				}
			}
		}

		// Token: 0x17000630 RID: 1584
		// (get) Token: 0x060016AE RID: 5806 RVA: 0x00067779 File Offset: 0x00065979
		public override int MaxFocusPerSkill
		{
			get
			{
				return 5;
			}
		}

		// Token: 0x17000631 RID: 1585
		// (get) Token: 0x060016AF RID: 5807 RVA: 0x0006777C File Offset: 0x0006597C
		public override int MaxAttribute
		{
			get
			{
				return 10;
			}
		}

		// Token: 0x060016B0 RID: 5808 RVA: 0x00067780 File Offset: 0x00065980
		public override int SkillsRequiredForLevel(int level)
		{
			if (level > 62)
			{
				return Campaign.Current.Models.CharacterDevelopmentModel.GetMaxSkillPoint();
			}
			return this._skillsRequiredForLevel[level];
		}

		// Token: 0x060016B1 RID: 5809 RVA: 0x000677A4 File Offset: 0x000659A4
		public override int GetMaxSkillPoint()
		{
			return int.MaxValue;
		}

		// Token: 0x060016B2 RID: 5810 RVA: 0x000677AB File Offset: 0x000659AB
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
			return this._xpRequiredForSkillLevel[skillLevel - 1];
		}

		// Token: 0x060016B3 RID: 5811 RVA: 0x000677CC File Offset: 0x000659CC
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
					if (skillXp < (float)characterDevelopmentModel.GetXpRequiredForSkillLevel(num2 + 1))
					{
						break;
					}
					num++;
				}
			}
			return num;
		}

		// Token: 0x060016B4 RID: 5812 RVA: 0x00067828 File Offset: 0x00065A28
		public override int GetXpAmountForSkillLevelChange(Hero hero, SkillObject skill, int skillLevelChange)
		{
			CharacterDevelopmentModel characterDevelopmentModel = Campaign.Current.Models.CharacterDevelopmentModel;
			int skillValue = hero.GetSkillValue(skill);
			return characterDevelopmentModel.GetXpRequiredForSkillLevel(skillValue + skillLevelChange + 1) - characterDevelopmentModel.GetXpRequiredForSkillLevel(skillValue + 1);
		}

		// Token: 0x060016B5 RID: 5813 RVA: 0x00067864 File Offset: 0x00065A64
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
			traitLevel = ((clampedTraitXp <= -4000) ? (-2) : ((clampedTraitXp <= -1000) ? (-1) : ((clampedTraitXp < 1000) ? 0 : ((clampedTraitXp < 4000) ? 1 : 2))));
			if (traitLevel < trait.MinValue)
			{
				traitLevel = trait.MinValue;
				return;
			}
			if (traitLevel > trait.MaxValue)
			{
				traitLevel = trait.MaxValue;
			}
		}

		// Token: 0x060016B6 RID: 5814 RVA: 0x0006792D File Offset: 0x00065B2D
		public override int GetTraitXpRequiredForTraitLevel(TraitObject trait, int traitLevel)
		{
			if (traitLevel < -1)
			{
				return -4000;
			}
			if (traitLevel == -1)
			{
				return -1000;
			}
			if (traitLevel == 0)
			{
				return 0;
			}
			if (traitLevel != 1)
			{
				return 4000;
			}
			return 1000;
		}

		// Token: 0x17000632 RID: 1586
		// (get) Token: 0x060016B7 RID: 5815 RVA: 0x00067957 File Offset: 0x00065B57
		public override int AttributePointsAtStart
		{
			get
			{
				return 15;
			}
		}

		// Token: 0x17000633 RID: 1587
		// (get) Token: 0x060016B8 RID: 5816 RVA: 0x0006795B File Offset: 0x00065B5B
		public override int LevelsPerAttributePoint
		{
			get
			{
				return 4;
			}
		}

		// Token: 0x17000634 RID: 1588
		// (get) Token: 0x060016B9 RID: 5817 RVA: 0x0006795E File Offset: 0x00065B5E
		public override int FocusPointsPerLevel
		{
			get
			{
				return 1;
			}
		}

		// Token: 0x17000635 RID: 1589
		// (get) Token: 0x060016BA RID: 5818 RVA: 0x00067961 File Offset: 0x00065B61
		public override int FocusPointsAtStart
		{
			get
			{
				return 5;
			}
		}

		// Token: 0x17000636 RID: 1590
		// (get) Token: 0x060016BB RID: 5819 RVA: 0x00067964 File Offset: 0x00065B64
		public override int MaxSkillRequiredForEpicPerkBonus
		{
			get
			{
				return 250;
			}
		}

		// Token: 0x17000637 RID: 1591
		// (get) Token: 0x060016BC RID: 5820 RVA: 0x0006796B File Offset: 0x00065B6B
		public override int MinSkillRequiredForEpicPerkBonus
		{
			get
			{
				return 200;
			}
		}

		// Token: 0x060016BD RID: 5821 RVA: 0x00067974 File Offset: 0x00065B74
		public override ExplainedNumber CalculateLearningLimit(IReadOnlyPropertyOwner<CharacterAttribute> characterAttributes, int focusValue, SkillObject skill, bool includeDescriptions = false)
		{
			float num = 0f;
			ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions, null);
			foreach (CharacterAttribute attribute in skill.Attributes)
			{
				num += (float)characterAttributes.GetPropertyValue(attribute);
			}
			float num2 = num / (float)skill.Attributes.Length;
			result.Add(Math.Max(0f, (num2 - 1f) * 10f), DefaultCharacterDevelopmentModel._attributeEffectText, null);
			result.Add((float)(focusValue * 30), DefaultCharacterDevelopmentModel._skillFocusText, null);
			result.LimitMin(0f);
			return result;
		}

		// Token: 0x060016BE RID: 5822 RVA: 0x00067A18 File Offset: 0x00065C18
		public override ExplainedNumber CalculateLearningRate(IReadOnlyPropertyOwner<CharacterAttribute> characterAttributes, int focusValue, int skillValue, SkillObject skill, bool includeDescriptions = false)
		{
			ExplainedNumber result = new ExplainedNumber(1.25f, includeDescriptions, null);
			float num = 0f;
			foreach (CharacterAttribute attribute in skill.Attributes)
			{
				num += (float)characterAttributes.GetPropertyValue(attribute);
			}
			float num2 = num / (float)skill.Attributes.Length;
			result.AddFactor(0.4f * num2, DefaultCharacterDevelopmentModel._attributeEffectText);
			int num3 = MathF.Round(Campaign.Current.Models.CharacterDevelopmentModel.CalculateLearningLimit(characterAttributes, focusValue, skill, false).ResultNumber);
			result.AddFactor((float)focusValue * 1f, DefaultCharacterDevelopmentModel._skillFocusText);
			if (skillValue > num3)
			{
				int num4 = skillValue - num3;
				result.AddFactor(-1f - 0.1f * (float)num4, DefaultCharacterDevelopmentModel._overLimitText);
			}
			result.LimitMin(0f);
			return result;
		}

		// Token: 0x060016BF RID: 5823 RVA: 0x00067AFC File Offset: 0x00065CFC
		public override SkillObject GetNextSkillToAddFocus(Hero hero)
		{
			SkillObject result = null;
			float num = float.MinValue;
			foreach (SkillObject skillObject in Skills.All)
			{
				if (hero.HeroDeveloper.CanAddFocusToSkill(skillObject))
				{
					int focus = hero.HeroDeveloper.GetFocus(skillObject);
					float num2 = (float)hero.GetSkillValue(skillObject) - Campaign.Current.Models.CharacterDevelopmentModel.CalculateLearningLimit(hero.CharacterAttributes, focus, skillObject, false).ResultNumber;
					if (num2 > num)
					{
						num = num2;
						result = skillObject;
					}
				}
			}
			return result;
		}

		// Token: 0x060016C0 RID: 5824 RVA: 0x00067BA8 File Offset: 0x00065DA8
		public override CharacterAttribute GetNextAttributeToUpgrade(Hero hero)
		{
			CharacterAttribute result = null;
			float num = float.MinValue;
			using (List<CharacterAttribute>.Enumerator enumerator = Attributes.All.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					CharacterAttribute currentAttribute = enumerator.Current;
					int attributeValue = hero.GetAttributeValue(currentAttribute);
					if (attributeValue < Campaign.Current.Models.CharacterDevelopmentModel.MaxAttribute)
					{
						float num2 = 0f;
						if (attributeValue == 0)
						{
							num2 = float.MaxValue;
						}
						else
						{
							float num3 = 0f;
							List<SkillObject> list = (from skill in Skills.All
								where skill.Attributes.Contains(currentAttribute)
								select skill).ToList<SkillObject>();
							foreach (SkillObject skill2 in list)
							{
								num3 += MathF.Max(0f, (float)(75 + hero.GetSkillValue(skill2)) - Campaign.Current.Models.CharacterDevelopmentModel.CalculateLearningLimit(hero.CharacterAttributes, hero.HeroDeveloper.GetFocus(skill2), skill2, false).ResultNumber);
							}
							num2 += num3 / (float)list.Count;
							int num4 = 1;
							foreach (CharacterAttribute characterAttribute in Attributes.All)
							{
								if (characterAttribute != currentAttribute)
								{
									int attributeValue2 = hero.GetAttributeValue(characterAttribute);
									if (num4 < attributeValue2)
									{
										num4 = attributeValue2;
									}
								}
							}
							float num5 = MathF.Sqrt((float)num4 / (float)attributeValue);
							num2 *= num5;
						}
						if (num2 > num)
						{
							num = num2;
							result = currentAttribute;
						}
					}
				}
			}
			return result;
		}

		// Token: 0x060016C1 RID: 5825 RVA: 0x00067DB0 File Offset: 0x00065FB0
		public override PerkObject GetNextPerkToChoose(Hero hero, PerkObject perk)
		{
			PerkObject result = perk;
			if (perk.AlternativePerk != null && MBRandom.RandomFloat < 0.5f)
			{
				result = perk.AlternativePerk;
			}
			return result;
		}

		// Token: 0x04000773 RID: 1907
		private const int MaxCharacterLevels = 62;

		// Token: 0x04000774 RID: 1908
		private const int SkillPointsAtLevel1 = 1;

		// Token: 0x04000775 RID: 1909
		private const int SkillPointsGainNeededInitialValue = 1000;

		// Token: 0x04000776 RID: 1910
		private const int SkillPointsGainNeededIncreasePerLevel = 1000;

		// Token: 0x04000777 RID: 1911
		private readonly int[] _skillsRequiredForLevel = new int[63];

		// Token: 0x04000778 RID: 1912
		private const int FocusPointsPerLevelConst = 1;

		// Token: 0x04000779 RID: 1913
		private const int LevelsPerAttributePointConst = 4;

		// Token: 0x0400077A RID: 1914
		private const int FocusPointsAtStartConst = 5;

		// Token: 0x0400077B RID: 1915
		private const int AttributePointsAtStartConst = 15;

		// Token: 0x0400077C RID: 1916
		private const int MaxSkillLevels = 1024;

		// Token: 0x0400077D RID: 1917
		private readonly int[] _xpRequiredForSkillLevel = new int[1024];

		// Token: 0x0400077E RID: 1918
		private const int XpRequirementForFirstLevel = 30;

		// Token: 0x0400077F RID: 1919
		private const int MaxSkillPoint = 2147483647;

		// Token: 0x04000780 RID: 1920
		private const float BaseLearningRate = 1.25f;

		// Token: 0x04000781 RID: 1921
		private const int TraitThreshold2 = 4000;

		// Token: 0x04000782 RID: 1922
		private const int TraitMaxValue1 = 2500;

		// Token: 0x04000783 RID: 1923
		private const int TraitThreshold1 = 1000;

		// Token: 0x04000784 RID: 1924
		private const int TraitMaxValue2 = 6000;

		// Token: 0x04000785 RID: 1925
		private const int SkillLevelVariant = 10;

		// Token: 0x04000786 RID: 1926
		private static readonly TextObject _attributeEffectText = new TextObject("{=jlrvzwFb}Attribute Effect", null);

		// Token: 0x04000787 RID: 1927
		private static readonly TextObject _skillFocusText = new TextObject("{=MRktqZwu}Skill Focus", null);

		// Token: 0x04000788 RID: 1928
		private static readonly TextObject _overLimitText = new TextObject("{=bcA7ZuyO}Learning Limit Exceeded", null);
	}
}
