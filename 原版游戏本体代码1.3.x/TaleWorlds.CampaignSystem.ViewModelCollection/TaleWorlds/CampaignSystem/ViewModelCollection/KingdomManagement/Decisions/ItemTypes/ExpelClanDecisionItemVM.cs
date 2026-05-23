using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions.ItemTypes
{
	// Token: 0x0200007C RID: 124
	public class ExpelClanDecisionItemVM : DecisionItemBaseVM
	{
		// Token: 0x1700031B RID: 795
		// (get) Token: 0x06000A42 RID: 2626 RVA: 0x0002C808 File Offset: 0x0002AA08
		public ExpelClanFromKingdomDecision ExpelDecision
		{
			get
			{
				ExpelClanFromKingdomDecision result;
				if ((result = this._expelDecision) == null)
				{
					result = (this._expelDecision = this._decision as ExpelClanFromKingdomDecision);
				}
				return result;
			}
		}

		// Token: 0x1700031C RID: 796
		// (get) Token: 0x06000A43 RID: 2627 RVA: 0x0002C833 File Offset: 0x0002AA33
		public Clan Clan
		{
			get
			{
				return this.ExpelDecision.ClanToExpel;
			}
		}

		// Token: 0x06000A44 RID: 2628 RVA: 0x0002C840 File Offset: 0x0002AA40
		public ExpelClanDecisionItemVM(ExpelClanFromKingdomDecision decision, Action onDecisionOver)
			: base(decision, onDecisionOver)
		{
			base.DecisionType = 2;
		}

		// Token: 0x06000A45 RID: 2629 RVA: 0x0002C854 File Offset: 0x0002AA54
		protected override void InitValues()
		{
			base.InitValues();
			base.DecisionType = 2;
			this.Members = new MBBindingList<HeroVM>();
			this.Fiefs = new MBBindingList<EncyclopediaSettlementVM>();
			GameTexts.SetVariable("RENOWN", this.Clan.Renown);
			string variableName = "STR1";
			TextObject encyclopediaText = this.Clan.EncyclopediaText;
			GameTexts.SetVariable(variableName, (encyclopediaText != null) ? encyclopediaText.ToString() : null);
			GameTexts.SetVariable("STR2", GameTexts.FindText("str_encyclopedia_renown", null).ToString());
			this.InformationText = GameTexts.FindText("str_STR1_space_STR2", null).ToString();
			this.Leader = new HeroVM(this.Clan.Leader, false);
			this.LeaderText = GameTexts.FindText("str_leader", null).ToString();
			this.MembersText = GameTexts.FindText("str_members", null).ToString();
			this.SettlementsText = GameTexts.FindText("str_fiefs", null).ToString();
			this.NameText = this.Clan.Name.ToString();
			int num = 0;
			float num2 = 0f;
			EncyclopediaPage pageOf = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Hero));
			foreach (Hero hero in this.Clan.Heroes)
			{
				if (hero.IsAlive && hero.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && pageOf.IsValidEncyclopediaItem(hero))
				{
					if (hero != this.Leader.Hero)
					{
						this.Members.Add(new HeroVM(hero, false));
					}
					num += hero.Gold;
				}
			}
			foreach (Hero hero2 in this.Clan.Companions)
			{
				if (hero2.IsAlive && hero2.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && pageOf.IsValidEncyclopediaItem(hero2))
				{
					if (hero2 != this.Leader.Hero)
					{
						this.Members.Add(new HeroVM(hero2, false));
					}
					num += hero2.Gold;
				}
			}
			foreach (MobileParty mobileParty in MobileParty.AllLordParties)
			{
				if (mobileParty.ActualClan == this.Clan && !mobileParty.IsDisbanding)
				{
					num2 += mobileParty.Party.CalculateCurrentStrength();
				}
			}
			this.ProsperityText = num.ToString();
			this.ProsperityHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetClanProsperityTooltip(this.Clan));
			this.StrengthText = num2.ToString();
			this.StrengthHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetClanStrengthTooltip(this.Clan));
			foreach (Town town in from s in this.Clan.Fiefs
				orderby s.IsCastle, s.IsTown
				select s)
			{
				if (town.Settlement.OwnerClan == this.Clan)
				{
					this.Fiefs.Add(new EncyclopediaSettlementVM(town.Settlement));
				}
			}
		}

		// Token: 0x1700031D RID: 797
		// (get) Token: 0x06000A46 RID: 2630 RVA: 0x0002CC1C File Offset: 0x0002AE1C
		// (set) Token: 0x06000A47 RID: 2631 RVA: 0x0002CC24 File Offset: 0x0002AE24
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

		// Token: 0x1700031E RID: 798
		// (get) Token: 0x06000A48 RID: 2632 RVA: 0x0002CC42 File Offset: 0x0002AE42
		// (set) Token: 0x06000A49 RID: 2633 RVA: 0x0002CC4A File Offset: 0x0002AE4A
		[DataSourceProperty]
		public MBBindingList<EncyclopediaSettlementVM> Fiefs
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
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaSettlementVM>>(value, "Fiefs");
				}
			}
		}

		// Token: 0x1700031F RID: 799
		// (get) Token: 0x06000A4A RID: 2634 RVA: 0x0002CC68 File Offset: 0x0002AE68
		// (set) Token: 0x06000A4B RID: 2635 RVA: 0x0002CC70 File Offset: 0x0002AE70
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

		// Token: 0x17000320 RID: 800
		// (get) Token: 0x06000A4C RID: 2636 RVA: 0x0002CC8E File Offset: 0x0002AE8E
		// (set) Token: 0x06000A4D RID: 2637 RVA: 0x0002CC96 File Offset: 0x0002AE96
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

		// Token: 0x17000321 RID: 801
		// (get) Token: 0x06000A4E RID: 2638 RVA: 0x0002CCB9 File Offset: 0x0002AEB9
		// (set) Token: 0x06000A4F RID: 2639 RVA: 0x0002CCC1 File Offset: 0x0002AEC1
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

		// Token: 0x17000322 RID: 802
		// (get) Token: 0x06000A50 RID: 2640 RVA: 0x0002CCE4 File Offset: 0x0002AEE4
		// (set) Token: 0x06000A51 RID: 2641 RVA: 0x0002CCEC File Offset: 0x0002AEEC
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

		// Token: 0x17000323 RID: 803
		// (get) Token: 0x06000A52 RID: 2642 RVA: 0x0002CD0F File Offset: 0x0002AF0F
		// (set) Token: 0x06000A53 RID: 2643 RVA: 0x0002CD17 File Offset: 0x0002AF17
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

		// Token: 0x17000324 RID: 804
		// (get) Token: 0x06000A54 RID: 2644 RVA: 0x0002CD3A File Offset: 0x0002AF3A
		// (set) Token: 0x06000A55 RID: 2645 RVA: 0x0002CD42 File Offset: 0x0002AF42
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

		// Token: 0x17000325 RID: 805
		// (get) Token: 0x06000A56 RID: 2646 RVA: 0x0002CD65 File Offset: 0x0002AF65
		// (set) Token: 0x06000A57 RID: 2647 RVA: 0x0002CD6D File Offset: 0x0002AF6D
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

		// Token: 0x17000326 RID: 806
		// (get) Token: 0x06000A58 RID: 2648 RVA: 0x0002CD90 File Offset: 0x0002AF90
		// (set) Token: 0x06000A59 RID: 2649 RVA: 0x0002CD98 File Offset: 0x0002AF98
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

		// Token: 0x17000327 RID: 807
		// (get) Token: 0x06000A5A RID: 2650 RVA: 0x0002CDBB File Offset: 0x0002AFBB
		// (set) Token: 0x06000A5B RID: 2651 RVA: 0x0002CDC3 File Offset: 0x0002AFC3
		[DataSourceProperty]
		public BasicTooltipViewModel ProsperityHint
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
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "ProsperityHint");
				}
			}
		}

		// Token: 0x17000328 RID: 808
		// (get) Token: 0x06000A5C RID: 2652 RVA: 0x0002CDE1 File Offset: 0x0002AFE1
		// (set) Token: 0x06000A5D RID: 2653 RVA: 0x0002CDE9 File Offset: 0x0002AFE9
		[DataSourceProperty]
		public BasicTooltipViewModel StrengthHint
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
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "StrengthHint");
				}
			}
		}

		// Token: 0x04000489 RID: 1161
		private ExpelClanFromKingdomDecision _expelDecision;

		// Token: 0x0400048A RID: 1162
		private MBBindingList<HeroVM> _members;

		// Token: 0x0400048B RID: 1163
		private MBBindingList<EncyclopediaSettlementVM> _fiefs;

		// Token: 0x0400048C RID: 1164
		private HeroVM _leader;

		// Token: 0x0400048D RID: 1165
		private string _nameText;

		// Token: 0x0400048E RID: 1166
		private string _membersText;

		// Token: 0x0400048F RID: 1167
		private string _settlementsText;

		// Token: 0x04000490 RID: 1168
		private string _leaderText;

		// Token: 0x04000491 RID: 1169
		private string _informationText;

		// Token: 0x04000492 RID: 1170
		private string _prosperityText;

		// Token: 0x04000493 RID: 1171
		private string _strengthText;

		// Token: 0x04000494 RID: 1172
		private BasicTooltipViewModel _prosperityHint;

		// Token: 0x04000495 RID: 1173
		private BasicTooltipViewModel _strengthHint;
	}
}
