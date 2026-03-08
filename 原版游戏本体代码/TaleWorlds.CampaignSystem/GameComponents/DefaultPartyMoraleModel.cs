using System;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000133 RID: 307
	public class DefaultPartyMoraleModel : PartyMoraleModel
	{
		// Token: 0x1700068A RID: 1674
		// (get) Token: 0x060018F4 RID: 6388 RVA: 0x0007AD5F File Offset: 0x00078F5F
		public override float HighMoraleValue
		{
			get
			{
				return 70f;
			}
		}

		// Token: 0x060018F5 RID: 6389 RVA: 0x0007AD66 File Offset: 0x00078F66
		public override int GetDailyStarvationMoralePenalty(PartyBase party)
		{
			return -5;
		}

		// Token: 0x060018F6 RID: 6390 RVA: 0x0007AD6A File Offset: 0x00078F6A
		public override int GetDailyNoWageMoralePenalty(MobileParty party)
		{
			return -3;
		}

		// Token: 0x060018F7 RID: 6391 RVA: 0x0007AD6E File Offset: 0x00078F6E
		private int GetStarvationMoralePenalty(MobileParty party)
		{
			return -30;
		}

		// Token: 0x060018F8 RID: 6392 RVA: 0x0007AD72 File Offset: 0x00078F72
		private int GetNoWageMoralePenalty(MobileParty party)
		{
			return -20;
		}

		// Token: 0x060018F9 RID: 6393 RVA: 0x0007AD76 File Offset: 0x00078F76
		public override float GetStandardBaseMorale(PartyBase party)
		{
			return 50f;
		}

		// Token: 0x060018FA RID: 6394 RVA: 0x0007AD7D File Offset: 0x00078F7D
		public override float GetVictoryMoraleChange(PartyBase party)
		{
			return 20f;
		}

		// Token: 0x060018FB RID: 6395 RVA: 0x0007AD84 File Offset: 0x00078F84
		public override float GetDefeatMoraleChange(PartyBase party)
		{
			return -20f;
		}

		// Token: 0x060018FC RID: 6396 RVA: 0x0007AD8C File Offset: 0x00078F8C
		private void CalculateFoodVarietyMoraleBonus(MobileParty party, ref ExplainedNumber result)
		{
			if (!party.Party.IsStarving)
			{
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
				if (num != 0f)
				{
					result.Add(num, this._foodBonusMoraleText, null);
					if (num > 0f && party.HasPerk(DefaultPerks.Steward.Gourmet, false))
					{
						if (party.IsCurrentlyAtSea)
						{
							num *= 0.5f;
						}
						result.Add(num, DefaultPerks.Steward.Gourmet.Name, null);
					}
				}
			}
		}

		// Token: 0x060018FD RID: 6397 RVA: 0x0007AED8 File Offset: 0x000790D8
		private void GetPartySizeMoraleEffect(MobileParty mobileParty, ref ExplainedNumber result)
		{
			if (!mobileParty.IsMilitia && !mobileParty.IsVillager)
			{
				int num = mobileParty.Party.NumberOfAllMembers - mobileParty.Party.PartySizeLimit;
				if (num > 0)
				{
					result.Add(-1f * MathF.Sqrt((float)num), this._partySizeMoraleText, null);
				}
			}
		}

		// Token: 0x060018FE RID: 6398 RVA: 0x0007AF2C File Offset: 0x0007912C
		private static void CheckPerkEffectOnPartyMorale(MobileParty party, PerkObject perk, bool isInfoNeeded, TextObject newInfo, int perkEffect, out TextObject outNewInfo, out int outPerkEffect)
		{
			outNewInfo = newInfo;
			outPerkEffect = perkEffect;
			if (party.LeaderHero != null && party.LeaderHero.GetPerkValue(perk))
			{
				if (isInfoNeeded)
				{
					MBTextManager.SetTextVariable("EFFECT_NAME", perk.Name, false);
					MBTextManager.SetTextVariable("NUM", 10);
					MBTextManager.SetTextVariable("STR1", newInfo, false);
					MBTextManager.SetTextVariable("STR2", GameTexts.FindText("str_party_effect", null), false);
					outNewInfo = GameTexts.FindText("str_new_item_line", null);
				}
				outPerkEffect += 10;
			}
		}

		// Token: 0x060018FF RID: 6399 RVA: 0x0007AFB4 File Offset: 0x000791B4
		private void GetMoraleEffectsFromPerks(MobileParty party, ref ExplainedNumber bonus)
		{
			if (party.HasPerk(DefaultPerks.Crossbow.PeasantLeader, false))
			{
				float num = this.CalculateTroopTierRatio(party);
				bonus.AddFactor(DefaultPerks.Crossbow.PeasantLeader.PrimaryBonus * num, DefaultPerks.Crossbow.PeasantLeader.Name);
			}
			Settlement currentSettlement = party.CurrentSettlement;
			if (((currentSettlement != null) ? currentSettlement.SiegeEvent : null) != null && party.HasPerk(DefaultPerks.Charm.SelfPromoter, true))
			{
				bonus.Add(DefaultPerks.Charm.SelfPromoter.SecondaryBonus, DefaultPerks.Charm.SelfPromoter.Name, null);
			}
			if (!party.IsCurrentlyAtSea && party.HasPerk(DefaultPerks.Steward.Logistician, false))
			{
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
					bonus.Add(DefaultPerks.Steward.Logistician.PrimaryBonus, DefaultPerks.Steward.Logistician.Name, null);
				}
			}
		}

		// Token: 0x06001900 RID: 6400 RVA: 0x0007B0B8 File Offset: 0x000792B8
		private float CalculateTroopTierRatio(MobileParty party)
		{
			int totalManCount = party.MemberRoster.TotalManCount;
			float num = 0f;
			foreach (TroopRosterElement troopRosterElement in party.MemberRoster.GetTroopRoster())
			{
				if (troopRosterElement.Character.Tier <= 3)
				{
					num += (float)troopRosterElement.Number;
				}
			}
			return num / (float)totalManCount;
		}

		// Token: 0x06001901 RID: 6401 RVA: 0x0007B138 File Offset: 0x00079338
		private void GetMoraleEffectsFromSkill(MobileParty party, ref ExplainedNumber bonus)
		{
			CharacterObject effectivePartyLeaderForSkill = SkillHelper.GetEffectivePartyLeaderForSkill(party.Party);
			if (effectivePartyLeaderForSkill != null && effectivePartyLeaderForSkill.GetSkillValue(DefaultSkills.Leadership) > 0)
			{
				SkillHelper.AddSkillBonusForCharacter(DefaultSkillEffects.LeadershipMoraleBonus, effectivePartyLeaderForSkill, ref bonus);
			}
		}

		// Token: 0x06001902 RID: 6402 RVA: 0x0007B170 File Offset: 0x00079370
		public override ExplainedNumber GetEffectivePartyMorale(MobileParty mobileParty, bool includeDescription = false)
		{
			ExplainedNumber result = new ExplainedNumber(50f, includeDescription, null);
			result.Add(mobileParty.RecentEventsMorale, this._recentEventsText, null);
			this.GetMoraleEffectsFromSkill(mobileParty, ref result);
			if (mobileParty.IsMilitia || mobileParty.IsGarrison)
			{
				if (mobileParty.IsMilitia)
				{
					if (mobileParty.HomeSettlement.IsStarving)
					{
						result.Add((float)this.GetStarvationMoralePenalty(mobileParty), this._starvationMoraleText, null);
					}
				}
				else if (SettlementHelper.IsGarrisonStarving(mobileParty.CurrentSettlement))
				{
					result.Add((float)this.GetStarvationMoralePenalty(mobileParty), this._starvationMoraleText, null);
				}
			}
			else if (mobileParty.Party.IsStarving)
			{
				result.Add((float)this.GetStarvationMoralePenalty(mobileParty), this._starvationMoraleText, null);
			}
			if (mobileParty.HasUnpaidWages > 0f)
			{
				result.Add(mobileParty.HasUnpaidWages * (float)this.GetNoWageMoralePenalty(mobileParty), this._noWageMoraleText, null);
			}
			this.GetMoraleEffectsFromPerks(mobileParty, ref result);
			this.CalculateFoodVarietyMoraleBonus(mobileParty, ref result);
			this.GetPartySizeMoraleEffect(mobileParty, ref result);
			return result;
		}

		// Token: 0x04000819 RID: 2073
		private const float BaseMoraleValue = 50f;

		// Token: 0x0400081A RID: 2074
		private readonly TextObject _recentEventsText = GameTexts.FindText("str_recent_events", null);

		// Token: 0x0400081B RID: 2075
		private readonly TextObject _starvationMoraleText = GameTexts.FindText("str_starvation_morale", null);

		// Token: 0x0400081C RID: 2076
		private readonly TextObject _noWageMoraleText = GameTexts.FindText("str_no_wage_morale", null);

		// Token: 0x0400081D RID: 2077
		private readonly TextObject _foodBonusMoraleText = GameTexts.FindText("str_food_bonus_morale", null);

		// Token: 0x0400081E RID: 2078
		private readonly TextObject _partySizeMoraleText = GameTexts.FindText("str_party_size_morale", null);
	}
}
