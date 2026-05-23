using System;
using Helpers;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors
{
	// Token: 0x0200046B RID: 1131
	public class AiArmyMemberBehavior : CampaignBehaviorBase
	{
		// Token: 0x17000E37 RID: 3639
		// (get) Token: 0x060047AE RID: 18350 RVA: 0x001671DC File Offset: 0x001653DC
		private float FollowingArmyLeaderMaxScore
		{
			get
			{
				return 20f;
			}
		}

		// Token: 0x17000E38 RID: 3640
		// (get) Token: 0x060047AF RID: 18351 RVA: 0x001671E3 File Offset: 0x001653E3
		private float FollowingArmyLeaderMinScore
		{
			get
			{
				return this.FollowingArmyLeaderMaxScore * 0.5f;
			}
		}

		// Token: 0x17000E39 RID: 3641
		// (get) Token: 0x060047B0 RID: 18352 RVA: 0x001671F1 File Offset: 0x001653F1
		private float ArmyLeaderIsUnreachableScore
		{
			get
			{
				return 0.02475f;
			}
		}

		// Token: 0x060047B1 RID: 18353 RVA: 0x001671F8 File Offset: 0x001653F8
		public override void RegisterEvents()
		{
			CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, new Action<MobileParty, PartyThinkParams>(this.AiHourlyTick));
			CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, new Action<SiegeEvent>(this.OnSiegeEventStarted));
		}

		// Token: 0x060047B2 RID: 18354 RVA: 0x00167228 File Offset: 0x00165428
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x060047B3 RID: 18355 RVA: 0x0016722C File Offset: 0x0016542C
		private void OnSiegeEventStarted(SiegeEvent siegeEvent)
		{
			for (int i = 0; i < siegeEvent.BesiegedSettlement.Parties.Count; i++)
			{
				if (siegeEvent.BesiegedSettlement.Parties[i].IsLordParty)
				{
					siegeEvent.BesiegedSettlement.Parties[i].SetMoveModeHold();
				}
			}
		}

		// Token: 0x060047B4 RID: 18356 RVA: 0x00167284 File Offset: 0x00165484
		public void AiHourlyTick(MobileParty mobileParty, PartyThinkParams p)
		{
			if (mobileParty.Army == null || mobileParty.Army.LeaderParty == mobileParty)
			{
				return;
			}
			if (mobileParty.AttachedTo == null)
			{
				if (mobileParty.Army.LeaderParty.CurrentSettlement != null && mobileParty.Army.LeaderParty.CurrentSettlement.IsUnderSiege && (mobileParty.Army.LeaderParty.CurrentSettlement.SiegeEvent.IsBlockadeActive || !mobileParty.HasNavalNavigationCapability))
				{
					return;
				}
				if (mobileParty.CurrentSettlement != null && mobileParty.CurrentSettlement.IsUnderSiege)
				{
					return;
				}
			}
			MobileParty.NavigationType navigationType = MobileParty.NavigationType.None;
			float num = float.MaxValue;
			bool isTargetingPort = false;
			bool isFromPort = false;
			if (mobileParty.Army.LeaderParty.CurrentSettlement != null)
			{
				SiegeEvent siegeEvent = mobileParty.Army.LeaderParty.CurrentSettlement.SiegeEvent;
				bool flag = siegeEvent == null;
				bool flag2 = mobileParty.HasNavalNavigationCapability && mobileParty.Army.LeaderParty.CurrentSettlement.HasPort && (siegeEvent == null || (!siegeEvent.IsBlockadeActive && mobileParty.HasNavalNavigationCapability));
				if (flag)
				{
					AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobileParty, mobileParty.Army.LeaderParty.CurrentSettlement, false, out navigationType, out num, out isFromPort);
				}
				if (flag2)
				{
					MobileParty.NavigationType navigationType2;
					float num2;
					bool flag3;
					AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobileParty, mobileParty.Army.LeaderParty.CurrentSettlement, true, out navigationType2, out num2, out flag3);
					if (num2 < num)
					{
						navigationType = navigationType2;
						num = num2;
						isFromPort = flag3;
						isTargetingPort = true;
					}
				}
			}
			else
			{
				AiHelper.GetBestNavigationTypeAndDistanceOfMobilePartyForMobileParty(mobileParty, mobileParty.Army.LeaderParty, out navigationType, out num);
			}
			ValueTuple<AIBehaviorData, float> valueTuple;
			if (navigationType != MobileParty.NavigationType.None)
			{
				float num3 = this.FollowingArmyLeaderMaxScore;
				float num4 = 1f;
				float num5 = (mobileParty.Army.LeaderParty.IsMainParty ? Campaign.Current.Models.ArmyManagementCalculationModel.PlayerMobilePartySizeRatioToCallToArmy : Campaign.Current.Models.ArmyManagementCalculationModel.AIMobilePartySizeRatioToCallToArmy);
				if ((float)mobileParty.GetNumDaysForFoodToLast() < Campaign.Current.Models.ArmyManagementCalculationModel.MinimumNeededFoodInDaysToCallToArmy || mobileParty.PartySizeRatio < num5)
				{
					num3 = this.FollowingArmyLeaderMinScore;
					float num6 = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(navigationType) * 0.5f;
					if (num6 > num)
					{
						num4 = MathF.Clamp(num6 / (num + 0.1f), 1f, this.FollowingArmyLeaderMaxScore / this.FollowingArmyLeaderMinScore);
					}
				}
				AIBehaviorData item = new AIBehaviorData(mobileParty.Army.LeaderParty, AiBehavior.EscortParty, navigationType, false, isFromPort, isTargetingPort);
				float item2 = MathF.Clamp(num3 * num4, this.FollowingArmyLeaderMinScore, this.FollowingArmyLeaderMaxScore);
				valueTuple = new ValueTuple<AIBehaviorData, float>(item, item2);
				p.AddBehaviorScore(valueTuple);
				return;
			}
			AIBehaviorData item3 = new AIBehaviorData(mobileParty.Army.LeaderParty, AiBehavior.EscortParty, mobileParty.NavigationCapability, false, isFromPort, false);
			float armyLeaderIsUnreachableScore = this.ArmyLeaderIsUnreachableScore;
			valueTuple = new ValueTuple<AIBehaviorData, float>(item3, armyLeaderIsUnreachableScore);
			p.AddBehaviorScore(valueTuple);
		}
	}
}
