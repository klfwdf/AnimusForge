using System;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000106 RID: 262
	public class DefaultCombatXpModel : CombatXpModel
	{
		// Token: 0x0600171C RID: 5916 RVA: 0x0006BA04 File Offset: 0x00069C04
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

		// Token: 0x0600171D RID: 5917 RVA: 0x0006BA30 File Offset: 0x00069C30
		public override ExplainedNumber GetXpFromHit(CharacterObject attackerTroop, CharacterObject captain, CharacterObject attackedTroop, PartyBase attackerParty, int damage, bool isFatal, CombatXpModel.MissionTypeEnum missionType)
		{
			int num = attackedTroop.MaxHitPoints();
			float leaderModifier = 0f;
			BattleSideEnum side = BattleSideEnum.Attacker;
			MapEvent.PowerCalculationContext context = MapEvent.PowerCalculationContext.PlainBattle;
			if (((attackerParty != null) ? attackerParty.MapEvent : null) != null)
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
			num5 *= DefaultCombatXpModel.GetXpfMultiplierForMissionType(missionType);
			ExplainedNumber result = new ExplainedNumber(num5, false, null);
			if (attackerParty != null)
			{
				DefaultCombatXpModel.GetBattleXpBonusFromPerks(attackerParty, ref result, attackerTroop);
			}
			bool flag = attackerParty == null || !attackerParty.IsMobile || attackerParty.MobileParty.IsCurrentlyAtSea;
			if (captain != null && captain.IsHero && !flag && captain.GetPerkValue(DefaultPerks.Leadership.InspiringLeader))
			{
				result.AddFactor(DefaultPerks.Leadership.InspiringLeader.SecondaryBonus, DefaultPerks.Leadership.InspiringLeader.Name);
			}
			return result;
		}

		// Token: 0x0600171E RID: 5918 RVA: 0x0006BB68 File Offset: 0x00069D68
		private static float GetXpfMultiplierForMissionType(CombatXpModel.MissionTypeEnum missionType)
		{
			float result;
			if (missionType == CombatXpModel.MissionTypeEnum.NoXp)
			{
				result = 0f;
			}
			else if (missionType == CombatXpModel.MissionTypeEnum.PracticeFight)
			{
				result = 0.0625f;
			}
			else if (missionType == CombatXpModel.MissionTypeEnum.Tournament)
			{
				result = 0.33f;
			}
			else if (missionType == CombatXpModel.MissionTypeEnum.SimulationBattle)
			{
				result = 0.9f;
			}
			else if (missionType == CombatXpModel.MissionTypeEnum.Battle)
			{
				result = 1f;
			}
			else
			{
				result = 1f;
			}
			return result;
		}

		// Token: 0x0600171F RID: 5919 RVA: 0x0006BBB7 File Offset: 0x00069DB7
		public override float GetXpMultiplierFromShotDifficulty(float shotDifficulty)
		{
			if (shotDifficulty > 14.4f)
			{
				shotDifficulty = 14.4f;
			}
			return MBMath.Lerp(0f, 2f, (shotDifficulty - 1f) / 13.4f, 1E-05f);
		}

		// Token: 0x17000642 RID: 1602
		// (get) Token: 0x06001720 RID: 5920 RVA: 0x0006BBE9 File Offset: 0x00069DE9
		public override float CaptainRadius
		{
			get
			{
				return 10f;
			}
		}

		// Token: 0x06001721 RID: 5921 RVA: 0x0006BBF0 File Offset: 0x00069DF0
		private static void GetBattleXpBonusFromPerks(PartyBase party, ref ExplainedNumber xpToGain, CharacterObject troop)
		{
			if (party.IsMobile && party.MobileParty.LeaderHero != null)
			{
				if (!troop.IsRanged)
				{
					if (!party.MobileParty.IsCurrentlyAtSea && party.MobileParty.HasPerk(DefaultPerks.OneHanded.Trainer, true))
					{
						xpToGain.AddFactor(DefaultPerks.OneHanded.Trainer.SecondaryBonus, DefaultPerks.OneHanded.Trainer.Name);
					}
					PerkHelper.AddPerkBonusForParty(DefaultPerks.TwoHanded.BaptisedInBlood, party.MobileParty, false, ref xpToGain, party.MobileParty.IsCurrentlyAtSea);
				}
				if (troop.HasThrowingWeapon() && party.MobileParty.HasPerk(DefaultPerks.Throwing.Resourceful, true))
				{
					xpToGain.AddFactor(DefaultPerks.Throwing.Resourceful.SecondaryBonus, DefaultPerks.Throwing.Resourceful.Name);
				}
				if (troop.IsInfantry)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.OneHanded.CorpsACorps, party.MobileParty, true, ref xpToGain, party.MobileParty.IsCurrentlyAtSea);
				}
				PerkHelper.AddPerkBonusForParty(DefaultPerks.OneHanded.LeadByExample, party.MobileParty, true, ref xpToGain, party.MobileParty.IsCurrentlyAtSea);
				if (troop.IsRanged)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Crossbow.MountedCrossbowman, party.MobileParty, false, ref xpToGain, party.MobileParty.IsCurrentlyAtSea);
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Bow.BullsEye, party.MobileParty, true, ref xpToGain, party.MobileParty.IsCurrentlyAtSea);
				}
				if (troop.Culture.IsBandit && party.MobileParty.HasPerk(DefaultPerks.Roguery.NoRestForTheWicked, false))
				{
					xpToGain.AddFactor(DefaultPerks.Roguery.NoRestForTheWicked.PrimaryBonus, DefaultPerks.Roguery.NoRestForTheWicked.Name);
				}
			}
			if (party.IsMobile && party.MobileParty.IsGarrison)
			{
				Settlement currentSettlement = party.MobileParty.CurrentSettlement;
				if (((currentSettlement != null) ? currentSettlement.Town.Governor : null) != null)
				{
					PerkHelper.AddPerkBonusForTown(DefaultPerks.TwoHanded.ProjectileDeflection, party.MobileParty.CurrentSettlement.Town, ref xpToGain);
					if (troop.IsMounted)
					{
						PerkHelper.AddPerkBonusForTown(DefaultPerks.Polearm.Guards, party.MobileParty.CurrentSettlement.Town, ref xpToGain);
					}
				}
			}
		}
	}
}
