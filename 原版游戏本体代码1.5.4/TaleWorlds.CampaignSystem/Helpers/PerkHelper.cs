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
	public const float NavalBattleEnvironmentMultiplier = 0.5f;

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
			if (item.PrimaryRole == PartyRole.Captain && item.PrimaryTroopUsageMask != TroopUsageFlags.Undefined && item.ApplicableInEnvironment(battleEnvironment, isPrimaryEffect: true) && (item.PrimaryTroopUsageMask == TroopUsageFlags.Any || troopUsageFlags.HasAllFlags(item.PrimaryTroopUsageMask)))
			{
				list.Add(item);
			}
			else if (item.SecondaryRole == PartyRole.Captain && item.SecondaryTroopUsageMask != TroopUsageFlags.Undefined && item.ApplicableInEnvironment(battleEnvironment, isPrimaryEffect: false) && (item.SecondaryTroopUsageMask == TroopUsageFlags.Any || troopUsageFlags.HasAllFlags(item.SecondaryTroopUsageMask)))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static bool PlayerHasAnyItemDonationPerk()
	{
		if (!MobileParty.MainParty.HasPerk(DefaultPerks.Steward.GivingHands, out var perkOwnerHero))
		{
			return MobileParty.MainParty.HasPerk(DefaultPerks.Steward.PaidInPromise, out perkOwnerHero, checkSecondaryRole: true);
		}
		return true;
	}

	public static bool AddPerkBonusForParty(PerkObject perk, MobileParty party, bool isPrimaryBonus, ref ExplainedNumber stat)
	{
		return AddPerkBonusForParty(perk, party.CurrentBattleEnvironment, party, isPrimaryBonus, ref stat);
	}

	public static bool AddPerkBonusForParty(PerkObject perk, BattleEnvironment battleEnvironment, MobileParty party, bool isPrimaryBonus, ref ExplainedNumber stat)
	{
		Hero perkOwnerHero = null;
		if (party != null && party.HasPerk(perk, battleEnvironment, out perkOwnerHero, !isPrimaryBonus))
		{
			CalculateContextualPerkData(perk, battleEnvironment, isPrimaryBonus, out var incrementType, out var perkBonus);
			AddToStat(ref stat, incrementType, perkBonus, perk.Name);
			return true;
		}
		return false;
	}

	public static bool AddPerkBonusForCharacter(PerkObject perk, BattleEnvironment battleEnvironment, CharacterObject character, bool isPrimaryBonus, ref ExplainedNumber bonuses)
	{
		if (perk.ApplicableInEnvironment(battleEnvironment, isPrimaryBonus))
		{
			if (((isPrimaryBonus && perk.PrimaryRole == PartyRole.Personal) || (!isPrimaryBonus && perk.SecondaryRole == PartyRole.Personal)) && character.GetPerkValue(perk))
			{
				CalculateContextualPerkData(perk, battleEnvironment, isPrimaryBonus, out var incrementType, out var perkBonus);
				AddToStat(ref bonuses, incrementType, perkBonus, perk.Name);
				return true;
			}
			if (((isPrimaryBonus && perk.PrimaryRole == PartyRole.ClanLeader) || (!isPrimaryBonus && perk.SecondaryRole == PartyRole.ClanLeader)) && character.IsHero && character.HeroObject.Clan?.Leader != null && character.HeroObject.Clan.Leader.GetPerkValue(perk))
			{
				CalculateContextualPerkData(perk, battleEnvironment, isPrimaryBonus, out var incrementType2, out var perkBonus2);
				AddToStat(ref bonuses, incrementType2, perkBonus2, perk.Name);
				return true;
			}
		}
		return false;
	}

	public static bool AddEpicPerkBonusForCharacterWithSkill(PerkObject perk, BattleEnvironment battleEnvironment, CharacterObject character, int effectiveSkill, bool isPrimaryBonus, ref ExplainedNumber bonuses, int skillRequired)
	{
		if (perk.ApplicableInEnvironment(battleEnvironment, isPrimaryBonus) && character.GetPerkValue(perk) && effectiveSkill > skillRequired)
		{
			CalculateContextualPerkData(perk, battleEnvironment, isPrimaryBonus, out var incrementType, out var perkBonus);
			AddToStat(ref bonuses, incrementType, perkBonus * (float)(effectiveSkill - skillRequired), perk.Name);
			return true;
		}
		return false;
	}

	public static bool AddEpicPerkBonusForCharacter(PerkObject perk, BattleEnvironment battleEnvironment, CharacterObject character, SkillObject skillType, bool isPrimaryBonus, ref ExplainedNumber bonuses, int skillRequired)
	{
		return AddEpicPerkBonusForCharacterWithSkill(perk, battleEnvironment, character, character.GetSkillValue(skillType), isPrimaryBonus, ref bonuses, skillRequired);
	}

	public static bool AddPerkBonusFromCaptain(PerkObject perk, BattleEnvironment battleEnvironment, CharacterObject captainCharacter, ref ExplainedNumber bonuses)
	{
		bool flag = perk.PrimaryRole == PartyRole.Captain;
		if ((flag || perk.SecondaryRole == PartyRole.Captain) && perk.ApplicableInEnvironment(battleEnvironment, flag) && captainCharacter != null && captainCharacter.GetPerkValue(perk))
		{
			CalculateContextualPerkData(perk, battleEnvironment, flag, out var incrementType, out var perkBonus);
			AddToStat(ref bonuses, incrementType, perkBonus, perk.Name);
			return true;
		}
		return false;
	}

	public static bool AddPerkBonusForTown(PerkObject perk, Town town, bool isPrimaryBonus, ref ExplainedNumber bonuses)
	{
		Hero governor = town.Governor;
		if (governor != null && governor.GetPerkValue(perk) && governor.CurrentSettlement != null && governor.CurrentSettlement == town.Settlement)
		{
			AddToStat(ref bonuses, isPrimaryBonus ? perk.PrimaryIncrementType : perk.SecondaryIncrementType, isPrimaryBonus ? perk.PrimaryBonus : perk.SecondaryBonus, perk.Name);
			return true;
		}
		return false;
	}

	public static Hero GetHeroForTownPerk(PerkObject perk, Town town)
	{
		if (perk.PrimaryRole == PartyRole.ClanLeader || perk.SecondaryRole == PartyRole.ClanLeader)
		{
			Hero hero = town.Owner.Settlement.OwnerClan?.Leader;
			if (hero != null && hero.GetPerkValue(perk))
			{
				return hero;
			}
		}
		if (perk.PrimaryRole == PartyRole.Governor || perk.SecondaryRole == PartyRole.Governor)
		{
			Hero governor = town.Governor;
			if (governor != null && governor.GetPerkValue(perk) && governor.CurrentSettlement != null && governor.CurrentSettlement == town.Settlement)
			{
				return governor;
			}
		}
		return null;
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

	private static void CalculateContextualPerkData(PerkObject perk, BattleEnvironment battleEnvironment, bool isPrimaryBonus, out EffectIncrementType incrementType, out float perkBonus)
	{
		PerkObject.EffectEnvironment effectEnvironment;
		if (isPrimaryBonus)
		{
			perkBonus = perk.PrimaryBonus;
			effectEnvironment = perk.PrimaryEffectEnvironment;
			incrementType = perk.PrimaryIncrementType;
		}
		else
		{
			perkBonus = perk.SecondaryBonus;
			effectEnvironment = perk.SecondaryEffectEnvironment;
			incrementType = perk.SecondaryIncrementType;
		}
		float num = ((effectEnvironment == PerkObject.EffectEnvironment.NavalReduced && battleEnvironment == BattleEnvironment.Naval) ? 0.5f : 1f);
		perkBonus *= num;
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
