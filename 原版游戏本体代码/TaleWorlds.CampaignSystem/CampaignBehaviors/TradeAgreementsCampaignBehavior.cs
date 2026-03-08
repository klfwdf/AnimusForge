using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000445 RID: 1093
	public class TradeAgreementsCampaignBehavior : CampaignBehaviorBase, ITradeAgreementsCampaignBehavior
	{
		// Token: 0x060045B4 RID: 17844 RVA: 0x0015AAB3 File Offset: 0x00158CB3
		public override void RegisterEvents()
		{
			CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, new Action<Kingdom>(this.OnKingdomDestroyed));
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.WarDeclared));
		}

		// Token: 0x060045B5 RID: 17845 RVA: 0x0015AAE4 File Offset: 0x00158CE4
		public void OnTradeAgreementOfferedToPlayer(Kingdom fromKingdom)
		{
			if (!Clan.PlayerClan.IsUnderMercenaryService)
			{
				KingdomDecision kingdomDecision = Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault(delegate(KingdomDecision s)
				{
					TradeAgreementDecision tradeAgreementDecision;
					return (tradeAgreementDecision = s as TradeAgreementDecision) != null && tradeAgreementDecision.TargetKingdom == fromKingdom;
				});
				if (kingdomDecision != null)
				{
					Clan.PlayerClan.Kingdom.RemoveDecision(kingdomDecision);
				}
				Clan.PlayerClan.Kingdom.AddDecision(new TradeAgreementDecision(TradeAgreementDecision.GetProposerClanForPlayerKingdom(fromKingdom), fromKingdom), true);
				return;
			}
			this.AcceptOffer(fromKingdom);
		}

		// Token: 0x060045B6 RID: 17846 RVA: 0x0015AB71 File Offset: 0x00158D71
		private void AcceptOffer(Kingdom fromKingdom)
		{
			this.MakeTradeAgreement(fromKingdom, Clan.PlayerClan.Kingdom, Campaign.Current.Models.TradeAgreementModel.GetTradeAgreementDurationInYears(fromKingdom, Clan.PlayerClan.Kingdom));
		}

		// Token: 0x060045B7 RID: 17847 RVA: 0x0015ABA4 File Offset: 0x00158DA4
		private void WarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
		{
			Kingdom kingdom;
			Kingdom kingdom2;
			if ((kingdom = faction1 as Kingdom) != null && (kingdom2 = faction2 as Kingdom) != null && this.HasTradeAgreement(kingdom, kingdom2))
			{
				this.EndTradeAgreement(kingdom, kingdom2);
			}
		}

		// Token: 0x060045B8 RID: 17848 RVA: 0x0015ABD7 File Offset: 0x00158DD7
		private void OnKingdomDestroyed(Kingdom kingdom)
		{
			this.EndTradeAgreementsOfKingdom(kingdom);
		}

		// Token: 0x060045B9 RID: 17849 RVA: 0x0015ABE0 File Offset: 0x00158DE0
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<List<TradeAgreementsCampaignBehavior.TradeAgreement>>("_tradeAgreements", ref this._tradeAgreements);
		}

		// Token: 0x060045BA RID: 17850 RVA: 0x0015ABF4 File Offset: 0x00158DF4
		public void MakeTradeAgreement(Kingdom kingdom1, Kingdom kingdom2, CampaignTime duration)
		{
			Debug.Print(string.Format("Trade agreement signed between {0} and {1}", kingdom1.Name, kingdom2.Name), 0, Debug.DebugColor.White, 17592186044416UL);
			TradeAgreementsCampaignBehavior.TradeAgreement item = new TradeAgreementsCampaignBehavior.TradeAgreement(kingdom1, kingdom2, CampaignTime.Now + duration);
			this._tradeAgreements.Add(item);
			CampaignEventDispatcher.Instance.OnTradeAgreementSigned(kingdom1, kingdom2);
		}

		// Token: 0x060045BB RID: 17851 RVA: 0x0015AC54 File Offset: 0x00158E54
		public void EndTradeAgreementsOfKingdom(Kingdom kingdom)
		{
			this._tradeAgreements.RemoveAll((TradeAgreementsCampaignBehavior.TradeAgreement t) => t.Kingdom1 == kingdom || t.Kingdom2 == kingdom);
		}

		// Token: 0x060045BC RID: 17852 RVA: 0x0015AC86 File Offset: 0x00158E86
		public void EndTradeAgreement(Kingdom kingdom1, Kingdom kingdom2)
		{
			this.RemoveTradeAgreement(kingdom1, kingdom2);
		}

		// Token: 0x060045BD RID: 17853 RVA: 0x0015AC90 File Offset: 0x00158E90
		public bool HasTradeAgreement(Kingdom kingdom1, Kingdom kingdom2)
		{
			bool result = false;
			CampaignTime campaignTime;
			if (this.TryGetTradeAgreementEndTime(kingdom1, kingdom2, out campaignTime))
			{
				if (campaignTime.IsPast)
				{
					this.EndTradeAgreement(kingdom1, kingdom2);
				}
				else
				{
					result = true;
				}
			}
			return result;
		}

		// Token: 0x060045BE RID: 17854 RVA: 0x0015ACC4 File Offset: 0x00158EC4
		public CampaignTime GetTradeAgreementEndDate(Kingdom kingdom1, Kingdom kingdom2)
		{
			CampaignTime result;
			if (!this.TryGetTradeAgreementEndTime(kingdom1, kingdom2, out result))
			{
				Debug.FailedAssert("Cant find trade agreement", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\TradeAgreementsCampaignBehavior.cs", "GetTradeAgreementEndDate", 146);
				return CampaignTime.Zero;
			}
			return result;
		}

		// Token: 0x060045BF RID: 17855 RVA: 0x0015AD00 File Offset: 0x00158F00
		private int GetTradeAgreementsCount(Kingdom kingdom)
		{
			int num = 0;
			foreach (Kingdom kingdom2 in Kingdom.All)
			{
				if (kingdom != kingdom2 && this.HasTradeAgreement(kingdom, kingdom2))
				{
					num++;
				}
			}
			return num;
		}

		// Token: 0x060045C0 RID: 17856 RVA: 0x0015AD60 File Offset: 0x00158F60
		private bool TryGetTradeAgreementEndTime(Kingdom kingdom1, Kingdom kingdom2, out CampaignTime endTime)
		{
			bool result = false;
			endTime = CampaignTime.Never;
			foreach (TradeAgreementsCampaignBehavior.TradeAgreement tradeAgreement in this._tradeAgreements)
			{
				if ((tradeAgreement.Kingdom1 == kingdom1 && tradeAgreement.Kingdom2 == kingdom2) || (tradeAgreement.Kingdom2 == kingdom1 && tradeAgreement.Kingdom1 == kingdom2))
				{
					endTime = tradeAgreement.EndTime;
					result = true;
					break;
				}
			}
			return result;
		}

		// Token: 0x060045C1 RID: 17857 RVA: 0x0015ADF0 File Offset: 0x00158FF0
		private void RemoveTradeAgreement(Kingdom kingdom1, Kingdom kingdom2)
		{
			int num = this._tradeAgreements.Count - 1;
			while (-1 < num)
			{
				TradeAgreementsCampaignBehavior.TradeAgreement tradeAgreement = this._tradeAgreements[num];
				if ((tradeAgreement.Kingdom1 == kingdom1 && tradeAgreement.Kingdom2 == kingdom2) || (tradeAgreement.Kingdom2 == kingdom1 && tradeAgreement.Kingdom1 == kingdom2))
				{
					this._tradeAgreements.RemoveAt(num);
					return;
				}
				num--;
			}
		}

		// Token: 0x0400137F RID: 4991
		private List<TradeAgreementsCampaignBehavior.TradeAgreement> _tradeAgreements = new List<TradeAgreementsCampaignBehavior.TradeAgreement>();

		// Token: 0x0200084F RID: 2127
		public class TradeAgreementsCampaignBehaviorTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x06006716 RID: 26390 RVA: 0x001C33D6 File Offset: 0x001C15D6
			public TradeAgreementsCampaignBehaviorTypeDefiner()
				: base(312260)
			{
			}

			// Token: 0x06006717 RID: 26391 RVA: 0x001C33E3 File Offset: 0x001C15E3
			protected override void DefineStructTypes()
			{
				base.AddStructDefinition(typeof(TradeAgreementsCampaignBehavior.TradeAgreement), 1, null);
			}

			// Token: 0x06006718 RID: 26392 RVA: 0x001C33F7 File Offset: 0x001C15F7
			protected override void DefineContainerDefinitions()
			{
				base.ConstructContainerDefinition(typeof(List<TradeAgreementsCampaignBehavior.TradeAgreement>));
			}
		}

		// Token: 0x02000850 RID: 2128
		internal readonly struct TradeAgreement
		{
			// Token: 0x06006719 RID: 26393 RVA: 0x001C3409 File Offset: 0x001C1609
			public TradeAgreement(Kingdom kingdom1, Kingdom kingdom2, CampaignTime endTime)
			{
				this = default(TradeAgreementsCampaignBehavior.TradeAgreement);
				this.Kingdom1 = kingdom1;
				this.Kingdom2 = kingdom2;
				this.EndTime = endTime;
			}

			// Token: 0x0600671A RID: 26394 RVA: 0x001C3428 File Offset: 0x001C1628
			public static void AutoGeneratedStaticCollectObjectsTradeAgreement(object o, List<object> collectedObjects)
			{
				((TradeAgreementsCampaignBehavior.TradeAgreement)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x0600671B RID: 26395 RVA: 0x001C3444 File Offset: 0x001C1644
			private void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				collectedObjects.Add(this.Kingdom1);
				collectedObjects.Add(this.Kingdom2);
				CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(this.EndTime, collectedObjects);
			}

			// Token: 0x0600671C RID: 26396 RVA: 0x001C346F File Offset: 0x001C166F
			internal static object AutoGeneratedGetMemberValueKingdom1(object o)
			{
				return ((TradeAgreementsCampaignBehavior.TradeAgreement)o).Kingdom1;
			}

			// Token: 0x0600671D RID: 26397 RVA: 0x001C347C File Offset: 0x001C167C
			internal static object AutoGeneratedGetMemberValueKingdom2(object o)
			{
				return ((TradeAgreementsCampaignBehavior.TradeAgreement)o).Kingdom2;
			}

			// Token: 0x0600671E RID: 26398 RVA: 0x001C3489 File Offset: 0x001C1689
			internal static object AutoGeneratedGetMemberValueEndTime(object o)
			{
				return ((TradeAgreementsCampaignBehavior.TradeAgreement)o).EndTime;
			}

			// Token: 0x04002363 RID: 9059
			[SaveableField(1)]
			public readonly Kingdom Kingdom1;

			// Token: 0x04002364 RID: 9060
			[SaveableField(2)]
			public readonly Kingdom Kingdom2;

			// Token: 0x04002365 RID: 9061
			[SaveableField(3)]
			public readonly CampaignTime EndTime;
		}
	}
}
