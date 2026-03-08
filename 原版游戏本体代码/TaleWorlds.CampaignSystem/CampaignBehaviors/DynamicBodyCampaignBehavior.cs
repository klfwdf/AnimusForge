using System;
using Helpers;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003E9 RID: 1001
	public class DynamicBodyCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x17000E0F RID: 3599
		// (get) Token: 0x06003D8A RID: 15754 RVA: 0x0010C36C File Offset: 0x0010A56C
		// (set) Token: 0x06003D8B RID: 15755 RVA: 0x0010C38B File Offset: 0x0010A58B
		private CampaignTime LastSettlementVisitTime
		{
			get
			{
				if (Hero.MainHero.CurrentSettlement != null)
				{
					this._lastSettlementVisitTime = CampaignTime.Now;
				}
				return this._lastSettlementVisitTime;
			}
			set
			{
				this._lastSettlementVisitTime = value;
			}
		}

		// Token: 0x17000E10 RID: 3600
		// (get) Token: 0x06003D8C RID: 15756 RVA: 0x0010C394 File Offset: 0x0010A594
		private float MaxPlayerWeight
		{
			get
			{
				return MathF.Min(1f, this._unmodifiedWeight * 1.3f);
			}
		}

		// Token: 0x17000E11 RID: 3601
		// (get) Token: 0x06003D8D RID: 15757 RVA: 0x0010C3AC File Offset: 0x0010A5AC
		private float MinPlayerWeight
		{
			get
			{
				return MathF.Max(0f, this._unmodifiedWeight * 0.7f);
			}
		}

		// Token: 0x17000E12 RID: 3602
		// (get) Token: 0x06003D8E RID: 15758 RVA: 0x0010C3C4 File Offset: 0x0010A5C4
		private float MaxPlayerBuild
		{
			get
			{
				return MathF.Min(1f, this._unmodifiedBuild * 1.3f);
			}
		}

		// Token: 0x17000E13 RID: 3603
		// (get) Token: 0x06003D8F RID: 15759 RVA: 0x0010C3DC File Offset: 0x0010A5DC
		private float MinPlayerBuild
		{
			get
			{
				return MathF.Max(0f, this._unmodifiedBuild * 0.7f);
			}
		}

		// Token: 0x06003D90 RID: 15760 RVA: 0x0010C3F4 File Offset: 0x0010A5F4
		private void DailyTick()
		{
			bool flag = this.LastSettlementVisitTime.ElapsedDaysUntilNow < 1f;
			bool flag2 = Hero.MainHero.PartyBelongedTo != null && Hero.MainHero.PartyBelongedTo.Party.IsStarving;
			float num = ((Hero.MainHero.CurrentSettlement == null && flag2) ? (-0.1f) : (flag ? 0.025f : (-0.025f)));
			Hero.MainHero.Weight = MBMath.ClampFloat(Hero.MainHero.Weight + num, this.MinPlayerWeight, this.MaxPlayerWeight);
			float num2 = ((MapEvent.PlayerMapEvent != null || PlayerSiege.PlayerSiegeEvent != null || this._lastEncounterTime.ElapsedDaysUntilNow < 2f) ? 0.025f : (-0.015f));
			Hero.MainHero.Build = MBMath.ClampFloat(Hero.MainHero.Build + num2, this.MinPlayerBuild, this.MaxPlayerBuild);
		}

		// Token: 0x06003D91 RID: 15761 RVA: 0x0010C4E4 File Offset: 0x0010A6E4
		private void OnSettlementLeft(MobileParty party, Settlement settlement)
		{
			if (party != null && party.IsMainParty)
			{
				this.LastSettlementVisitTime = CampaignTime.Now;
			}
		}

		// Token: 0x06003D92 RID: 15762 RVA: 0x0010C4FC File Offset: 0x0010A6FC
		private void OnMapEventEnded(MapEvent mapEvent)
		{
			if (mapEvent.IsPlayerMapEvent)
			{
				this._lastEncounterTime = CampaignTime.Now;
			}
		}

		// Token: 0x06003D93 RID: 15763 RVA: 0x0010C511 File Offset: 0x0010A711
		private void OnPlayerBodyPropertiesChanged()
		{
			this._unmodifiedBuild = Hero.MainHero.Build;
			this._unmodifiedWeight = Hero.MainHero.Weight;
		}

		// Token: 0x06003D94 RID: 15764 RVA: 0x0010C533 File Offset: 0x0010A733
		private void OnPlayerCharacterChanged(Hero oldPlayer, Hero newPlayer, MobileParty newMainParty, bool isMainPartyChanged)
		{
			this._unmodifiedBuild = newPlayer.Build;
			this._unmodifiedWeight = newPlayer.Weight;
		}

		// Token: 0x06003D95 RID: 15765 RVA: 0x0010C550 File Offset: 0x0010A750
		private void OnHeroCreated(Hero hero, bool bornNaturally)
		{
			if (!bornNaturally)
			{
				DynamicBodyProperties dynamicBodyPropertiesBetweenMinMaxRange = CharacterHelper.GetDynamicBodyPropertiesBetweenMinMaxRange(hero.CharacterObject);
				hero.Weight = dynamicBodyPropertiesBetweenMinMaxRange.Weight;
				hero.Build = dynamicBodyPropertiesBetweenMinMaxRange.Build;
			}
		}

		// Token: 0x06003D96 RID: 15766 RVA: 0x0010C584 File Offset: 0x0010A784
		private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter starter)
		{
			this._lastSettlementVisitTime = CampaignTime.Now;
			this._lastEncounterTime = CampaignTime.Now;
			this.OnPlayerBodyPropertiesChanged();
		}

		// Token: 0x06003D97 RID: 15767 RVA: 0x0010C5A4 File Offset: 0x0010A7A4
		public override void RegisterEvents()
		{
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement>(this.OnSettlementLeft));
			CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.DailyTick));
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(this.OnMapEventEnded));
			CampaignEvents.HeroCreated.AddNonSerializedListener(this, new Action<Hero, bool>(this.OnHeroCreated));
			CampaignEvents.OnPlayerBodyPropertiesChangedEvent.AddNonSerializedListener(this, new Action(this.OnPlayerBodyPropertiesChanged));
			CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(this, new Action(this.OnPlayerBodyPropertiesChanged));
			CampaignEvents.OnPlayerCharacterChangedEvent.AddNonSerializedListener(this, new Action<Hero, Hero, MobileParty, bool>(this.OnPlayerCharacterChanged));
			CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreatedPartialFollowUpEnd));
		}

		// Token: 0x06003D98 RID: 15768 RVA: 0x0010C66C File Offset: 0x0010A86C
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<CampaignTime>("_lastSettlementVisitTime", ref this._lastSettlementVisitTime);
			dataStore.SyncData<CampaignTime>("_lastEncounterTime", ref this._lastEncounterTime);
			dataStore.SyncData<float>("_unmodifiedWeight", ref this._unmodifiedWeight);
			dataStore.SyncData<float>("_unmodifiedBuild", ref this._unmodifiedBuild);
		}

		// Token: 0x0400128D RID: 4749
		private const float DailyBuildDecrease = -0.015f;

		// Token: 0x0400128E RID: 4750
		private const float DailyBuildIncrease = 0.025f;

		// Token: 0x0400128F RID: 4751
		private const float DailyWeightDecreaseWhenStarving = -0.1f;

		// Token: 0x04001290 RID: 4752
		private const float DailyWeightDecreaseWhenNotStarving = -0.025f;

		// Token: 0x04001291 RID: 4753
		private const float DailyWeightIncrease = 0.025f;

		// Token: 0x04001292 RID: 4754
		private CampaignTime _lastSettlementVisitTime;

		// Token: 0x04001293 RID: 4755
		private CampaignTime _lastEncounterTime;

		// Token: 0x04001294 RID: 4756
		private float _unmodifiedWeight = -1f;

		// Token: 0x04001295 RID: 4757
		private float _unmodifiedBuild = -1f;
	}
}
