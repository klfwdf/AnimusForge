using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200014F RID: 335
	public class DefaultSettlementSecurityModel : SettlementSecurityModel
	{
		// Token: 0x170006B3 RID: 1715
		// (get) Token: 0x060019F7 RID: 6647 RVA: 0x00082989 File Offset: 0x00080B89
		public override int MaximumSecurityInSettlement
		{
			get
			{
				return 100;
			}
		}

		// Token: 0x170006B4 RID: 1716
		// (get) Token: 0x060019F8 RID: 6648 RVA: 0x0008298D File Offset: 0x00080B8D
		public override int SecurityDriftMedium
		{
			get
			{
				return 50;
			}
		}

		// Token: 0x170006B5 RID: 1717
		// (get) Token: 0x060019F9 RID: 6649 RVA: 0x00082991 File Offset: 0x00080B91
		public override float MapEventSecurityEffectRadius
		{
			get
			{
				return 50f;
			}
		}

		// Token: 0x170006B6 RID: 1718
		// (get) Token: 0x060019FA RID: 6650 RVA: 0x00082998 File Offset: 0x00080B98
		public override float HideoutClearedSecurityEffectRadius
		{
			get
			{
				return 100f;
			}
		}

		// Token: 0x170006B7 RID: 1719
		// (get) Token: 0x060019FB RID: 6651 RVA: 0x0008299F File Offset: 0x00080B9F
		public override int HideoutClearedSecurityGain
		{
			get
			{
				return 6;
			}
		}

		// Token: 0x170006B8 RID: 1720
		// (get) Token: 0x060019FC RID: 6652 RVA: 0x000829A2 File Offset: 0x00080BA2
		public override int ThresholdForTaxCorruption
		{
			get
			{
				return 50;
			}
		}

		// Token: 0x170006B9 RID: 1721
		// (get) Token: 0x060019FD RID: 6653 RVA: 0x000829A6 File Offset: 0x00080BA6
		public override int ThresholdForHigherTaxCorruption
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x170006BA RID: 1722
		// (get) Token: 0x060019FE RID: 6654 RVA: 0x000829A9 File Offset: 0x00080BA9
		public override int ThresholdForTaxBoost
		{
			get
			{
				return 75;
			}
		}

		// Token: 0x170006BB RID: 1723
		// (get) Token: 0x060019FF RID: 6655 RVA: 0x000829AD File Offset: 0x00080BAD
		public override int SettlementTaxBoostPercentage
		{
			get
			{
				return 5;
			}
		}

		// Token: 0x170006BC RID: 1724
		// (get) Token: 0x06001A00 RID: 6656 RVA: 0x000829B0 File Offset: 0x00080BB0
		public override int SettlementTaxPenaltyPercentage
		{
			get
			{
				return 10;
			}
		}

		// Token: 0x170006BD RID: 1725
		// (get) Token: 0x06001A01 RID: 6657 RVA: 0x000829B4 File Offset: 0x00080BB4
		public override int ThresholdForNotableRelationBonus
		{
			get
			{
				return 75;
			}
		}

		// Token: 0x170006BE RID: 1726
		// (get) Token: 0x06001A02 RID: 6658 RVA: 0x000829B8 File Offset: 0x00080BB8
		public override int ThresholdForNotableRelationPenalty
		{
			get
			{
				return 50;
			}
		}

		// Token: 0x170006BF RID: 1727
		// (get) Token: 0x06001A03 RID: 6659 RVA: 0x000829BC File Offset: 0x00080BBC
		public override int DailyNotableRelationBonus
		{
			get
			{
				return 1;
			}
		}

		// Token: 0x170006C0 RID: 1728
		// (get) Token: 0x06001A04 RID: 6660 RVA: 0x000829BF File Offset: 0x00080BBF
		public override int DailyNotableRelationPenalty
		{
			get
			{
				return -1;
			}
		}

		// Token: 0x170006C1 RID: 1729
		// (get) Token: 0x06001A05 RID: 6661 RVA: 0x000829C2 File Offset: 0x00080BC2
		public override int DailyNotablePowerBonus
		{
			get
			{
				return 1;
			}
		}

		// Token: 0x170006C2 RID: 1730
		// (get) Token: 0x06001A06 RID: 6662 RVA: 0x000829C5 File Offset: 0x00080BC5
		public override int DailyNotablePowerPenalty
		{
			get
			{
				return -1;
			}
		}

		// Token: 0x06001A07 RID: 6663 RVA: 0x000829C8 File Offset: 0x00080BC8
		public override ExplainedNumber CalculateSecurityChange(Town town, bool includeDescriptions = false)
		{
			ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions, null);
			this.CalculateInfestedHideoutEffectsOnSecurity(town, ref result);
			this.CalculateRaidedVillageEffectsOnSecurity(town, ref result);
			this.CalculateUnderSiegeEffectsOnSecurity(town, ref result);
			this.CalculateProsperityEffectOnSecurity(town, ref result);
			this.CalculateGarrisonEffectsOnSecurity(town, ref result);
			this.CalculatePolicyEffectsOnSecurity(town, ref result);
			this.CalculateGovernorEffectsOnSecurity(town, ref result);
			this.CalculateProjectEffectsOnSecurity(town, ref result);
			this.CalculateIssueEffectsOnSecurity(town, ref result);
			this.CalculatePerkEffectsOnSecurity(town, ref result);
			this.CalculateSecurityDrift(town, ref result);
			this.CalculateSettlementProjectSecurityBonuses(town, ref result);
			this.CalculateSettlementPatrolPartiesBonuses(town, ref result);
			return result;
		}

		// Token: 0x06001A08 RID: 6664 RVA: 0x00082A5C File Offset: 0x00080C5C
		private void CalculateSettlementPatrolPartiesBonuses(Town town, ref ExplainedNumber result)
		{
			if (town.Settlement.PatrolParty != null)
			{
				foreach (Building building in town.Buildings)
				{
					if (building.BuildingType == DefaultBuildingTypes.SettlementGuardHouse && building.CurrentLevel > 0)
					{
						result.Add((float)building.CurrentLevel * 0.5f + 0.5f, this.PatrolPartiesText, null);
						break;
					}
				}
			}
		}

		// Token: 0x06001A09 RID: 6665 RVA: 0x00082AF0 File Offset: 0x00080CF0
		private void CalculateSettlementProjectSecurityBonuses(Town town, ref ExplainedNumber result)
		{
			town.AddEffectOfBuildings(BuildingEffectEnum.SecurityPerDay, ref result);
		}

		// Token: 0x06001A0A RID: 6666 RVA: 0x00082AFB File Offset: 0x00080CFB
		private void CalculateProsperityEffectOnSecurity(Town town, ref ExplainedNumber explainedNumber)
		{
			explainedNumber.Add(MathF.Max(-5f, -0.0005f * town.Prosperity), this.ProsperityText, null);
		}

		// Token: 0x06001A0B RID: 6667 RVA: 0x00082B20 File Offset: 0x00080D20
		private void CalculateUnderSiegeEffectsOnSecurity(Town town, ref ExplainedNumber explainedNumber)
		{
			if (town.Settlement.IsUnderSiege)
			{
				explainedNumber.Add(-3f, this.UnderSiegeText, null);
			}
		}

		// Token: 0x06001A0C RID: 6668 RVA: 0x00082B44 File Offset: 0x00080D44
		private void CalculateRaidedVillageEffectsOnSecurity(Town town, ref ExplainedNumber explainedNumber)
		{
			float num = 0f;
			using (List<Village>.Enumerator enumerator = town.Settlement.BoundVillages.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.VillageState == Village.VillageStates.Looted)
					{
						num += -2f;
						break;
					}
				}
			}
			explainedNumber.Add(num, this.LootedVillagesText, null);
		}

		// Token: 0x06001A0D RID: 6669 RVA: 0x00082BBC File Offset: 0x00080DBC
		private void CalculateInfestedHideoutEffectsOnSecurity(Town town, ref ExplainedNumber explainedNumber)
		{
			float num = Campaign.Current.EstimatedAverageBanditPartySpeed * (float)CampaignTime.HoursInDay * 0.5f;
			foreach (Hideout hideout in Hideout.All)
			{
				if (hideout.IsInfested && Campaign.Current.Models.MapDistanceModel.GetDistance(town.Settlement, hideout.Settlement, false, false, MobileParty.NavigationType.Default) < num)
				{
					explainedNumber.Add(-2f, this.NearbyHideoutText, null);
					break;
				}
			}
		}

		// Token: 0x06001A0E RID: 6670 RVA: 0x00082C64 File Offset: 0x00080E64
		private void CalculateSecurityDrift(Town town, ref ExplainedNumber explainedNumber)
		{
			explainedNumber.Add(-1f * (town.Security - (float)this.SecurityDriftMedium) / 15f, this.SecurityDriftText, null);
		}

		// Token: 0x06001A0F RID: 6671 RVA: 0x00082C90 File Offset: 0x00080E90
		private void CalculatePolicyEffectsOnSecurity(Town town, ref ExplainedNumber explainedNumber)
		{
			Kingdom kingdom = town.Settlement.OwnerClan.Kingdom;
			if (kingdom != null)
			{
				if (town.IsTown)
				{
					if (kingdom.ActivePolicies.Contains(DefaultPolicies.Bailiffs))
					{
						explainedNumber.Add(1f, DefaultPolicies.Bailiffs.Name, null);
					}
					if (kingdom.ActivePolicies.Contains(DefaultPolicies.Serfdom))
					{
						explainedNumber.Add(1f, DefaultPolicies.Serfdom.Name, null);
					}
					if (kingdom.ActivePolicies.Contains(DefaultPolicies.Magistrates))
					{
						explainedNumber.Add(1f, DefaultPolicies.Magistrates.Name, null);
					}
				}
				if (kingdom.ActivePolicies.Contains(DefaultPolicies.TrialByJury))
				{
					explainedNumber.Add(-0.2f, DefaultPolicies.TrialByJury.Name, null);
				}
			}
		}

		// Token: 0x06001A10 RID: 6672 RVA: 0x00082D5C File Offset: 0x00080F5C
		private void CalculateGovernorEffectsOnSecurity(Town town, ref ExplainedNumber explainedNumber)
		{
		}

		// Token: 0x06001A11 RID: 6673 RVA: 0x00082D60 File Offset: 0x00080F60
		private void CalculateGarrisonEffectsOnSecurity(Town town, ref ExplainedNumber result)
		{
			if (town.GarrisonParty != null && town.GarrisonParty.MemberRoster.Count != 0 && town.GarrisonParty.MemberRoster.TotalHealthyCount != 0)
			{
				ExplainedNumber explainedNumber = new ExplainedNumber(0.01f, false, null);
				PerkHelper.AddPerkBonusForTown(DefaultPerks.OneHanded.StandUnited, town, ref explainedNumber);
				float num;
				float num2;
				float num3;
				this.CalculateStrengthOfGarrisonParty(town.GarrisonParty.Party, out num, out num2, out num3);
				float num4 = num * explainedNumber.ResultNumber;
				result.Add(num4, this.GarrisonText, null);
				if (PerkHelper.GetPerkValueForTown(DefaultPerks.Leadership.Authority, town))
				{
					result.Add(num4 * DefaultPerks.Leadership.Authority.PrimaryBonus, DefaultPerks.Leadership.Authority.Name, null);
				}
				if (PerkHelper.GetPerkValueForTown(DefaultPerks.Riding.ReliefForce, town))
				{
					float num5 = num3 / num;
					result.Add(num4 * num5 * DefaultPerks.Riding.ReliefForce.SecondaryBonus, DefaultPerks.Riding.ReliefForce.Name, null);
				}
				float num6 = num2 / num;
				if (PerkHelper.GetPerkValueForTown(DefaultPerks.Bow.MountedArchery, town))
				{
					result.Add(num4 * num6 * DefaultPerks.Bow.MountedArchery.SecondaryBonus, DefaultPerks.Bow.MountedArchery.Name, null);
				}
				if (PerkHelper.GetPerkValueForTown(DefaultPerks.Bow.RangersSwiftness, town))
				{
					result.Add(num4 * num6 * DefaultPerks.Bow.RangersSwiftness.SecondaryBonus, DefaultPerks.Bow.RangersSwiftness.Name, null);
				}
				if (PerkHelper.GetPerkValueForTown(DefaultPerks.Crossbow.RenownMarksmen, town))
				{
					result.Add(num4 * num6 * DefaultPerks.Crossbow.RenownMarksmen.SecondaryBonus, DefaultPerks.Crossbow.RenownMarksmen.Name, null);
				}
			}
		}

		// Token: 0x06001A12 RID: 6674 RVA: 0x00082EDC File Offset: 0x000810DC
		private void CalculateStrengthOfGarrisonParty(PartyBase party, out float totalStrength, out float archerStrength, out float cavalryStrength)
		{
			totalStrength = 0f;
			archerStrength = 0f;
			cavalryStrength = 0f;
			float leaderModifier = 0f;
			MapEvent.PowerCalculationContext context = MapEvent.PowerCalculationContext.Siege;
			BattleSideEnum side = BattleSideEnum.Defender;
			if (party.MapEvent != null)
			{
				side = party.Side;
				Hero leaderHero = party.LeaderHero;
				leaderModifier = ((leaderHero != null) ? leaderHero.PowerModifier : 0f);
				context = party.MapEvent.SimulationContext;
			}
			for (int i = 0; i < party.MemberRoster.Count; i++)
			{
				TroopRosterElement elementCopyAtIndex = party.MemberRoster.GetElementCopyAtIndex(i);
				if (elementCopyAtIndex.Character != null)
				{
					float troopPower = Campaign.Current.Models.MilitaryPowerModel.GetTroopPower(elementCopyAtIndex.Character, side, context, leaderModifier);
					float num = (float)(elementCopyAtIndex.Number - elementCopyAtIndex.WoundedNumber) * troopPower;
					if (elementCopyAtIndex.Character.IsMounted)
					{
						cavalryStrength += num;
					}
					if (elementCopyAtIndex.Character.IsRanged)
					{
						archerStrength += num;
					}
					totalStrength += num;
				}
			}
		}

		// Token: 0x06001A13 RID: 6675 RVA: 0x00082FD8 File Offset: 0x000811D8
		private void CalculatePerkEffectsOnSecurity(Town town, ref ExplainedNumber result)
		{
			float num = (float)town.Settlement.Parties.Where(delegate(MobileParty x)
			{
				Clan actualClan = x.ActualClan;
				if (actualClan != null && !actualClan.IsAtWarWith(town.MapFaction))
				{
					Hero leaderHero = x.LeaderHero;
					return leaderHero != null && leaderHero.GetPerkValue(DefaultPerks.Leadership.Presence);
				}
				return false;
			}).Count<MobileParty>() * DefaultPerks.Leadership.Presence.PrimaryBonus;
			if (num > 0f)
			{
				result.Add(num, DefaultPerks.Leadership.Presence.Name, null);
			}
			if (town.Governor != null && town.Governor.GetPerkValue(DefaultPerks.Roguery.KnowHow))
			{
				PerkHelper.AddPerkBonusForTown(DefaultPerks.Roguery.KnowHow, town, ref result);
			}
			PerkHelper.AddPerkBonusForTown(DefaultPerks.OneHanded.ToBeBlunt, town, ref result);
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Throwing.Focus, town, ref result);
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Polearm.Skewer, town, ref result);
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Tactics.Gensdarmes, town, ref result);
		}

		// Token: 0x06001A14 RID: 6676 RVA: 0x000830B8 File Offset: 0x000812B8
		private void CalculateProjectEffectsOnSecurity(Town town, ref ExplainedNumber explainedNumber)
		{
		}

		// Token: 0x06001A15 RID: 6677 RVA: 0x000830BA File Offset: 0x000812BA
		private void CalculateIssueEffectsOnSecurity(Town town, ref ExplainedNumber explainedNumber)
		{
			Campaign.Current.Models.IssueModel.GetIssueEffectsOfSettlement(DefaultIssueEffects.SettlementSecurity, town.Settlement, ref explainedNumber);
		}

		// Token: 0x06001A16 RID: 6678 RVA: 0x000830DC File Offset: 0x000812DC
		public override float GetLootedNearbyPartySecurityEffect(Town town, float sumOfAttackedPartyStrengths)
		{
			return -1f * sumOfAttackedPartyStrengths * 0.005f;
		}

		// Token: 0x06001A17 RID: 6679 RVA: 0x000830EB File Offset: 0x000812EB
		public override float GetNearbyBanditPartyDefeatedSecurityEffect(Town town, float sumOfAttackedPartyStrengths)
		{
			return sumOfAttackedPartyStrengths * 0.005f;
		}

		// Token: 0x06001A18 RID: 6680 RVA: 0x000830F4 File Offset: 0x000812F4
		public override void CalculateGoldGainDueToHighSecurity(Town town, ref ExplainedNumber explainedNumber)
		{
			float num = MBMath.Map(town.Security, (float)this.ThresholdForTaxBoost, (float)this.MaximumSecurityInSettlement, 0f, (float)this.SettlementTaxBoostPercentage);
			explainedNumber.AddFactor(num * 0.01f, this.Security);
		}

		// Token: 0x06001A19 RID: 6681 RVA: 0x0008313C File Offset: 0x0008133C
		public override void CalculateGoldCutDueToLowSecurity(Town town, ref ExplainedNumber explainedNumber)
		{
			float num = MBMath.Map(town.Security, (float)this.ThresholdForHigherTaxCorruption, (float)this.ThresholdForTaxCorruption, (float)this.SettlementTaxPenaltyPercentage, 0f);
			explainedNumber.AddFactor(-1f * num * 0.01f, this.CorruptionText);
		}

		// Token: 0x040008A3 RID: 2211
		private const float GarrisonHighSecurityGain = 3f;

		// Token: 0x040008A4 RID: 2212
		private const float GarrisonLowSecurityPenalty = -3f;

		// Token: 0x040008A5 RID: 2213
		private const float NearbyHideoutPenalty = -2f;

		// Token: 0x040008A6 RID: 2214
		private const float VillageLootedSecurityEffect = -2f;

		// Token: 0x040008A7 RID: 2215
		private const float UnderSiegeSecurityEffect = -3f;

		// Token: 0x040008A8 RID: 2216
		private const float MaxProsperityEffect = -5f;

		// Token: 0x040008A9 RID: 2217
		private const float PerProsperityEffect = -0.0005f;

		// Token: 0x040008AA RID: 2218
		private readonly TextObject GarrisonText = GameTexts.FindText("str_garrison", null);

		// Token: 0x040008AB RID: 2219
		private readonly TextObject LootedVillagesText = GameTexts.FindText("str_looted_villages", null);

		// Token: 0x040008AC RID: 2220
		private readonly TextObject CorruptionText = GameTexts.FindText("str_corruption", null);

		// Token: 0x040008AD RID: 2221
		private readonly TextObject NearbyHideoutText = GameTexts.FindText("str_nearby_hideout", null);

		// Token: 0x040008AE RID: 2222
		private readonly TextObject UnderSiegeText = GameTexts.FindText("str_under_siege", null);

		// Token: 0x040008AF RID: 2223
		private readonly TextObject ProsperityText = GameTexts.FindText("str_prosperity", null);

		// Token: 0x040008B0 RID: 2224
		private readonly TextObject Security = GameTexts.FindText("str_security", null);

		// Token: 0x040008B1 RID: 2225
		private readonly TextObject SecurityDriftText = GameTexts.FindText("str_security_drift", null);

		// Token: 0x040008B2 RID: 2226
		private readonly TextObject PatrolPartiesText = GameTexts.FindText("str_patrol_parties", null);
	}
}
