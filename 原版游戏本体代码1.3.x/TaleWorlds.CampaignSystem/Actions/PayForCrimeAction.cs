using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004BE RID: 1214
	public static class PayForCrimeAction
	{
		// Token: 0x06004A01 RID: 18945 RVA: 0x001748C8 File Offset: 0x00172AC8
		private static void ApplyInternal(IFaction faction, CrimeModel.PaymentMethod paymentMethod)
		{
			bool flag = false;
			if (paymentMethod.HasAnyFlag(CrimeModel.PaymentMethod.Gold))
			{
				GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, (int)PayForCrimeAction.GetClearCrimeCost(faction, CrimeModel.PaymentMethod.Gold), false);
				SkillLevelingManager.OnBribeGiven((int)PayForCrimeAction.GetClearCrimeCost(faction, CrimeModel.PaymentMethod.Gold));
			}
			if (paymentMethod.HasAnyFlag(CrimeModel.PaymentMethod.Influence))
			{
				ChangeClanInfluenceAction.Apply(Clan.PlayerClan, -PayForCrimeAction.GetClearCrimeCost(faction, CrimeModel.PaymentMethod.Influence));
			}
			if (paymentMethod.HasAnyFlag(CrimeModel.PaymentMethod.Punishment))
			{
				if (MathF.Clamp(1f - (float)Hero.MainHero.HitPoints * 0.01f, 0.001f, 1f) * 0.25f > MBRandom.RandomFloat)
				{
					flag = true;
					KillCharacterAction.ApplyByMurder(Hero.MainHero, null, true);
				}
				else
				{
					Hero.MainHero.MakeWounded(null, KillCharacterAction.KillCharacterActionDetail.None);
					float num = 0.5f;
					if (MBRandom.RandomFloat < num)
					{
						SkillLevelingManager.OnMainHeroTortured();
					}
				}
			}
			if (paymentMethod.HasAnyFlag(CrimeModel.PaymentMethod.Execution))
			{
				flag = true;
				KillCharacterAction.ApplyByMurder(Hero.MainHero, null, true);
			}
			if (!flag)
			{
				float num2 = MathF.Min(faction.MainHeroCrimeRating, Campaign.Current.Models.CrimeModel.GetCrimeRatingAfterPunishment());
				ChangeCrimeRatingAction.Apply(faction, num2 - faction.MainHeroCrimeRating, true);
			}
		}

		// Token: 0x06004A02 RID: 18946 RVA: 0x001749D0 File Offset: 0x00172BD0
		public static float GetClearCrimeCost(IFaction faction, CrimeModel.PaymentMethod paymentMethod)
		{
			return Campaign.Current.Models.CrimeModel.GetCost(faction, paymentMethod, Campaign.Current.Models.CrimeModel.GetMinAcceptableCrimeRating(faction));
		}

		// Token: 0x06004A03 RID: 18947 RVA: 0x001749FD File Offset: 0x00172BFD
		public static void Apply(IFaction faction, CrimeModel.PaymentMethod paymentMethod)
		{
			PayForCrimeAction.ApplyInternal(faction, paymentMethod);
		}
	}
}
