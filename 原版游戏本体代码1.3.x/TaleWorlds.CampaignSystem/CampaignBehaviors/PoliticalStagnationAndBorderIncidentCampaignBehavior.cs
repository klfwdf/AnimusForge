using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000432 RID: 1074
	public class PoliticalStagnationAndBorderIncidentCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x0600444B RID: 17483 RVA: 0x0014CF50 File Offset: 0x0014B150
		public override void RegisterEvents()
		{
			CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.DailyTick));
			CampaignEvents.HourlyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(this.HourlyTickSettlement));
			CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.NewGameCreated));
			CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, new Action(this.LoadFinished));
		}

		// Token: 0x0600444C RID: 17484 RVA: 0x0014CFB9 File Offset: 0x0014B1B9
		private void LoadFinished()
		{
			if (this._lastUpdateTimePerSettlement == null)
			{
				this.InitializeDictionary();
			}
		}

		// Token: 0x0600444D RID: 17485 RVA: 0x0014CFC9 File Offset: 0x0014B1C9
		private void NewGameCreated(CampaignGameStarter obj)
		{
			this.InitializeDictionary();
		}

		// Token: 0x0600444E RID: 17486 RVA: 0x0014CFD4 File Offset: 0x0014B1D4
		private void InitializeDictionary()
		{
			this._lastUpdateTimePerSettlement = new Dictionary<Settlement, CampaignTime>();
			foreach (Settlement settlement in Settlement.All)
			{
				if (settlement.IsFortification || settlement.IsVillage)
				{
					this._lastUpdateTimePerSettlement.Add(settlement, CampaignTime.Now - CampaignTime.Hours(MBRandom.RandomFloat * 3f));
				}
			}
		}

		// Token: 0x0600444F RID: 17487 RVA: 0x0014D060 File Offset: 0x0014B260
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<Dictionary<Settlement, CampaignTime>>("_lastUpdateTimePerSettlement", ref this._lastUpdateTimePerSettlement);
		}

		// Token: 0x06004450 RID: 17488 RVA: 0x0014D074 File Offset: 0x0014B274
		public void HourlyTickSettlement(Settlement settlement)
		{
			if (this._lastUpdateTimePerSettlement.ContainsKey(settlement) && this._lastUpdateTimePerSettlement[settlement].ElapsedHoursUntilNow > 3f)
			{
				float radius = Campaign.Current.EstimatedMaximumLordPartySpeedExceptPlayer * 0.25f * (float)CampaignTime.HoursInDay * 0.5f;
				LocatableSearchData<MobileParty> locatableSearchData = MobileParty.StartFindingLocatablesAroundPosition(settlement.Position.ToVec2(), radius);
				for (MobileParty mobileParty = MobileParty.FindNextLocatable(ref locatableSearchData); mobileParty != null; mobileParty = MobileParty.FindNextLocatable(ref locatableSearchData))
				{
					if (!mobileParty.IsGarrison && !mobileParty.IsMilitia)
					{
						if (mobileParty.Ai.IsAlerted && mobileParty.MapFaction == settlement.MapFaction && (mobileParty.IsCaravan || mobileParty.IsVillager))
						{
							if (mobileParty.IsCurrentlyAtSea)
							{
								settlement.NearbyNavalThreatIntensity += 0.6f;
							}
							else
							{
								settlement.NearbyLandThreatIntensity += 0.6f;
							}
						}
						if (mobileParty.Aggressiveness > 0f)
						{
							if (mobileParty.CurrentSettlement == null && mobileParty.Army == null && (mobileParty.IsBandit || FactionManager.IsAtWarAgainstFaction(mobileParty.MapFaction, settlement.MapFaction)))
							{
								float threatValueOfEnemyToSettlement = this.GetThreatValueOfEnemyToSettlement(mobileParty, settlement);
								if (mobileParty.IsCurrentlyAtSea)
								{
									settlement.NearbyNavalThreatIntensity += threatValueOfEnemyToSettlement * 3f;
								}
								else
								{
									settlement.NearbyLandThreatIntensity += threatValueOfEnemyToSettlement * 3f;
								}
							}
							else if (mobileParty.MapFaction == settlement.MapFaction)
							{
								bool flag = mobileParty.DefaultBehavior == AiBehavior.PatrolAroundPoint && !mobileParty.TargetPosition.IsOnLand;
								float num = this.GetThreatValueOfParty(mobileParty);
								if (flag)
								{
									num *= 0.5f;
								}
								if (mobileParty.IsCurrentlyAtSea || flag)
								{
									settlement.NearbyNavalAllyIntensity += num * 3f;
								}
								else
								{
									settlement.NearbyLandAllyIntensity += num * 3f;
								}
							}
						}
					}
				}
				settlement.NearbyLandThreatIntensity *= 0.85f;
				settlement.NearbyLandAllyIntensity *= 0.8f;
				settlement.NearbyNavalThreatIntensity *= 0.85f;
				settlement.NearbyNavalAllyIntensity *= 0.8f;
				this._lastUpdateTimePerSettlement[settlement] = CampaignTime.Now;
			}
		}

		// Token: 0x06004451 RID: 17489 RVA: 0x0014D2BE File Offset: 0x0014B4BE
		private float GetThreatValueOfParty(MobileParty mobileParty)
		{
			return MathF.Min(1f, mobileParty.Party.EstimatedStrength / 400f * MathF.Min(1f, mobileParty.Aggressiveness));
		}

		// Token: 0x06004452 RID: 17490 RVA: 0x0014D2EC File Offset: 0x0014B4EC
		private float GetThreatValueOfEnemyToSettlement(MobileParty mobileParty, Settlement settlement)
		{
			float num = this.GetThreatValueOfParty(mobileParty);
			if (mobileParty == MobileParty.MainParty)
			{
				num *= 2f;
			}
			if (!mobileParty.IsLordParty)
			{
				num *= 0.5f;
			}
			if (mobileParty.DefaultBehavior == AiBehavior.PatrolAroundPoint && mobileParty.TargetSettlement == settlement)
			{
				num *= 2f;
			}
			if (mobileParty.MapEvent != null && mobileParty.MapEvent.IsFieldBattle)
			{
				num = 3f * num;
			}
			return num;
		}

		// Token: 0x06004453 RID: 17491 RVA: 0x0014D35C File Offset: 0x0014B55C
		public void DailyTick()
		{
			foreach (Kingdom kingdom in Kingdom.All)
			{
				PoliticalStagnationAndBorderIncidentCampaignBehavior.UpdatePoliticallyStagnation(kingdom);
			}
		}

		// Token: 0x06004454 RID: 17492 RVA: 0x0014D3AC File Offset: 0x0014B5AC
		private static void UpdatePoliticallyStagnation(Kingdom kingdom)
		{
			float num = 1f + (float)MathF.Min(60, kingdom.Fiefs.Count) * 0.2f;
			float num2 = 2f + (float)MathF.Min(60, kingdom.Fiefs.Count) * 0.6f;
			int num3 = 1;
			foreach (Kingdom kingdom2 in Kingdom.All)
			{
				if (FactionManager.IsAtWarAgainstFaction(kingdom, kingdom2))
				{
					if ((float)kingdom2.Fiefs.Count >= num2)
					{
						num3 = -2;
						break;
					}
					if ((float)kingdom2.Fiefs.Count >= num)
					{
						num3 = -1;
					}
				}
			}
			kingdom.PoliticalStagnation += num3;
			if (kingdom.PoliticalStagnation < 0)
			{
				kingdom.PoliticalStagnation = 0;
				return;
			}
			if (kingdom.PoliticalStagnation > 300)
			{
				kingdom.PoliticalStagnation = 300;
			}
		}

		// Token: 0x0400133B RID: 4923
		private const float ThreatUpdateTimeThresholdInHours = 3f;

		// Token: 0x0400133C RID: 4924
		private Dictionary<Settlement, CampaignTime> _lastUpdateTimePerSettlement;
	}
}
