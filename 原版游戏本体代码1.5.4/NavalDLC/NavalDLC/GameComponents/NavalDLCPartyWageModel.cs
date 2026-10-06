using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Localization;

namespace NavalDLC.GameComponents;

public class NavalDLCPartyWageModel : PartyWageModel
{
	private const float PartyWageReductionAtSea = 0.2f;

	private const float ConvoyPartyWageCut = -0.8f;

	private readonly TextObject _convoyPartyWageCutText = new TextObject("{=lDxu6pez}Convoy Wage Multiplier");

	private readonly TextObject _partyWageReductionAtSea = new TextObject("{=sWhNhHkV}Wage Reduction At Sea");

	public override int MaxWagePaymentLimit => base.BaseModel.MaxWagePaymentLimit;

	public override int GetCharacterWage(CharacterObject character)
	{
		return base.BaseModel.GetCharacterWage(character);
	}

	public override ExplainedNumber GetTotalWage(MobileParty mobileParty, TroopRoster troopRoster, bool includeDescriptions = false)
	{
		ExplainedNumber totalWage = base.BaseModel.GetTotalWage(mobileParty, troopRoster, includeDescriptions);
		Hero perkOwnerHero = null;
		bool flag = !mobileParty.HasPerk(DefaultPerks.Steward.AidCorps, out perkOwnerHero);
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < troopRoster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = troopRoster.GetElementCopyAtIndex(i);
			CharacterObject character = elementCopyAtIndex.Character;
			int num3 = (flag ? elementCopyAtIndex.Number : (elementCopyAtIndex.Number - elementCopyAtIndex.WoundedNumber));
			if (!character.IsHero)
			{
				int num4 = character.TroopWage * num3;
				if (!character.IsMariner)
				{
					num += num4;
				}
				if (character.IsMounted)
				{
					num2 += num4;
				}
			}
		}
		if (mobileParty.IsCurrentlyAtSea)
		{
			totalWage.AddFactor(-0.2f, _partyWageReductionAtSea);
			if (mobileParty.IsCaravan)
			{
				totalWage.AddFactor(-0.8f, _convoyPartyWageCutText);
			}
			Hero perkOwnerHero2 = null;
			if (mobileParty.HasPerk(NavalPerks.Boatswain.Optimization, out perkOwnerHero2))
			{
				float num5 = (float)num / totalWage.BaseNumber;
				if (num5 > 0f)
				{
					float value = NavalPerks.Boatswain.Optimization.PrimaryBonus * num5;
					totalWage.AddFactor(value, NavalPerks.Boatswain.Optimization.Name);
				}
			}
			Hero perkOwnerHero3 = null;
			if (mobileParty.HasPerk(NavalPerks.Boatswain.NavalHorde, out perkOwnerHero3))
			{
				float num6 = (float)num2 / totalWage.BaseNumber;
				if (num6 > 0f)
				{
					float value2 = NavalPerks.Boatswain.NavalHorde.PrimaryBonus * num6;
					totalWage.AddFactor(value2, NavalPerks.Boatswain.NavalHorde.Name);
				}
			}
		}
		return totalWage;
	}

	public override ExplainedNumber GetTroopRecruitmentCost(CharacterObject troop, Hero buyerHero, bool withoutItemCost = false)
	{
		ExplainedNumber stat = base.BaseModel.GetTroopRecruitmentCost(troop, buyerHero, withoutItemCost);
		if (troop.IsMariner && buyerHero != null && buyerHero.PartyBelongedTo != null)
		{
			PerkHelper.AddPerkBonusForParty(NavalPerks.Boatswain.PopularCaptain, buyerHero.PartyBelongedTo, isPrimaryBonus: true, ref stat);
		}
		return stat;
	}
}
