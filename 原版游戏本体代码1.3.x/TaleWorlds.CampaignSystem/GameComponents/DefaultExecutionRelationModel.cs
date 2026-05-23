using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000114 RID: 276
	public class DefaultExecutionRelationModel : ExecutionRelationModel
	{
		// Token: 0x17000658 RID: 1624
		// (get) Token: 0x060017CA RID: 6090 RVA: 0x00071600 File Offset: 0x0006F800
		public override int HeroKillingHeroClanRelationPenalty
		{
			get
			{
				return -40;
			}
		}

		// Token: 0x17000659 RID: 1625
		// (get) Token: 0x060017CB RID: 6091 RVA: 0x00071604 File Offset: 0x0006F804
		public override int HeroKillingHeroFriendRelationPenalty
		{
			get
			{
				return -10;
			}
		}

		// Token: 0x1700065A RID: 1626
		// (get) Token: 0x060017CC RID: 6092 RVA: 0x00071608 File Offset: 0x0006F808
		public override int PlayerExecutingHeroFactionRelationPenaltyDishonorable
		{
			get
			{
				return -5;
			}
		}

		// Token: 0x1700065B RID: 1627
		// (get) Token: 0x060017CD RID: 6093 RVA: 0x0007160C File Offset: 0x0006F80C
		public override int PlayerExecutingHeroClanRelationPenaltyDishonorable
		{
			get
			{
				return -30;
			}
		}

		// Token: 0x1700065C RID: 1628
		// (get) Token: 0x060017CE RID: 6094 RVA: 0x00071610 File Offset: 0x0006F810
		public override int PlayerExecutingHeroFriendRelationPenaltyDishonorable
		{
			get
			{
				return -15;
			}
		}

		// Token: 0x1700065D RID: 1629
		// (get) Token: 0x060017CF RID: 6095 RVA: 0x00071614 File Offset: 0x0006F814
		public override int PlayerExecutingHeroHonorPenalty
		{
			get
			{
				return -1000;
			}
		}

		// Token: 0x1700065E RID: 1630
		// (get) Token: 0x060017D0 RID: 6096 RVA: 0x0007161B File Offset: 0x0006F81B
		public override int PlayerExecutingHeroFactionRelationPenalty
		{
			get
			{
				return -10;
			}
		}

		// Token: 0x1700065F RID: 1631
		// (get) Token: 0x060017D1 RID: 6097 RVA: 0x0007161F File Offset: 0x0006F81F
		public override int PlayerExecutingHeroHonorableNobleRelationPenalty
		{
			get
			{
				return -10;
			}
		}

		// Token: 0x17000660 RID: 1632
		// (get) Token: 0x060017D2 RID: 6098 RVA: 0x00071623 File Offset: 0x0006F823
		public override int PlayerExecutingHeroClanRelationPenalty
		{
			get
			{
				return -60;
			}
		}

		// Token: 0x17000661 RID: 1633
		// (get) Token: 0x060017D3 RID: 6099 RVA: 0x00071627 File Offset: 0x0006F827
		public override int PlayerExecutingHeroFriendRelationPenalty
		{
			get
			{
				return -30;
			}
		}

		// Token: 0x060017D4 RID: 6100 RVA: 0x0007162C File Offset: 0x0006F82C
		public override int GetRelationChangeForExecutingHero(Hero victim, Hero hero, out bool showQuickNotification)
		{
			int result = 0;
			showQuickNotification = false;
			if (victim.GetTraitLevel(DefaultTraits.Honor) < 0)
			{
				if (!hero.IsHumanPlayerCharacter && hero != victim && hero.Clan != null && hero.Clan.Leader == hero)
				{
					if (hero.Clan == victim.Clan)
					{
						result = Campaign.Current.Models.ExecutionRelationModel.PlayerExecutingHeroClanRelationPenaltyDishonorable;
						showQuickNotification = true;
					}
					else if (victim.IsFriend(hero))
					{
						result = Campaign.Current.Models.ExecutionRelationModel.PlayerExecutingHeroFriendRelationPenaltyDishonorable;
						showQuickNotification = true;
					}
					else if (hero.MapFaction == victim.MapFaction && hero.CharacterObject.Occupation == Occupation.Lord)
					{
						result = Campaign.Current.Models.ExecutionRelationModel.PlayerExecutingHeroFactionRelationPenaltyDishonorable;
						showQuickNotification = true;
					}
				}
			}
			else if (!hero.IsHumanPlayerCharacter && hero != victim && hero.Clan != null && hero.Clan.Leader == hero)
			{
				if (hero.Clan == victim.Clan)
				{
					result = Campaign.Current.Models.ExecutionRelationModel.PlayerExecutingHeroClanRelationPenalty;
					showQuickNotification = true;
				}
				else if (victim.IsFriend(hero))
				{
					result = Campaign.Current.Models.ExecutionRelationModel.PlayerExecutingHeroFriendRelationPenalty;
					showQuickNotification = true;
				}
				else if (hero.MapFaction == victim.MapFaction && hero.CharacterObject.Occupation == Occupation.Lord)
				{
					result = Campaign.Current.Models.ExecutionRelationModel.PlayerExecutingHeroFactionRelationPenalty;
					showQuickNotification = false;
				}
				else if (hero.GetTraitLevel(DefaultTraits.Honor) > 0 && !victim.Clan.IsRebelClan)
				{
					result = Campaign.Current.Models.ExecutionRelationModel.PlayerExecutingHeroHonorableNobleRelationPenalty;
					showQuickNotification = true;
				}
			}
			return result;
		}
	}
}
