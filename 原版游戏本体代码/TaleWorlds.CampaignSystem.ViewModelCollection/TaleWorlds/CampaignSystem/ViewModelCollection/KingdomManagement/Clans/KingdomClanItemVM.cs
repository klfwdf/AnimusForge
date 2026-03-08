using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Clans
{
	// Token: 0x02000086 RID: 134
	public class KingdomClanItemVM : KingdomItemVM
	{
		// Token: 0x06000B36 RID: 2870 RVA: 0x0002F6D8 File Offset: 0x0002D8D8
		public KingdomClanItemVM(Clan clan, Action<KingdomClanItemVM> onSelect)
		{
			this.Clan = clan;
			this._onSelect = onSelect;
			this.Banner = new BannerImageIdentifierVM(clan.Banner, false);
			this.Banner_9 = new BannerImageIdentifierVM(clan.Banner, true);
			this.RefreshValues();
			this.Refresh();
		}

		// Token: 0x06000B37 RID: 2871 RVA: 0x0002F730 File Offset: 0x0002D930
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = this.Clan.Name.ToString();
			GameTexts.SetVariable("TIER", this.Clan.Tier);
			this.TierText = GameTexts.FindText("str_clan_tier", null).ToString();
		}

		// Token: 0x06000B38 RID: 2872 RVA: 0x0002F784 File Offset: 0x0002D984
		public void Refresh()
		{
			this.Members = new MBBindingList<HeroVM>();
			this.ClanType = 0;
			if (this.Clan.IsUnderMercenaryService)
			{
				this.ClanType = 2;
			}
			else if (this.Clan.Kingdom.RulingClan == this.Clan)
			{
				this.ClanType = 1;
			}
			foreach (Hero hero in from h in this.Clan.Heroes
				where !h.IsDisabled && !h.IsNotSpawned && h.IsAlive && !h.IsChild
				select h)
			{
				this.Members.Add(new HeroVM(hero, false));
			}
			this.NumOfMembers = this.Members.Count;
			this.Fiefs = new MBBindingList<KingdomClanFiefItemVM>();
			foreach (Settlement settlement in from s in this.Clan.Settlements
				where s.IsTown || s.IsCastle
				select s)
			{
				this.Fiefs.Add(new KingdomClanFiefItemVM(settlement));
			}
			this.NumOfFiefs = this.Fiefs.Count;
			this.Influence = (int)this.Clan.Influence;
		}

		// Token: 0x06000B39 RID: 2873 RVA: 0x0002F8FC File Offset: 0x0002DAFC
		protected override void OnSelect()
		{
			base.OnSelect();
			this._onSelect(this);
		}

		// Token: 0x1700038C RID: 908
		// (get) Token: 0x06000B3A RID: 2874 RVA: 0x0002F910 File Offset: 0x0002DB10
		// (set) Token: 0x06000B3B RID: 2875 RVA: 0x0002F918 File Offset: 0x0002DB18
		[DataSourceProperty]
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				if (value != this._name)
				{
					this._name = value;
					base.OnPropertyChangedWithValue<string>(value, "Name");
				}
			}
		}

		// Token: 0x1700038D RID: 909
		// (get) Token: 0x06000B3C RID: 2876 RVA: 0x0002F93B File Offset: 0x0002DB3B
		// (set) Token: 0x06000B3D RID: 2877 RVA: 0x0002F943 File Offset: 0x0002DB43
		[DataSourceProperty]
		public int ClanType
		{
			get
			{
				return this._clanType;
			}
			set
			{
				if (value != this._clanType)
				{
					this._clanType = value;
					base.OnPropertyChangedWithValue(value, "ClanType");
				}
			}
		}

		// Token: 0x1700038E RID: 910
		// (get) Token: 0x06000B3E RID: 2878 RVA: 0x0002F961 File Offset: 0x0002DB61
		// (set) Token: 0x06000B3F RID: 2879 RVA: 0x0002F969 File Offset: 0x0002DB69
		[DataSourceProperty]
		public int NumOfMembers
		{
			get
			{
				return this._numOfMembers;
			}
			set
			{
				if (value != this._numOfMembers)
				{
					this._numOfMembers = value;
					base.OnPropertyChangedWithValue(value, "NumOfMembers");
				}
			}
		}

		// Token: 0x1700038F RID: 911
		// (get) Token: 0x06000B40 RID: 2880 RVA: 0x0002F987 File Offset: 0x0002DB87
		// (set) Token: 0x06000B41 RID: 2881 RVA: 0x0002F98F File Offset: 0x0002DB8F
		[DataSourceProperty]
		public int NumOfFiefs
		{
			get
			{
				return this._numOfFiefs;
			}
			set
			{
				if (value != this._numOfFiefs)
				{
					this._numOfFiefs = value;
					base.OnPropertyChangedWithValue(value, "NumOfFiefs");
				}
			}
		}

		// Token: 0x17000390 RID: 912
		// (get) Token: 0x06000B42 RID: 2882 RVA: 0x0002F9AD File Offset: 0x0002DBAD
		// (set) Token: 0x06000B43 RID: 2883 RVA: 0x0002F9B5 File Offset: 0x0002DBB5
		[DataSourceProperty]
		public string TierText
		{
			get
			{
				return this._tierText;
			}
			set
			{
				if (value != this._tierText)
				{
					this._tierText = value;
					base.OnPropertyChangedWithValue<string>(value, "TierText");
				}
			}
		}

		// Token: 0x17000391 RID: 913
		// (get) Token: 0x06000B44 RID: 2884 RVA: 0x0002F9D8 File Offset: 0x0002DBD8
		// (set) Token: 0x06000B45 RID: 2885 RVA: 0x0002F9E0 File Offset: 0x0002DBE0
		[DataSourceProperty]
		public BannerImageIdentifierVM Banner
		{
			get
			{
				return this._banner;
			}
			set
			{
				if (value != this._banner)
				{
					this._banner = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "Banner");
				}
			}
		}

		// Token: 0x17000392 RID: 914
		// (get) Token: 0x06000B46 RID: 2886 RVA: 0x0002F9FE File Offset: 0x0002DBFE
		// (set) Token: 0x06000B47 RID: 2887 RVA: 0x0002FA06 File Offset: 0x0002DC06
		[DataSourceProperty]
		public BannerImageIdentifierVM Banner_9
		{
			get
			{
				return this._banner_9;
			}
			set
			{
				if (value != this._banner_9)
				{
					this._banner_9 = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "Banner_9");
				}
			}
		}

		// Token: 0x17000393 RID: 915
		// (get) Token: 0x06000B48 RID: 2888 RVA: 0x0002FA24 File Offset: 0x0002DC24
		// (set) Token: 0x06000B49 RID: 2889 RVA: 0x0002FA2C File Offset: 0x0002DC2C
		[DataSourceProperty]
		public MBBindingList<HeroVM> Members
		{
			get
			{
				return this._members;
			}
			set
			{
				if (value != this._members)
				{
					this._members = value;
					base.OnPropertyChangedWithValue<MBBindingList<HeroVM>>(value, "Members");
				}
			}
		}

		// Token: 0x17000394 RID: 916
		// (get) Token: 0x06000B4A RID: 2890 RVA: 0x0002FA4A File Offset: 0x0002DC4A
		// (set) Token: 0x06000B4B RID: 2891 RVA: 0x0002FA52 File Offset: 0x0002DC52
		[DataSourceProperty]
		public MBBindingList<KingdomClanFiefItemVM> Fiefs
		{
			get
			{
				return this._fiefs;
			}
			set
			{
				if (value != this._fiefs)
				{
					this._fiefs = value;
					base.OnPropertyChangedWithValue<MBBindingList<KingdomClanFiefItemVM>>(value, "Fiefs");
				}
			}
		}

		// Token: 0x17000395 RID: 917
		// (get) Token: 0x06000B4C RID: 2892 RVA: 0x0002FA70 File Offset: 0x0002DC70
		// (set) Token: 0x06000B4D RID: 2893 RVA: 0x0002FA78 File Offset: 0x0002DC78
		[DataSourceProperty]
		public int Influence
		{
			get
			{
				return this._influence;
			}
			set
			{
				if (value != this._influence)
				{
					this._influence = value;
					base.OnPropertyChangedWithValue(value, "Influence");
				}
			}
		}

		// Token: 0x040004F7 RID: 1271
		private readonly Action<KingdomClanItemVM> _onSelect;

		// Token: 0x040004F8 RID: 1272
		public readonly Clan Clan;

		// Token: 0x040004F9 RID: 1273
		private string _name;

		// Token: 0x040004FA RID: 1274
		private BannerImageIdentifierVM _banner;

		// Token: 0x040004FB RID: 1275
		private BannerImageIdentifierVM _banner_9;

		// Token: 0x040004FC RID: 1276
		private MBBindingList<HeroVM> _members;

		// Token: 0x040004FD RID: 1277
		private MBBindingList<KingdomClanFiefItemVM> _fiefs;

		// Token: 0x040004FE RID: 1278
		private int _influence;

		// Token: 0x040004FF RID: 1279
		private int _numOfMembers;

		// Token: 0x04000500 RID: 1280
		private int _numOfFiefs;

		// Token: 0x04000501 RID: 1281
		private string _tierText;

		// Token: 0x04000502 RID: 1282
		private int _clanType = -1;

		// Token: 0x020001E5 RID: 485
		private enum ClanTypes
		{
			// Token: 0x04001140 RID: 4416
			Normal,
			// Token: 0x04001141 RID: 4417
			Leader,
			// Token: 0x04001142 RID: 4418
			Mercenary
		}
	}
}
