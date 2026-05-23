using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Helpers;

public static class SkillHelper
{
	public static void AddSkillBonusForSkillLevel(SkillEffect skillEffect, ref ExplainedNumber explainedNumber, int skillLevel)
	{
		float skillEffectValue = skillEffect.GetSkillEffectValue(skillLevel);
		AddToStat(ref explainedNumber, skillEffect.IncrementType, skillEffectValue, explainedNumber.IncludeDescriptions ? GameTexts.FindText("role", skillEffect.Role.ToString()) : null);
	}

	public static void AddSkillBonusForParty(SkillEffect skillEffect, MobileParty party, ref ExplainedNumber explainedNumber)
	{
		CharacterObject characterObject = null;
		characterObject = ((skillEffect.Role == PartyRole.PartyLeader && party.LeaderHero != null) ? party.LeaderHero?.CharacterObject : ((party.GetEffectiveRoleHolder(skillEffect.Role) == null) ? GetEffectivePartyLeaderForSkill(party.Party) : party.GetEffectiveRoleHolder(skillEffect.Role).CharacterObject));
		if (characterObject != null)
		{
			int skillValue = characterObject.GetSkillValue(skillEffect.EffectedSkill);
			float skillEffectValue = skillEffect.GetSkillEffectValue(skillValue);
			AddToStat(ref explainedNumber, skillEffect.IncrementType, skillEffectValue, explainedNumber.IncludeDescriptions ? GameTexts.FindText("role", skillEffect.Role.ToString()) : null);
		}
	}

	public static void AddSkillBonusForTown(SkillEffect skillEffect, Town town, ref ExplainedNumber explainedNumber)
	{
		CharacterObject characterObject = null;
		if (skillEffect.Role == PartyRole.ClanLeader)
		{
			characterObject = town.Owner.Settlement.OwnerClan?.Leader.CharacterObject;
		}
		else if (skillEffect.Role == PartyRole.Governor)
		{
			characterObject = town.Governor?.CharacterObject;
		}
		if (characterObject != null)
		{
			int skillValue = characterObject.GetSkillValue(skillEffect.EffectedSkill);
			float skillEffectValue = skillEffect.GetSkillEffectValue(skillValue);
			AddToStat(ref explainedNumber, skillEffect.IncrementType, skillEffectValue, explainedNumber.IncludeDescriptions ? GameTexts.FindText("role", skillEffect.Role.ToString()) : null);
		}
	}

	public static void AddSkillBonusForCharacter(SkillEffect skillEffect, CharacterObject character, ref ExplainedNumber explainedNumber)
	{
		int skillValue = character.GetSkillValue(skillEffect.EffectedSkill);
		float skillEffectValue = skillEffect.GetSkillEffectValue(skillValue);
		AddToStat(ref explainedNumber, skillEffect.IncrementType, skillEffectValue, explainedNumber.IncludeDescriptions ? GameTexts.FindText("role", skillEffect.Role.ToString()) : null);
	}

	public static TextObject GetEffectDescriptionForSkillLevel(SkillEffect effect, int level)
	{
		float skillEffectValue = effect.GetSkillEffectValue(level);
		float f = ((effect.IncrementType == EffectIncrementType.AddFactor) ? (skillEffectValue * 100f) : skillEffectValue);
		effect.Description.SetTextVariable("a0", MathF.Abs(f).ToString("0.0"));
		return effect.Description;
	}

	private static void AddToStat(ref ExplainedNumber stat, EffectIncrementType effectIncrementType, float number, TextObject text)
	{
		switch (effectIncrementType)
		{
		case EffectIncrementType.Add:
			stat.Add(number, text);
			break;
		case EffectIncrementType.AddFactor:
			stat.AddFactor(number, text);
			break;
		}
	}

	public static CharacterObject GetEffectivePartyLeaderForSkill(PartyBase party)
	{
		if (party == null)
		{
			return null;
		}
		if (party.LeaderHero == null)
		{
			TroopRoster memberRoster = party.MemberRoster;
			if (memberRoster == null || memberRoster.TotalManCount <= 0)
			{
				return null;
			}
			return party.MemberRoster.GetCharacterAtIndex(0);
		}
		return party.LeaderHero.CharacterObject;
	}
}
