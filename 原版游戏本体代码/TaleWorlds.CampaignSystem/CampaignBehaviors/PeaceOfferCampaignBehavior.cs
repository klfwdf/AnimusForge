using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x0200042A RID: 1066
	public class PeaceOfferCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x17000E20 RID: 3616
		// (get) Token: 0x06004361 RID: 17249 RVA: 0x001468DC File Offset: 0x00144ADC
		private static TextObject PeacePanelTitleText
		{
			get
			{
				return new TextObject("{=ho5EndaV}Decision", null);
			}
		}

		// Token: 0x17000E21 RID: 3617
		// (get) Token: 0x06004362 RID: 17250 RVA: 0x001468E9 File Offset: 0x00144AE9
		private static TextObject PeacePanelOkText
		{
			get
			{
				return new TextObject("{=oHaWR73d}Ok", null);
			}
		}

		// Token: 0x17000E22 RID: 3618
		// (get) Token: 0x06004363 RID: 17251 RVA: 0x001468F6 File Offset: 0x00144AF6
		private static TextObject PeacePanelAffirmativeText
		{
			get
			{
				return new TextObject("{=Y94H6XnK}Accept", null);
			}
		}

		// Token: 0x17000E23 RID: 3619
		// (get) Token: 0x06004364 RID: 17252 RVA: 0x00146903 File Offset: 0x00144B03
		private static TextObject PeacePanelNegativeText
		{
			get
			{
				return new TextObject("{=cOgmdp9e}Decline", null);
			}
		}

		// Token: 0x06004365 RID: 17253 RVA: 0x00146910 File Offset: 0x00144B10
		public override void RegisterEvents()
		{
			CampaignEvents.OnPeaceOfferedToPlayerEvent.AddNonSerializedListener(this, new Action<IFaction, int, int>(this.OnPeaceOffered));
			CampaignEvents.OnPeaceOfferResolvedEvent.AddNonSerializedListener(this, new Action<IFaction>(this.OnPeaceOfferResolved));
		}

		// Token: 0x06004366 RID: 17254 RVA: 0x00146940 File Offset: 0x00144B40
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<int>("_currentPeaceOfferTributeAmount", ref this._currentPeaceOfferTributeAmount);
			dataStore.SyncData<IFaction>("_opponentFaction", ref this._opponentFaction);
		}

		// Token: 0x06004367 RID: 17255 RVA: 0x00146968 File Offset: 0x00144B68
		private void OnPeaceOffered(IFaction opponentFaction, int tributeAmount, int tributeDuration)
		{
			if (this._opponentFaction == null)
			{
				this._opponentFaction = opponentFaction;
				this._currentPeaceOfferTributeAmount = tributeAmount;
				this._currentPeaceOfferTributeDuration = tributeDuration;
				TextObject textObject = ((tributeAmount > 0) ? ((Hero.MainHero.MapFaction.Leader == Hero.MainHero) ? PeaceOfferCampaignBehavior.PeaceOfferTributePaidPanelDescriptionText : PeaceOfferCampaignBehavior.PeaceOfferTributePaidPanelPlayerIsVassalDescriptionText) : ((tributeAmount < 0) ? ((Hero.MainHero.MapFaction.Leader == Hero.MainHero) ? PeaceOfferCampaignBehavior.PeaceOfferTributeWantedPanelDescriptionText : PeaceOfferCampaignBehavior.PeaceOfferTributeWantedPanelPlayerIsVassalDescriptionText) : ((Hero.MainHero.MapFaction.Leader == Hero.MainHero) ? PeaceOfferCampaignBehavior.PeaceOfferDefaultPanelDescriptionText : PeaceOfferCampaignBehavior.PeaceOfferDefaultPanelPlayerIsVassalDescriptionText)));
				textObject.SetTextVariable("MAP_FACTION_NAME", opponentFaction.InformalName);
				textObject.SetTextVariable("GOLD_AMOUNT", MathF.Abs(this._currentPeaceOfferTributeAmount));
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
				TextObject peacePanelNegativeText = PeaceOfferCampaignBehavior.PeacePanelNegativeText;
				this._influenceCostOfDecline = 0;
				Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
				if (Hero.MainHero.MapFaction.Leader == Hero.MainHero)
				{
					InformationManager.ShowInquiry(new InquiryData(PeaceOfferCampaignBehavior.PeacePanelTitleText.ToString(), textObject.ToString(), true, (float)this._influenceCostOfDecline <= 0.1f || Hero.MainHero.Clan.Influence >= (float)this._influenceCostOfDecline, PeaceOfferCampaignBehavior.PeacePanelAffirmativeText.ToString(), peacePanelNegativeText.ToString(), new Action(this.AcceptPeaceOffer), new Action(this.DeclinePeaceOffer), "", 0f, null, null, null), true, false);
					return;
				}
				InformationManager.ShowInquiry(new InquiryData(PeaceOfferCampaignBehavior.PeacePanelTitleText.ToString(), textObject.ToString(), false, true, PeaceOfferCampaignBehavior.PeacePanelOkText.ToString(), PeaceOfferCampaignBehavior.PeacePanelOkText.ToString(), new Action(this.OkPeaceOffer), new Action(this.OkPeaceOffer), "", 0f, null, null, null), true, false);
			}
		}

		// Token: 0x06004368 RID: 17256 RVA: 0x00146B46 File Offset: 0x00144D46
		private void OnPeaceOfferResolved(IFaction opponentFaction)
		{
			if (Hero.MainHero.MapFaction.Leader != Hero.MainHero && opponentFaction != null)
			{
				this._opponentFaction = opponentFaction;
				this.OkPeaceOffer();
			}
		}

		// Token: 0x06004369 RID: 17257 RVA: 0x00146B70 File Offset: 0x00144D70
		private void OkPeaceOffer()
		{
			if (Clan.PlayerClan.IsUnderMercenaryService)
			{
				this.AcceptPeaceOffer();
				return;
			}
			Kingdom kingdom = Clan.PlayerClan.Kingdom;
			KingdomDecision kingdomDecision = kingdom.UnresolvedDecisions.FirstOrDefault(delegate(KingdomDecision s)
			{
				MakePeaceKingdomDecision makePeaceKingdomDecision;
				return (makePeaceKingdomDecision = s as MakePeaceKingdomDecision) != null && makePeaceKingdomDecision.ProposerClan.MapFaction == Hero.MainHero.MapFaction && makePeaceKingdomDecision.FactionToMakePeaceWith == this._opponentFaction;
			});
			if (kingdomDecision != null)
			{
				kingdom.RemoveDecision(kingdomDecision);
			}
			MakePeaceKingdomDecision kingdomDecision2 = new MakePeaceKingdomDecision(Hero.MainHero.MapFaction.Leader.Clan, this._opponentFaction, -this._currentPeaceOfferTributeAmount, this._currentPeaceOfferTributeDuration, true, true);
			((Kingdom)Hero.MainHero.MapFaction).AddDecision(kingdomDecision2, true);
			this._opponentFaction = null;
		}

		// Token: 0x0600436A RID: 17258 RVA: 0x00146C09 File Offset: 0x00144E09
		private void AcceptPeaceOffer()
		{
			MakePeaceAction.ApplyByKingdomDecision(this._opponentFaction, Hero.MainHero.MapFaction, this._currentPeaceOfferTributeAmount, this._currentPeaceOfferTributeDuration);
			this._opponentFaction = null;
		}

		// Token: 0x0600436B RID: 17259 RVA: 0x00146C33 File Offset: 0x00144E33
		private void DeclinePeaceOffer()
		{
			this._opponentFaction = null;
			ChangeClanInfluenceAction.Apply(Clan.PlayerClan, (float)(-(float)this._influenceCostOfDecline));
		}

		// Token: 0x04001321 RID: 4897
		private static TextObject PeaceOfferDefaultPanelDescriptionText = new TextObject("{=IB1xsVEr}A courier has arrived from the {MAP_FACTION_NAME}. They offer you a white peace. Your vassals have left the decision with you.", null);

		// Token: 0x04001322 RID: 4898
		private static TextObject PeaceOfferTributePaidPanelDescriptionText = new TextObject("{=JJQ0Hp4m}A courier has arrived from the {MAP_FACTION_NAME}. The {MAP_FACTION_NAME} will pay {GOLD_AMOUNT} {GOLD_ICON} in tribute each day to end the war between your realms. Your vassals have left the decision with you.", null);

		// Token: 0x04001323 RID: 4899
		private static TextObject PeaceOfferTributeWantedPanelDescriptionText = new TextObject("{=Nd0Vhkxn}A courier has arrived from the {MAP_FACTION_NAME}. They offer you peace if you agree to pay a {GOLD_AMOUNT} {GOLD_ICON} daily tribute. Your vassals have left the decision with you.", null);

		// Token: 0x04001324 RID: 4900
		private static TextObject PeaceOfferDefaultPanelPlayerIsVassalDescriptionText = new TextObject("{=gNf0ALKw}A courier has arrived from the {MAP_FACTION_NAME}. They offer you a white peace. Your kingdom will vote whether to accept the offer.", null);

		// Token: 0x04001325 RID: 4901
		private static TextObject PeaceOfferTributePaidPanelPlayerIsVassalDescriptionText = new TextObject("{=SR9FC5jH}A courier has arrived from the {MAP_FACTION_NAME} bearing a peace offer. The {MAP_FACTION_NAME} will pay {GOLD_AMOUNT} {GOLD_ICON} in tribute each day to end the war between your realms. Your kingdom will vote whether to accept the offer.", null);

		// Token: 0x04001326 RID: 4902
		private static TextObject PeaceOfferTributeWantedPanelPlayerIsVassalDescriptionText = new TextObject("{=sbFboHmV}A courier has arrived from the {MAP_FACTION_NAME}. They offer you peace if you agree to pay a {GOLD_AMOUNT} {GOLD_ICON} daily tribute. Your kingdom will vote whether to accept the offer.", null);

		// Token: 0x04001327 RID: 4903
		private IFaction _opponentFaction;

		// Token: 0x04001328 RID: 4904
		private int _currentPeaceOfferTributeAmount;

		// Token: 0x04001329 RID: 4905
		private int _currentPeaceOfferTributeDuration;

		// Token: 0x0400132A RID: 4906
		private int _influenceCostOfDecline;
	}
}
