using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultCombatXpModel : CombatXpModel
{
	public override float CaptainRadius => 10f;

	public override SkillObject GetSkillForWeapon(WeaponComponentData weapon, bool isSiegeEngineHit)
	{
		SkillObject result = DefaultSkills.Athletics;
		if (isSiegeEngineHit)
		{
			result = DefaultSkills.Engineering;
		}
		else if (weapon != null)
		{
			result = weapon.RelevantSkill;
		}
		return result;
	}

	public override ExplainedNumber GetXpFromHit(CharacterObject attackerTroop, CharacterObject captain, CharacterObject attackedTroop, PartyBase attackerParty, int damage, bool isFatal, MissionTypeEnum missionType)
	{
		int num = attackedTroop.MaxHitPoints();
		float leaderModifier = 0f;
		BattleSideEnum side = BattleSideEnum.Attacker;
		MapEvent.PowerCalculationContext context = MapEvent.PowerCalculationContext.PlainBattle;
		if (attackerParty?.MapEvent != null)
		{
			leaderModifier = attackerParty.MapEventSide.LeaderSimulationModifier;
			side = attackerParty.Side;
			context = attackerParty.MapEvent.SimulationContext;
		}
		float troopPower = Campaign.Current.Models.MilitaryPowerModel.GetTroopPower(attackedTroop, side.GetOppositeSide(), context, leaderModifier);
		float num2 = Campaign.Current.Models.MilitaryPowerModel.GetTroopPower(attackerTroop, side, context, leaderModifier) + 0.5f;
		float num3 = troopPower + 0.5f;
		int num4 = MathF.Min(damage, num) + (isFatal ? num : 0);
		float num5 = 0.4f * num2 * num3 * (float)num4;
		num5 *= GetXpfMultiplierForMissionType(missionType);
		ExplainedNumber xpToGain = new ExplainedNumber(num5);
		if (attackerParty != null)
		{
			GetBattleXpBonusFromPerks(attackerParty, ref xpToGain, attackerTroop);
		}
		bool flag = attackerParty == null || !attackerParty.IsMobile || attackerParty.MobileParty.IsCurrentlyAtSea;
		if (captain != null && captain.IsHero && !flag)
		{
			BattleEnvironment battleEnvironment = ((attackerParty != null && attackerParty.MobileParty != null) ? attackerParty.MobileParty.CurrentBattleEnvironment : BattleEnvironment.Any);
			PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Leadership.InspiringLeader, battleEnvironment, captain, ref xpToGain);
		}
		return xpToGain;
	}

	private static float GetXpfMultiplierForMissionType(MissionTypeEnum missionType)
	{
		return missionType switch
		{
			MissionTypeEnum.NoXp => 0f, 
			MissionTypeEnum.PracticeFight => 0.0625f, 
			MissionTypeEnum.Tournament => 0.33f, 
			MissionTypeEnum.SimulationBattle => 0.9f, 
			MissionTypeEnum.Battle => 1f, 
			_ => 1f, 
		};
	}

	public override float GetXpMultiplierFromShotDifficulty(float shotDifficulty)
	{
		if (shotDifficulty > 14.4f)
		{
			shotDifficulty = 14.4f;
		}
		return MBMath.Lerp(0f, 2f, (shotDifficulty - 1f) / 13.4f);
	}

	private static void GetBattleXpBonusFromPerks(PartyBase party, ref ExplainedNumber xpToGain, CharacterObject troop)
	{
		if (party.IsMobile && party.MobileParty.LeaderHero != null)
		{
			if (!troop.IsRanged)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.OneHanded.Trainer, party.MobileParty, isPrimaryBonus: false, ref xpToGain);
				PerkHelper.AddPerkBonusForParty(DefaultPerks.TwoHanded.BaptisedInBlood, party.MobileParty, isPrimaryBonus: false, ref xpToGain);
			}
			if (troop.HasThrowingWeapon())
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Throwing.Resourceful, party.MobileParty, isPrimaryBonus: false, ref xpToGain);
			}
			if (troop.IsInfantry)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.OneHanded.CorpsACorps, party.MobileParty, isPrimaryBonus: true, ref xpToGain);
			}
			PerkHelper.AddPerkBonusForParty(DefaultPerks.OneHanded.LeadByExample, party.MobileParty, isPrimaryBonus: true, ref xpToGain);
			if (troop.IsRanged)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Crossbow.MountedCrossbowman, party.MobileParty, isPrimaryBonus: false, ref xpToGain);
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Bow.BullsEye, party.MobileParty, isPrimaryBonus: true, ref xpToGain);
			}
			if (troop.Culture.IsBandit)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Roguery.NoRestForTheWicked, party.MobileParty, isPrimaryBonus: true, ref xpToGain);
			}
		}
		if (party.IsMobile && party.MobileParty.IsGarrison && party.MobileParty.CurrentSettlement?.Town.Governor != null)
		{
			PerkHelper.AddPerkBonusForTown(DefaultPerks.TwoHanded.ProjectileDeflection, party.MobileParty.CurrentSettlement.Town, isPrimaryBonus: false, ref xpToGain);
			if (troop.IsMounted)
			{
				PerkHelper.AddPerkBonusForTown(DefaultPerks.Polearm.Guards, party.MobileParty.CurrentSettlement.Town, isPrimaryBonus: false, ref xpToGain);
			}
		}
	}
}
