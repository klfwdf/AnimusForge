using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CharacterCreationContent;

public class NarrativeMenuOptionArgs
{
	public MBList<SkillObject> AffectedSkills { get; private set; }

	public int SkillLevelToAdd { get; private set; }

	public MBList<TraitObject> AffectedTraits { get; private set; }

	public int TraitLevelToAdd { get; private set; }

	public int FocusToAdd { get; private set; }

	public int UnspentFocusToAdd { get; private set; }

	public CharacterAttribute EffectedAttribute { get; private set; }

	public int AttributeLevelToAdd { get; private set; }

	public int UnspentAttributeToAdd { get; private set; }

	public int RenownToAdd { get; private set; }

	public int GoldToAdd { get; private set; }

	public TextObject PositiveEffectText => GetPositiveEffectText(AffectedSkills.ToMBList(), EffectedAttribute, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, AffectedTraits.ToMBList(), TraitLevelToAdd, RenownToAdd, GoldToAdd, UnspentFocusToAdd, UnspentAttributeToAdd);

	public NarrativeMenuOptionArgs()
	{
		AffectedSkills = new MBList<SkillObject>();
		AffectedTraits = new MBList<TraitObject>();
	}

	public void SetAffectedSkills(SkillObject[] affectedSkills)
	{
		AffectedSkills = affectedSkills.ToMBList();
	}

	public void SetFocusToSkills(int focusToAdd)
	{
		FocusToAdd = focusToAdd;
	}

	public void SetLevelToSkills(int levelToAdd)
	{
		SkillLevelToAdd = levelToAdd;
	}

	public void SetAffectedTraits(TraitObject[] affectedTraits)
	{
		AffectedTraits = affectedTraits.ToMBList();
	}

	public void SetLevelToTraits(int levelToAdd)
	{
		TraitLevelToAdd = levelToAdd;
	}

	public void SetLevelToAttribute(CharacterAttribute characterAttribute, int levelToAdd)
	{
		EffectedAttribute = characterAttribute;
		AttributeLevelToAdd = levelToAdd;
	}

	public void SetRenownToAdd(int value)
	{
		RenownToAdd = value;
	}

	public void SetUnspentFocusToAdd(int value)
	{
		UnspentFocusToAdd = value;
	}

	public void SetUnspentAttributeToAdd(int value)
	{
		UnspentAttributeToAdd = value;
	}

	private TextObject GetPositiveEffectText(MBList<SkillObject> skills, CharacterAttribute attribute, int focusToAdd = 0, int skillLevelToAdd = 0, int attributeLevelToAdd = 0, MBList<TraitObject> traits = null, int traitLevelToAdd = 0, int renownToAdd = 0, int goldToAdd = 0, int unspentFocustoAdd = 0, int unspentAttributeToAdd = 0)
	{
		TextObject textObject;
		if (skills.Count == 3)
		{
			textObject = new TextObject("{=jeWV2uV3}{EXP_VALUE} Skill {?IS_PLURAL_SKILL}Levels{?}Level{\\?} and {FOCUS_VALUE} Focus {?IS_PLURAL_FOCUS}Points{?}Point{\\?} to {SKILL_ONE}, {SKILL_TWO} and {SKILL_THREE}{NEWLINE}{ATTR_VALUE} Attribute {?IS_PLURAL_ATR}Points{?}Point{\\?} to {ATTR_NAME}{TRAIT_DESC}{RENOWN_DESC}{GOLD_DESC}");
			textObject.SetTextVariable("SKILL_ONE", skills.ElementAt(0).Name);
			textObject.SetTextVariable("SKILL_TWO", skills.ElementAt(1).Name);
			textObject.SetTextVariable("SKILL_THREE", skills.ElementAt(2).Name);
		}
		else if (skills.Count == 2)
		{
			textObject = new TextObject("{=5JTEvvaO}{EXP_VALUE} Skill {?IS_PLURAL_SKILL}Levels{?}Level{\\?} and {FOCUS_VALUE} Focus {?IS_PLURAL_FOCUS}Points{?}Point{\\?} to {SKILL_ONE} and {SKILL_TWO}{NEWLINE}{ATTR_VALUE} Attribute {?IS_PLURAL_ATR}Points{?}Point{\\?} to {ATTR_NAME}{TRAIT_DESC}{RENOWN_DESC}{GOLD_DESC}");
			textObject.SetTextVariable("SKILL_ONE", skills.ElementAt(0).Name);
			textObject.SetTextVariable("SKILL_TWO", skills.ElementAt(1).Name);
		}
		else if (skills.Count == 1)
		{
			textObject = new TextObject("{=uw2kKrQk}{EXP_VALUE} Skill {?IS_PLURAL_SKILL}Levels{?}Level{\\?} and {FOCUS_VALUE} Focus {?IS_PLURAL_FOCUS}Points{?}Point{\\?} to {SKILL_ONE}{NEWLINE}{ATTR_VALUE} Attribute {?IS_PLURAL_ATR}Points{?}Point{\\?} to {ATTR_NAME}{TRAIT_DESC}{RENOWN_DESC}{GOLD_DESC}");
			textObject.SetTextVariable("SKILL_ONE", skills.ElementAt(0).Name);
		}
		else
		{
			textObject = new TextObject("{=NDWdnpI5}{UNSPENT_FOCUS_VALUE} unspent Focus {?IS_PLURAL_FOCUS}Points{?}Point{\\?}{NEWLINE}{UNSPENT_ATTR_VALUE} unspent Attribute {?IS_PLURAL_ATR}Points{?}Point{\\?}");
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
				textObject2 = new TextObject("{=DuQvj7zd}{newline}+{VALUE} to {TRAIT_NAME}");
				textObject2.SetTextVariable("TRAIT_NAME", traits.ElementAt(0).Name);
			}
			else if (traits.Count == 2)
			{
				textObject2 = new TextObject("{=F1syZDs4}{newline}+{VALUE} to {TRAIT_NAME_ONE} and {TRAIT_NAME_TWO}");
				textObject2.SetTextVariable("TRAIT_NAME_ONE", traits.ElementAt(0).Name);
				textObject2.SetTextVariable("TRAIT_NAME_TWO", traits.ElementAt(1).Name);
			}
			else if (traits.Count == 3)
			{
				textObject2 = new TextObject("{=i20baAus}{newline}+{VALUE} to {TRAIT_NAME_ONE}, {TRAIT_NAME_TWO} and {TRAIT_NAME_THREE}");
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
			TextObject textObject3 = new TextObject("{=KXtaJNo4}{newline}+{VALUE} renown");
			textObject3.SetTextVariable("VALUE", renownToAdd);
			textObject.SetTextVariable("RENOWN_DESC", textObject3);
		}
		else
		{
			textObject.SetTextVariable("RENOWN_DESC", TextObject.GetEmpty());
		}
		if (goldToAdd > 0)
		{
			TextObject textObject4 = new TextObject("{=YBqmnNGv}{newline}+{VALUE} gold");
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
