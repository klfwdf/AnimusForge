using System;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200013B RID: 315
	public class DefaultPartyTroopUpgradeModel : PartyTroopUpgradeModel
	{
		// Token: 0x0600194A RID: 6474 RVA: 0x0007D824 File Offset: 0x0007BA24
		public override bool CanPartyUpgradeTroopToTarget(PartyBase upgradingParty, CharacterObject upgradeableCharacter, CharacterObject upgradeTarget)
		{
			bool flag = Campaign.Current.Models.PartyTroopUpgradeModel.DoesPartyHaveRequiredItemsForUpgrade(upgradingParty, upgradeTarget);
			PerkObject perkObject;
			bool flag2 = Campaign.Current.Models.PartyTroopUpgradeModel.DoesPartyHaveRequiredPerksForUpgrade(upgradingParty, upgradeableCharacter, upgradeTarget, out perkObject);
			return Campaign.Current.Models.PartyTroopUpgradeModel.IsTroopUpgradeable(upgradingParty, upgradeableCharacter) && upgradeableCharacter.UpgradeTargets.Contains(upgradeTarget) && flag2 && flag;
		}

		// Token: 0x0600194B RID: 6475 RVA: 0x0007D88D File Offset: 0x0007BA8D
		public override bool IsTroopUpgradeable(PartyBase party, CharacterObject character)
		{
			return !character.IsHero && character.UpgradeTargets.Length != 0;
		}

		// Token: 0x0600194C RID: 6476 RVA: 0x0007D8A4 File Offset: 0x0007BAA4
		public override int GetXpCostForUpgrade(PartyBase party, CharacterObject characterObject, CharacterObject upgradeTarget)
		{
			if (upgradeTarget != null && characterObject.UpgradeTargets.Contains(upgradeTarget))
			{
				int tier = upgradeTarget.Tier;
				int num = 0;
				for (int i = characterObject.Tier + 1; i <= tier; i++)
				{
					if (i <= 1)
					{
						num += 100;
					}
					else if (i == 2)
					{
						num += 300;
					}
					else if (i == 3)
					{
						num += 550;
					}
					else if (i == 4)
					{
						num += 900;
					}
					else if (i == 5)
					{
						num += 1300;
					}
					else if (i == 6)
					{
						num += 1700;
					}
					else if (i == 7)
					{
						num += 2100;
					}
					else
					{
						int num2 = upgradeTarget.Level + 4;
						num += (int)(1.333f * (float)num2 * (float)num2);
					}
				}
				return num;
			}
			return 100000000;
		}

		// Token: 0x0600194D RID: 6477 RVA: 0x0007D964 File Offset: 0x0007BB64
		public override ExplainedNumber GetGoldCostForUpgrade(PartyBase party, CharacterObject characterObject, CharacterObject upgradeTarget)
		{
			PartyWageModel partyWageModel = Campaign.Current.Models.PartyWageModel;
			int roundedResultNumber = partyWageModel.GetTroopRecruitmentCost(upgradeTarget, null, true).RoundedResultNumber;
			int roundedResultNumber2 = partyWageModel.GetTroopRecruitmentCost(characterObject, null, true).RoundedResultNumber;
			bool flag = characterObject.Occupation == Occupation.Mercenary || characterObject.Occupation == Occupation.Gangster;
			ExplainedNumber result = new ExplainedNumber((float)(roundedResultNumber - roundedResultNumber2) / ((!flag) ? 2f : 3f), false, null);
			if (party.MobileParty.HasPerk(DefaultPerks.Steward.SoundReserves, false))
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Steward.SoundReserves, party.MobileParty, true, ref result, false);
			}
			if (characterObject.IsRanged && party.MobileParty.HasPerk(DefaultPerks.Bow.RenownedArcher, true))
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Bow.RenownedArcher, party.MobileParty, false, ref result, false);
			}
			if (characterObject.IsMounted && PartyBaseHelper.HasFeat(party, DefaultCulturalFeats.KhuzaitRecruitUpgradeFeat))
			{
				result.AddFactor(DefaultCulturalFeats.KhuzaitRecruitUpgradeFeat.EffectBonus, GameTexts.FindText("str_culture", null));
			}
			if (flag && party.MobileParty.HasPerk(DefaultPerks.Steward.Contractors, false))
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Steward.Contractors, party.MobileParty, true, ref result, false);
			}
			return result;
		}

		// Token: 0x0600194E RID: 6478 RVA: 0x0007DA8B File Offset: 0x0007BC8B
		public override int GetSkillXpFromUpgradingTroops(PartyBase party, CharacterObject troop, int numberOfTroops)
		{
			return (troop.Level + 10) * numberOfTroops;
		}

		// Token: 0x0600194F RID: 6479 RVA: 0x0007DA98 File Offset: 0x0007BC98
		public override bool DoesPartyHaveRequiredItemsForUpgrade(PartyBase party, CharacterObject upgradeTarget)
		{
			ItemCategory upgradeRequiresItemFromCategory = upgradeTarget.UpgradeRequiresItemFromCategory;
			if (upgradeRequiresItemFromCategory != null)
			{
				int num = 0;
				for (int i = 0; i < party.ItemRoster.Count; i++)
				{
					ItemRosterElement itemRosterElement = party.ItemRoster[i];
					if (itemRosterElement.EquipmentElement.Item.ItemCategory == upgradeRequiresItemFromCategory)
					{
						num += itemRosterElement.Amount;
					}
				}
				return num > 0;
			}
			return true;
		}

		// Token: 0x06001950 RID: 6480 RVA: 0x0007DAFC File Offset: 0x0007BCFC
		public override bool DoesPartyHaveRequiredPerksForUpgrade(PartyBase party, CharacterObject character, CharacterObject upgradeTarget, out PerkObject requiredPerk)
		{
			requiredPerk = null;
			if (character.Culture.IsBandit && !upgradeTarget.Culture.IsBandit)
			{
				requiredPerk = DefaultPerks.Leadership.VeteransRespect;
				return party.MobileParty.HasPerk(requiredPerk, true);
			}
			return true;
		}

		// Token: 0x06001951 RID: 6481 RVA: 0x0007DB38 File Offset: 0x0007BD38
		public override float GetUpgradeChanceForTroopUpgrade(PartyBase party, CharacterObject troop, int upgradeTargetIndex)
		{
			float result = 1f;
			int num = troop.UpgradeTargets.Length;
			if (num > 1 && upgradeTargetIndex >= 0 && upgradeTargetIndex < num)
			{
				if (party.LeaderHero != null && party.LeaderHero.PreferredUpgradeFormation != FormationClass.NumberOfAllFormations)
				{
					FormationClass preferredUpgradeFormation = party.LeaderHero.PreferredUpgradeFormation;
					if (CharacterHelper.SearchForFormationInTroopTree(troop.UpgradeTargets[upgradeTargetIndex], preferredUpgradeFormation))
					{
						result = 9999f;
					}
				}
				else
				{
					Hero leaderHero = party.LeaderHero;
					int num2 = ((leaderHero != null) ? leaderHero.RandomValue : party.Id.GetHashCode());
					int deterministicHashCode = troop.StringId.GetDeterministicHashCode();
					uint num3 = (uint)((num2 >> ((troop.Tier * 3) & 31)) ^ deterministicHashCode);
					if ((long)upgradeTargetIndex == (long)((ulong)num3 % (ulong)((long)num)))
					{
						result = 9999f;
					}
				}
			}
			return result;
		}
	}
}
