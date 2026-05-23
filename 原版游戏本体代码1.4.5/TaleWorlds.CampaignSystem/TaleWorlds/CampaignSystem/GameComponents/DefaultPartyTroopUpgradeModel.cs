using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultPartyTroopUpgradeModel : PartyTroopUpgradeModel
{
	public override bool CanPartyUpgradeTroopToTarget(PartyBase upgradingParty, CharacterObject upgradeableCharacter, CharacterObject upgradeTarget)
	{
		bool flag = Campaign.Current.Models.PartyTroopUpgradeModel.DoesPartyHaveRequiredItemsForUpgrade(upgradingParty, upgradeTarget);
		PerkObject requiredPerk;
		bool flag2 = Campaign.Current.Models.PartyTroopUpgradeModel.DoesPartyHaveRequiredPerksForUpgrade(upgradingParty, upgradeableCharacter, upgradeTarget, out requiredPerk);
		return Campaign.Current.Models.PartyTroopUpgradeModel.IsTroopUpgradeable(upgradingParty, upgradeableCharacter) && upgradeableCharacter.UpgradeTargets.Contains(upgradeTarget) && flag2 && flag;
	}

	public override bool IsTroopUpgradeable(PartyBase party, CharacterObject character)
	{
		if (!character.IsHero)
		{
			return character.UpgradeTargets.Length != 0;
		}
		return false;
	}

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
					continue;
				}
				switch (i)
				{
				case 2:
					num += 300;
					break;
				case 3:
					num += 550;
					break;
				case 4:
					num += 900;
					break;
				case 5:
					num += 1300;
					break;
				case 6:
					num += 1700;
					break;
				case 7:
					num += 2100;
					break;
				default:
				{
					int num2 = upgradeTarget.Level + 4;
					num += (int)(1.333f * (float)num2 * (float)num2);
					break;
				}
				}
			}
			return num;
		}
		return 100000000;
	}

	public override ExplainedNumber GetGoldCostForUpgrade(PartyBase party, CharacterObject characterObject, CharacterObject upgradeTarget)
	{
		PartyWageModel partyWageModel = Campaign.Current.Models.PartyWageModel;
		int roundedResultNumber = partyWageModel.GetTroopRecruitmentCost(upgradeTarget, null, withoutItemCost: true).RoundedResultNumber;
		int roundedResultNumber2 = partyWageModel.GetTroopRecruitmentCost(characterObject, null, withoutItemCost: true).RoundedResultNumber;
		bool flag = characterObject.Occupation == Occupation.Mercenary || characterObject.Occupation == Occupation.Gangster;
		ExplainedNumber stat = new ExplainedNumber((float)(roundedResultNumber - roundedResultNumber2) / ((!flag) ? 2f : 3f));
		if (party.MobileParty.HasPerk(DefaultPerks.Steward.SoundReserves))
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Steward.SoundReserves, party.MobileParty, isPrimaryBonus: true, ref stat);
		}
		if (characterObject.IsRanged && party.MobileParty.HasPerk(DefaultPerks.Bow.RenownedArcher, checkSecondaryRole: true))
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Bow.RenownedArcher, party.MobileParty, isPrimaryBonus: false, ref stat);
		}
		if (characterObject.IsMounted && PartyBaseHelper.HasFeat(party, DefaultCulturalFeats.KhuzaitRecruitUpgradeFeat))
		{
			stat.AddFactor(DefaultCulturalFeats.KhuzaitRecruitUpgradeFeat.EffectBonus, GameTexts.FindText("str_culture"));
		}
		if (flag && party.MobileParty.HasPerk(DefaultPerks.Steward.Contractors))
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Steward.Contractors, party.MobileParty, isPrimaryBonus: true, ref stat);
		}
		return stat;
	}

	public override int GetSkillXpFromUpgradingTroops(PartyBase party, CharacterObject troop, int numberOfTroops)
	{
		return (troop.Level + 10) * numberOfTroops;
	}

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

	public override bool DoesPartyHaveRequiredPerksForUpgrade(PartyBase party, CharacterObject character, CharacterObject upgradeTarget, out PerkObject requiredPerk)
	{
		requiredPerk = null;
		if (character.Culture.IsBandit && !upgradeTarget.Culture.IsBandit)
		{
			requiredPerk = DefaultPerks.Leadership.VeteransRespect;
			return party.MobileParty.HasPerk(requiredPerk, checkSecondaryRole: true);
		}
		return true;
	}

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
				int num2 = party.LeaderHero?.RandomValue ?? party.Id.GetHashCode();
				int deterministicHashCode = troop.StringId.GetDeterministicHashCode();
				uint num3 = (uint)((num2 >> troop.Tier * 3) ^ deterministicHashCode);
				if (upgradeTargetIndex == num3 % num)
				{
					result = 9999f;
				}
			}
		}
		return result;
	}
}
