using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages
{
	// Token: 0x020000CE RID: 206
	[EncyclopediaViewModel(typeof(Clan))]
	public class EncyclopediaClanPageVM : EncyclopediaContentPageVM
	{
		// Token: 0x06001361 RID: 4961 RVA: 0x0004DAE0 File Offset: 0x0004BCE0
		public EncyclopediaClanPageVM(EncyclopediaPageArgs args)
			: base(args)
		{
			this._faction = base.Obj as IFaction;
			this._clan = this._faction as Clan;
			this.Members = new MBBindingList<HeroVM>();
			this.Enemies = new MBBindingList<EncyclopediaFactionVM>();
			this.Settlements = new MBBindingList<EncyclopediaSettlementVM>();
			this.History = new MBBindingList<EncyclopediaHistoryEventVM>();
			this.ClanInfo = new MBBindingList<StringPairItemVM>();
			base.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(this._clan);
			this.RefreshValues();
		}

		// Token: 0x06001362 RID: 4962 RVA: 0x0004DB74 File Offset: 0x0004BD74
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.StrengthHint = new HintViewModel(GameTexts.FindText("str_strength", null), null);
			this.ProsperityHint = new HintViewModel(GameTexts.FindText("str_prosperity", null), null);
			this.MembersText = GameTexts.FindText("str_members", null).ToString();
			this.AlliesText = new TextObject("{=bfQLwMUp}Clans", null).ToString();
			this.EnemiesText = new TextObject("{=zZlWRZjO}Wars", null).ToString();
			this.SettlementsText = GameTexts.FindText("str_settlements", null).ToString();
			this.VillagesText = GameTexts.FindText("str_villages", null).ToString();
			this.DestroyedText = new TextObject("{=w8Yzf0F0}Destroyed", null).ToString();
			this.PartOfText = GameTexts.FindText("str_encyclopedia_clan_part_of_kingdom", null).ToString();
			this.LeaderText = GameTexts.FindText("str_leader", null).ToString();
			this.InfoText = GameTexts.FindText("str_info", null).ToString();
			base.UpdateBookmarkHintText();
			this.Refresh();
		}

		// Token: 0x06001363 RID: 4963 RVA: 0x0004DC88 File Offset: 0x0004BE88
		public override void Refresh()
		{
			this.Members.Clear();
			this.Enemies.Clear();
			this.Settlements.Clear();
			this.History.Clear();
			this.ClanInfo.Clear();
			TextObject encyclopediaText = this._faction.EncyclopediaText;
			this.InformationText = ((encyclopediaText != null) ? encyclopediaText.ToString() : null);
			this.Leader = new HeroVM(this._faction.Leader, true);
			this.NameText = this._clan.Name.ToString();
			this.HasParentKingdom = this._clan.Kingdom != null;
			this.ParentKingdom = (this.HasParentKingdom ? new EncyclopediaFactionVM(((Clan)this._faction).Kingdom) : null);
			if (this._faction.IsKingdomFaction)
			{
				this.DescriptorText = GameTexts.FindText("str_kingdom_faction", null).ToString();
			}
			else if (this._faction.IsBanditFaction)
			{
				this.DescriptorText = GameTexts.FindText("str_bandit_faction", null).ToString();
			}
			else if (this._faction.IsMinorFaction)
			{
				this.DescriptorText = GameTexts.FindText("str_minor_faction", null).ToString();
			}
			int num = 0;
			float num2 = 0f;
			EncyclopediaPage pageOf = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Hero));
			IEnumerable<Hero> heroes = this._faction.Heroes;
			Clan clan = this._clan;
			foreach (Hero hero in heroes.Union((clan != null) ? clan.Companions : null))
			{
				if (pageOf.IsValidEncyclopediaItem(hero))
				{
					if (hero != this.Leader.Hero)
					{
						this.Members.Add(new HeroVM(hero, true));
					}
					num += hero.Gold;
				}
			}
			this.Members.Sort(new HeroAgeComparer(false));
			this.Banner = new BannerImageIdentifierVM(this._faction.Banner, true);
			foreach (MobileParty mobileParty in MobileParty.AllLordParties)
			{
				if (mobileParty.MapFaction == this._faction && !mobileParty.IsDisbanding)
				{
					num2 += mobileParty.Party.CalculateCurrentStrength();
				}
			}
			this.ProsperityText = num.ToString();
			this.StrengthText = num2.ToString();
			for (int i = Campaign.Current.LogEntryHistory.GameActionLogs.Count - 1; i >= 0; i--)
			{
				IEncyclopediaLog encyclopediaLog;
				if ((encyclopediaLog = Campaign.Current.LogEntryHistory.GameActionLogs[i] as IEncyclopediaLog) != null && ((this._faction.IsKingdomFaction && encyclopediaLog.IsVisibleInEncyclopediaPageOf<Kingdom>((Kingdom)this._faction)) || (this._faction.IsClan && encyclopediaLog.IsVisibleInEncyclopediaPageOf<Clan>((Clan)this._faction))))
				{
					this.History.Add(new EncyclopediaHistoryEventVM(encyclopediaLog));
				}
			}
			EncyclopediaPage pageOf2 = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Clan));
			foreach (IFaction faction in (from x in Campaign.Current.Factions
				orderby !x.IsKingdomFaction
				select x).ThenBy((IFaction f) => f.Name.ToString()))
			{
				IFaction mapFaction = faction.MapFaction;
				if (pageOf2.IsValidEncyclopediaItem(mapFaction) && mapFaction != this._faction.MapFaction && mapFaction != this._faction && !mapFaction.IsBanditFaction && FactionManager.IsAtWarAgainstFaction(this._faction.MapFaction, mapFaction) && !this.Enemies.Any((EncyclopediaFactionVM x) => x.Faction == mapFaction))
				{
					this.Enemies.Add(new EncyclopediaFactionVM(mapFaction));
				}
			}
			EncyclopediaPage pageOf3 = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Settlement));
			foreach (Settlement settlement in from s in Settlement.All
				orderby s.IsVillage, s.IsCastle, s.IsTown
				select s)
			{
				if ((settlement.MapFaction == this._faction || (settlement.OwnerClan == this._faction && settlement.OwnerClan.Leader != null)) && pageOf3.IsValidEncyclopediaItem(settlement) && (settlement.IsTown || settlement.IsCastle))
				{
					this.Settlements.Add(new EncyclopediaSettlementVM(settlement));
				}
			}
			GameTexts.SetVariable("LEFT", new TextObject("{=tTLvo8sM}Clan Tier", null).ToString());
			this.ClanInfo.Add(new StringPairItemVM(GameTexts.FindText("str_LEFT_colon", null).ToString(), this._clan.Tier.ToString(), null));
			GameTexts.SetVariable("LEFT", new TextObject("{=ODEnkg0o}Clan Strength", null).ToString());
			this.ClanInfo.Add(new StringPairItemVM(GameTexts.FindText("str_LEFT_colon", null).ToString(), this._clan.CurrentTotalStrength.ToString("F0"), null));
			GameTexts.SetVariable("LEFT", GameTexts.FindText("str_wealth", null).ToString());
			this.ClanInfo.Add(new StringPairItemVM(GameTexts.FindText("str_LEFT_colon", null).ToString(), CampaignUIHelper.GetClanWealthStatusText(this._clan), null));
			this.IsClanDestroyed = this._clan.IsEliminated;
		}

		// Token: 0x06001364 RID: 4964 RVA: 0x0004E30C File Offset: 0x0004C50C
		public override string GetName()
		{
			return this._clan.Name.ToString();
		}

		// Token: 0x06001365 RID: 4965 RVA: 0x0004E320 File Offset: 0x0004C520
		public override string GetNavigationBarURL()
		{
			return HyperlinkTexts.GetGenericHyperlinkText("Home", GameTexts.FindText("str_encyclopedia_home", null).ToString()) + " \\ " + HyperlinkTexts.GetGenericHyperlinkText("ListPage-Clans", GameTexts.FindText("str_encyclopedia_clans", null).ToString()) + " \\ " + this.GetName();
		}

		// Token: 0x06001366 RID: 4966 RVA: 0x0004E388 File Offset: 0x0004C588
		public override void ExecuteSwitchBookmarkedState()
		{
			base.ExecuteSwitchBookmarkedState();
			if (base.IsBookmarked)
			{
				Campaign.Current.EncyclopediaManager.ViewDataTracker.AddEncyclopediaBookmarkToItem(this._clan);
				return;
			}
			Campaign.Current.EncyclopediaManager.ViewDataTracker.RemoveEncyclopediaBookmarkFromItem(this._clan);
		}

		// Token: 0x17000657 RID: 1623
		// (get) Token: 0x06001367 RID: 4967 RVA: 0x0004E3D8 File Offset: 0x0004C5D8
		// (set) Token: 0x06001368 RID: 4968 RVA: 0x0004E3E0 File Offset: 0x0004C5E0
		[DataSourceProperty]
		public MBBindingList<StringPairItemVM> ClanInfo
		{
			get
			{
				return this._clanInfo;
			}
			set
			{
				if (value != this._clanInfo)
				{
					this._clanInfo = value;
					base.OnPropertyChangedWithValue<MBBindingList<StringPairItemVM>>(value, "ClanInfo");
				}
			}
		}

		// Token: 0x17000658 RID: 1624
		// (get) Token: 0x06001369 RID: 4969 RVA: 0x0004E3FE File Offset: 0x0004C5FE
		// (set) Token: 0x0600136A RID: 4970 RVA: 0x0004E406 File Offset: 0x0004C606
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

		// Token: 0x17000659 RID: 1625
		// (get) Token: 0x0600136B RID: 4971 RVA: 0x0004E424 File Offset: 0x0004C624
		// (set) Token: 0x0600136C RID: 4972 RVA: 0x0004E42C File Offset: 0x0004C62C
		[DataSourceProperty]
		public MBBindingList<EncyclopediaFactionVM> Enemies
		{
			get
			{
				return this._enemies;
			}
			set
			{
				if (value != this._enemies)
				{
					this._enemies = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaFactionVM>>(value, "Enemies");
				}
			}
		}

		// Token: 0x1700065A RID: 1626
		// (get) Token: 0x0600136D RID: 4973 RVA: 0x0004E44A File Offset: 0x0004C64A
		// (set) Token: 0x0600136E RID: 4974 RVA: 0x0004E452 File Offset: 0x0004C652
		[DataSourceProperty]
		public MBBindingList<EncyclopediaSettlementVM> Settlements
		{
			get
			{
				return this._settlements;
			}
			set
			{
				if (value != this._settlements)
				{
					this._settlements = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaSettlementVM>>(value, "Settlements");
				}
			}
		}

		// Token: 0x1700065B RID: 1627
		// (get) Token: 0x0600136F RID: 4975 RVA: 0x0004E470 File Offset: 0x0004C670
		// (set) Token: 0x06001370 RID: 4976 RVA: 0x0004E478 File Offset: 0x0004C678
		[DataSourceProperty]
		public MBBindingList<EncyclopediaHistoryEventVM> History
		{
			get
			{
				return this._history;
			}
			set
			{
				if (value != this._history)
				{
					this._history = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaHistoryEventVM>>(value, "History");
				}
			}
		}

		// Token: 0x1700065C RID: 1628
		// (get) Token: 0x06001371 RID: 4977 RVA: 0x0004E496 File Offset: 0x0004C696
		// (set) Token: 0x06001372 RID: 4978 RVA: 0x0004E49E File Offset: 0x0004C69E
		[DataSourceProperty]
		public EncyclopediaFactionVM ParentKingdom
		{
			get
			{
				return this._parentKingdom;
			}
			set
			{
				if (value != this._parentKingdom)
				{
					this._parentKingdom = value;
					base.OnPropertyChangedWithValue<EncyclopediaFactionVM>(value, "ParentKingdom");
				}
			}
		}

		// Token: 0x1700065D RID: 1629
		// (get) Token: 0x06001373 RID: 4979 RVA: 0x0004E4BC File Offset: 0x0004C6BC
		// (set) Token: 0x06001374 RID: 4980 RVA: 0x0004E4C4 File Offset: 0x0004C6C4
		[DataSourceProperty]
		public bool HasParentKingdom
		{
			get
			{
				return this._hasParentKingdom;
			}
			set
			{
				if (value != this._hasParentKingdom)
				{
					this._hasParentKingdom = value;
					base.OnPropertyChangedWithValue(value, "HasParentKingdom");
				}
			}
		}

		// Token: 0x1700065E RID: 1630
		// (get) Token: 0x06001375 RID: 4981 RVA: 0x0004E4E2 File Offset: 0x0004C6E2
		// (set) Token: 0x06001376 RID: 4982 RVA: 0x0004E4EA File Offset: 0x0004C6EA
		[DataSourceProperty]
		public bool IsClanDestroyed
		{
			get
			{
				return this._isClanDestroyed;
			}
			set
			{
				if (value != this._isClanDestroyed)
				{
					this._isClanDestroyed = value;
					base.OnPropertyChangedWithValue(value, "IsClanDestroyed");
				}
			}
		}

		// Token: 0x1700065F RID: 1631
		// (get) Token: 0x06001377 RID: 4983 RVA: 0x0004E508 File Offset: 0x0004C708
		// (set) Token: 0x06001378 RID: 4984 RVA: 0x0004E510 File Offset: 0x0004C710
		[DataSourceProperty]
		public string DestroyedText
		{
			get
			{
				return this._destroyedText;
			}
			set
			{
				if (value != this._destroyedText)
				{
					this._destroyedText = value;
					base.OnPropertyChangedWithValue<string>(value, "DestroyedText");
				}
			}
		}

		// Token: 0x17000660 RID: 1632
		// (get) Token: 0x06001379 RID: 4985 RVA: 0x0004E533 File Offset: 0x0004C733
		// (set) Token: 0x0600137A RID: 4986 RVA: 0x0004E53B File Offset: 0x0004C73B
		[DataSourceProperty]
		public string PartOfText
		{
			get
			{
				return this._partOfText;
			}
			set
			{
				if (value != this._partOfText)
				{
					this._partOfText = value;
					base.OnPropertyChangedWithValue<string>(value, "PartOfText");
				}
			}
		}

		// Token: 0x17000661 RID: 1633
		// (get) Token: 0x0600137B RID: 4987 RVA: 0x0004E55E File Offset: 0x0004C75E
		// (set) Token: 0x0600137C RID: 4988 RVA: 0x0004E566 File Offset: 0x0004C766
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

		// Token: 0x17000662 RID: 1634
		// (get) Token: 0x0600137D RID: 4989 RVA: 0x0004E589 File Offset: 0x0004C789
		// (set) Token: 0x0600137E RID: 4990 RVA: 0x0004E591 File Offset: 0x0004C791
		[DataSourceProperty]
		public string InfoText
		{
			get
			{
				return this._infoText;
			}
			set
			{
				if (value != this._infoText)
				{
					this._infoText = value;
					base.OnPropertyChangedWithValue<string>(value, "InfoText");
				}
			}
		}

		// Token: 0x17000663 RID: 1635
		// (get) Token: 0x0600137F RID: 4991 RVA: 0x0004E5B4 File Offset: 0x0004C7B4
		// (set) Token: 0x06001380 RID: 4992 RVA: 0x0004E5BC File Offset: 0x0004C7BC
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
					base.OnPropertyChangedWithValue<HeroVM>(value, "Leader");
				}
			}
		}

		// Token: 0x17000664 RID: 1636
		// (get) Token: 0x06001381 RID: 4993 RVA: 0x0004E5DA File Offset: 0x0004C7DA
		// (set) Token: 0x06001382 RID: 4994 RVA: 0x0004E5E2 File Offset: 0x0004C7E2
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

		// Token: 0x17000665 RID: 1637
		// (get) Token: 0x06001383 RID: 4995 RVA: 0x0004E600 File Offset: 0x0004C800
		// (set) Token: 0x06001384 RID: 4996 RVA: 0x0004E608 File Offset: 0x0004C808
		[DataSourceProperty]
		public string NameText
		{
			get
			{
				return this._nameText;
			}
			set
			{
				if (value != this._nameText)
				{
					this._nameText = value;
					base.OnPropertyChangedWithValue<string>(value, "NameText");
				}
			}
		}

		// Token: 0x17000666 RID: 1638
		// (get) Token: 0x06001385 RID: 4997 RVA: 0x0004E62B File Offset: 0x0004C82B
		// (set) Token: 0x06001386 RID: 4998 RVA: 0x0004E633 File Offset: 0x0004C833
		[DataSourceProperty]
		public string MembersText
		{
			get
			{
				return this._membersText;
			}
			set
			{
				if (value != this._membersText)
				{
					this._membersText = value;
					base.OnPropertyChangedWithValue<string>(value, "MembersText");
				}
			}
		}

		// Token: 0x17000667 RID: 1639
		// (get) Token: 0x06001387 RID: 4999 RVA: 0x0004E656 File Offset: 0x0004C856
		// (set) Token: 0x06001388 RID: 5000 RVA: 0x0004E65E File Offset: 0x0004C85E
		[DataSourceProperty]
		public string EnemiesText
		{
			get
			{
				return this._enemiesText;
			}
			set
			{
				if (value != this._enemiesText)
				{
					this._enemiesText = value;
					base.OnPropertyChangedWithValue<string>(value, "EnemiesText");
				}
			}
		}

		// Token: 0x17000668 RID: 1640
		// (get) Token: 0x06001389 RID: 5001 RVA: 0x0004E681 File Offset: 0x0004C881
		// (set) Token: 0x0600138A RID: 5002 RVA: 0x0004E689 File Offset: 0x0004C889
		[DataSourceProperty]
		public string AlliesText
		{
			get
			{
				return this._alliesText;
			}
			set
			{
				if (value != this._alliesText)
				{
					this._alliesText = value;
					base.OnPropertyChangedWithValue<string>(value, "AlliesText");
				}
			}
		}

		// Token: 0x17000669 RID: 1641
		// (get) Token: 0x0600138B RID: 5003 RVA: 0x0004E6AC File Offset: 0x0004C8AC
		// (set) Token: 0x0600138C RID: 5004 RVA: 0x0004E6B4 File Offset: 0x0004C8B4
		[DataSourceProperty]
		public string SettlementsText
		{
			get
			{
				return this._settlementsText;
			}
			set
			{
				if (value != this._settlementsText)
				{
					this._settlementsText = value;
					base.OnPropertyChangedWithValue<string>(value, "SettlementsText");
				}
			}
		}

		// Token: 0x1700066A RID: 1642
		// (get) Token: 0x0600138D RID: 5005 RVA: 0x0004E6D7 File Offset: 0x0004C8D7
		// (set) Token: 0x0600138E RID: 5006 RVA: 0x0004E6DF File Offset: 0x0004C8DF
		[DataSourceProperty]
		public string VillagesText
		{
			get
			{
				return this._villagesText;
			}
			set
			{
				if (value != this._villagesText)
				{
					this._villagesText = value;
					base.OnPropertyChangedWithValue<string>(value, "VillagesText");
				}
			}
		}

		// Token: 0x1700066B RID: 1643
		// (get) Token: 0x0600138F RID: 5007 RVA: 0x0004E702 File Offset: 0x0004C902
		// (set) Token: 0x06001390 RID: 5008 RVA: 0x0004E70A File Offset: 0x0004C90A
		[DataSourceProperty]
		public string InformationText
		{
			get
			{
				return this._informationText;
			}
			set
			{
				if (value != this._informationText)
				{
					this._informationText = value;
					base.OnPropertyChangedWithValue<string>(value, "InformationText");
				}
			}
		}

		// Token: 0x1700066C RID: 1644
		// (get) Token: 0x06001391 RID: 5009 RVA: 0x0004E72D File Offset: 0x0004C92D
		// (set) Token: 0x06001392 RID: 5010 RVA: 0x0004E735 File Offset: 0x0004C935
		[DataSourceProperty]
		public string LeaderText
		{
			get
			{
				return this._leaderText;
			}
			set
			{
				if (value != this._leaderText)
				{
					this._leaderText = value;
					base.OnPropertyChangedWithValue<string>(value, "LeaderText");
				}
			}
		}

		// Token: 0x1700066D RID: 1645
		// (get) Token: 0x06001393 RID: 5011 RVA: 0x0004E758 File Offset: 0x0004C958
		// (set) Token: 0x06001394 RID: 5012 RVA: 0x0004E760 File Offset: 0x0004C960
		[DataSourceProperty]
		public string DescriptorText
		{
			get
			{
				return this._descriptorText;
			}
			set
			{
				if (value != this._descriptorText)
				{
					this._descriptorText = value;
					base.OnPropertyChangedWithValue<string>(value, "DescriptorText");
				}
			}
		}

		// Token: 0x1700066E RID: 1646
		// (get) Token: 0x06001395 RID: 5013 RVA: 0x0004E783 File Offset: 0x0004C983
		// (set) Token: 0x06001396 RID: 5014 RVA: 0x0004E78B File Offset: 0x0004C98B
		[DataSourceProperty]
		public string ProsperityText
		{
			get
			{
				return this._prosperityText;
			}
			set
			{
				if (value != this._prosperityText)
				{
					this._prosperityText = value;
					base.OnPropertyChangedWithValue<string>(value, "ProsperityText");
				}
			}
		}

		// Token: 0x1700066F RID: 1647
		// (get) Token: 0x06001397 RID: 5015 RVA: 0x0004E7AE File Offset: 0x0004C9AE
		// (set) Token: 0x06001398 RID: 5016 RVA: 0x0004E7B6 File Offset: 0x0004C9B6
		[DataSourceProperty]
		public string StrengthText
		{
			get
			{
				return this._strengthText;
			}
			set
			{
				if (value != this._strengthText)
				{
					this._strengthText = value;
					base.OnPropertyChangedWithValue<string>(value, "StrengthText");
				}
			}
		}

		// Token: 0x17000670 RID: 1648
		// (get) Token: 0x06001399 RID: 5017 RVA: 0x0004E7D9 File Offset: 0x0004C9D9
		// (set) Token: 0x0600139A RID: 5018 RVA: 0x0004E7E1 File Offset: 0x0004C9E1
		[DataSourceProperty]
		public HintViewModel ProsperityHint
		{
			get
			{
				return this._prosperityHint;
			}
			set
			{
				if (value != this._prosperityHint)
				{
					this._prosperityHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ProsperityHint");
				}
			}
		}

		// Token: 0x17000671 RID: 1649
		// (get) Token: 0x0600139B RID: 5019 RVA: 0x0004E7FF File Offset: 0x0004C9FF
		// (set) Token: 0x0600139C RID: 5020 RVA: 0x0004E807 File Offset: 0x0004CA07
		[DataSourceProperty]
		public HintViewModel StrengthHint
		{
			get
			{
				return this._strengthHint;
			}
			set
			{
				if (value != this._strengthHint)
				{
					this._strengthHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "StrengthHint");
				}
			}
		}

		// Token: 0x040008E1 RID: 2273
		private readonly IFaction _faction;

		// Token: 0x040008E2 RID: 2274
		private readonly Clan _clan;

		// Token: 0x040008E3 RID: 2275
		private MBBindingList<StringPairItemVM> _clanInfo;

		// Token: 0x040008E4 RID: 2276
		private MBBindingList<HeroVM> _members;

		// Token: 0x040008E5 RID: 2277
		private MBBindingList<EncyclopediaFactionVM> _enemies;

		// Token: 0x040008E6 RID: 2278
		private MBBindingList<EncyclopediaSettlementVM> _settlements;

		// Token: 0x040008E7 RID: 2279
		private MBBindingList<EncyclopediaHistoryEventVM> _history;

		// Token: 0x040008E8 RID: 2280
		private HeroVM _leader;

		// Token: 0x040008E9 RID: 2281
		private BannerImageIdentifierVM _banner;

		// Token: 0x040008EA RID: 2282
		private string _membersText;

		// Token: 0x040008EB RID: 2283
		private string _enemiesText;

		// Token: 0x040008EC RID: 2284
		private string _alliesText;

		// Token: 0x040008ED RID: 2285
		private string _settlementsText;

		// Token: 0x040008EE RID: 2286
		private string _villagesText;

		// Token: 0x040008EF RID: 2287
		private string _leaderText;

		// Token: 0x040008F0 RID: 2288
		private string _descriptorText;

		// Token: 0x040008F1 RID: 2289
		private string _informationText;

		// Token: 0x040008F2 RID: 2290
		private string _prosperityText;

		// Token: 0x040008F3 RID: 2291
		private string _strengthText;

		// Token: 0x040008F4 RID: 2292
		private string _destroyedText;

		// Token: 0x040008F5 RID: 2293
		private string _partOfText;

		// Token: 0x040008F6 RID: 2294
		private string _tierText;

		// Token: 0x040008F7 RID: 2295
		private string _infoText;

		// Token: 0x040008F8 RID: 2296
		private HintViewModel _prosperityHint;

		// Token: 0x040008F9 RID: 2297
		private HintViewModel _strengthHint;

		// Token: 0x040008FA RID: 2298
		private EncyclopediaFactionVM _parentKingdom;

		// Token: 0x040008FB RID: 2299
		private string _nameText;

		// Token: 0x040008FC RID: 2300
		private bool _hasParentKingdom;

		// Token: 0x040008FD RID: 2301
		private bool _isClanDestroyed;
	}
}
