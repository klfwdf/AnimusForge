using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultPartyMoraleModel : PartyMoraleModel
{
	private const float BaseMoraleValue = 50f;

	private readonly TextObject _recentEventsText = GameTexts.FindText("str_recent_events");

	private readonly TextObject _starvationMoraleText = GameTexts.FindText("str_starvation_morale");

	private readonly TextObject _noWageMoraleText = GameTexts.FindText("str_no_wage_morale");

	private readonly TextObject _foodBonusMoraleText = GameTexts.FindText("str_food_bonus_morale");

	private readonly TextObject _partySizeMoraleText = GameTexts.FindText("str_party_size_morale");

	public override float HighMoraleValue => 70f;

	public override int GetDailyStarvationMoralePenalty(PartyBase party)
	{
		return -5;
	}

	public override int GetDailyNoWageMoralePenalty(MobileParty party)
	{
		return -3;
	}

	private int GetStarvationMoralePenalty(MobileParty party)
	{
		return -30;
	}

	private int GetNoWageMoralePenalty(MobileParty party)
	{
		return -20;
	}

	public override float GetStandardBaseMorale(PartyBase party)
	{
		return 50f;
	}

	public override float GetVictoryMoraleChange(PartyBase party)
	{
		return 20f;
	}

	public override float GetDefeatMoraleChange(PartyBase party)
	{
		return -20f;
	}

	private void CalculateFoodVarietyMoraleBonus(MobileParty party, ref ExplainedNumber result)
	{
		if (party.Party.IsStarving)
		{
			return;
		}
		float num;
		switch (party.ItemRoster.FoodVariety)
		{
		case 0:
		case 1:
			num = -2f;
			break;
		case 2:
			num = -1f;
			break;
		case 3:
			num = 0f;
			break;
		case 4:
			num = 1f;
			break;
		case 5:
			num = 2f;
			break;
		case 6:
			num = 3f;
			break;
		case 7:
			num = 5f;
			break;
		case 8:
			num = 6f;
			break;
		case 9:
			num = 7f;
			break;
		case 10:
			num = 8f;
			break;
		case 11:
			num = 9f;
			break;
		case 12:
			num = 10f;
			break;
		default:
			num = 10f;
			break;
		}
		if (num < 0f && party.LeaderHero != null && !party.IsCurrentlyAtSea && party.LeaderHero.GetPerkValue(DefaultPerks.Steward.WarriorsDiet))
		{
			num = 0f;
		}
		if (num == 0f)
		{
			return;
		}
		result.Add(num, _foodBonusMoraleText);
		if (num > 0f && party.HasPerk(DefaultPerks.Steward.Gourmet))
		{
			if (party.IsCurrentlyAtSea)
			{
				num *= 0.5f;
			}
			result.Add(num, DefaultPerks.Steward.Gourmet.Name);
		}
	}

	private void GetPartySizeMoraleEffect(MobileParty mobileParty, ref ExplainedNumber result)
	{
		if (!mobileParty.IsMilitia && !mobileParty.IsVillager)
		{
			int num = mobileParty.Party.NumberOfAllMembers - mobileParty.Party.PartySizeLimit;
			if (num > 0)
			{
				result.Add(-1f * MathF.Sqrt(num), _partySizeMoraleText);
			}
		}
	}

	private static void CheckPerkEffectOnPartyMorale(MobileParty party, PerkObject perk, bool isInfoNeeded, TextObject newInfo, int perkEffect, out TextObject outNewInfo, out int outPerkEffect)
	{
		outNewInfo = newInfo;
		outPerkEffect = perkEffect;
		if (party.LeaderHero != null && party.LeaderHero.GetPerkValue(perk))
		{
			if (isInfoNeeded)
			{
				MBTextManager.SetTextVariable("EFFECT_NAME", perk.Name);
				MBTextManager.SetTextVariable("NUM", 10);
				MBTextManager.SetTextVariable("STR1", newInfo);
				MBTextManager.SetTextVariable("STR2", GameTexts.FindText("str_party_effect"));
				outNewInfo = GameTexts.FindText("str_new_item_line");
			}
			outPerkEffect += 10;
		}
	}

	private void GetMoraleEffectsFromPerks(MobileParty party, ref ExplainedNumber bonus)
	{
		if (party.HasPerk(DefaultPerks.Crossbow.PeasantLeader))
		{
			float num = CalculateTroopTierRatio(party);
			bonus.AddFactor(DefaultPerks.Crossbow.PeasantLeader.PrimaryBonus * num, DefaultPerks.Crossbow.PeasantLeader.Name);
		}
		if (party.CurrentSettlement?.SiegeEvent != null && party.HasPerk(DefaultPerks.Charm.SelfPromoter, checkSecondaryRole: true))
		{
			bonus.Add(DefaultPerks.Charm.SelfPromoter.SecondaryBonus, DefaultPerks.Charm.SelfPromoter.Name);
		}
		if (party.IsCurrentlyAtSea || !party.HasPerk(DefaultPerks.Steward.Logistician))
		{
			return;
		}
		int num2 = 0;
		for (int i = 0; i < party.MemberRoster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = party.MemberRoster.GetElementCopyAtIndex(i);
			if (elementCopyAtIndex.Character.IsMounted)
			{
				num2 += elementCopyAtIndex.Number;
			}
		}
		if (party.Party.NumberOfMounts > party.MemberRoster.TotalManCount - num2)
		{
			bonus.Add(DefaultPerks.Steward.Logistician.PrimaryBonus, DefaultPerks.Steward.Logistician.Name);
		}
	}

	private float CalculateTroopTierRatio(MobileParty party)
	{
		int totalManCount = party.MemberRoster.TotalManCount;
		float num = 0f;
		foreach (TroopRosterElement item in party.MemberRoster.GetTroopRoster())
		{
			if (item.Character.Tier <= 3)
			{
				num += (float)item.Number;
			}
		}
		return num / (float)totalManCount;
	}

	private void GetMoraleEffectsFromSkill(MobileParty party, ref ExplainedNumber bonus)
	{
		CharacterObject effectivePartyLeaderForSkill = SkillHelper.GetEffectivePartyLeaderForSkill(party.Party);
		if (effectivePartyLeaderForSkill != null && effectivePartyLeaderForSkill.GetSkillValue(DefaultSkills.Leadership) > 0)
		{
			SkillHelper.AddSkillBonusForCharacter(DefaultSkillEffects.LeadershipMoraleBonus, effectivePartyLeaderForSkill, ref bonus);
		}
	}

	public override ExplainedNumber GetEffectivePartyMorale(MobileParty mobileParty, bool includeDescription = false)
	{
		ExplainedNumber bonus = new ExplainedNumber(50f, includeDescription);
		bonus.Add(mobileParty.RecentEventsMorale, _recentEventsText);
		GetMoraleEffectsFromSkill(mobileParty, ref bonus);
		if (mobileParty.IsMilitia || mobileParty.IsGarrison)
		{
			if (mobileParty.IsMilitia)
			{
				if (mobileParty.HomeSettlement.IsStarving)
				{
					bonus.Add(GetStarvationMoralePenalty(mobileParty), _starvationMoraleText);
				}
			}
			else if (SettlementHelper.IsGarrisonStarving(mobileParty.CurrentSettlement))
			{
				bonus.Add(GetStarvationMoralePenalty(mobileParty), _starvationMoraleText);
			}
		}
		else if (mobileParty.Party.IsStarving)
		{
			bonus.Add(GetStarvationMoralePenalty(mobileParty), _starvationMoraleText);
		}
		if (mobileParty.HasUnpaidWages > 0f)
		{
			bonus.Add(mobileParty.HasUnpaidWages * (float)GetNoWageMoralePenalty(mobileParty), _noWageMoraleText);
		}
		GetMoraleEffectsFromPerks(mobileParty, ref bonus);
		CalculateFoodVarietyMoraleBonus(mobileParty, ref bonus);
		GetPartySizeMoraleEffect(mobileParty, ref bonus);
		return bonus;
	}
}
