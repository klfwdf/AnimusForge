using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;

namespace NavalDLC.GameComponents;

internal class NavalDLCPartyTroopUpgradeModel : PartyTroopUpgradeModel
{
	public override bool CanPartyUpgradeTroopToTarget(PartyBase party, CharacterObject character, CharacterObject target)
	{
		return base.BaseModel.CanPartyUpgradeTroopToTarget(party, character, target);
	}

	public override bool IsTroopUpgradeable(PartyBase party, CharacterObject character)
	{
		return base.BaseModel.IsTroopUpgradeable(party, character);
	}

	public override bool DoesPartyHaveRequiredItemsForUpgrade(PartyBase party, CharacterObject upgradeTarget)
	{
		return base.BaseModel.DoesPartyHaveRequiredItemsForUpgrade(party, upgradeTarget);
	}

	public override bool DoesPartyHaveRequiredPerksForUpgrade(PartyBase party, CharacterObject character, CharacterObject upgradeTarget, out PerkObject requiredPerk)
	{
		return base.BaseModel.DoesPartyHaveRequiredPerksForUpgrade(party, character, upgradeTarget, out requiredPerk);
	}

	public override ExplainedNumber GetGoldCostForUpgrade(PartyBase party, CharacterObject characterObject, CharacterObject upgradeTarget)
	{
		ExplainedNumber stat = base.BaseModel.GetGoldCostForUpgrade(party, characterObject, upgradeTarget);
		if (party.IsMobile && characterObject.IsMariner && !characterObject.IsHero)
		{
			PerkHelper.AddPerkBonusForParty(NavalPerks.Boatswain.EfficientCaptain, party.MobileParty, isPrimaryBonus: true, ref stat);
		}
		return stat;
	}

	public override int GetXpCostForUpgrade(PartyBase party, CharacterObject characterObject, CharacterObject upgradeTarget)
	{
		return base.BaseModel.GetXpCostForUpgrade(party, characterObject, upgradeTarget);
	}

	public override int GetSkillXpFromUpgradingTroops(PartyBase party, CharacterObject troop, int numberOfTroops)
	{
		return base.BaseModel.GetSkillXpFromUpgradingTroops(party, troop, numberOfTroops);
	}

	public override float GetUpgradeChanceForTroopUpgrade(PartyBase party, CharacterObject troop, int upgradeTargetIndex)
	{
		return base.BaseModel.GetUpgradeChanceForTroopUpgrade(party, troop, upgradeTargetIndex);
	}
}
