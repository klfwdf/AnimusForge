using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000028 RID: 40
	public class Army : ITrackableCampaignObject, ITrackableBase
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x0600017B RID: 379 RVA: 0x00010E8C File Offset: 0x0000F08C
		private float MinimumDistanceToTargetWhileGatheringAsAttackerArmy
		{
			get
			{
				return Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(this.LeaderParty.NavigationCapability) * 0.66f;
			}
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x0600017C RID: 380 RVA: 0x00010EA9 File Offset: 0x0000F0A9
		public float GatheringPositionMaxDistanceToTheSettlement
		{
			get
			{
				return Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(this.LeaderParty.NavigationCapability) * 0.2f;
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x0600017D RID: 381 RVA: 0x00010EC6 File Offset: 0x0000F0C6
		public float GatheringPositionMinDistanceToTheSettlement
		{
			get
			{
				return Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(this.LeaderParty.NavigationCapability) * 0.1f;
			}
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x0600017E RID: 382 RVA: 0x00010EE3 File Offset: 0x0000F0E3
		public MBReadOnlyList<MobileParty> Parties
		{
			get
			{
				return this._parties;
			}
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600017F RID: 383 RVA: 0x00010EEB File Offset: 0x0000F0EB
		public TextObject EncyclopediaLinkWithName
		{
			get
			{
				return this.ArmyOwner.EncyclopediaLinkWithName;
			}
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000180 RID: 384 RVA: 0x00010EF8 File Offset: 0x0000F0F8
		// (set) Token: 0x06000181 RID: 385 RVA: 0x00010F00 File Offset: 0x0000F100
		[SaveableProperty(3)]
		public Army.ArmyTypes ArmyType { get; set; }

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000182 RID: 386 RVA: 0x00010F09 File Offset: 0x0000F109
		// (set) Token: 0x06000183 RID: 387 RVA: 0x00010F11 File Offset: 0x0000F111
		[SaveableProperty(4)]
		public Hero ArmyOwner { get; set; }

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000184 RID: 388 RVA: 0x00010F1A File Offset: 0x0000F11A
		// (set) Token: 0x06000185 RID: 389 RVA: 0x00010F22 File Offset: 0x0000F122
		[SaveableProperty(5)]
		public float Cohesion { get; set; }

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000186 RID: 390 RVA: 0x00010F2C File Offset: 0x0000F12C
		public float DailyCohesionChange
		{
			get
			{
				return Campaign.Current.Models.ArmyManagementCalculationModel.CalculateDailyCohesionChange(this, false).ResultNumber;
			}
		}

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000187 RID: 391 RVA: 0x00010F57 File Offset: 0x0000F157
		public ExplainedNumber DailyCohesionChangeExplanation
		{
			get
			{
				return Campaign.Current.Models.ArmyManagementCalculationModel.CalculateDailyCohesionChange(this, true);
			}
		}

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000188 RID: 392 RVA: 0x00010F6F File Offset: 0x0000F16F
		public int CohesionThresholdForDispersion
		{
			get
			{
				return Campaign.Current.Models.ArmyManagementCalculationModel.CohesionThresholdForDispersion;
			}
		}

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000189 RID: 393 RVA: 0x00010F85 File Offset: 0x0000F185
		// (set) Token: 0x0600018A RID: 394 RVA: 0x00010F8D File Offset: 0x0000F18D
		[SaveableProperty(13)]
		public float Morale { get; private set; }

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x0600018B RID: 395 RVA: 0x00010F96 File Offset: 0x0000F196
		// (set) Token: 0x0600018C RID: 396 RVA: 0x00010F9E File Offset: 0x0000F19E
		[SaveableProperty(14)]
		public MobileParty LeaderParty { get; private set; }

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x0600018D RID: 397 RVA: 0x00010FA7 File Offset: 0x0000F1A7
		public int LeaderPartyAndAttachedPartiesCount
		{
			get
			{
				return this.LeaderParty.AttachedParties.Count + 1;
			}
		}

		// Token: 0x0600018E RID: 398 RVA: 0x00010FBB File Offset: 0x0000F1BB
		public override string ToString()
		{
			return this.Name.ToString();
		}

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x0600018F RID: 399 RVA: 0x00010FC8 File Offset: 0x0000F1C8
		public float EstimatedStrength
		{
			get
			{
				float num = this.LeaderParty.Party.EstimatedStrength;
				foreach (MobileParty mobileParty in this.LeaderParty.AttachedParties)
				{
					num += mobileParty.Party.EstimatedStrength;
				}
				return num;
			}
		}

		// Token: 0x06000190 RID: 400 RVA: 0x0001103C File Offset: 0x0000F23C
		public float CalculateCurrentStrength()
		{
			float num = this.LeaderParty.Party.CalculateCurrentStrength();
			foreach (MobileParty mobileParty in this.LeaderParty.AttachedParties)
			{
				num += mobileParty.Party.CalculateCurrentStrength();
			}
			return num;
		}

		// Token: 0x06000191 RID: 401 RVA: 0x000110B0 File Offset: 0x0000F2B0
		public float GetCustomStrength(BattleSideEnum side, MapEvent.PowerCalculationContext context)
		{
			float num = this.LeaderParty.Party.GetCustomStrength(side, context);
			foreach (MobileParty mobileParty in this.LeaderParty.AttachedParties)
			{
				num += mobileParty.Party.GetCustomStrength(side, context);
			}
			return num;
		}

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x06000192 RID: 402 RVA: 0x00011128 File Offset: 0x0000F328
		// (set) Token: 0x06000193 RID: 403 RVA: 0x00011130 File Offset: 0x0000F330
		public Kingdom Kingdom
		{
			get
			{
				return this._kingdom;
			}
			set
			{
				if (value != this._kingdom)
				{
					Kingdom kingdom = this._kingdom;
					if (kingdom != null)
					{
						kingdom.RemoveArmyInternal(this);
					}
					this._kingdom = value;
					Kingdom kingdom2 = this._kingdom;
					if (kingdom2 == null)
					{
						return;
					}
					kingdom2.AddArmyInternal(this);
				}
			}
		}

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000194 RID: 404 RVA: 0x00011165 File Offset: 0x0000F365
		// (set) Token: 0x06000195 RID: 405 RVA: 0x0001116D File Offset: 0x0000F36D
		public IMapPoint AiBehaviorObject
		{
			get
			{
				return this._aiBehaviorObject;
			}
			set
			{
				if (value != this._aiBehaviorObject && this.Parties.Contains(MobileParty.MainParty) && this.LeaderParty != MobileParty.MainParty)
				{
					this.StopTrackingTargetSettlement();
					this.StartTrackingTargetSettlement(value);
				}
				this._aiBehaviorObject = value;
			}
		}

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000196 RID: 406 RVA: 0x000111AB File Offset: 0x0000F3AB
		// (set) Token: 0x06000197 RID: 407 RVA: 0x000111B3 File Offset: 0x0000F3B3
		[SaveableProperty(17)]
		public TextObject Name { get; private set; }

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x06000198 RID: 408 RVA: 0x000111BC File Offset: 0x0000F3BC
		private float InactivityThreshold
		{
			get
			{
				return (float)CampaignTime.HoursInDay * 2f;
			}
		}

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x06000199 RID: 409 RVA: 0x000111CC File Offset: 0x0000F3CC
		public int TotalHealthyMembers
		{
			get
			{
				return this.LeaderParty.Party.NumberOfHealthyMembers + this.LeaderParty.AttachedParties.Sum((MobileParty mobileParty) => mobileParty.Party.NumberOfHealthyMembers);
			}
		}

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x0600019A RID: 410 RVA: 0x0001121C File Offset: 0x0000F41C
		public int TotalManCount
		{
			get
			{
				return this.LeaderParty.Party.MemberRoster.TotalManCount + this.LeaderParty.AttachedParties.Sum((MobileParty mobileParty) => mobileParty.Party.MemberRoster.TotalManCount);
			}
		}

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x0600019B RID: 411 RVA: 0x00011270 File Offset: 0x0000F470
		public int TotalRegularCount
		{
			get
			{
				return this.LeaderParty.Party.MemberRoster.TotalRegulars + this.LeaderParty.AttachedParties.Sum((MobileParty mobileParty) => mobileParty.Party.MemberRoster.TotalRegulars);
			}
		}

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x0600019C RID: 412 RVA: 0x000112C2 File Offset: 0x0000F4C2
		public bool IsReady
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x0600019D RID: 413 RVA: 0x000112C5 File Offset: 0x0000F4C5
		public bool IsArmyInGatheringState
		{
			get
			{
				return this.LeaderParty.AttachedParties.Count + 1 < this._parties.Count;
			}
		}

		// Token: 0x0600019E RID: 414 RVA: 0x000112E8 File Offset: 0x0000F4E8
		public Army(Kingdom kingdom, MobileParty leaderParty, Army.ArmyTypes armyType)
		{
			this.Kingdom = kingdom;
			this._parties = new MBList<MobileParty>();
			this._armyGatheringStartTime = 0f;
			this._creationTime = CampaignTime.Now;
			this.LeaderParty = leaderParty;
			this.LeaderParty.Army = this;
			this.ArmyOwner = this.LeaderParty.LeaderHero;
			this.UpdateName();
			this.ArmyType = armyType;
			this.AddEventHandlers();
			this.Cohesion = 100f;
		}

		// Token: 0x0600019F RID: 415 RVA: 0x00011368 File Offset: 0x0000F568
		public void UpdateName()
		{
			this.Name = new TextObject("{=nbmctMLk}{LEADER_NAME}{.o} Army", null);
			this.Name.SetTextVariable("LEADER_NAME", (this.ArmyOwner != null) ? this.ArmyOwner.Name : ((this.LeaderParty.Owner != null) ? this.LeaderParty.Owner.Name : TextObject.GetEmpty()));
		}

		// Token: 0x060001A0 RID: 416 RVA: 0x000113D0 File Offset: 0x0000F5D0
		private void AddEventHandlers()
		{
			if (this._creationTime == default(CampaignTime))
			{
				this._creationTime = CampaignTime.HoursFromNow(MBRandom.RandomFloat - 2f);
			}
			CampaignTime g = CampaignTime.Now - this._creationTime;
			CampaignTime initialWait = CampaignTime.Hours(1f + (float)((int)g.ToHours)) - g;
			this._hourlyTickEvent = CampaignPeriodicEventManager.CreatePeriodicEvent(CampaignTime.Hours(1f), initialWait);
			this._hourlyTickEvent.AddHandler(new MBCampaignEvent.CampaignEventDelegate(this.HourlyTick));
			this._tickEvent = CampaignPeriodicEventManager.CreatePeriodicEvent(CampaignTime.Hours(0.1f), CampaignTime.Hours(1f));
			this._tickEvent.AddHandler(new MBCampaignEvent.CampaignEventDelegate(this.Tick));
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
			CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, new Action<SiegeEvent>(this.OnSiegeStarted));
		}

		// Token: 0x060001A1 RID: 417 RVA: 0x000114C8 File Offset: 0x0000F6C8
		private void OnSiegeStarted(SiegeEvent siegeEvent)
		{
			Settlement settlement;
			if (this.IsArmyInGatheringState && (settlement = this.AiBehaviorObject as Settlement) != null && settlement == siegeEvent.BesiegedSettlement && this.LeaderParty.SiegeEvent == null)
			{
				this.FindBestGatheringSettlementAndMoveTheLeader(settlement);
			}
		}

		// Token: 0x060001A2 RID: 418 RVA: 0x0001150C File Offset: 0x0000F70C
		private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			Settlement settlement2;
			if (this.IsArmyInGatheringState && (settlement2 = this.AiBehaviorObject as Settlement) != null && settlement2 == settlement && settlement.MapFaction != this.LeaderParty.MapFaction)
			{
				this.FindBestGatheringSettlementAndMoveTheLeader(settlement);
			}
		}

		// Token: 0x060001A3 RID: 419 RVA: 0x0001154E File Offset: 0x0000F74E
		internal void OnAfterLoad()
		{
			this.AddEventHandlers();
		}

		// Token: 0x060001A4 RID: 420 RVA: 0x00011556 File Offset: 0x0000F756
		public bool DoesLeaderPartyAndAttachedPartiesContain(MobileParty party)
		{
			return this.LeaderParty == party || this.LeaderParty.AttachedParties.IndexOf(party) >= 0;
		}

		// Token: 0x060001A5 RID: 421 RVA: 0x0001157C File Offset: 0x0000F77C
		public void BoostCohesionWithInfluence(float cohesionToGain, int cost)
		{
			if (this.LeaderParty.LeaderHero.Clan.Influence >= (float)cost)
			{
				ChangeClanInfluenceAction.Apply(this.LeaderParty.LeaderHero.Clan, (float)(-(float)cost));
				this.Cohesion += cohesionToGain;
				this._numberOfBoosts++;
			}
		}

		// Token: 0x060001A6 RID: 422 RVA: 0x000115D8 File Offset: 0x0000F7D8
		private void ThinkAboutCohesionBoost()
		{
			float num = 0f;
			foreach (MobileParty mobileParty in this.Parties)
			{
				float partySizeRatio = mobileParty.PartySizeRatio;
				num += partySizeRatio;
			}
			float b = num / (float)this.Parties.Count;
			float num2 = MathF.Min(1f, b);
			float num3 = Campaign.Current.Models.TargetScoreCalculatingModel.CurrentObjectiveValue(this.LeaderParty);
			if (num3 > 0.01f)
			{
				num3 *= num2;
				num3 *= ((this._numberOfBoosts == 0) ? 1f : (1f / MathF.Pow(1f + (float)this._numberOfBoosts, 0.7f)));
				ArmyManagementCalculationModel armyManagementCalculationModel = Campaign.Current.Models.ArmyManagementCalculationModel;
				float num4 = MathF.Min(100f, 100f - this.Cohesion);
				int num5 = armyManagementCalculationModel.CalculateTotalInfluenceCost(this, num4);
				if (this.LeaderParty.Party.Owner.Clan.Influence > (float)num5)
				{
					float num6 = MathF.Min(9f, MathF.Sqrt(this.LeaderParty.Party.Owner.Clan.Influence / (float)num5));
					float num7 = ((this.LeaderParty.BesiegedSettlement != null) ? 2f : 1f);
					if (this.LeaderParty.BesiegedSettlement == null && this.LeaderParty.DefaultBehavior == AiBehavior.BesiegeSettlement)
					{
						float distance;
						if (this.LeaderParty.CurrentSettlement != null)
						{
							distance = Campaign.Current.Models.MapDistanceModel.GetDistance(this.LeaderParty.CurrentSettlement, this.LeaderParty.TargetSettlement, this.LeaderParty.IsCurrentlyAtSea, this.LeaderParty.IsTargetingPort, this.LeaderParty.NavigationCapability);
						}
						else
						{
							float num8;
							distance = Campaign.Current.Models.MapDistanceModel.GetDistance(this.LeaderParty, this.LeaderParty.TargetSettlement, this.LeaderParty.IsTargetingPort, this.LeaderParty.NavigationCapability, out num8);
						}
						float num9 = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(this.LeaderParty.NavigationCapability) * 2f;
						if (distance < num9)
						{
							num7 += (1f - distance / num9) * (1f - distance / num9);
						}
					}
					float num10 = num3 * num7 * 0.25f * num6;
					if (MBRandom.RandomFloat < num10)
					{
						this.BoostCohesionWithInfluence(num4, num5);
					}
				}
			}
		}

		// Token: 0x060001A7 RID: 423 RVA: 0x00011864 File Offset: 0x0000FA64
		public void RecalculateArmyMorale()
		{
			float num = 0f;
			foreach (MobileParty mobileParty in this.Parties)
			{
				num += mobileParty.Morale;
			}
			this.Morale = num / (float)this.Parties.Count;
		}

		// Token: 0x060001A8 RID: 424 RVA: 0x000118D4 File Offset: 0x0000FAD4
		private void HourlyTick(MBCampaignEvent campaignEvent, object[] delegateParams)
		{
			bool flag = this.LeaderParty.CurrentSettlement != null && this.LeaderParty.CurrentSettlement.SiegeEvent != null;
			if (this.LeaderParty.MapEvent != null || flag)
			{
				return;
			}
			this.RecalculateArmyMorale();
			this.Cohesion += this.DailyCohesionChange / (float)CampaignTime.HoursInDay;
			if (this.LeaderParty != MobileParty.MainParty)
			{
				if (this._armyGatheringStartTime == 0f)
				{
					this.CheckAndSetArmyGatheringTime();
				}
				this.MoveLeaderToGatheringLocationIfNeeded();
				if (this.Cohesion < 50f)
				{
					this.ThinkAboutCohesionBoost();
					if (this.Cohesion < 30f && this.LeaderParty.MapEvent == null && this.LeaderParty.SiegeEvent == null)
					{
						DisbandArmyAction.ApplyByCohesionDepleted(this);
						return;
					}
				}
				if (this.LeaderParty.DefaultBehavior == AiBehavior.BesiegeSettlement && this.IsAnotherEnemyBesiegingTarget())
				{
					this.FinishArmyObjective();
				}
			}
			this.CheckArmyDispersion();
			this.ApplyHostileActionInfluenceAwards();
		}

		// Token: 0x060001A9 RID: 425 RVA: 0x000119C8 File Offset: 0x0000FBC8
		private void CheckAndSetArmyGatheringTime()
		{
			Settlement toSettlement;
			float num;
			if ((toSettlement = this.AiBehaviorObject as Settlement) != null && this.LeaderParty.DefaultBehavior == AiBehavior.GoToPoint && ((this.LeaderParty.CurrentSettlement != null) ? Campaign.Current.Models.MapDistanceModel.GetDistance(this.LeaderParty.CurrentSettlement, toSettlement, false, false, this.LeaderParty.DesiredAiNavigationType) : Campaign.Current.Models.MapDistanceModel.GetDistance(this.LeaderParty, toSettlement, false, this.LeaderParty.DesiredAiNavigationType, out num)) < this.GatheringPositionMaxDistanceToTheSettlement * 2f)
			{
				this._armyGatheringStartTime = Campaign.CurrentTime;
			}
		}

		// Token: 0x060001AA RID: 426 RVA: 0x00011A74 File Offset: 0x0000FC74
		private void Tick(MBCampaignEvent campaignEvent, object[] delegateParams)
		{
			foreach (MobileParty mobileParty in this._parties)
			{
				if (mobileParty.AttachedTo == null && mobileParty.Army != null && mobileParty.ShortTermTargetParty == this.LeaderParty && mobileParty.MapEvent == null && mobileParty.IsCurrentlyAtSea == this.LeaderParty.IsCurrentlyAtSea && (mobileParty.Position - this.LeaderParty.Position).LengthSquared < Campaign.Current.Models.EncounterModel.NeededMaximumDistanceForEncounteringMobileParty)
				{
					this.AddPartyToMergedParties(mobileParty);
					if (mobileParty.IsMainParty)
					{
						Campaign.Current.CameraFollowParty = this.LeaderParty.Party;
					}
					CampaignEventDispatcher.Instance.OnArmyOverlaySetDirty();
				}
			}
		}

		// Token: 0x060001AB RID: 427 RVA: 0x00011B68 File Offset: 0x0000FD68
		private void CheckArmyDispersion()
		{
			if (this.LeaderParty == MobileParty.MainParty)
			{
				if (this.Cohesion <= 0.1f)
				{
					DisbandArmyAction.ApplyByCohesionDepleted(this);
					return;
				}
			}
			else
			{
				int num = (this.LeaderParty.Party.IsStarving ? 1 : 0);
				using (List<MobileParty>.Enumerator enumerator = this.LeaderParty.AttachedParties.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (enumerator.Current.Party.IsStarving)
						{
							num++;
						}
					}
				}
				if ((float)num / (float)this.LeaderPartyAndAttachedPartiesCount > 0.5f)
				{
					DisbandArmyAction.ApplyByFoodProblem(this);
					return;
				}
				if (MBRandom.RandomFloat < 0.25f)
				{
					if (!this.LeaderParty.MapFaction.FactionsAtWarWith.AnyQ((IFaction x) => x.Fiefs.Any<Town>()))
					{
						DisbandArmyAction.ApplyByNoActiveWar(this);
						return;
					}
				}
				if (this.Cohesion <= 0.1f)
				{
					DisbandArmyAction.ApplyByCohesionDepleted(this);
					return;
				}
				if (!this.LeaderParty.IsActive)
				{
					DisbandArmyAction.ApplyByUnknownReason(this);
				}
				this.CheckInactivity();
			}
		}

		// Token: 0x060001AC RID: 428 RVA: 0x00011C94 File Offset: 0x0000FE94
		private void CheckInactivity()
		{
			if (!this.IsWaitingForArmyMembers())
			{
				AiBehavior aiBehavior = this.LeaderParty.DefaultBehavior;
				switch (aiBehavior)
				{
				case AiBehavior.Hold:
					this._inactivityCounter++;
					goto IL_A1;
				case AiBehavior.None:
					goto IL_A1;
				case AiBehavior.GoToSettlement:
					if (!this.LeaderParty.TargetSettlement.MapFaction.IsAtWarWith(this.LeaderParty.MapFaction))
					{
						this._inactivityCounter++;
						goto IL_A1;
					}
					goto IL_A1;
				case AiBehavior.AssaultSettlement:
				case AiBehavior.RaidSettlement:
				case AiBehavior.BesiegeSettlement:
					break;
				default:
					if (aiBehavior == AiBehavior.PatrolAroundPoint)
					{
						this._inactivityCounter++;
						goto IL_A1;
					}
					if (aiBehavior != AiBehavior.DefendSettlement)
					{
						goto IL_A1;
					}
					break;
				}
				this._inactivityCounter -= 2;
				IL_A1:
				aiBehavior = this.LeaderParty.ShortTermBehavior;
				if (aiBehavior == AiBehavior.EngageParty)
				{
					this._inactivityCounter--;
				}
			}
			this._inactivityCounter = MBMath.ClampInt(this._inactivityCounter, 0, (int)this.InactivityThreshold);
			if ((float)this._inactivityCounter >= this.InactivityThreshold)
			{
				DisbandArmyAction.ApplyByInactivity(this);
			}
		}

		// Token: 0x060001AD RID: 429 RVA: 0x00011D90 File Offset: 0x0000FF90
		private void MoveLeaderToGatheringLocationIfNeeded()
		{
			if (this.AiBehaviorObject != null && this.LeaderParty.MapEvent == null && this.LeaderParty.ShortTermBehavior == AiBehavior.Hold)
			{
				Settlement settlement = this.AiBehaviorObject as Settlement;
				CampaignVec2 centerPosition = (this.LeaderParty.IsTargetingPort ? settlement.PortPosition : settlement.GatePosition);
				if (!settlement.IsUnderSiege && !settlement.IsUnderRaid)
				{
					this.SendLeaderPartyToReachablePointAroundPosition(centerPosition, 6f, 3f);
				}
			}
		}

		// Token: 0x060001AE RID: 430 RVA: 0x00011E08 File Offset: 0x00010008
		private void ApplyHostileActionInfluenceAwards()
		{
			if (this.LeaderParty.LeaderHero != null && this.LeaderParty.MapEvent != null)
			{
				if (this.LeaderParty.MapEvent.IsRaid && this.LeaderParty.MapEvent.DefenderSide.TroopCount == 0)
				{
					float hourlyInfluenceAwardForRaidingEnemyVillage = Campaign.Current.Models.DiplomacyModel.GetHourlyInfluenceAwardForRaidingEnemyVillage(this.LeaderParty);
					GainKingdomInfluenceAction.ApplyForRaidingEnemyVillage(this.LeaderParty, hourlyInfluenceAwardForRaidingEnemyVillage);
					return;
				}
				if (this.LeaderParty.BesiegedSettlement != null && this.LeaderParty.MapFaction.IsAtWarWith(this.LeaderParty.BesiegedSettlement.MapFaction))
				{
					float hourlyInfluenceAwardForBesiegingEnemyFortification = Campaign.Current.Models.DiplomacyModel.GetHourlyInfluenceAwardForBesiegingEnemyFortification(this.LeaderParty);
					GainKingdomInfluenceAction.ApplyForBesiegingEnemySettlement(this.LeaderParty, hourlyInfluenceAwardForBesiegingEnemyFortification);
				}
			}
		}

		// Token: 0x060001AF RID: 431 RVA: 0x00011EDC File Offset: 0x000100DC
		public TextObject GetNotificationText()
		{
			if (this.LeaderParty != MobileParty.MainParty)
			{
				TextObject textObject = GameTexts.FindText("str_army_gather", null);
				StringHelpers.SetCharacterProperties("ARMY_LEADER", this.LeaderParty.LeaderHero.CharacterObject, textObject, false);
				textObject.SetTextVariable("SETTLEMENT_NAME", this.AiBehaviorObject.Name);
				return textObject;
			}
			return null;
		}

		// Token: 0x060001B0 RID: 432 RVA: 0x00011F39 File Offset: 0x00010139
		public TextObject GetLongTermBehaviorText(bool setWithLink = false)
		{
			if (this.LeaderParty.IsMainParty)
			{
				return this.GetLongTermBehaviorTextForPlayerParty();
			}
			return this.GetLongTermBehaviorTextForAILeadedParty(setWithLink);
		}

		// Token: 0x060001B1 RID: 433 RVA: 0x00011F58 File Offset: 0x00010158
		private TextObject GetLongTermBehaviorTextForPlayerParty()
		{
			TextObject textObject;
			if (MobileParty.MainParty.TargetSettlement != null && MobileParty.MainParty.CurrentSettlement != MobileParty.MainParty.TargetSettlement)
			{
				textObject = GameTexts.FindText("str_army_going_to_settlement", null);
				textObject.SetTextVariable("SETTLEMENT_NAME", this.LeaderParty.Ai.AiBehaviorPartyBase.Name);
			}
			else if (MobileParty.MainParty.CurrentSettlement != null)
			{
				textObject = GameTexts.FindText("str_army_waiting_in_settlement", null);
				textObject.SetTextVariable("SETTLEMENT_NAME", MobileParty.MainParty.CurrentSettlement.Name);
			}
			else if (MobileParty.MainParty.TargetParty != null)
			{
				textObject = new TextObject("{=P4QFKVSU}Moving to {TARGET_PARTY}.", null);
				textObject.SetTextVariable("TARGET_PARTY", MobileParty.MainParty.TargetParty.Name);
			}
			else if (MobileParty.MainParty.IsMoving)
			{
				textObject = new TextObject("{=b9TbdM9A}Moving to a point.", null);
			}
			else
			{
				textObject = new TextObject("{=RClxLG6N}Holding.", null);
			}
			return textObject;
		}

		// Token: 0x060001B2 RID: 434 RVA: 0x0001204C File Offset: 0x0001024C
		private TextObject GetLongTermBehaviorTextForAILeadedParty(bool setWithLink)
		{
			AiBehavior defaultBehavior = this.LeaderParty.DefaultBehavior;
			if (defaultBehavior <= AiBehavior.GoToPoint)
			{
				switch (defaultBehavior)
				{
				case AiBehavior.Hold:
					break;
				case AiBehavior.None:
				case AiBehavior.AssaultSettlement:
					goto IL_29A;
				case AiBehavior.GoToSettlement:
				{
					TextObject textObject = ((this.LeaderParty.CurrentSettlement == null) ? GameTexts.FindText("str_army_going_to_settlement", null) : GameTexts.FindText("str_army_waiting_in_settlement", null));
					textObject.SetTextVariable("SETTLEMENT_NAME", (setWithLink && this.AiBehaviorObject is Settlement) ? ((Settlement)this.AiBehaviorObject).EncyclopediaLinkWithName : (this.AiBehaviorObject.Name ?? this.LeaderParty.Ai.AiBehaviorPartyBase.Name));
					return textObject;
				}
				case AiBehavior.RaidSettlement:
				{
					Settlement settlement = (Settlement)this.AiBehaviorObject;
					TextObject textObject = ((this.LeaderParty.MapEvent != null && this.LeaderParty.MapEvent.IsRaid) ? GameTexts.FindText("str_army_raiding", null) : GameTexts.FindText("str_army_raiding_travelling", null));
					textObject.SetTextVariable("SETTLEMENT_NAME", setWithLink ? settlement.EncyclopediaLinkWithName : this.AiBehaviorObject.Name);
					return textObject;
				}
				case AiBehavior.BesiegeSettlement:
				{
					Settlement settlement2 = (Settlement)this.AiBehaviorObject;
					TextObject textObject = GameTexts.FindText((this.LeaderParty.SiegeEvent != null) ? "str_army_besieging" : "str_army_besieging_travelling", null);
					if (settlement2.IsVillage)
					{
						textObject = GameTexts.FindText("str_army_patrolling_travelling", null);
					}
					textObject.SetTextVariable("SETTLEMENT_NAME", setWithLink ? settlement2.EncyclopediaLinkWithName : this.AiBehaviorObject.Name);
					return textObject;
				}
				default:
					if (defaultBehavior != AiBehavior.GoToPoint)
					{
						goto IL_29A;
					}
					break;
				}
				if (this.IsWaitingForArmyMembers() && this.AiBehaviorObject != null)
				{
					TextObject textObject = GameTexts.FindText("str_army_gathering", null);
					textObject.SetTextVariable("SETTLEMENT_NAME", setWithLink ? ((Settlement)this.AiBehaviorObject).EncyclopediaLinkWithName : this.AiBehaviorObject.Name);
					return textObject;
				}
			}
			else
			{
				if (defaultBehavior == AiBehavior.PatrolAroundPoint)
				{
					TextObject textObject = GameTexts.FindText("str_army_patrolling_travelling", null);
					textObject.SetTextVariable("SETTLEMENT_NAME", setWithLink ? ((Settlement)this.AiBehaviorObject).EncyclopediaLinkWithName : this.AiBehaviorObject.Name);
					return textObject;
				}
				if (defaultBehavior == AiBehavior.DefendSettlement)
				{
					Settlement settlement2 = (Settlement)this.AiBehaviorObject;
					TextObject textObject = ((this.LeaderParty.Position.Distance(settlement2.Position) > Campaign.Current.EstimatedAverageLordPartySpeed * (float)CampaignTime.HoursInDay * 0.33f) ? GameTexts.FindText("str_army_defending_travelling", null) : GameTexts.FindText("str_army_defending", null));
					textObject.SetTextVariable("SETTLEMENT_NAME", setWithLink ? settlement2.EncyclopediaLinkWithName : this.AiBehaviorObject.Name);
					return textObject;
				}
			}
			IL_29A:
			if (this.LeaderParty.MapEvent != null)
			{
				TextObject textObject;
				if (this.LeaderParty.MapEvent.MapEventSettlement != null)
				{
					textObject = ((this.LeaderParty.MapEventSide == this.LeaderParty.MapEvent.DefenderSide) ? new TextObject("{=rGy8vjOv}Defending {TARGET_SETTLEMENT}.", null) : new TextObject("{=exnL6SS7}Attacking {TARGET_SETTLEMENT}.", null));
					textObject.SetTextVariable("TARGET_SETTLEMENT", this.LeaderParty.MapEvent.MapEventSettlement.Name);
					return textObject;
				}
				textObject = new TextObject("{=5bzk75Ql}Engaging {TARGET_PARTY}.", null);
				textObject.SetTextVariable("TARGET_PARTY", (this.LeaderParty.MapEventSide == this.LeaderParty.MapEvent.DefenderSide) ? this.LeaderParty.MapEvent.AttackerSide.LeaderParty.Name : this.LeaderParty.MapEvent.DefenderSide.LeaderParty.Name);
				return textObject;
			}
			else
			{
				if (this.LeaderParty.SiegeEvent != null)
				{
					TextObject textObject = new TextObject("{=JTxI3sW2}Besieging {TARGET_SETTLEMENT}.", null);
					textObject.SetTextVariable("TARGET_SETTLEMENT", this.LeaderParty.BesiegedSettlement.Name);
					return textObject;
				}
				return TextObject.GetEmpty();
			}
		}

		// Token: 0x060001B3 RID: 435 RVA: 0x00012418 File Offset: 0x00010618
		public void Gather(Settlement initialHostileSettlement, MBReadOnlyList<MobileParty> partiesToCallToArmy = null)
		{
			Settlement gatheringPoint = null;
			if (this.LeaderParty != MobileParty.MainParty)
			{
				this.FindBestGatheringSettlementAndMoveTheLeader(initialHostileSettlement);
				if (partiesToCallToArmy == null)
				{
					goto IL_93;
				}
				using (List<MobileParty>.Enumerator enumerator = partiesToCallToArmy.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						MobileParty mobileParty = enumerator.Current;
						mobileParty.Army = this;
					}
					goto IL_93;
				}
			}
			Settlement settlement;
			if ((settlement = SettlementHelper.FindNearestSettlementToMobileParty(MobileParty.MainParty, MobileParty.MainParty.NavigationCapability, (Settlement x) => x.IsFortification || x.IsVillage)) == null)
			{
				CampaignVec2 position = MobileParty.MainParty.Position;
				settlement = SettlementHelper.FindNearestSettlementToPoint(position, null);
			}
			gatheringPoint = settlement;
			IL_93:
			GatherArmyAction.Apply(this.LeaderParty, gatheringPoint);
		}

		// Token: 0x060001B4 RID: 436 RVA: 0x000124D4 File Offset: 0x000106D4
		private void FindBestGatheringSettlementAndMoveTheLeader(Settlement focusSettlement)
		{
			Settlement settlement = null;
			Hero leaderHero = this.LeaderParty.LeaderHero;
			float num = float.MinValue;
			if (leaderHero != null && leaderHero.IsActive)
			{
				using (List<Settlement>.Enumerator enumerator = this.Kingdom.Settlements.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						Settlement settlement2 = enumerator.Current;
						if (settlement2.IsFortification && !settlement2.IsUnderSiege)
						{
							float distance;
							if (this.LeaderParty.CurrentSettlement == null)
							{
								float num2;
								distance = Campaign.Current.Models.MapDistanceModel.GetDistance(this.LeaderParty, settlement2, false, this.LeaderParty.NavigationCapability, out num2);
							}
							else
							{
								float num2;
								distance = Campaign.Current.Models.MapDistanceModel.GetDistance(this.LeaderParty.CurrentSettlement, settlement2, false, false, this.LeaderParty.NavigationCapability, out num2);
							}
							if (distance < Campaign.MapDiagonalSquared)
							{
								float distance2 = Campaign.Current.Models.MapDistanceModel.GetDistance(focusSettlement, settlement2, false, false, this.LeaderParty.NavigationCapability);
								if (distance2 < Campaign.MapDiagonalSquared && distance2 > this.MinimumDistanceToTargetWhileGatheringAsAttackerArmy)
								{
									float num3 = 0f;
									if (settlement == null)
									{
										num3 += 0.001f;
									}
									if (settlement2 != focusSettlement && settlement2.Party.MapEvent == null)
									{
										if (settlement2.MapFaction == this.Kingdom)
										{
											num3 += 10f;
										}
										else if (!FactionManager.IsAtWarAgainstFaction(settlement2.MapFaction, this.Kingdom))
										{
											num3 += 2f;
										}
										bool flag = false;
										foreach (Army army in this.Kingdom.Armies)
										{
											if (army != this && army.AiBehaviorObject == settlement2)
											{
												flag = true;
											}
										}
										if (!flag)
										{
											num3 += 10f;
										}
										float num4 = distance2 / (Campaign.MapDiagonalSquared * 0.1f);
										float num5 = 20f * (1f - num4);
										float num6 = (settlement2.Position.ToVec2() - this.LeaderParty.Position.ToVec2()).Length / (Campaign.MapDiagonalSquared * 0.1f);
										float num7 = 5f * (1f - num6);
										float num8 = num3 + num5 * 0.5f + num7 * 0.1f;
										if (num8 > num)
										{
											num = num8;
											settlement = settlement2;
										}
									}
								}
							}
						}
					}
					goto IL_295;
				}
			}
			settlement = (Settlement)this.AiBehaviorObject;
			IL_295:
			if (settlement == null)
			{
				settlement = SettlementHelper.FindNearestFortificationToMobileParty(this.LeaderParty, this.LeaderParty.NavigationCapability, null);
			}
			this.AiBehaviorObject = settlement;
			CampaignVec2 gatePosition = settlement.GatePosition;
			this.SendLeaderPartyToReachablePointAroundPosition(gatePosition, this.GatheringPositionMaxDistanceToTheSettlement, this.GatheringPositionMinDistanceToTheSettlement);
		}

		// Token: 0x060001B5 RID: 437 RVA: 0x000127E8 File Offset: 0x000109E8
		public bool IsWaitingForArmyMembers()
		{
			if (this._armyGatheringStartTime > 0f)
			{
				float num = Campaign.CurrentTime - this._armyGatheringStartTime;
				float num2 = this.EstimatedStrength / this.Parties.SumQ((MobileParty x) => x.Party.EstimatedStrength);
				bool flag;
				if (num < Campaign.Current.Models.ArmyManagementCalculationModel.MaximumWaitTime)
				{
					flag = num2 > 0.9f;
				}
				else
				{
					float num3 = (num - Campaign.Current.Models.ArmyManagementCalculationModel.MaximumWaitTime) * 0.01f;
					flag = num2 > 0.75f - num3;
				}
				return !flag;
			}
			return true;
		}

		// Token: 0x060001B6 RID: 438 RVA: 0x00012898 File Offset: 0x00010A98
		private bool IsAnotherEnemyBesiegingTarget()
		{
			Settlement settlement = (Settlement)this.AiBehaviorObject;
			return this.ArmyType == Army.ArmyTypes.Besieger && settlement.IsUnderSiege && settlement.SiegeEvent.BesiegerCamp.MapFaction.IsAtWarWith(this.LeaderParty.MapFaction);
		}

		// Token: 0x060001B7 RID: 439 RVA: 0x000128E3 File Offset: 0x00010AE3
		public void FinishArmyObjective()
		{
			this.LeaderParty.SetMoveModeHold();
			this.AiBehaviorObject = null;
		}

		// Token: 0x060001B8 RID: 440 RVA: 0x000128F8 File Offset: 0x00010AF8
		internal void DisperseInternal(Army.ArmyDispersionReason reason = Army.ArmyDispersionReason.Unknown)
		{
			if (this._armyIsDispersing)
			{
				return;
			}
			CampaignEventDispatcher.Instance.OnArmyDispersed(this, reason, this.Parties.Contains(MobileParty.MainParty));
			this._armyIsDispersing = true;
			int num = 0;
			for (int i = this.Parties.Count - 1; i >= num; i--)
			{
				MobileParty mobileParty = this.Parties[i];
				bool flag = mobileParty.AttachedTo == this.LeaderParty;
				mobileParty.Army = null;
				if (flag && mobileParty.CurrentSettlement == null && mobileParty.IsActive && (!this.LeaderParty.IsCurrentlyAtSea || mobileParty.HasNavalNavigationCapability))
				{
					mobileParty.Position = NavigationHelper.FindReachablePointAroundPosition(this.LeaderParty.Position, mobileParty.NavigationCapability, 1f, 0f, false);
					mobileParty.SetMoveModeHold();
				}
			}
			this._parties.Clear();
			this.Kingdom = null;
			if (this.LeaderParty == MobileParty.MainParty)
			{
				MapState mapState = Game.Current.GameStateManager.ActiveState as MapState;
				if (mapState != null)
				{
					mapState.OnDispersePlayerLeadedArmy();
				}
			}
			this._hourlyTickEvent.DeletePeriodicEvent();
			this._tickEvent.DeletePeriodicEvent();
			this._armyIsDispersing = false;
		}

		// Token: 0x060001B9 RID: 441 RVA: 0x00012A1C File Offset: 0x00010C1C
		public Vec2 GetRelativePositionForParty(MobileParty mobileParty, Vec2 armyFacing)
		{
			float num = 0.5f;
			float num2 = (float)MathF.Ceiling(-1f + MathF.Sqrt(1f + 8f * (float)(this.LeaderParty.AttachedParties.Count - 1))) / 4f * num * 0.5f + num;
			int num3 = -1;
			for (int i = 0; i < this.LeaderParty.AttachedParties.Count; i++)
			{
				if (this.LeaderParty.AttachedParties[i] == mobileParty)
				{
					num3 = i;
					break;
				}
			}
			int num4 = MathF.Ceiling((-1f + MathF.Sqrt(1f + 8f * (float)(num3 + 2))) / 2f) - 1;
			int num5 = num3 + 1 - num4 * (num4 + 1) / 2;
			bool flag = (num4 & 1) != 0;
			num5 = ((((num5 & 1) != 0) ? (-1 - num5) : num5) >> 1) * (flag ? (-1) : 1);
			float num6 = 1.25f;
			CampaignVec2 v = new CampaignVec2(this.LeaderParty.VisualPosition2DWithoutError + -armyFacing * 0.1f * (float)this.LeaderParty.AttachedParties.Count, !this.LeaderParty.IsCurrentlyAtSea);
			Vec2 vec = v.ToVec2() - (float)MathF.Sign((float)num5 - (((num4 & 1) != 0) ? 0.5f : 0f)) * armyFacing.LeftVec() * num2;
			int[] invalidTerrainTypesForNavigationType = Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(this.LeaderParty.IsCurrentlyAtSea ? MobileParty.NavigationType.Naval : MobileParty.NavigationType.Default);
			Vec2 lastPointOnNavigationMeshFromPositionToDestination = Campaign.Current.MapSceneWrapper.GetLastPointOnNavigationMeshFromPositionToDestination(v.Face, v.ToVec2(), vec, invalidTerrainTypesForNavigationType);
			if ((vec - lastPointOnNavigationMeshFromPositionToDestination).LengthSquared > 2.25E-06f)
			{
				num = num * (v - lastPointOnNavigationMeshFromPositionToDestination).Length / num2;
				num6 = num6 * (v - lastPointOnNavigationMeshFromPositionToDestination).Length / (num2 / 1.5f);
			}
			if (this.LeaderParty.IsCurrentlyAtSea)
			{
				num6 *= 3f;
				num *= 3f;
			}
			return new Vec2((flag ? (-num * 0.5f) : 0f) + (float)num5 * num + mobileParty.Party.RandomFloat(-0.25f, 0.25f) * 0.6f * num, ((float)(-(float)num4) + mobileParty.Party.RandomFloatWithSeed(1U, -0.25f, 0.25f)) * num6 * 0.3f);
		}

		// Token: 0x060001BA RID: 442 RVA: 0x00012CA8 File Offset: 0x00010EA8
		public void AddPartyToMergedParties(MobileParty mobileParty)
		{
			mobileParty.AttachedTo = this.LeaderParty;
			if (mobileParty.IsMainParty)
			{
				MapState mapState = GameStateManager.Current.ActiveState as MapState;
				if (mapState != null)
				{
					mapState.OnJoinArmy();
				}
				Hero leaderHero = this.LeaderParty.LeaderHero;
				if (leaderHero != null && leaderHero != Hero.MainHero && !leaderHero.HasMet)
				{
					leaderHero.SetHasMet();
				}
			}
		}

		// Token: 0x060001BB RID: 443 RVA: 0x00012D08 File Offset: 0x00010F08
		internal void OnRemovePartyInternal(MobileParty mobileParty)
		{
			mobileParty.Ai.SetInitiative(1f, 1f, 24f);
			this._parties.Remove(mobileParty);
			CampaignEventDispatcher.Instance.OnPartyRemovedFromArmy(mobileParty);
			if (this == MobileParty.MainParty.Army && !this._armyIsDispersing)
			{
				CampaignEventDispatcher.Instance.OnArmyOverlaySetDirty();
			}
			mobileParty.AttachedTo = null;
			if (this.LeaderParty == mobileParty && !this._armyIsDispersing)
			{
				DisbandArmyAction.ApplyByLeaderPartyRemoved(this);
			}
			if (mobileParty == MobileParty.MainParty)
			{
				Campaign.Current.CameraFollowParty = MobileParty.MainParty.Party;
				this.StopTrackingTargetSettlement();
			}
			Army army = mobileParty.Army;
			if (((army != null) ? army.LeaderParty : null) == mobileParty)
			{
				this.FinishArmyObjective();
				if (!this._armyIsDispersing)
				{
					Army army2 = mobileParty.Army;
					if (((army2 != null) ? army2.LeaderParty.LeaderHero : null) == null)
					{
						DisbandArmyAction.ApplyByArmyLeaderIsDead(mobileParty.Army);
					}
					else
					{
						DisbandArmyAction.ApplyByObjectiveFinished(mobileParty.Army);
					}
				}
			}
			else if (this.Parties.Count == 0 && !this._armyIsDispersing)
			{
				if (mobileParty.Army != null && MobileParty.MainParty.Army != null && mobileParty.Army == MobileParty.MainParty.Army && Hero.MainHero.IsPrisoner)
				{
					DisbandArmyAction.ApplyByPlayerTakenPrisoner(this);
				}
				else
				{
					DisbandArmyAction.ApplyByNotEnoughParty(this);
				}
			}
			mobileParty.Party.SetVisualAsDirty();
			mobileParty.Party.UpdateVisibilityAndInspected(MobileParty.MainParty.Position, 0f);
		}

		// Token: 0x060001BC RID: 444 RVA: 0x00012E7C File Offset: 0x0001107C
		internal void OnAddPartyInternal(MobileParty mobileParty)
		{
			this._parties.Add(mobileParty);
			mobileParty.Ai.RethinkAtNextHourlyTick = true;
			CampaignEventDispatcher.Instance.OnPartyJoinedArmy(mobileParty);
			if (this == MobileParty.MainParty.Army && this.LeaderParty != MobileParty.MainParty)
			{
				this.StartTrackingTargetSettlement(this.AiBehaviorObject);
				CampaignEventDispatcher.Instance.OnArmyOverlaySetDirty();
			}
			if (!mobileParty.IsMainParty)
			{
				mobileParty.Ai.RethinkAtNextHourlyTick = true;
			}
			if (mobileParty != MobileParty.MainParty && this.LeaderParty != MobileParty.MainParty && this.LeaderParty.LeaderHero != null)
			{
				int num = -Campaign.Current.Models.ArmyManagementCalculationModel.CalculatePartyInfluenceCost(this.LeaderParty, mobileParty);
				ChangeClanInfluenceAction.Apply(this.LeaderParty.LeaderHero.Clan, (float)num);
			}
		}

		// Token: 0x060001BD RID: 445 RVA: 0x00012F46 File Offset: 0x00011146
		private void SendLeaderPartyToReachablePointAroundPosition(CampaignVec2 centerPosition, float distanceLimit, float innerCenterMinimumDistanceLimit = 0f)
		{
			this.LeaderParty.SetMoveGoToPoint(NavigationHelper.FindReachablePointAroundPosition(centerPosition, MobileParty.NavigationType.Default, distanceLimit, innerCenterMinimumDistanceLimit, false), this.LeaderParty.NavigationCapability);
		}

		// Token: 0x060001BE RID: 446 RVA: 0x00012F68 File Offset: 0x00011168
		private void StartTrackingTargetSettlement(IMapPoint targetObject)
		{
			Settlement settlement = targetObject as Settlement;
			if (settlement != null)
			{
				Campaign.Current.VisualTrackerManager.RegisterObject(settlement);
			}
		}

		// Token: 0x060001BF RID: 447 RVA: 0x00012F90 File Offset: 0x00011190
		private void StopTrackingTargetSettlement()
		{
			Settlement settlement = this.AiBehaviorObject as Settlement;
			if (settlement != null)
			{
				Campaign.Current.VisualTrackerManager.RemoveTrackedObject(settlement, false);
			}
		}

		// Token: 0x060001C0 RID: 448 RVA: 0x00012FC0 File Offset: 0x000111C0
		public void SetPositionAfterMapChange(CampaignVec2 newPosition)
		{
			this.LeaderParty.SetPositionAfterMapChange(newPosition);
			foreach (MobileParty mobileParty in this.LeaderParty.AttachedParties)
			{
				mobileParty.SetPositionAfterMapChange(newPosition);
			}
		}

		// Token: 0x060001C1 RID: 449 RVA: 0x00013024 File Offset: 0x00011224
		public void CheckPositionsForMapChangeAndUpdateIfNeeded()
		{
			if (!NavigationHelper.IsPositionValidForNavigationType(this.LeaderParty.Position, this.LeaderParty.NavigationCapability))
			{
				CampaignVec2 closestNavMeshFaceCenterPositionForPosition = NavigationHelper.GetClosestNavMeshFaceCenterPositionForPosition(this.LeaderParty.Position, Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(this.LeaderParty.NavigationCapability));
				this.LeaderParty.Position = NavigationHelper.FindReachablePointAroundPosition(closestNavMeshFaceCenterPositionForPosition, this.LeaderParty.NavigationCapability, 8f, 1f, false);
				foreach (MobileParty mobileParty in this.LeaderParty.AttachedParties)
				{
					mobileParty.SetPositionAfterMapChange(this.LeaderParty.Position);
				}
			}
		}

		// Token: 0x060001C2 RID: 450 RVA: 0x000130FC File Offset: 0x000112FC
		Banner ITrackableCampaignObject.GetBanner()
		{
			return this.LeaderParty.Banner;
		}

		// Token: 0x060001C3 RID: 451 RVA: 0x00013109 File Offset: 0x00011309
		TextObject ITrackableBase.GetName()
		{
			return this.Name;
		}

		// Token: 0x060001C4 RID: 452 RVA: 0x00013111 File Offset: 0x00011311
		Vec3 ITrackableBase.GetPosition()
		{
			return this.LeaderParty.GetPositionAsVec3();
		}

		// Token: 0x060001C5 RID: 453 RVA: 0x0001311E File Offset: 0x0001131E
		internal static void AutoGeneratedStaticCollectObjectsArmy(object o, List<object> collectedObjects)
		{
			((Army)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		// Token: 0x060001C6 RID: 454 RVA: 0x0001312C File Offset: 0x0001132C
		protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			collectedObjects.Add(this._parties);
			CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(this._creationTime, collectedObjects);
			collectedObjects.Add(this._kingdom);
			collectedObjects.Add(this._aiBehaviorObject);
			collectedObjects.Add(this.ArmyOwner);
			collectedObjects.Add(this.LeaderParty);
			collectedObjects.Add(this.Name);
		}

		// Token: 0x060001C7 RID: 455 RVA: 0x00013192 File Offset: 0x00011392
		internal static object AutoGeneratedGetMemberValueArmyType(object o)
		{
			return ((Army)o).ArmyType;
		}

		// Token: 0x060001C8 RID: 456 RVA: 0x000131A4 File Offset: 0x000113A4
		internal static object AutoGeneratedGetMemberValueArmyOwner(object o)
		{
			return ((Army)o).ArmyOwner;
		}

		// Token: 0x060001C9 RID: 457 RVA: 0x000131B1 File Offset: 0x000113B1
		internal static object AutoGeneratedGetMemberValueCohesion(object o)
		{
			return ((Army)o).Cohesion;
		}

		// Token: 0x060001CA RID: 458 RVA: 0x000131C3 File Offset: 0x000113C3
		internal static object AutoGeneratedGetMemberValueMorale(object o)
		{
			return ((Army)o).Morale;
		}

		// Token: 0x060001CB RID: 459 RVA: 0x000131D5 File Offset: 0x000113D5
		internal static object AutoGeneratedGetMemberValueLeaderParty(object o)
		{
			return ((Army)o).LeaderParty;
		}

		// Token: 0x060001CC RID: 460 RVA: 0x000131E2 File Offset: 0x000113E2
		internal static object AutoGeneratedGetMemberValueName(object o)
		{
			return ((Army)o).Name;
		}

		// Token: 0x060001CD RID: 461 RVA: 0x000131EF File Offset: 0x000113EF
		internal static object AutoGeneratedGetMemberValue_parties(object o)
		{
			return ((Army)o)._parties;
		}

		// Token: 0x060001CE RID: 462 RVA: 0x000131FC File Offset: 0x000113FC
		internal static object AutoGeneratedGetMemberValue_creationTime(object o)
		{
			return ((Army)o)._creationTime;
		}

		// Token: 0x060001CF RID: 463 RVA: 0x0001320E File Offset: 0x0001140E
		internal static object AutoGeneratedGetMemberValue_armyGatheringStartTime(object o)
		{
			return ((Army)o)._armyGatheringStartTime;
		}

		// Token: 0x060001D0 RID: 464 RVA: 0x00013220 File Offset: 0x00011420
		internal static object AutoGeneratedGetMemberValue_armyIsDispersing(object o)
		{
			return ((Army)o)._armyIsDispersing;
		}

		// Token: 0x060001D1 RID: 465 RVA: 0x00013232 File Offset: 0x00011432
		internal static object AutoGeneratedGetMemberValue_numberOfBoosts(object o)
		{
			return ((Army)o)._numberOfBoosts;
		}

		// Token: 0x060001D2 RID: 466 RVA: 0x00013244 File Offset: 0x00011444
		internal static object AutoGeneratedGetMemberValue_kingdom(object o)
		{
			return ((Army)o)._kingdom;
		}

		// Token: 0x060001D3 RID: 467 RVA: 0x00013251 File Offset: 0x00011451
		internal static object AutoGeneratedGetMemberValue_aiBehaviorObject(object o)
		{
			return ((Army)o)._aiBehaviorObject;
		}

		// Token: 0x060001D4 RID: 468 RVA: 0x0001325E File Offset: 0x0001145E
		internal static object AutoGeneratedGetMemberValue_inactivityCounter(object o)
		{
			return ((Army)o)._inactivityCounter;
		}

		// Token: 0x04000007 RID: 7
		private const float CheckingForBoostingCohesionThreshold = 50f;

		// Token: 0x04000008 RID: 8
		private const float DisbandCohesionThreshold = 30f;

		// Token: 0x04000009 RID: 9
		private const float StrengthThresholdRatioForGathering = 0.9f;

		// Token: 0x0400000A RID: 10
		private const float StrengthThresholdRatioForGatheringAfterTimeThreshold = 0.75f;

		// Token: 0x0400000B RID: 11
		[SaveableField(1)]
		private readonly MBList<MobileParty> _parties;

		// Token: 0x0400000F RID: 15
		[SaveableField(19)]
		private CampaignTime _creationTime;

		// Token: 0x04000010 RID: 16
		[SaveableField(7)]
		private float _armyGatheringStartTime;

		// Token: 0x04000011 RID: 17
		[SaveableField(10)]
		private bool _armyIsDispersing;

		// Token: 0x04000012 RID: 18
		[SaveableField(11)]
		private int _numberOfBoosts;

		// Token: 0x04000015 RID: 21
		[SaveableField(15)]
		private Kingdom _kingdom;

		// Token: 0x04000016 RID: 22
		[SaveableField(16)]
		private IMapPoint _aiBehaviorObject;

		// Token: 0x04000018 RID: 24
		[SaveableField(20)]
		private int _inactivityCounter;

		// Token: 0x04000019 RID: 25
		[CachedData]
		private MBCampaignEvent _hourlyTickEvent;

		// Token: 0x0400001A RID: 26
		[CachedData]
		private MBCampaignEvent _tickEvent;

		// Token: 0x020004EF RID: 1263
		public enum ArmyTypes
		{
			// Token: 0x040014FB RID: 5371
			Besieger,
			// Token: 0x040014FC RID: 5372
			Raider,
			// Token: 0x040014FD RID: 5373
			Defender,
			// Token: 0x040014FE RID: 5374
			Patrolling,
			// Token: 0x040014FF RID: 5375
			NumberOfArmyTypes
		}

		// Token: 0x020004F0 RID: 1264
		public enum ArmyDispersionReason
		{
			// Token: 0x04001501 RID: 5377
			Unknown,
			// Token: 0x04001502 RID: 5378
			DismissalRequestedWithInfluence,
			// Token: 0x04001503 RID: 5379
			NotEnoughParty,
			// Token: 0x04001504 RID: 5380
			KingdomChanged,
			// Token: 0x04001505 RID: 5381
			CohesionDepleted,
			// Token: 0x04001506 RID: 5382
			ObjectiveFinished,
			// Token: 0x04001507 RID: 5383
			LeaderPartyRemoved,
			// Token: 0x04001508 RID: 5384
			PlayerTakenPrisoner,
			// Token: 0x04001509 RID: 5385
			CannotElectNewLeader,
			// Token: 0x0400150A RID: 5386
			LeaderCannotArrivePointOnTime,
			// Token: 0x0400150B RID: 5387
			ArmyLeaderIsDead,
			// Token: 0x0400150C RID: 5388
			FoodProblem,
			// Token: 0x0400150D RID: 5389
			NotEnoughTroop,
			// Token: 0x0400150E RID: 5390
			NoActiveWar,
			// Token: 0x0400150F RID: 5391
			NoShipToUse,
			// Token: 0x04001510 RID: 5392
			Inactivity
		}
	}
}
