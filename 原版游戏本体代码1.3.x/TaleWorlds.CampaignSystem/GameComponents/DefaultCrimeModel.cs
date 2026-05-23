using System;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000108 RID: 264
	public class DefaultCrimeModel : CrimeModel
	{
		// Token: 0x06001725 RID: 5925 RVA: 0x0006BF26 File Offset: 0x0006A126
		public override bool DoesPlayerHaveAnyCrimeRating(IFaction faction)
		{
			return faction.MainHeroCrimeRating > 0f;
		}

		// Token: 0x06001726 RID: 5926 RVA: 0x0006BF35 File Offset: 0x0006A135
		public override bool IsPlayerCrimeRatingSevere(IFaction faction)
		{
			return faction.MainHeroCrimeRating >= 65f;
		}

		// Token: 0x06001727 RID: 5927 RVA: 0x0006BF47 File Offset: 0x0006A147
		public override bool IsPlayerCrimeRatingModerate(IFaction faction)
		{
			return faction.MainHeroCrimeRating > 30f && faction.MainHeroCrimeRating <= 65f;
		}

		// Token: 0x06001728 RID: 5928 RVA: 0x0006BF68 File Offset: 0x0006A168
		public override bool IsPlayerCrimeRatingMild(IFaction faction)
		{
			return faction.MainHeroCrimeRating > 0f && faction.MainHeroCrimeRating <= 30f;
		}

		// Token: 0x06001729 RID: 5929 RVA: 0x0006BF8C File Offset: 0x0006A18C
		public override float GetCost(IFaction faction, CrimeModel.PaymentMethod paymentMethod, float minimumCrimeRating)
		{
			float x = MathF.Max(0f, faction.MainHeroCrimeRating - minimumCrimeRating);
			if (paymentMethod == CrimeModel.PaymentMethod.Gold)
			{
				return (float)((int)(MathF.Pow(x, 1.2f) * 100f));
			}
			if (paymentMethod != CrimeModel.PaymentMethod.Influence)
			{
				return 0f;
			}
			return MathF.Pow(x, 1.2f);
		}

		// Token: 0x0600172A RID: 5930 RVA: 0x0006BFDC File Offset: 0x0006A1DC
		public override ExplainedNumber GetDailyCrimeRatingChange(IFaction faction, bool includeDescriptions = false)
		{
			ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions, null);
			int num = faction.Settlements.Count(delegate(Settlement x)
			{
				if (x.IsTown)
				{
					return x.Alleys.Any((Alley y) => y.Owner == Hero.MainHero);
				}
				return false;
			});
			result.Add((float)num * Campaign.Current.Models.AlleyModel.GetDailyCrimeRatingOfAlley, includeDescriptions ? new TextObject("{=t87T82jq}Owned alleys", null) : null, null);
			if (faction.MainHeroCrimeRating.ApproximatelyEqualsTo(0f, 1E-05f))
			{
				return result;
			}
			Clan clan = faction as Clan;
			if (Hero.MainHero.Clan == faction)
			{
				result.Add(-5f, includeDescriptions ? new TextObject("{=eNtRt6F5}Your own Clan", null) : null, null);
			}
			else if (faction.IsKingdomFaction && faction.Leader == Hero.MainHero)
			{
				result.Add(-5f, includeDescriptions ? new TextObject("{=xer2bta5}Your own Kingdom", null) : null, null);
			}
			else if (Hero.MainHero.MapFaction == faction)
			{
				result.Add(-1.5f, includeDescriptions ? new TextObject("{=QRwaQIbm}Is in Kingdom", null) : null, null);
			}
			else if (clan != null && Hero.MainHero.MapFaction == clan.Kingdom)
			{
				result.Add(-1.25f, includeDescriptions ? new TextObject("{=hXGByLG9}Sharing the same Kingdom", null) : null, null);
			}
			else if (Hero.MainHero.Clan.IsAtWarWith(faction))
			{
				result.Add(-0.25f, includeDescriptions ? new TextObject("{=BYTrUJyj}In War", null) : null, null);
			}
			else
			{
				result.Add(-1f, includeDescriptions ? new TextObject("{=basevalue}Base", null) : null, null);
			}
			PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Roguery.WhiteLies, Hero.MainHero.CharacterObject, true, ref result, false);
			return result;
		}

		// Token: 0x17000643 RID: 1603
		// (get) Token: 0x0600172B RID: 5931 RVA: 0x0006C1A8 File Offset: 0x0006A3A8
		public override float DeclareWarCrimeRatingThreshold
		{
			get
			{
				return 60f;
			}
		}

		// Token: 0x0600172C RID: 5932 RVA: 0x0006C1AF File Offset: 0x0006A3AF
		public override float GetMaxCrimeRating()
		{
			return 100f;
		}

		// Token: 0x0600172D RID: 5933 RVA: 0x0006C1B6 File Offset: 0x0006A3B6
		public override float GetMinAcceptableCrimeRating(IFaction faction)
		{
			if (faction != Hero.MainHero.MapFaction)
			{
				return 30f;
			}
			return 20f;
		}

		// Token: 0x0600172E RID: 5934 RVA: 0x0006C1D0 File Offset: 0x0006A3D0
		public override float GetCrimeRatingAfterPunishment()
		{
			return 25f;
		}

		// Token: 0x040007B5 RID: 1973
		private const float ModerateCrimeRatingThreshold = 30f;

		// Token: 0x040007B6 RID: 1974
		private const float SevereCrimeRatingThreshold = 65f;
	}
}
