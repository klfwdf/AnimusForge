using System;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy
{
	// Token: 0x02000070 RID: 112
	public class KingdomTruceItemVM : KingdomDiplomacyItemVM
	{
		// Token: 0x0600092A RID: 2346 RVA: 0x00028DAB File Offset: 0x00026FAB
		public KingdomTruceItemVM(IFaction faction1, IFaction faction2, Action<KingdomDiplomacyItemVM> onSelection)
			: base(faction1, faction2)
		{
			this._onSelection = onSelection;
			this.UpdateDiplomacyProperties();
		}

		// Token: 0x0600092B RID: 2347 RVA: 0x00028DC2 File Offset: 0x00026FC2
		protected override void OnSelect()
		{
			this.UpdateDiplomacyProperties();
			this._onSelection(this);
		}

		// Token: 0x0600092C RID: 2348 RVA: 0x00028DD8 File Offset: 0x00026FD8
		protected override void UpdateDiplomacyProperties()
		{
			base.UpdateDiplomacyProperties();
			base.Stats.Add(new KingdomWarComparableStatVM((int)this.Faction1.CurrentTotalStrength, (int)this.Faction2.CurrentTotalStrength, GameTexts.FindText("str_total_strength", null), this._faction1Color, this._faction2Color, 10000, null, null));
			base.Stats.Add(new KingdomWarComparableStatVM(this._faction1Towns.Count, this._faction2Towns.Count, GameTexts.FindText("str_towns", null), this._faction1Color, this._faction2Color, 25, new BasicTooltipViewModel(() => CampaignUIHelper.GetTruceOwnedSettlementsTooltip(this._faction1Towns, this.Faction1.Name, true)), new BasicTooltipViewModel(() => CampaignUIHelper.GetTruceOwnedSettlementsTooltip(this._faction2Towns, this.Faction2.Name, true))));
			base.Stats.Add(new KingdomWarComparableStatVM(this._faction1Castles.Count, this._faction2Castles.Count, GameTexts.FindText("str_castles", null), this._faction1Color, this._faction2Color, 25, new BasicTooltipViewModel(() => CampaignUIHelper.GetTruceOwnedSettlementsTooltip(this._faction1Castles, this.Faction1.Name, false)), new BasicTooltipViewModel(() => CampaignUIHelper.GetTruceOwnedSettlementsTooltip(this._faction2Castles, this.Faction2.Name, false))));
			StanceLink stanceWith = this._playerKingdom.GetStanceWith(this.Faction2);
			this.TributePaid = stanceWith.GetDailyTributeToPay(this._playerKingdom);
			if (stanceWith.IsNeutral && this.TributePaid != 0)
			{
				base.Stats.Add(new KingdomWarComparableStatVM(MathF.Max(stanceWith.GetTotalTributePaid(this.Faction2), 0), MathF.Max(stanceWith.GetTotalTributePaid(this.Faction1), 0), GameTexts.FindText("str_comparison_tribute_received", null), this._faction1Color, this._faction2Color, 10000, null, null));
			}
			if (!this.Faction1.IsKingdomFaction || !this.Faction2.IsKingdomFaction)
			{
				this.HasTradeAgreement = false;
				this.HasAlliance = false;
				this.TradeAgreementEndTimeStr = null;
				this.AllianceEndTimeStr = null;
				return;
			}
			ITradeAgreementsCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
			this.HasTradeAgreement = campaignBehavior != null && campaignBehavior.HasTradeAgreement(this.Faction1 as Kingdom, this.Faction2 as Kingdom);
			IAllianceCampaignBehavior campaignBehavior2 = Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>();
			this.HasAlliance = campaignBehavior2 != null && campaignBehavior2.IsAllyWithKingdom(this.Faction1 as Kingdom, this.Faction2 as Kingdom);
			if (this.HasTradeAgreement)
			{
				int num = MathF.Ceiling(Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>().GetTradeAgreementEndDate(this.Faction1 as Kingdom, this.Faction2 as Kingdom).RemainingDaysFromNow);
				this.TradeAgreementEndTimeStr = new TextObject("{=6ayEZQE1}Expires in {DAYS} {?DAYS > 1}days{?}day{\\?}.", null).SetTextVariable("DAYS", num.ToString()).ToString();
			}
			else
			{
				this.TradeAgreementEndTimeStr = null;
			}
			if (this.HasAlliance)
			{
				int num2 = MathF.Ceiling(Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>().GetAllianceEndDate(this.Faction1 as Kingdom, this.Faction2 as Kingdom).RemainingDaysFromNow);
				this.AllianceEndTimeStr = new TextObject("{=6ayEZQE1}Expires in {DAYS} {?DAYS > 1}days{?}day{\\?}.", null).SetTextVariable("DAYS", num2.ToString()).ToString();
				return;
			}
			this.AllianceEndTimeStr = null;
		}

		// Token: 0x170002B1 RID: 689
		// (get) Token: 0x0600092D RID: 2349 RVA: 0x000290F0 File Offset: 0x000272F0
		// (set) Token: 0x0600092E RID: 2350 RVA: 0x000290F8 File Offset: 0x000272F8
		[DataSourceProperty]
		public int TributePaid
		{
			get
			{
				return this._tributePaid;
			}
			set
			{
				if (value != this._tributePaid)
				{
					this._tributePaid = value;
					base.OnPropertyChangedWithValue(value, "TributePaid");
				}
			}
		}

		// Token: 0x170002B2 RID: 690
		// (get) Token: 0x0600092F RID: 2351 RVA: 0x00029116 File Offset: 0x00027316
		// (set) Token: 0x06000930 RID: 2352 RVA: 0x0002911E File Offset: 0x0002731E
		[DataSourceProperty]
		public bool HasTradeAgreement
		{
			get
			{
				return this._hasTradeAgreement;
			}
			set
			{
				if (value != this._hasTradeAgreement)
				{
					this._hasTradeAgreement = value;
					base.OnPropertyChangedWithValue(value, "HasTradeAgreement");
				}
			}
		}

		// Token: 0x170002B3 RID: 691
		// (get) Token: 0x06000931 RID: 2353 RVA: 0x0002913C File Offset: 0x0002733C
		// (set) Token: 0x06000932 RID: 2354 RVA: 0x00029144 File Offset: 0x00027344
		[DataSourceProperty]
		public bool HasAlliance
		{
			get
			{
				return this._hasAlliance;
			}
			set
			{
				if (value != this._hasAlliance)
				{
					this._hasAlliance = value;
					base.OnPropertyChangedWithValue(value, "HasAlliance");
				}
			}
		}

		// Token: 0x170002B4 RID: 692
		// (get) Token: 0x06000933 RID: 2355 RVA: 0x00029162 File Offset: 0x00027362
		// (set) Token: 0x06000934 RID: 2356 RVA: 0x0002916A File Offset: 0x0002736A
		[DataSourceProperty]
		public string AllianceEndTimeStr
		{
			get
			{
				return this._allianceEndTimeStr;
			}
			set
			{
				if (value != this._allianceEndTimeStr)
				{
					this._allianceEndTimeStr = value;
					base.OnPropertyChangedWithValue<string>(value, "AllianceEndTimeStr");
				}
			}
		}

		// Token: 0x170002B5 RID: 693
		// (get) Token: 0x06000935 RID: 2357 RVA: 0x0002918D File Offset: 0x0002738D
		// (set) Token: 0x06000936 RID: 2358 RVA: 0x00029195 File Offset: 0x00027395
		[DataSourceProperty]
		public string TradeAgreementEndTimeStr
		{
			get
			{
				return this._tradeAgreementEndTimeStr;
			}
			set
			{
				if (value != this._tradeAgreementEndTimeStr)
				{
					this._tradeAgreementEndTimeStr = value;
					base.OnPropertyChangedWithValue<string>(value, "TradeAgreementEndTimeStr");
				}
			}
		}

		// Token: 0x04000400 RID: 1024
		private readonly Action<KingdomDiplomacyItemVM> _onSelection;

		// Token: 0x04000401 RID: 1025
		private int _tributePaid;

		// Token: 0x04000402 RID: 1026
		private bool _hasTradeAgreement;

		// Token: 0x04000403 RID: 1027
		private bool _hasAlliance;

		// Token: 0x04000404 RID: 1028
		private string _tradeAgreementEndTimeStr;

		// Token: 0x04000405 RID: 1029
		private string _allianceEndTimeStr;
	}
}
