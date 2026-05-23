using System;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CharacterCreationContent
{
	// Token: 0x02000216 RID: 534
	public class NarrativeMenuOptionArgs
	{
		// Token: 0x170007E6 RID: 2022
		// (get) Token: 0x06002005 RID: 8197 RVA: 0x0008EAA6 File Offset: 0x0008CCA6
		// (set) Token: 0x06002006 RID: 8198 RVA: 0x0008EAAE File Offset: 0x0008CCAE
		public MBList<SkillObject> AffectedSkills { get; private set; }

		// Token: 0x170007E7 RID: 2023
		// (get) Token: 0x06002007 RID: 8199 RVA: 0x0008EAB7 File Offset: 0x0008CCB7
		// (set) Token: 0x06002008 RID: 8200 RVA: 0x0008EABF File Offset: 0x0008CCBF
		public int SkillLevelToAdd { get; private set; }

		// Token: 0x170007E8 RID: 2024
		// (get) Token: 0x06002009 RID: 8201 RVA: 0x0008EAC8 File Offset: 0x0008CCC8
		// (set) Token: 0x0600200A RID: 8202 RVA: 0x0008EAD0 File Offset: 0x0008CCD0
		public MBList<TraitObject> AffectedTraits { get; private set; }

		// Token: 0x170007E9 RID: 2025
		// (get) Token: 0x0600200B RID: 8203 RVA: 0x0008EAD9 File Offset: 0x0008CCD9
		// (set) Token: 0x0600200C RID: 8204 RVA: 0x0008EAE1 File Offset: 0x0008CCE1
		public int TraitLevelToAdd { get; private set; }

		// Token: 0x170007EA RID: 2026
		// (get) Token: 0x0600200D RID: 8205 RVA: 0x0008EAEA File Offset: 0x0008CCEA
		// (set) Token: 0x0600200E RID: 8206 RVA: 0x0008EAF2 File Offset: 0x0008CCF2
		public int FocusToAdd { get; private set; }

		// Token: 0x170007EB RID: 2027
		// (get) Token: 0x0600200F RID: 8207 RVA: 0x0008EAFB File Offset: 0x0008CCFB
		// (set) Token: 0x06002010 RID: 8208 RVA: 0x0008EB03 File Offset: 0x0008CD03
		public int UnspentFocusToAdd { get; private set; }

		// Token: 0x170007EC RID: 2028
		// (get) Token: 0x06002011 RID: 8209 RVA: 0x0008EB0C File Offset: 0x0008CD0C
		// (set) Token: 0x06002012 RID: 8210 RVA: 0x0008EB14 File Offset: 0x0008CD14
		public CharacterAttribute EffectedAttribute { get; private set; }

		// Token: 0x170007ED RID: 2029
		// (get) Token: 0x06002013 RID: 8211 RVA: 0x0008EB1D File Offset: 0x0008CD1D
		// (set) Token: 0x06002014 RID: 8212 RVA: 0x0008EB25 File Offset: 0x0008CD25
		public int AttributeLevelToAdd { get; private set; }

		// Token: 0x170007EE RID: 2030
		// (get) Token: 0x06002015 RID: 8213 RVA: 0x0008EB2E File Offset: 0x0008CD2E
		// (set) Token: 0x06002016 RID: 8214 RVA: 0x0008EB36 File Offset: 0x0008CD36
		public int UnspentAttributeToAdd { get; private set; }

		// Token: 0x170007EF RID: 2031
		// (get) Token: 0x06002017 RID: 8215 RVA: 0x0008EB3F File Offset: 0x0008CD3F
		// (set) Token: 0x06002018 RID: 8216 RVA: 0x0008EB47 File Offset: 0x0008CD47
		public int RenownToAdd { get; private set; }

		// Token: 0x170007F0 RID: 2032
		// (get) Token: 0x06002019 RID: 8217 RVA: 0x0008EB50 File Offset: 0x0008CD50
		// (set) Token: 0x0600201A RID: 8218 RVA: 0x0008EB58 File Offset: 0x0008CD58
		public int GoldToAdd { get; private set; }

		// Token: 0x170007F1 RID: 2033
		// (get) Token: 0x0600201B RID: 8219 RVA: 0x0008EB64 File Offset: 0x0008CD64
		public TextObject PositiveEffectText
		{
			get
			{
				return this.GetPositiveEffectText(this.AffectedSkills.ToMBList<SkillObject>(), this.EffectedAttribute, this.FocusToAdd, this.SkillLevelToAdd, this.AttributeLevelToAdd, this.AffectedTraits.ToMBList<TraitObject>(), this.TraitLevelToAdd, this.RenownToAdd, this.GoldToAdd, this.UnspentFocusToAdd, this.UnspentAttributeToAdd);
			}
		}

		// Token: 0x0600201C RID: 8220 RVA: 0x0008EBC3 File Offset: 0x0008CDC3
		public NarrativeMenuOptionArgs()
		{
			this.AffectedSkills = new MBList<SkillObject>();
			this.AffectedTraits = new MBList<TraitObject>();
		}

		// Token: 0x0600201D RID: 8221 RVA: 0x0008EBE1 File Offset: 0x0008CDE1
		public void SetAffectedSkills(SkillObject[] affectedSkills)
		{
			this.AffectedSkills = affectedSkills.ToMBList<SkillObject>();
		}

		// Token: 0x0600201E RID: 8222 RVA: 0x0008EBEF File Offset: 0x0008CDEF
		public void SetFocusToSkills(int focusToAdd)
		{
			this.FocusToAdd = focusToAdd;
		}

		// Token: 0x0600201F RID: 8223 RVA: 0x0008EBF8 File Offset: 0x0008CDF8
		public void SetLevelToSkills(int levelToAdd)
		{
			this.SkillLevelToAdd = levelToAdd;
		}

		// Token: 0x06002020 RID: 8224 RVA: 0x0008EC01 File Offset: 0x0008CE01
		public void SetAffectedTraits(TraitObject[] affectedTraits)
		{
			this.AffectedTraits = affectedTraits.ToMBList<TraitObject>();
		}

		// Token: 0x06002021 RID: 8225 RVA: 0x0008EC0F File Offset: 0x0008CE0F
		public void SetLevelToTraits(int levelToAdd)
		{
			this.TraitLevelToAdd = levelToAdd;
		}

		// Token: 0x06002022 RID: 8226 RVA: 0x0008EC18 File Offset: 0x0008CE18
		public void SetLevelToAttribute(CharacterAttribute characterAttribute, int levelToAdd)
		{
			this.EffectedAttribute = characterAttribute;
			this.AttributeLevelToAdd = levelToAdd;
		}

		// Token: 0x06002023 RID: 8227 RVA: 0x0008EC28 File Offset: 0x0008CE28
		public void SetRenownToAdd(int value)
		{
			this.RenownToAdd = value;
		}

		// Token: 0x06002024 RID: 8228 RVA: 0x0008EC31 File Offset: 0x0008CE31
		public void SetUnspentFocusToAdd(int value)
		{
			this.UnspentFocusToAdd = value;
		}

		// Token: 0x06002025 RID: 8229 RVA: 0x0008EC3A File Offset: 0x0008CE3A
		public void SetUnspentAttributeToAdd(int value)
		{
			this.UnspentAttributeToAdd = value;
		}

		// Token: 0x06002026 RID: 8230 RVA: 0x0008EC44 File Offset: 0x0008CE44
		private TextObject GetPositiveEffectText(MBList<SkillObject> skills, CharacterAttribute attribute, int focusToAdd = 0, int skillLevelToAdd = 0, int attributeLevelToAdd = 0, MBList<TraitObject> traits = null, int traitLevelToAdd = 0, int renownToAdd = 0, int goldToAdd = 0, int unspentFocustoAdd = 0, int unspentAttributeToAdd = 0)
		{
			TextObject textObject;
			if (skills.Count == 3)
			{
				textObject = new TextObject("{=jeWV2uV3}{EXP_VALUE} Skill {?IS_PLURAL_SKILL}Levels{?}Level{\\?} and {FOCUS_VALUE} Focus {?IS_PLURAL_FOCUS}Points{?}Point{\\?} to {SKILL_ONE}, {SKILL_TWO} and {SKILL_THREE}{NEWLINE}{ATTR_VALUE} Attribute {?IS_PLURAL_ATR}Points{?}Point{\\?} to {ATTR_NAME}{TRAIT_DESC}{RENOWN_DESC}{GOLD_DESC}", null);
				textObject.SetTextVariable("SKILL_ONE", skills.ElementAt(0).Name);
				textObject.SetTextVariable("SKILL_TWO", skills.ElementAt(1).Name);
				textObject.SetTextVariable("SKILL_THREE", skills.ElementAt(2).Name);
			}
			else if (skills.Count == 2)
			{
				textObject = new TextObject("{=5JTEvvaO}{EXP_VALUE} Skill {?IS_PLURAL_SKILL}Levels{?}Level{\\?} and {FOCUS_VALUE} Focus {?IS_PLURAL_FOCUS}Points{?}Point{\\?} to {SKILL_ONE} and {SKILL_TWO}{NEWLINE}{ATTR_VALUE} Attribute {?IS_PLURAL_ATR}Points{?}Point{\\?} to {ATTR_NAME}{TRAIT_DESC}{RENOWN_DESC}{GOLD_DESC}", null);
				textObject.SetTextVariable("SKILL_ONE", skills.ElementAt(0).Name);
				textObject.SetTextVariable("SKILL_TWO", skills.ElementAt(1).Name);
			}
			else if (skills.Count == 1)
			{
				textObject = new TextObject("{=uw2kKrQk}{EXP_VALUE} Skill {?IS_PLURAL_SKILL}Levels{?}Level{\\?} and {FOCUS_VALUE} Focus {?IS_PLURAL_FOCUS}Points{?}Point{\\?} to {SKILL_ONE}{NEWLINE}{ATTR_VALUE} Attribute {?IS_PLURAL_ATR}Points{?}Point{\\?} to {ATTR_NAME}{TRAIT_DESC}{RENOWN_DESC}{GOLD_DESC}", null);
				textObject.SetTextVariable("SKILL_ONE", skills.ElementAt(0).Name);
			}
			else
			{
				textObject = new TextObject("{=NDWdnpI5}{UNSPENT_FOCUS_VALUE} unspent Focus {?IS_PLURAL_FOCUS}Points{?}Point{\\?}{NEWLINE}{UNSPENT_ATTR_VALUE} unspent Attribute {?IS_PLURAL_ATR}Points{?}Point{\\?}", null);
			}
			if (skills.Count > 0)
			{
				textObject.SetTextVariable("FOCUS_VALUE", focusToAdd);
				textObject.SetTextVariable("EXP_VALUE", skillLevelToAdd);
				textObject.SetTextVariable("ATTR_VALUE", attributeLevelToAdd);
				textObject.SetTextVariable("IS_PLURAL_SKILL", (skillLevelToAdd > 1) ? 1 : 0);
				textObject.SetTextVariable("IS_PLURAL_FOCUS", (focusToAdd > 1) ? 1 : 0);
				textObject.SetTextVariable("IS_PLURAL_ATR", (attributeLevelToAdd > 1) ? 1 : 0);
			}
			else
			{
				textObject.SetTextVariable("IS_PLURAL_FOCUS", (unspentFocustoAdd > 1) ? 1 : 0);
				textObject.SetTextVariable("IS_PLURAL_ATR", (unspentAttributeToAdd > 1) ? 1 : 0);
			}
			if (attribute != null)
			{
				textObject.SetTextVariable("ATTR_NAME", attribute.Name);
			}
			textObject.SetTextVariable("UNSPENT_FOCUS_VALUE", unspentFocustoAdd);
			textObject.SetTextVariable("UNSPENT_ATTR_VALUE", unspentAttributeToAdd);
			if (traits != null && traits.Count > 0 && traits.Count < 4)
			{
				TextObject textObject2 = TextObject.GetEmpty();
				if (traits.Count == 1)
				{
					textObject2 = new TextObject("{=DuQvj7zd}{newline}+{VALUE} to {TRAIT_NAME}", null);
					textObject2.SetTextVariable("TRAIT_NAME", traits.ElementAt(0).Name);
				}
				else if (traits.Count == 2)
				{
					textObject2 = new TextObject("{=F1syZDs4}{newline}+{VALUE} to {TRAIT_NAME_ONE} and {TRAIT_NAME_TWO}", null);
					textObject2.SetTextVariable("TRAIT_NAME_ONE", traits.ElementAt(0).Name);
					textObject2.SetTextVariable("TRAIT_NAME_TWO", traits.ElementAt(1).Name);
				}
				else if (traits.Count == 3)
				{
					textObject2 = new TextObject("{=i20baAus}{newline}+{VALUE} to {TRAIT_NAME_ONE}, {TRAIT_NAME_TWO} and {TRAIT_NAME_THREE}", null);
					textObject2.SetTextVariable("TRAIT_NAME_ONE", traits.ElementAt(0).Name);
					textObject2.SetTextVariable("TRAIT_NAME_TWO", traits.ElementAt(1).Name);
					textObject2.SetTextVariable("TRAIT_NAME_THREE", traits.ElementAt(2).Name);
				}
				if (!textObject2.IsEmpty())
				{
					textObject.SetTextVariable("TRAIT_DESC", textObject2);
					textObject2.SetTextVariable("VALUE", traitLevelToAdd);
				}
			}
			else
			{
				textObject.SetTextVariable("TRAIT_DESC", TextObject.GetEmpty());
			}
			if (renownToAdd > 0)
			{
				TextObject textObject3 = new TextObject("{=KXtaJNo4}{newline}+{VALUE} renown", null);
				textObject3.SetTextVariable("VALUE", renownToAdd);
				textObject.SetTextVariable("RENOWN_DESC", textObject3);
			}
			else
			{
				textObject.SetTextVariable("RENOWN_DESC", TextObject.GetEmpty());
			}
			if (goldToAdd > 0)
			{
				TextObject textObject4 = new TextObject("{=YBqmnNGv}{newline}+{VALUE} gold", null);
				textObject4.SetTextVariable("VALUE", goldToAdd);
				textObject.SetTextVariable("GOLD_DESC", textObject4);
			}
			else
			{
				textObject.SetTextVariable("GOLD_DESC", TextObject.GetEmpty());
			}
			return textObject;
		}
	}
}
