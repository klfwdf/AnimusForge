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

	public static IEnumerable<PerkObject> GetCaptainPerksForTroopUsages(TroopUsageFlags troopUsageFlags, BattleEnvironment battleEnvironment = BattleEnvironment.Any)
	{
		List<PerkObject> list = new List<PerkObject>();
		foreach (PerkObject item in PerkObject.All)
		{
			if (item.PrimaryRole == PartyRole.Captain && item.PrimaryTroopUsageMask != TroopUsageFlags.Undefined && TryGetPerkEffectBonus(item, isPrimary: true, out var bonus, battleEnvironment) && (item.PrimaryTroopUsageMask == TroopUsageFlags.Any || troopUsageFlags.HasAllFlags(item.PrimaryTroopUsageMask)))
			{
				list.Add(item);
			}
			else if (item.SecondaryRole == PartyRole.Captain && item.SecondaryTroopUsageMask != TroopUsageFlags.Undefined && TryGetPerkEffectBonus(item, isPrimary: false, out bonus, battleEnvironment) && (item.SecondaryTroopUsageMask == TroopUsageFlags.Any || troopUsageFlags.HasAllFlags(item.SecondaryTroopUsageMask)))
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

	public static bool TryGetPerkEffectBonus(PerkObject perk, bool isPrimary, out float bonus, BattleEnvironment battleEnvironment = BattleEnvironment.Any)
	{
		bonus = (isPrimary ? perk.PrimaryBonus : perk.SecondaryBonus);
		if (battleEnvironment == BattleEnvironment.Any)
		{
			return true;
		}
		PerkObject.EffectEnvironment effectEnvironment = (isPrimary ? perk.PrimaryEffectEnvironment : perk.SecondaryEffectEnvironment);
		if (battleEnvironment == BattleEnvironment.Land)
		{
			if (effectEnvironment == PerkObject.EffectEnvironment.NavalOnly)
			{
				bonus = 0f;
				return false;
			}
			return true;
		}
		switch (effectEnvironment)
		{
		case PerkObject.EffectEnvironment.LandOnly:
			bonus = 0f;
			return false;
		case PerkObject.EffectEnvironment.NavalReduced:
			bonus *= 0.5f;
			break;
		}
		return true;
	}

	public static bool AddPerkBonusForParty(PerkObject perk, MobileParty party, bool isPrimaryBonus, ref ExplainedNumber stat, BattleEnvironment battleEnvironment = BattleEnvironment.Any)
	{
		if (party == null)
		{
			return false;
		}
		if (battleEnvironment == BattleEnvironment.Any)
		{
			battleEnvironment = party.CurrentBattleEnvironment;
		}
		if (TryGetPerkEffectBonus(perk, isPrimaryBonus, out var bonus, battleEnvironment) && party.GetPerkOwnerHero(perk, !isPrimaryBonus) != null)
		{
			EffectIncrementType effectIncrementType = (isPrimaryBonus ? perk.PrimaryIncrementType : perk.SecondaryIncrementType);
			AddToStat(ref stat, effectIncrementType, bonus, perk.Name);
			return true;
		}
		return false;
	}

	public static bool AddPerkBonusForCharacter(PerkObject perk, CharacterObject character, bool isPrimaryBonus, ref ExplainedNumber bonuses, BattleEnvironment battleEnvironment = BattleEnvironment.Any)
	{
		if (character == null || !TryGetPerkEffectBonus(perk, isPrimaryBonus, out var bonus, battleEnvironment))
		{
			return false;
		}
		PartyRole partyRole = (isPrimaryBonus ? perk.PrimaryRole : perk.SecondaryRole);
		EffectIncrementType effectIncrementType = (isPrimaryBonus ? perk.PrimaryIncrementType : perk.SecondaryIncrementType);
		if (partyRole == PartyRole.Personal && character.GetPerkValue(perk))
		{
			AddToStat(ref bonuses, effectIncrementType, bonus, perk.Name);
			return true;
		}
		if (partyRole == PartyRole.ClanLeader && character.IsHero && character.HeroObject.Clan?.Leader != null && character.HeroObject.Clan.Leader.GetPerkValue(perk))
		{
			AddToStat(ref bonuses, effectIncrementType, bonus, perk.Name);
			return true;
		}
		return false;
	}

	public static bool AddEpicPerkBonusForCharacter(PerkObject perk, CharacterObject character, SkillObject skillType, bool isPrimaryBonus, ref ExplainedNumber bonuses, int skillRequired, int effectiveSkill = -1, BattleEnvironment battleEnvironment = BattleEnvironment.Any)
	{
		if (character == null || !character.GetPerkValue(perk))
		{
			return false;
		}
		int num = ((effectiveSkill >= 0) ? effectiveSkill : character.GetSkillValue(skillType));
		if (num > skillRequired && TryGetPerkEffectBonus(perk, isPrimaryBonus, out var bonus, battleEnvironment))
		{
			EffectIncrementType effectIncrementType = (isPrimaryBonus ? perk.PrimaryIncrementType : perk.SecondaryIncrementType);
			float number = bonus * (float)(num - skillRequired);
			AddToStat(ref bonuses, effectIncrementType, number, perk.Name);
			return true;
		}
		return false;
	}

	public static bool AddPerkBonusFromCaptain(PerkObject perk, CharacterObject captainCharacter, ref ExplainedNumber bonuses, BattleEnvironment battleEnvironment = BattleEnvironment.Any)
	{
		bool flag = perk.PrimaryRole == PartyRole.Captain;
		if ((flag || perk.SecondaryRole == PartyRole.Captain) && captainCharacter != null && captainCharacter.GetPerkValue(perk) && TryGetPerkEffectBonus(perk, flag, out var bonus, battleEnvironment))
		{
			EffectIncrementType effectIncrementType = (flag ? perk.PrimaryIncrementType : perk.SecondaryIncrementType);
			AddToStat(ref bonuses, effectIncrementType, bonus, perk.Name);
			return true;
		}
		return false;
	}

	public static bool AddPerkBonusForTown(PerkObject perk, Town town, bool isPrimaryBonus, ref ExplainedNumber bonuses)
	{
		Hero governor = town.Governor;
		if (governor != null && governor.GetPerkValue(perk) && governor.CurrentSettlement != null && governor.CurrentSettlement == town.Settlement)
		{
			EffectIncrementType effectIncrementType = (isPrimaryBonus ? perk.PrimaryIncrementType : perk.SecondaryIncrementType);
			float number = (isPrimaryBonus ? perk.PrimaryBonus : perk.SecondaryBonus);
			AddToStat(ref bonuses, effectIncrementType, number, perk.Name);
			return true;
		}
		return false;
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
}
