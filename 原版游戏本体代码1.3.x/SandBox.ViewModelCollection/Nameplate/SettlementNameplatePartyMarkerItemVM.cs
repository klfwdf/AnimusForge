using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Nameplate
{
	// Token: 0x0200001E RID: 30
	public class SettlementNameplatePartyMarkerItemVM : ViewModel
	{
		// Token: 0x170000E8 RID: 232
		// (get) Token: 0x060002CC RID: 716 RVA: 0x0000C1F1 File Offset: 0x0000A3F1
		// (set) Token: 0x060002CD RID: 717 RVA: 0x0000C1F9 File Offset: 0x0000A3F9
		public MobileParty Party { get; private set; }

		// Token: 0x170000E9 RID: 233
		// (get) Token: 0x060002CE RID: 718 RVA: 0x0000C202 File Offset: 0x0000A402
		// (set) Token: 0x060002CF RID: 719 RVA: 0x0000C20A File Offset: 0x0000A40A
		public int SortIndex { get; private set; }

		// Token: 0x060002D0 RID: 720 RVA: 0x0000C214 File Offset: 0x0000A414
		public SettlementNameplatePartyMarkerItemVM(MobileParty mobileParty)
		{
			this.Party = mobileParty;
			if (mobileParty.IsCaravan)
			{
				this.IsCaravan = true;
				this.SortIndex = 1;
				return;
			}
			if (mobileParty.IsLordParty && mobileParty.LeaderHero != null)
			{
				this.IsLord = true;
				Clan actualClan = mobileParty.ActualClan;
				this.Visual = new BannerImageIdentifierVM((actualClan != null) ? actualClan.Banner : null, true);
				this.SortIndex = 0;
				return;
			}
			this.IsDefault = true;
			this.SortIndex = 2;
		}

		// Token: 0x170000EA RID: 234
		// (get) Token: 0x060002D1 RID: 721 RVA: 0x0000C290 File Offset: 0x0000A490
		// (set) Token: 0x060002D2 RID: 722 RVA: 0x0000C298 File Offset: 0x0000A498
		public BannerImageIdentifierVM Visual
		{
			get
			{
				return this._visual;
			}
			set
			{
				if (value != this._visual)
				{
					this._visual = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "Visual");
				}
			}
		}

		// Token: 0x170000EB RID: 235
		// (get) Token: 0x060002D3 RID: 723 RVA: 0x0000C2B6 File Offset: 0x0000A4B6
		// (set) Token: 0x060002D4 RID: 724 RVA: 0x0000C2BE File Offset: 0x0000A4BE
		public bool IsCaravan
		{
			get
			{
				return this._isCaravan;
			}
			set
			{
				if (value != this._isCaravan)
				{
					this._isCaravan = value;
					base.OnPropertyChangedWithValue(value, "IsCaravan");
				}
			}
		}

		// Token: 0x170000EC RID: 236
		// (get) Token: 0x060002D5 RID: 725 RVA: 0x0000C2DC File Offset: 0x0000A4DC
		// (set) Token: 0x060002D6 RID: 726 RVA: 0x0000C2E4 File Offset: 0x0000A4E4
		public bool IsLord
		{
			get
			{
				return this._isLord;
			}
			set
			{
				if (value != this._isLord)
				{
					this._isLord = value;
					base.OnPropertyChangedWithValue(value, "IsLord");
				}
			}
		}

		// Token: 0x170000ED RID: 237
		// (get) Token: 0x060002D7 RID: 727 RVA: 0x0000C302 File Offset: 0x0000A502
		// (set) Token: 0x060002D8 RID: 728 RVA: 0x0000C30A File Offset: 0x0000A50A
		public bool IsDefault
		{
			get
			{
				return this._isDefault;
			}
			set
			{
				if (value != this._isDefault)
				{
					this._isDefault = value;
					base.OnPropertyChangedWithValue(value, "IsDefault");
				}
			}
		}

		// Token: 0x0400015F RID: 351
		private BannerImageIdentifierVM _visual;

		// Token: 0x04000160 RID: 352
		private bool _isCaravan;

		// Token: 0x04000161 RID: 353
		private bool _isLord;

		// Token: 0x04000162 RID: 354
		private bool _isDefault;
	}
}
