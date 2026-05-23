using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages
{
	// Token: 0x020000D2 RID: 210
	[EncyclopediaViewModel(typeof(Kingdom))]
	public class EncyclopediaFactionPageVM : EncyclopediaContentPageVM
	{
		// Token: 0x060013BC RID: 5052 RVA: 0x0004ECE4 File Offset: 0x0004CEE4
		public EncyclopediaFactionPageVM(EncyclopediaPageArgs args)
			: base(args)
		{
			this._faction = base.Obj as Kingdom;
			this.Clans = new MBBindingList<EncyclopediaFactionVM>();
			this.Enemies = new MBBindingList<EncyclopediaFactionVM>();
			this.TradeAgreements = new MBBindingList<EncyclopediaFactionVM>();
			this.Alliances = new MBBindingList<EncyclopediaFactionVM>();
			this.Settlements = new MBBindingList<EncyclopediaSettlementVM>();
			this.History = new MBBindingList<EncyclopediaHistoryEventVM>();
			base.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(this._faction);
			this.RefreshValues();
		}

		// Token: 0x060013BD RID: 5053 RVA: 0x0004ED74 File Offset: 0x0004CF74
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.StrengthHint = new HintViewModel(GameTexts.FindText("str_strength", null), null);
			this.ProsperityHint = new HintViewModel(GameTexts.FindText("str_prosperity", null), null);
			this.MembersText = GameTexts.FindText("str_members", null).ToString();
			this.ClansText = new TextObject("{=bfQLwMUp}Clans", null).ToString();
			this.EnemiesText = new TextObject("{=zZlWRZjO}Wars", null).ToString();
			this.TradeAgreementsText = new TextObject("{=pWyRg13f}Trade Agreements", null).ToString();
			this.AlliancesText = new TextObject("{=f90A6PGd}Alliances", null).ToString();
			this.SettlementsText = new TextObject("{=LBNzsqyb}Fiefs", null).ToString();
			this.VillagesText = GameTexts.FindText("str_villages", null).ToString();
			TextObject encyclopediaText = this._faction.EncyclopediaText;
			this.InformationText = ((encyclopediaText != null) ? encyclopediaText.ToString() : null) ?? string.Empty;
			base.UpdateBookmarkHintText();
			this.Refresh();
		}

		// Token: 0x060013BE RID: 5054 RVA: 0x0004EE84 File Offset: 0x0004D084
		public override void Refresh()
		{
			base.IsLoadingOver = false;
			this.Clans.Clear();
			this.Enemies.Clear();
			this.TradeAgreements.Clear();
			this.Alliances.Clear();
			this.Settlements.Clear();
			this.History.Clear();
			this.Leader = new HeroVM(this._faction.Leader, false);
			this.LeaderText = GameTexts.FindText("str_leader", null).ToString();
			this.NameText = this._faction.Name.ToString();
			this.DescriptorText = GameTexts.FindText("str_kingdom_faction", null).ToString();
			int num = 0;
			float num2 = 0f;
			EncyclopediaPage pageOf = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Hero));
			foreach (Hero hero in this._faction.AliveLords)
			{
				if (pageOf.IsValidEncyclopediaItem(hero))
				{
					num += hero.Gold;
				}
			}
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
				if ((encyclopediaLog = Campaign.Current.LogEntryHistory.GameActionLogs[i] as IEncyclopediaLog) != null && encyclopediaLog.IsVisibleInEncyclopediaPageOf<Kingdom>(this._faction))
				{
					this.History.Add(new EncyclopediaHistoryEventVM(encyclopediaLog));
				}
			}
			EncyclopediaPage pageOf2 = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Clan));
			IOrderedEnumerable<IFaction> orderedEnumerable = (from x in Campaign.Current.Factions
				orderby !x.IsKingdomFaction
				select x).ThenBy((IFaction f) => f.Name.ToString());
			using (IEnumerator<IFaction> enumerator3 = orderedEnumerable.GetEnumerator())
			{
				while (enumerator3.MoveNext())
				{
					IFaction factionObject = enumerator3.Current;
					if (pageOf2.IsValidEncyclopediaItem(factionObject) && factionObject != this._faction && !factionObject.IsBanditFaction && FactionManager.IsAtWarAgainstFaction(this._faction, factionObject.MapFaction) && !this.Enemies.Any((EncyclopediaFactionVM x) => x.Faction == factionObject.MapFaction))
					{
						this.Enemies.Add(new EncyclopediaFactionVM(factionObject.MapFaction));
					}
				}
			}
			foreach (IFaction faction in from x in orderedEnumerable
				where x.IsKingdomFaction
				select x)
			{
				Kingdom kingdom;
				if (pageOf2.IsValidEncyclopediaItem(faction) && faction != this._faction && (kingdom = faction as Kingdom) != null)
				{
					if (this.HasTradeAgreementWithFaction(this._faction, kingdom.MapFaction) && !this.TradeAgreements.Any((EncyclopediaFactionVM x) => x.Faction == kingdom.MapFaction))
					{
						this.TradeAgreements.Add(new EncyclopediaFactionVM(kingdom.MapFaction));
					}
					if (this.HasAllianceWithFaction(this._faction, kingdom.MapFaction) && !this.Alliances.Any((EncyclopediaFactionVM x) => x.Faction == kingdom.MapFaction))
					{
						this.Alliances.Add(new EncyclopediaFactionVM(kingdom.MapFaction));
					}
				}
			}
			foreach (Clan faction2 in from c in Campaign.Current.Clans
				where c.Kingdom == this._faction
				select c)
			{
				this.Clans.Add(new EncyclopediaFactionVM(faction2));
			}
			EncyclopediaPage pageOf3 = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Settlement));
			foreach (Settlement settlement in from s in Settlement.All
				where s.IsTown || s.IsCastle
				orderby s.IsCastle, s.IsTown
				select s)
			{
				if ((settlement.MapFaction == this._faction || (settlement.OwnerClan == this._faction.RulingClan && settlement.OwnerClan.Leader != null)) && pageOf3.IsValidEncyclopediaItem(settlement))
				{
					this.Settlements.Add(new EncyclopediaSettlementVM(settlement));
				}
			}
			base.IsLoadingOver = true;
		}

		// Token: 0x060013BF RID: 5055 RVA: 0x0004F490 File Offset: 0x0004D690
		private bool HasTradeAgreementWithFaction(IFaction faction1, IFaction faction2)
		{
			return faction1 != null && faction2 != null && faction1 != faction2 && !faction1.IsEliminated && !faction2.IsEliminated && faction1.IsKingdomFaction && faction2.IsKingdomFaction && Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>().HasTradeAgreement(faction1 as Kingdom, faction2 as Kingdom);
		}

		// Token: 0x060013C0 RID: 5056 RVA: 0x0004F4E4 File Offset: 0x0004D6E4
		private bool HasAllianceWithFaction(IFaction faction1, IFaction faction2)
		{
			return faction1 != null && faction2 != null && faction1 != faction2 && !faction1.IsEliminated && !faction2.IsEliminated && faction1.IsKingdomFaction && faction2.IsKingdomFaction && Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>().IsAllyWithKingdom(faction1 as Kingdom, faction2 as Kingdom);
		}

		// Token: 0x060013C1 RID: 5057 RVA: 0x0004F538 File Offset: 0x0004D738
		public override string GetName()
		{
			return this._faction.Name.ToString();
		}

		// Token: 0x060013C2 RID: 5058 RVA: 0x0004F54C File Offset: 0x0004D74C
		public override string GetNavigationBarURL()
		{
			return HyperlinkTexts.GetGenericHyperlinkText("Home", GameTexts.FindText("str_encyclopedia_home", null).ToString()) + " \\ " + HyperlinkTexts.GetGenericHyperlinkText("ListPage-Kingdoms", GameTexts.FindText("str_encyclopedia_kingdoms", null).ToString()) + " \\ " + this.GetName();
		}

		// Token: 0x060013C3 RID: 5059 RVA: 0x0004F5B4 File Offset: 0x0004D7B4
		public override void ExecuteSwitchBookmarkedState()
		{
			base.ExecuteSwitchBookmarkedState();
			if (base.IsBookmarked)
			{
				Campaign.Current.EncyclopediaManager.ViewDataTracker.AddEncyclopediaBookmarkToItem(this._faction);
				return;
			}
			Campaign.Current.EncyclopediaManager.ViewDataTracker.RemoveEncyclopediaBookmarkFromItem(this._faction);
		}

		// Token: 0x1700067A RID: 1658
		// (get) Token: 0x060013C4 RID: 5060 RVA: 0x0004F604 File Offset: 0x0004D804
		// (set) Token: 0x060013C5 RID: 5061 RVA: 0x0004F60C File Offset: 0x0004D80C
		[DataSourceProperty]
		public MBBindingList<EncyclopediaFactionVM> Clans
		{
			get
			{
				return this._clans;
			}
			set
			{
				if (value != this._clans)
				{
					this._clans = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaFactionVM>>(value, "Clans");
				}
			}
		}

		// Token: 0x1700067B RID: 1659
		// (get) Token: 0x060013C6 RID: 5062 RVA: 0x0004F62A File Offset: 0x0004D82A
		// (set) Token: 0x060013C7 RID: 5063 RVA: 0x0004F632 File Offset: 0x0004D832
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

		// Token: 0x1700067C RID: 1660
		// (get) Token: 0x060013C8 RID: 5064 RVA: 0x0004F650 File Offset: 0x0004D850
		// (set) Token: 0x060013C9 RID: 5065 RVA: 0x0004F658 File Offset: 0x0004D858
		[DataSourceProperty]
		public MBBindingList<EncyclopediaFactionVM> TradeAgreements
		{
			get
			{
				return this._tradeAgreements;
			}
			set
			{
				if (value != this._tradeAgreements)
				{
					this._tradeAgreements = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaFactionVM>>(value, "TradeAgreements");
				}
			}
		}

		// Token: 0x1700067D RID: 1661
		// (get) Token: 0x060013CA RID: 5066 RVA: 0x0004F676 File Offset: 0x0004D876
		// (set) Token: 0x060013CB RID: 5067 RVA: 0x0004F67E File Offset: 0x0004D87E
		[DataSourceProperty]
		public MBBindingList<EncyclopediaFactionVM> Alliances
		{
			get
			{
				return this._alliances;
			}
			set
			{
				if (value != this._alliances)
				{
					this._alliances = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaFactionVM>>(value, "Alliances");
				}
			}
		}

		// Token: 0x1700067E RID: 1662
		// (get) Token: 0x060013CC RID: 5068 RVA: 0x0004F69C File Offset: 0x0004D89C
		// (set) Token: 0x060013CD RID: 5069 RVA: 0x0004F6A4 File Offset: 0x0004D8A4
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

		// Token: 0x1700067F RID: 1663
		// (get) Token: 0x060013CE RID: 5070 RVA: 0x0004F6C2 File Offset: 0x0004D8C2
		// (set) Token: 0x060013CF RID: 5071 RVA: 0x0004F6CA File Offset: 0x0004D8CA
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

		// Token: 0x17000680 RID: 1664
		// (get) Token: 0x060013D0 RID: 5072 RVA: 0x0004F6E8 File Offset: 0x0004D8E8
		// (set) Token: 0x060013D1 RID: 5073 RVA: 0x0004F6F0 File Offset: 0x0004D8F0
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

		// Token: 0x17000681 RID: 1665
		// (get) Token: 0x060013D2 RID: 5074 RVA: 0x0004F70E File Offset: 0x0004D90E
		// (set) Token: 0x060013D3 RID: 5075 RVA: 0x0004F716 File Offset: 0x0004D916
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

		// Token: 0x17000682 RID: 1666
		// (get) Token: 0x060013D4 RID: 5076 RVA: 0x0004F734 File Offset: 0x0004D934
		// (set) Token: 0x060013D5 RID: 5077 RVA: 0x0004F73C File Offset: 0x0004D93C
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

		// Token: 0x17000683 RID: 1667
		// (get) Token: 0x060013D6 RID: 5078 RVA: 0x0004F75F File Offset: 0x0004D95F
		// (set) Token: 0x060013D7 RID: 5079 RVA: 0x0004F767 File Offset: 0x0004D967
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

		// Token: 0x17000684 RID: 1668
		// (get) Token: 0x060013D8 RID: 5080 RVA: 0x0004F78A File Offset: 0x0004D98A
		// (set) Token: 0x060013D9 RID: 5081 RVA: 0x0004F792 File Offset: 0x0004D992
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

		// Token: 0x17000685 RID: 1669
		// (get) Token: 0x060013DA RID: 5082 RVA: 0x0004F7B5 File Offset: 0x0004D9B5
		// (set) Token: 0x060013DB RID: 5083 RVA: 0x0004F7BD File Offset: 0x0004D9BD
		[DataSourceProperty]
		public string TradeAgreementsText
		{
			get
			{
				return this._tradeAgreementsText;
			}
			set
			{
				if (value != this._tradeAgreementsText)
				{
					this._tradeAgreementsText = value;
					base.OnPropertyChangedWithValue<string>(value, "TradeAgreementsText");
				}
			}
		}

		// Token: 0x17000686 RID: 1670
		// (get) Token: 0x060013DC RID: 5084 RVA: 0x0004F7E0 File Offset: 0x0004D9E0
		// (set) Token: 0x060013DD RID: 5085 RVA: 0x0004F7E8 File Offset: 0x0004D9E8
		[DataSourceProperty]
		public string AlliancesText
		{
			get
			{
				return this._alliancesText;
			}
			set
			{
				if (value != this._alliancesText)
				{
					this._alliancesText = value;
					base.OnPropertyChangedWithValue<string>(value, "AlliancesText");
				}
			}
		}

		// Token: 0x17000687 RID: 1671
		// (get) Token: 0x060013DE RID: 5086 RVA: 0x0004F80B File Offset: 0x0004DA0B
		// (set) Token: 0x060013DF RID: 5087 RVA: 0x0004F813 File Offset: 0x0004DA13
		[DataSourceProperty]
		public string ClansText
		{
			get
			{
				return this._clansText;
			}
			set
			{
				if (value != this._clansText)
				{
					this._clansText = value;
					base.OnPropertyChangedWithValue<string>(value, "ClansText");
				}
			}
		}

		// Token: 0x17000688 RID: 1672
		// (get) Token: 0x060013E0 RID: 5088 RVA: 0x0004F836 File Offset: 0x0004DA36
		// (set) Token: 0x060013E1 RID: 5089 RVA: 0x0004F83E File Offset: 0x0004DA3E
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

		// Token: 0x17000689 RID: 1673
		// (get) Token: 0x060013E2 RID: 5090 RVA: 0x0004F861 File Offset: 0x0004DA61
		// (set) Token: 0x060013E3 RID: 5091 RVA: 0x0004F869 File Offset: 0x0004DA69
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

		// Token: 0x1700068A RID: 1674
		// (get) Token: 0x060013E4 RID: 5092 RVA: 0x0004F88C File Offset: 0x0004DA8C
		// (set) Token: 0x060013E5 RID: 5093 RVA: 0x0004F894 File Offset: 0x0004DA94
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

		// Token: 0x1700068B RID: 1675
		// (get) Token: 0x060013E6 RID: 5094 RVA: 0x0004F8B7 File Offset: 0x0004DAB7
		// (set) Token: 0x060013E7 RID: 5095 RVA: 0x0004F8BF File Offset: 0x0004DABF
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

		// Token: 0x1700068C RID: 1676
		// (get) Token: 0x060013E8 RID: 5096 RVA: 0x0004F8E2 File Offset: 0x0004DAE2
		// (set) Token: 0x060013E9 RID: 5097 RVA: 0x0004F8EA File Offset: 0x0004DAEA
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

		// Token: 0x1700068D RID: 1677
		// (get) Token: 0x060013EA RID: 5098 RVA: 0x0004F90D File Offset: 0x0004DB0D
		// (set) Token: 0x060013EB RID: 5099 RVA: 0x0004F915 File Offset: 0x0004DB15
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

		// Token: 0x1700068E RID: 1678
		// (get) Token: 0x060013EC RID: 5100 RVA: 0x0004F938 File Offset: 0x0004DB38
		// (set) Token: 0x060013ED RID: 5101 RVA: 0x0004F940 File Offset: 0x0004DB40
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

		// Token: 0x1700068F RID: 1679
		// (get) Token: 0x060013EE RID: 5102 RVA: 0x0004F963 File Offset: 0x0004DB63
		// (set) Token: 0x060013EF RID: 5103 RVA: 0x0004F96B File Offset: 0x0004DB6B
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

		// Token: 0x17000690 RID: 1680
		// (get) Token: 0x060013F0 RID: 5104 RVA: 0x0004F989 File Offset: 0x0004DB89
		// (set) Token: 0x060013F1 RID: 5105 RVA: 0x0004F991 File Offset: 0x0004DB91
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

		// Token: 0x0400090C RID: 2316
		private Kingdom _faction;

		// Token: 0x0400090D RID: 2317
		private MBBindingList<EncyclopediaFactionVM> _clans;

		// Token: 0x0400090E RID: 2318
		private MBBindingList<EncyclopediaFactionVM> _enemies;

		// Token: 0x0400090F RID: 2319
		private MBBindingList<EncyclopediaFactionVM> _tradeAgreements;

		// Token: 0x04000910 RID: 2320
		private MBBindingList<EncyclopediaFactionVM> _alliances;

		// Token: 0x04000911 RID: 2321
		private MBBindingList<EncyclopediaSettlementVM> _settlements;

		// Token: 0x04000912 RID: 2322
		private MBBindingList<EncyclopediaHistoryEventVM> _history;

		// Token: 0x04000913 RID: 2323
		private HeroVM _leader;

		// Token: 0x04000914 RID: 2324
		private BannerImageIdentifierVM _banner;

		// Token: 0x04000915 RID: 2325
		private string _membersText;

		// Token: 0x04000916 RID: 2326
		private string _enemiesText;

		// Token: 0x04000917 RID: 2327
		private string _tradeAgreementsText;

		// Token: 0x04000918 RID: 2328
		private string _alliancesText;

		// Token: 0x04000919 RID: 2329
		private string _clansText;

		// Token: 0x0400091A RID: 2330
		private string _settlementsText;

		// Token: 0x0400091B RID: 2331
		private string _villagesText;

		// Token: 0x0400091C RID: 2332
		private string _leaderText;

		// Token: 0x0400091D RID: 2333
		private string _descriptorText;

		// Token: 0x0400091E RID: 2334
		private string _prosperityText;

		// Token: 0x0400091F RID: 2335
		private string _strengthText;

		// Token: 0x04000920 RID: 2336
		private string _informationText;

		// Token: 0x04000921 RID: 2337
		private HintViewModel _prosperityHint;

		// Token: 0x04000922 RID: 2338
		private HintViewModel _strengthHint;

		// Token: 0x04000923 RID: 2339
		private string _nameText;
	}
}
