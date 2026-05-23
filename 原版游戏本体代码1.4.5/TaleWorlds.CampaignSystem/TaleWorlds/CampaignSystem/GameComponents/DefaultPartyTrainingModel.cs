using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultPartyTrainingModel : PartyTrainingModel
{
	public override int GetXpReward(CharacterObject character)
	{
		int num = character.Level + 6;
		return num * num / 3;
	}

	public override ExplainedNumber GetEffectiveDailyExperience(MobileParty mobileParty, TroopRosterElement troop)
	{
		ExplainedNumber stat = default(ExplainedNumber);
		if (mobileParty.IsLordParty && !troop.Character.IsHero && (mobileParty.Army == null || mobileParty.Army.LeaderParty != MobileParty.MainParty) && mobileParty.MapEvent == null && (mobileParty.Party.Owner == null || mobileParty.Party.Owner.Clan != Clan.PlayerClan))
		{
			if (mobileParty.LeaderHero != null && mobileParty.LeaderHero == mobileParty.ActualClan.Leader)
			{
				stat.Add(15f + (float)troop.Character.Tier * 3f);
			}
			else
			{
				stat.Add(10f + (float)troop.Character.Tier * 2f);
			}
		}
		if (mobileParty.IsActive && !mobileParty.IsCurrentlyAtSea && mobileParty.HasPerk(DefaultPerks.Leadership.CombatTips))
		{
			stat.Add(GetPerkExperiencesForTroops(DefaultPerks.Leadership.CombatTips));
		}
		if (mobileParty.IsActive && !mobileParty.IsCurrentlyAtSea && mobileParty.HasPerk(DefaultPerks.Leadership.RaiseTheMeek) && troop.Character.Tier < 3)
		{
			stat.Add(GetPerkExperiencesForTroops(DefaultPerks.Leadership.RaiseTheMeek));
		}
		if (mobileParty.IsGarrison && mobileParty.CurrentSettlement?.Town.Governor != null && mobileParty.CurrentSettlement.Town.Governor.GetPerkValue(DefaultPerks.Bow.BullsEye))
		{
			stat.Add(GetPerkExperiencesForTroops(DefaultPerks.Bow.BullsEye));
		}
		if (mobileParty.IsActive && mobileParty.HasPerk(DefaultPerks.Polearm.Drills, checkSecondaryRole: true))
		{
			stat.Add(GetPerkExperiencesForTroops(DefaultPerks.Polearm.Drills));
		}
		if (mobileParty.IsActive && !mobileParty.IsCurrentlyAtSea && mobileParty.HasPerk(DefaultPerks.OneHanded.MilitaryTradition) && troop.Character.IsInfantry)
		{
			stat.Add(GetPerkExperiencesForTroops(DefaultPerks.OneHanded.MilitaryTradition));
		}
		if (mobileParty.IsActive && !mobileParty.IsCurrentlyAtSea && mobileParty.HasPerk(DefaultPerks.Athletics.WalkItOff, checkSecondaryRole: true) && !troop.Character.IsMounted && mobileParty.IsMoving)
		{
			stat.Add(GetPerkExperiencesForTroops(DefaultPerks.Athletics.WalkItOff));
		}
		if (mobileParty.IsActive && mobileParty.HasPerk(DefaultPerks.Throwing.Saddlebags, checkSecondaryRole: true) && troop.Character.IsInfantry)
		{
			stat.Add(GetPerkExperiencesForTroops(DefaultPerks.Throwing.Saddlebags));
		}
		if (mobileParty.IsActive && mobileParty.HasPerk(DefaultPerks.Athletics.AGoodDaysRest, checkSecondaryRole: true) && !troop.Character.IsMounted && !mobileParty.IsMoving && mobileParty.CurrentSettlement != null)
		{
			stat.Add(GetPerkExperiencesForTroops(DefaultPerks.Athletics.AGoodDaysRest));
		}
		if (mobileParty.IsActive && !mobileParty.IsCurrentlyAtSea && mobileParty.HasPerk(DefaultPerks.Bow.Trainer, checkSecondaryRole: true) && troop.Character.IsRanged)
		{
			stat.Add(GetPerkExperiencesForTroops(DefaultPerks.Bow.Trainer));
		}
		if (mobileParty.IsActive && !mobileParty.IsCurrentlyAtSea && mobileParty.HasPerk(DefaultPerks.Crossbow.RenownMarksmen) && troop.Character.IsRanged)
		{
			stat.Add(GetPerkExperiencesForTroops(DefaultPerks.Crossbow.RenownMarksmen));
		}
		if (mobileParty.IsActive && mobileParty.IsMoving)
		{
			if (mobileParty.Morale > 75f)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.ForcedMarch, mobileParty, isPrimaryBonus: false, ref stat);
			}
			if (mobileParty.TotalWeightCarried > (float)mobileParty.InventoryCapacity && !mobileParty.IsCurrentlyAtSea)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.Unburdened, mobileParty, isPrimaryBonus: false, ref stat);
			}
		}
		if (mobileParty.IsActive && !mobileParty.IsCurrentlyAtSea && mobileParty.HasPerk(DefaultPerks.Steward.SevenVeterans) && troop.Character.Tier >= 4)
		{
			stat.Add(GetPerkExperiencesForTroops(DefaultPerks.Steward.SevenVeterans));
		}
		if (mobileParty.IsActive && !mobileParty.IsCurrentlyAtSea && mobileParty.HasPerk(DefaultPerks.Steward.DrillSergant))
		{
			stat.Add(GetPerkExperiencesForTroops(DefaultPerks.Steward.DrillSergant));
		}
		if (troop.Character.Culture.IsBandit)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Roguery.NoRestForTheWicked, mobileParty, isPrimaryBonus: true, ref stat);
		}
		return stat;
	}

	private int GetPerkExperiencesForTroops(PerkObject perk)
	{
		if (perk == DefaultPerks.Leadership.CombatTips || perk == DefaultPerks.Leadership.RaiseTheMeek || perk == DefaultPerks.OneHanded.MilitaryTradition || perk == DefaultPerks.Crossbow.RenownMarksmen || perk == DefaultPerks.Steward.DrillSergant)
		{
			return MathF.Round(perk.PrimaryBonus);
		}
		if (perk == DefaultPerks.Polearm.Drills || perk == DefaultPerks.Athletics.WalkItOff || perk == DefaultPerks.Athletics.AGoodDaysRest || perk == DefaultPerks.Bow.Trainer || perk == DefaultPerks.Bow.BullsEye || perk == DefaultPerks.Throwing.Saddlebags)
		{
			return MathF.Round(perk.SecondaryBonus);
		}
		return 0;
	}

	public override int GenerateSharedXp(CharacterObject troop, int xp, MobileParty mobileParty)
	{
		ExplainedNumber stat = new ExplainedNumber(xp);
		PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.LeaderOfMasses, mobileParty, isPrimaryBonus: false, ref stat, mobileParty.IsCurrentlyAtSea);
		if (troop.IsRegular)
		{
			if (troop.IsMounted && !mobileParty.IsCurrentlyAtSea)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.LeadByExample, mobileParty, isPrimaryBonus: false, ref stat);
			}
			if (troop.IsRanged)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.MakeADifference, mobileParty, isPrimaryBonus: false, ref stat, mobileParty.IsCurrentlyAtSea);
			}
		}
		return (int)(stat.ResultNumber - (float)xp);
	}

	public override ExplainedNumber CalculateXpGainFromBattles(FlattenedTroopRosterElement troopRosterElement, PartyBase party)
	{
		ExplainedNumber stat = new ExplainedNumber(troopRosterElement.XpGained);
		if ((party.MapEvent.IsPlayerSimulation || !party.MapEvent.IsPlayerMapEvent) && party.IsMobile && !party.MobileParty.IsCurrentlyAtSea)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.TrustedCommander, party.MobileParty, isPrimaryBonus: false, ref stat);
		}
		return stat;
	}
}
