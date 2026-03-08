using System;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Armies
{
	// Token: 0x02000089 RID: 137
	public class KingdomArmyItemVM : KingdomItemVM
	{
		// Token: 0x170003B3 RID: 947
		// (get) Token: 0x06000B9F RID: 2975 RVA: 0x000309E9 File Offset: 0x0002EBE9
		// (set) Token: 0x06000BA0 RID: 2976 RVA: 0x000309F1 File Offset: 0x0002EBF1
		public float DistanceToMainParty { get; set; }

		// Token: 0x06000BA1 RID: 2977 RVA: 0x000309FC File Offset: 0x0002EBFC
		public KingdomArmyItemVM(Army army, Action<KingdomArmyItemVM> onSelect)
		{
			this.Army = army;
			this._onSelect = onSelect;
			this._viewDataTracker = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
			CampaignUIHelper.GetCharacterCode(army.ArmyOwner.CharacterObject, false);
			this.Leader = new HeroVM(this.Army.LeaderParty.LeaderHero, false);
			this.LordCount = army.Parties.Count;
			this.Strength = army.Parties.Sum((MobileParty p) => p.Party.NumberOfAllMembers);
			this.Location = CampaignUIHelper.GetPartyLocationText(army.LeaderParty);
			this.Behavior = army.GetLongTermBehaviorText(true).ToString();
			this.UpdateIsNew();
			this.Cohesion = (int)this.Army.Cohesion;
			this.Parties = new MBBindingList<KingdomArmyPartyItemVM>();
			foreach (MobileParty party in this.Army.Parties)
			{
				this.Parties.Add(new KingdomArmyPartyItemVM(party));
			}
			this.DistanceToMainParty = DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(army.LeaderParty, MobileParty.MainParty, army.LeaderParty.NavigationCapability);
			this.IsMainArmy = army.LeaderParty == MobileParty.MainParty;
			this.RefreshValues();
		}

		// Token: 0x06000BA2 RID: 2978 RVA: 0x00030B74 File Offset: 0x0002ED74
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.ArmyName = this.Army.Name.ToString();
			GameTexts.SetVariable("STR1", GameTexts.FindText("str_cohesion", null));
			GameTexts.SetVariable("STR2", this.Cohesion.ToString());
			this.CohesionLabel = GameTexts.FindText("str_STR1_space_STR2", null).ToString();
			GameTexts.SetVariable("LEFT", GameTexts.FindText("str_men_count", null));
			GameTexts.SetVariable("RIGHT", this.Strength.ToString());
			this.StrengthLabel = GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon", null).ToString();
			this.ShipCount = this.Army.Parties.Sum(delegate(MobileParty p)
			{
				MBReadOnlyList<Ship> ships = p.Ships;
				if (ships == null)
				{
					return 0;
				}
				return ships.Count;
			});
			this.ShipCountLabel = GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon", null).SetTextVariable("LEFT", new TextObject("{=URbKirPS}Ship Count", null)).SetTextVariable("RIGHT", this.ShipCount)
				.ToString();
			this.Parties.ApplyActionOnAllItems(delegate(KingdomArmyPartyItemVM x)
			{
				x.RefreshValues();
			});
		}

		// Token: 0x06000BA3 RID: 2979 RVA: 0x00030CBD File Offset: 0x0002EEBD
		protected override void OnSelect()
		{
			base.OnSelect();
			this._onSelect(this);
			this.ExecuteResetNew();
		}

		// Token: 0x06000BA4 RID: 2980 RVA: 0x00030CD7 File Offset: 0x0002EED7
		private void ExecuteResetNew()
		{
			if (base.IsNew)
			{
				this._viewDataTracker.OnArmyExamined(this.Army);
				this.UpdateIsNew();
			}
		}

		// Token: 0x06000BA5 RID: 2981 RVA: 0x00030CF8 File Offset: 0x0002EEF8
		private void UpdateIsNew()
		{
			base.IsNew = this._viewDataTracker.UnExaminedArmies.Any((Army a) => a == this.Army);
		}

		// Token: 0x06000BA6 RID: 2982 RVA: 0x00030D1C File Offset: 0x0002EF1C
		protected void ExecuteLink(string link)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(link);
		}

		// Token: 0x170003B4 RID: 948
		// (get) Token: 0x06000BA7 RID: 2983 RVA: 0x00030D2E File Offset: 0x0002EF2E
		// (set) Token: 0x06000BA8 RID: 2984 RVA: 0x00030D36 File Offset: 0x0002EF36
		[DataSourceProperty]
		public MBBindingList<KingdomArmyPartyItemVM> Parties
		{
			get
			{
				return this._parties;
			}
			set
			{
				if (value != this._parties)
				{
					this._parties = value;
					base.OnPropertyChangedWithValue<MBBindingList<KingdomArmyPartyItemVM>>(value, "Parties");
				}
			}
		}

		// Token: 0x170003B5 RID: 949
		// (get) Token: 0x06000BA9 RID: 2985 RVA: 0x00030D54 File Offset: 0x0002EF54
		// (set) Token: 0x06000BAA RID: 2986 RVA: 0x00030D5C File Offset: 0x0002EF5C
		[DataSourceProperty]
		public HeroVM Leader
		{
			get
			{
				return this._leader;
			}
			set
			{
				if (value != this._leader)
				{
					this._leader = value;
					base.OnPropertyChanged("Visual");
				}
			}
		}

		// Token: 0x170003B6 RID: 950
		// (get) Token: 0x06000BAB RID: 2987 RVA: 0x00030D79 File Offset: 0x0002EF79
		// (set) Token: 0x06000BAC RID: 2988 RVA: 0x00030D81 File Offset: 0x0002EF81
		[DataSourceProperty]
		public string ArmyName
		{
			get
			{
				return this._armyName;
			}
			set
			{
				if (value != this._armyName)
				{
					this._armyName = value;
					base.OnPropertyChangedWithValue<string>(value, "ArmyName");
				}
			}
		}

		// Token: 0x170003B7 RID: 951
		// (get) Token: 0x06000BAD RID: 2989 RVA: 0x00030DA4 File Offset: 0x0002EFA4
		// (set) Token: 0x06000BAE RID: 2990 RVA: 0x00030DAC File Offset: 0x0002EFAC
		[DataSourceProperty]
		public int Cohesion
		{
			get
			{
				return this._cohesion;
			}
			set
			{
				if (value != this._cohesion)
				{
					this._cohesion = value;
					base.OnPropertyChangedWithValue(value, "Cohesion");
				}
			}
		}

		// Token: 0x170003B8 RID: 952
		// (get) Token: 0x06000BAF RID: 2991 RVA: 0x00030DCA File Offset: 0x0002EFCA
		// (set) Token: 0x06000BB0 RID: 2992 RVA: 0x00030DD2 File Offset: 0x0002EFD2
		[DataSourceProperty]
		public string CohesionLabel
		{
			get
			{
				return this._cohesionLabel;
			}
			set
			{
				if (value != this._cohesionLabel)
				{
					this._cohesionLabel = value;
					base.OnPropertyChangedWithValue<string>(value, "CohesionLabel");
				}
			}
		}

		// Token: 0x170003B9 RID: 953
		// (get) Token: 0x06000BB1 RID: 2993 RVA: 0x00030DF5 File Offset: 0x0002EFF5
		// (set) Token: 0x06000BB2 RID: 2994 RVA: 0x00030DFD File Offset: 0x0002EFFD
		[DataSourceProperty]
		public int LordCount
		{
			get
			{
				return this._lordCount;
			}
			set
			{
				if (value != this._lordCount)
				{
					this._lordCount = value;
					base.OnPropertyChangedWithValue(value, "LordCount");
				}
			}
		}

		// Token: 0x170003BA RID: 954
		// (get) Token: 0x06000BB3 RID: 2995 RVA: 0x00030E1B File Offset: 0x0002F01B
		// (set) Token: 0x06000BB4 RID: 2996 RVA: 0x00030E23 File Offset: 0x0002F023
		[DataSourceProperty]
		public int Strength
		{
			get
			{
				return this._strength;
			}
			set
			{
				if (value != this._strength)
				{
					this._strength = value;
					base.OnPropertyChangedWithValue(value, "Strength");
				}
			}
		}

		// Token: 0x170003BB RID: 955
		// (get) Token: 0x06000BB5 RID: 2997 RVA: 0x00030E41 File Offset: 0x0002F041
		// (set) Token: 0x06000BB6 RID: 2998 RVA: 0x00030E49 File Offset: 0x0002F049
		[DataSourceProperty]
		public string StrengthLabel
		{
			get
			{
				return this._strengthLabel;
			}
			set
			{
				if (value != this._strengthLabel)
				{
					this._strengthLabel = value;
					base.OnPropertyChangedWithValue<string>(value, "StrengthLabel");
				}
			}
		}

		// Token: 0x170003BC RID: 956
		// (get) Token: 0x06000BB7 RID: 2999 RVA: 0x00030E6C File Offset: 0x0002F06C
		// (set) Token: 0x06000BB8 RID: 3000 RVA: 0x00030E74 File Offset: 0x0002F074
		[DataSourceProperty]
		public int ShipCount
		{
			get
			{
				return this._shipCount;
			}
			set
			{
				if (value != this._shipCount)
				{
					this._shipCount = value;
					base.OnPropertyChangedWithValue(value, "ShipCount");
				}
			}
		}

		// Token: 0x170003BD RID: 957
		// (get) Token: 0x06000BB9 RID: 3001 RVA: 0x00030E92 File Offset: 0x0002F092
		// (set) Token: 0x06000BBA RID: 3002 RVA: 0x00030E9A File Offset: 0x0002F09A
		[DataSourceProperty]
		public string ShipCountLabel
		{
			get
			{
				return this._shipCountLabel;
			}
			set
			{
				if (value != this._shipCountLabel)
				{
					this._shipCountLabel = value;
					base.OnPropertyChangedWithValue<string>(value, "ShipCountLabel");
				}
			}
		}

		// Token: 0x170003BE RID: 958
		// (get) Token: 0x06000BBB RID: 3003 RVA: 0x00030EBD File Offset: 0x0002F0BD
		// (set) Token: 0x06000BBC RID: 3004 RVA: 0x00030EC5 File Offset: 0x0002F0C5
		[DataSourceProperty]
		public string Location
		{
			get
			{
				return this._location;
			}
			set
			{
				if (value != this._location)
				{
					this._location = value;
					base.OnPropertyChangedWithValue<string>(value, "Location");
				}
			}
		}

		// Token: 0x170003BF RID: 959
		// (get) Token: 0x06000BBD RID: 3005 RVA: 0x00030EE8 File Offset: 0x0002F0E8
		// (set) Token: 0x06000BBE RID: 3006 RVA: 0x00030EF0 File Offset: 0x0002F0F0
		[DataSourceProperty]
		public string Behavior
		{
			get
			{
				return this._behavior;
			}
			set
			{
				if (value != this._behavior)
				{
					this._behavior = value;
					base.OnPropertyChanged("Objective");
				}
			}
		}

		// Token: 0x170003C0 RID: 960
		// (get) Token: 0x06000BBF RID: 3007 RVA: 0x00030F12 File Offset: 0x0002F112
		// (set) Token: 0x06000BC0 RID: 3008 RVA: 0x00030F1A File Offset: 0x0002F11A
		[DataSourceProperty]
		public bool IsMainArmy
		{
			get
			{
				return this._isMainArmy;
			}
			set
			{
				if (value != this._isMainArmy)
				{
					this._isMainArmy = value;
					base.OnPropertyChangedWithValue(value, "IsMainArmy");
				}
			}
		}

		// Token: 0x04000528 RID: 1320
		public readonly Army Army;

		// Token: 0x0400052A RID: 1322
		private readonly Action<KingdomArmyItemVM> _onSelect;

		// Token: 0x0400052B RID: 1323
		private readonly IViewDataTracker _viewDataTracker;

		// Token: 0x0400052C RID: 1324
		private HeroVM _leader;

		// Token: 0x0400052D RID: 1325
		private MBBindingList<KingdomArmyPartyItemVM> _parties;

		// Token: 0x0400052E RID: 1326
		private string _armyName;

		// Token: 0x0400052F RID: 1327
		private int _strength;

		// Token: 0x04000530 RID: 1328
		private int _cohesion;

		// Token: 0x04000531 RID: 1329
		private string _strengthLabel;

		// Token: 0x04000532 RID: 1330
		private string _shipCountLabel;

		// Token: 0x04000533 RID: 1331
		private int _shipCount;

		// Token: 0x04000534 RID: 1332
		private int _lordCount;

		// Token: 0x04000535 RID: 1333
		private string _location;

		// Token: 0x04000536 RID: 1334
		private string _behavior;

		// Token: 0x04000537 RID: 1335
		private string _cohesionLabel;

		// Token: 0x04000538 RID: 1336
		private bool _isMainArmy;
	}
}
