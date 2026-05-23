using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Helpers;

public static class PerkHelper
{
	public const float NavalMultiplier = 0.5f;

	public static void ClearPerksForSkill(Hero hero, SkillObject skill)
	{
		foreach (PerkObject item in PerkObject.All)
		{
			if (item.Skill == skill)
			{
				ClearPermanentBonusesIfExists(hero, item);
				hero.SetPerkValueInternal(item, value: false);
			}
		}
		PartyBase.MainParty.MemberRoster.UpdateVersion();
		hero.HitPoints = MathF.Min(hero.HitPoints, hero.MaxHitPoints);
	}

	private static void ClearPermanentBonusesIfExists(Hero hero, PerkObject perk)
	{
		if (hero.GetPerkValue(perk))
		{
			if (perk == DefaultPerks.Crafting.VigorousSmith)
			{
				hero.HeroDeveloper.RemoveAttribute(DefaultCharacterAttributes.Vigor, 1);
			}
			else if (perk == DefaultPerks.Crafting.StrongSmith)
			{
				hero.HeroDeveloper.RemoveAttribute(DefaultCharacterAttributes.Control, 1);
			}
			else if (perk == DefaultPerks.Crafting.EnduringSmith)
			{
				hero.HeroDeveloper.RemoveAttribute(DefaultCharacterAttributes.Endurance, 1);
			}
			else if (perk == DefaultPerks.Crafting.WeaponMasterSmith)
			{
				hero.HeroDeveloper.RemoveFocus(DefaultSkills.OneHanded, 1);
				hero.HeroDeveloper.RemoveFocus(DefaultSkills.TwoHanded, 1);
			}
			else if (perk == DefaultPerks.Athletics.Durable)
			{
				hero.HeroDeveloper.RemoveAttribute(DefaultCharacterAttributes.Endurance, 1);
			}
			else if (perk == DefaultPerks.Athletics.Steady)
			{
				hero.HeroDeveloper.RemoveAttribute(DefaultCharacterAttributes.Control, 1);
			}
			else if (perk == DefaultPerks.Athletics.Strong)
			{
				hero.HeroDeveloper.RemoveAttribute(DefaultCharacterAttributes.Vigor, 1);
			}
		}
	}

	public static IEnumerable<PerkObject> GetCaptainPerksForTroopUsages(TroopUsageFlags troopUsageFlags)
	{
		List<PerkObject> list = new List<PerkObject>();
		foreach (PerkObject item in PerkObject.All)
		{
			bool num = item.PrimaryTroopUsageMask != TroopUsageFlags.Undefined && troopUsageFlags.HasAllFlags(item.PrimaryTroopUsageMask);
			bool flag = item.SecondaryTroopUsageMask != TroopUsageFlags.Undefined && troopUsageFlags.HasAllFlags(item.SecondaryTroopUsageMask);
			if (num || flag)
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static bool PlayerHasAnyItemDonationPerk()
	{
		if (!MobileParty.MainParty.HasPerk(DefaultPerks.Steward.GivingHands))
		{
			return MobileParty.MainParty.HasPerk(DefaultPerks.Steward.PaidInPromise, checkSecondaryRole: true);
		}
		return true;
	}

	public static void AddPerkBonusForParty(PerkObject perk, MobileParty party, bool isPrimaryBonus, ref ExplainedNumber stat, bool shouldApplyNavalMultiplier = false)
	{
		if (party != null && party.HasPerk(perk, !isPrimaryBonus))
		{
			float num = (isPrimaryBonus ? perk.PrimaryBonus : perk.SecondaryBonus);
			if (shouldApplyNavalMultiplier)
			{
				num *= 0.5f;
			}
			EffectIncrementType effectIncrementType = (isPrimaryBonus ? perk.PrimaryIncrementType : perk.SecondaryIncrementType);
			AddToStat(ref stat, effectIncrementType, num, perk.Name);
		}
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

	public static void AddPerkBonusForCharacter(PerkObject perk, CharacterObject character, bool isPrimaryBonus, ref ExplainedNumber bonuses, bool shouldApplyNavalMultiplier = false)
	{
		float num = (shouldApplyNavalMultiplier ? 0.5f : 1f);
		if (isPrimaryBonus && perk.PrimaryRole == PartyRole.Personal)
		{
			if (character.GetPerkValue(perk))
			{
				AddToStat(ref bonuses, perk.PrimaryIncrementType, perk.PrimaryBonus * num, perk.Name);
			}
		}
		else if (!isPrimaryBonus && perk.SecondaryRole == PartyRole.Personal && character.GetPerkValue(perk))
		{
			AddToStat(ref bonuses, perk.SecondaryIncrementType, perk.SecondaryBonus * num, perk.Name);
		}
		if (isPrimaryBonus && perk.PrimaryRole == PartyRole.ClanLeader)
		{
			if (character.IsHero && character.HeroObject.Clan?.Leader != null && character.HeroObject.Clan.Leader.GetPerkValue(perk))
			{
				AddToStat(ref bonuses, perk.PrimaryIncrementType, perk.PrimaryBonus * num, perk.Name);
			}
		}
		else if (!isPrimaryBonus && perk.SecondaryRole == PartyRole.ClanLeader && character.IsHero && character.HeroObject.Clan.Leader != null && character.HeroObject.Clan.Leader.GetPerkValue(perk))
		{
			AddToStat(ref bonuses, perk.SecondaryIncrementType, perk.SecondaryBonus * num, perk.Name);
		}
	}

	public static void AddEpicPerkBonusForCharacter(PerkObject perk, CharacterObject character, SkillObject skillType, bool applyPrimaryBonus, ref ExplainedNumber bonuses, int skillRequired, bool shouldApplyNavalMultiplier = false)
	{
		if (!character.GetPerkValue(perk))
		{
			return;
		}
		int skillValue = character.GetSkillValue(skillType);
		if (skillValue > skillRequired)
		{
			float num = (shouldApplyNavalMultiplier ? 0.5f : 1f);
			if (applyPrimaryBonus)
			{
				AddToStat(ref bonuses, perk.PrimaryIncrementType, perk.PrimaryBonus * (float)(skillValue - skillRequired) * num, perk.Name);
			}
			else
			{
				AddToStat(ref bonuses, perk.SecondaryIncrementType, perk.SecondaryBonus * (float)(skillValue - skillRequired) * num, perk.Name);
			}
		}
	}

	public static void AddPerkBonusFromCaptain(PerkObject perk, CharacterObject captainCharacter, ref ExplainedNumber bonuses)
	{
		if (perk.PrimaryRole == PartyRole.Captain)
		{
			if (captainCharacter != null && captainCharacter.GetPerkValue(perk))
			{
				AddToStat(ref bonuses, perk.PrimaryIncrementType, perk.PrimaryBonus, perk.Name);
			}
		}
		else if (perk.SecondaryRole == PartyRole.Captain && captainCharacter != null && captainCharacter.GetPerkValue(perk))
		{
			AddToStat(ref bonuses, perk.SecondaryIncrementType, perk.SecondaryBonus, perk.Name);
		}
	}

	public static void AddPerkBonusForTown(PerkObject perk, Town town, ref ExplainedNumber bonuses)
	{
		bool flag = perk.PrimaryRole == PartyRole.Governor;
		bool flag2 = perk.SecondaryRole == PartyRole.Governor;
		if (!(flag || flag2))
		{
			return;
		}
		Hero governor = town.Governor;
		if (governor != null && governor.GetPerkValue(perk) && governor.CurrentSettlement != null && governor.CurrentSettlement == town.Settlement)
		{
			if (flag)
			{
				AddToStat(ref bonuses, perk.PrimaryIncrementType, perk.PrimaryBonus, perk.Name);
			}
			else
			{
				AddToStat(ref bonuses, perk.SecondaryIncrementType, perk.SecondaryBonus, perk.Name);
			}
		}
	}

	public static bool GetPerkValueForTown(PerkObject perk, Town town)
	{
		if (perk.PrimaryRole == PartyRole.ClanLeader || perk.SecondaryRole == PartyRole.ClanLeader)
		{
			Hero hero = town.Owner.Settlement.OwnerClan?.Leader;
			if (hero != null && hero.GetPerkValue(perk))
			{
				return true;
			}
		}
		if (perk.PrimaryRole == PartyRole.Governor || perk.SecondaryRole == PartyRole.Governor)
		{
			Hero governor = town.Governor;
			if (governor != null && governor.GetPerkValue(perk) && governor.CurrentSettlement != null && governor.CurrentSettlement == town.Settlement)
			{
				return true;
			}
		}
		return false;
	}

	public static List<PerkObject> GetGovernorPerksForHero(Hero hero)
	{
		List<PerkObject> list = new List<PerkObject>();
		foreach (PerkObject item in PerkObject.All)
		{
			if ((item.PrimaryRole == PartyRole.Governor || item.SecondaryRole == PartyRole.Governor) && hero.GetPerkValue(item))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static (TextObject, TextObject) GetGovernorEngineeringSkillEffectForHero(Hero governor)
	{
		if (governor != null && governor.GetSkillValue(DefaultSkills.Engineering) > 0)
		{
			SkillEffect townProjectBuildingBonus = DefaultSkillEffects.TownProjectBuildingBonus;
			int skillValue = governor.GetSkillValue(townProjectBuildingBonus.EffectedSkill);
			TextObject effectDescriptionForSkillLevel = SkillHelper.GetEffectDescriptionForSkillLevel(townProjectBuildingBonus, skillValue);
			return (DefaultSkills.Engineering.Name, effectDescriptionForSkillLevel);
		}
		return (TextObject.GetEmpty(), new TextObject("{=0rBsbw1T}No effect"));
	}

	public static int AvailablePerkCountOfHero(Hero hero)
	{
		MBList<PerkObject> mBList = new MBList<PerkObject>();
		foreach (PerkObject item in PerkObject.All)
		{
			SkillObject skill = item.Skill;
			if ((float)hero.GetSkillValue(skill) >= item.RequiredSkillValue && !hero.GetPerkValue(item) && (item.AlternativePerk == null || !hero.GetPerkValue(item.AlternativePerk)) && !mBList.Contains(item.AlternativePerk))
			{
				mBList.Add(item);
			}
		}
		return mBList.Count;
	}
}
