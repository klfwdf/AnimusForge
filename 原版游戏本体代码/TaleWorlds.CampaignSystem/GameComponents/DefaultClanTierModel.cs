using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000104 RID: 260
	public class DefaultClanTierModel : ClanTierModel
	{
		// Token: 0x1700063A RID: 1594
		// (get) Token: 0x060016FA RID: 5882 RVA: 0x0006AA7D File Offset: 0x00068C7D
		public override int MinClanTier
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x1700063B RID: 1595
		// (get) Token: 0x060016FB RID: 5883 RVA: 0x0006AA80 File Offset: 0x00068C80
		public override int MaxClanTier
		{
			get
			{
				return 6;
			}
		}

		// Token: 0x1700063C RID: 1596
		// (get) Token: 0x060016FC RID: 5884 RVA: 0x0006AA83 File Offset: 0x00068C83
		public override int MercenaryEligibleTier
		{
			get
			{
				return 1;
			}
		}

		// Token: 0x1700063D RID: 1597
		// (get) Token: 0x060016FD RID: 5885 RVA: 0x0006AA86 File Offset: 0x00068C86
		public override int VassalEligibleTier
		{
			get
			{
				return 2;
			}
		}

		// Token: 0x1700063E RID: 1598
		// (get) Token: 0x060016FE RID: 5886 RVA: 0x0006AA89 File Offset: 0x00068C89
		public override int BannerEligibleTier
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x1700063F RID: 1599
		// (get) Token: 0x060016FF RID: 5887 RVA: 0x0006AA8C File Offset: 0x00068C8C
		public override int RebelClanStartingTier
		{
			get
			{
				return 3;
			}
		}

		// Token: 0x17000640 RID: 1600
		// (get) Token: 0x06001700 RID: 5888 RVA: 0x0006AA8F File Offset: 0x00068C8F
		public override int CompanionToLordClanStartingTier
		{
			get
			{
				return 2;
			}
		}

		// Token: 0x17000641 RID: 1601
		// (get) Token: 0x06001701 RID: 5889 RVA: 0x0006AA92 File Offset: 0x00068C92
		private int KingdomEligibleTier
		{
			get
			{
				return Campaign.Current.Models.KingdomCreationModel.MinimumClanTierToCreateKingdom;
			}
		}

		// Token: 0x06001702 RID: 5890 RVA: 0x0006AAA8 File Offset: 0x00068CA8
		public override int CalculateInitialRenown(Clan clan)
		{
			int num = DefaultClanTierModel.TierLowerRenownLimits[clan.Tier];
			int num2 = ((clan.Tier >= this.MaxClanTier) ? (DefaultClanTierModel.TierLowerRenownLimits[this.MaxClanTier] + 1500) : DefaultClanTierModel.TierLowerRenownLimits[clan.Tier + 1]);
			int maxValue = (int)((float)num2 - (float)(num2 - num) * 0.4f);
			return MBRandom.RandomInt(num, maxValue);
		}

		// Token: 0x06001703 RID: 5891 RVA: 0x0006AB09 File Offset: 0x00068D09
		public override int CalculateInitialInfluence(Clan clan)
		{
			return (int)(150f + (float)MBRandom.RandomInt((int)((float)this.CalculateInitialRenown(clan) / 15f)) + (float)MBRandom.RandomInt(MBRandom.RandomInt(MBRandom.RandomInt(400))));
		}

		// Token: 0x06001704 RID: 5892 RVA: 0x0006AB40 File Offset: 0x00068D40
		public override int CalculateTier(Clan clan)
		{
			int result = this.MinClanTier;
			for (int i = this.MinClanTier + 1; i <= this.MaxClanTier; i++)
			{
				if (clan.Renown >= (float)DefaultClanTierModel.TierLowerRenownLimits[i])
				{
					result = i;
				}
			}
			return result;
		}

		// Token: 0x06001705 RID: 5893 RVA: 0x0006AB80 File Offset: 0x00068D80
		public override ValueTuple<ExplainedNumber, bool> HasUpcomingTier(Clan clan, out TextObject extraExplanation, bool includeDescriptions = false)
		{
			bool flag = clan.Tier < this.MaxClanTier;
			ExplainedNumber item = new ExplainedNumber(0f, includeDescriptions, null);
			extraExplanation = null;
			if (flag)
			{
				int num = this.GetPartyLimitForTier(clan, clan.Tier + 1) - this.GetPartyLimitForTier(clan, clan.Tier);
				if (num != 0)
				{
					item.Add((float)num, this._partyLimitBonusText, null);
				}
				int num2 = this.GetCompanionLimitFromTier(clan.Tier + 1) - this.GetCompanionLimitFromTier(clan.Tier);
				if (num2 != 0)
				{
					item.Add((float)num2, this._companionLimitBonusText, null);
				}
				int nextClanTierPartySizeEffectChangeForHero = Campaign.Current.Models.PartySizeLimitModel.GetNextClanTierPartySizeEffectChangeForHero(clan.Leader);
				if (nextClanTierPartySizeEffectChangeForHero > 0)
				{
					item.Add((float)nextClanTierPartySizeEffectChangeForHero, this._additionalCurrentPartySizeBonus, null);
				}
				int num3 = Campaign.Current.Models.WorkshopModel.GetMaxWorkshopCountForClanTier(clan.Tier + 1) - Campaign.Current.Models.WorkshopModel.GetMaxWorkshopCountForClanTier(clan.Tier);
				if (num3 > 0)
				{
					item.Add((float)num3, this._additionalWorkshopCountBonus, null);
				}
				if (clan.Tier + 1 == this.MercenaryEligibleTier)
				{
					extraExplanation = this._mercenaryEligibleText;
				}
				else if (clan.Tier + 1 == this.VassalEligibleTier)
				{
					extraExplanation = this._vassalEligibleText;
				}
				else if (clan.Tier + 1 == this.KingdomEligibleTier)
				{
					extraExplanation = this._kingdomEligibleText;
				}
			}
			return new ValueTuple<ExplainedNumber, bool>(item, flag);
		}

		// Token: 0x06001706 RID: 5894 RVA: 0x0006ACE7 File Offset: 0x00068EE7
		public override int GetRequiredRenownForTier(int tier)
		{
			return DefaultClanTierModel.TierLowerRenownLimits[tier];
		}

		// Token: 0x06001707 RID: 5895 RVA: 0x0006ACF0 File Offset: 0x00068EF0
		public override int GetPartyLimitForTier(Clan clan, int clanTierToCheck)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(0f, false, null);
			if (!clan.IsMinorFaction)
			{
				if (clanTierToCheck < 3)
				{
					explainedNumber.Add(1f, null, null);
				}
				else if (clanTierToCheck < 5)
				{
					explainedNumber.Add(2f, null, null);
				}
				else
				{
					explainedNumber.Add(3f, null, null);
				}
			}
			else
			{
				explainedNumber.Add(MathF.Clamp((float)clanTierToCheck, 1f, 4f), null, null);
			}
			this.AddPartyLimitPerkEffects(clan, ref explainedNumber);
			return MathF.Round(explainedNumber.ResultNumber);
		}

		// Token: 0x06001708 RID: 5896 RVA: 0x0006AD7A File Offset: 0x00068F7A
		private void AddPartyLimitPerkEffects(Clan clan, ref ExplainedNumber result)
		{
			if (clan.Leader != null && clan.Leader.GetPerkValue(DefaultPerks.Leadership.TalentMagnet))
			{
				result.Add(DefaultPerks.Leadership.TalentMagnet.SecondaryBonus, DefaultPerks.Leadership.TalentMagnet.Name, null);
			}
		}

		// Token: 0x06001709 RID: 5897 RVA: 0x0006ADB4 File Offset: 0x00068FB4
		public override int GetCompanionLimit(Clan clan)
		{
			int num = this.GetCompanionLimitFromTier(clan.Tier);
			if (clan.Leader.GetPerkValue(DefaultPerks.Leadership.WePledgeOurSwords))
			{
				num += (int)DefaultPerks.Leadership.WePledgeOurSwords.PrimaryBonus;
			}
			if (clan.Leader.GetPerkValue(DefaultPerks.Charm.Camaraderie))
			{
				num += (int)DefaultPerks.Charm.Camaraderie.SecondaryBonus;
			}
			return num;
		}

		// Token: 0x0600170A RID: 5898 RVA: 0x0006AE0F File Offset: 0x0006900F
		private int GetCompanionLimitFromTier(int clanTier)
		{
			return clanTier + 3;
		}

		// Token: 0x040007AD RID: 1965
		private static readonly int[] TierLowerRenownLimits = new int[] { 0, 50, 150, 350, 900, 2350, 6150 };

		// Token: 0x040007AE RID: 1966
		private readonly TextObject _partyLimitBonusText = GameTexts.FindText("str_clan_tier_party_limit_bonus", null);

		// Token: 0x040007AF RID: 1967
		private readonly TextObject _companionLimitBonusText = GameTexts.FindText("str_clan_tier_companion_limit_bonus", null);

		// Token: 0x040007B0 RID: 1968
		private readonly TextObject _mercenaryEligibleText = GameTexts.FindText("str_clan_tier_mercenary_eligible", null);

		// Token: 0x040007B1 RID: 1969
		private readonly TextObject _vassalEligibleText = GameTexts.FindText("str_clan_tier_vassal_eligible", null);

		// Token: 0x040007B2 RID: 1970
		private readonly TextObject _additionalCurrentPartySizeBonus = GameTexts.FindText("str_clan_tier_party_size_bonus", null);

		// Token: 0x040007B3 RID: 1971
		private readonly TextObject _additionalWorkshopCountBonus = GameTexts.FindText("str_clan_tier_workshop_count_bonus", null);

		// Token: 0x040007B4 RID: 1972
		private readonly TextObject _kingdomEligibleText = GameTexts.FindText("str_clan_tier_kingdom_eligible", null);
	}
}
