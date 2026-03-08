using System;
using Helpers;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy
{
	// Token: 0x02000072 RID: 114
	public class KingdomWarItemVM : KingdomDiplomacyItemVM
	{
		// Token: 0x0600094F RID: 2383 RVA: 0x00029430 File Offset: 0x00027630
		public KingdomWarItemVM(StanceLink war, Action<KingdomWarItemVM> onSelect)
			: base(war.Faction1, war.Faction2)
		{
			this._war = war;
			this._onSelect = onSelect;
			this.IsBehaviorSelectionEnabled = this.Faction1.IsKingdomFaction && this.Faction1.Leader == Hero.MainHero;
			StanceLink stanceWith = this.Faction1.GetStanceWith(this.Faction2);
			this._warProgressOfFaction1 = Campaign.Current.Models.DiplomacyModel.GetWarProgressScore(this.Faction1, this.Faction2, true);
			this._warProgressOfFaction2 = Campaign.Current.Models.DiplomacyModel.GetWarProgressScore(this.Faction2, this.Faction1, true);
			this._numberOfTownsCapturedByFaction1 = stanceWith.GetSuccessfulTownSieges(this.Faction1);
			this._numberOfTownsCapturedByFaction2 = stanceWith.GetSuccessfulTownSieges(this.Faction2);
			this._numberOfCastlesCapturedByFaction1 = stanceWith.GetSuccessfulSieges(this.Faction1) - this._numberOfTownsCapturedByFaction1;
			this._numberOfCastlesCapturedByFaction2 = stanceWith.GetSuccessfulSieges(this.Faction2) - this._numberOfTownsCapturedByFaction2;
			this._numberOfRaidsMadeByFaction1 = stanceWith.GetSuccessfulRaids(this.Faction1);
			this._numberOfRaidsMadeByFaction2 = stanceWith.GetSuccessfulRaids(this.Faction2);
			this.RefreshValues();
			this.WarLog = new MBBindingList<KingdomWarLogItemVM>();
			foreach (ValueTuple<LogEntry, IFaction, IFaction> valueTuple in DiplomacyHelper.GetLogsForWar(war))
			{
				LogEntry item = valueTuple.Item1;
				IFaction item2 = valueTuple.Item2;
				IEncyclopediaLog log;
				if ((log = item as IEncyclopediaLog) != null)
				{
					this.WarLog.Add(new KingdomWarLogItemVM(log, item2));
				}
			}
		}

		// Token: 0x06000950 RID: 2384 RVA: 0x000295DC File Offset: 0x000277DC
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.UpdateDiplomacyProperties();
		}

		// Token: 0x06000951 RID: 2385 RVA: 0x000295EA File Offset: 0x000277EA
		protected override void OnSelect()
		{
			this.UpdateDiplomacyProperties();
			this._onSelect(this);
			base.IsSelected = true;
		}

		// Token: 0x06000952 RID: 2386 RVA: 0x00029608 File Offset: 0x00027808
		protected override void UpdateDiplomacyProperties()
		{
			base.UpdateDiplomacyProperties();
			GameTexts.SetVariable("FACTION_1_NAME", this.Faction1.Name.ToString());
			GameTexts.SetVariable("FACTION_2_NAME", this.Faction2.Name.ToString());
			this.WarName = GameTexts.FindText("str_war_faction_versus_faction", null).ToString();
			StanceLink stanceWith = this.Faction1.GetStanceWith(this.Faction2);
			this.Score = stanceWith.GetSuccessfulSieges(this.Faction1) + stanceWith.GetSuccessfulRaids(this.Faction1);
			this.CasualtiesOfFaction1 = stanceWith.GetCasualties(this.Faction1);
			this.CasualtiesOfFaction2 = stanceWith.GetCasualties(this.Faction2);
			int num = MathF.Ceiling(this._war.WarStartDate.ElapsedDaysUntilNow + 0.01f);
			TextObject textObject = GameTexts.FindText("str_for_DAY_days", null);
			textObject.SetTextVariable("DAY", num.ToString());
			textObject.SetTextVariable("DAY_IS_PLURAL", (num > 1) ? 1 : 0);
			this.NumberOfDaysSinceWarBegan = textObject.ToString();
			base.Stats.Add(new KingdomWarComparableStatVM((int)this.Faction1.CurrentTotalStrength, (int)this.Faction2.CurrentTotalStrength, GameTexts.FindText("str_total_strength", null), this._faction1Color, this._faction2Color, 10000, null, null));
			base.Stats.Add(new KingdomWarComparableStatVM(stanceWith.GetCasualties(this.Faction2), stanceWith.GetCasualties(this.Faction1), GameTexts.FindText("str_war_casualties_inflicted", null), this._faction1Color, this._faction2Color, 10000, null, null));
			base.Stats.Add(new KingdomWarComparableStatVM(this._numberOfTownsCapturedByFaction1, this._numberOfTownsCapturedByFaction2, GameTexts.FindText("str_war_captured_towns", null), this._faction1Color, this._faction2Color, 25, null, null));
			base.Stats.Add(new KingdomWarComparableStatVM(this._numberOfCastlesCapturedByFaction1, this._numberOfCastlesCapturedByFaction2, GameTexts.FindText("str_war_captured_castles", null), this._faction1Color, this._faction2Color, 25, null, null));
			base.Stats.Add(new KingdomWarComparableStatVM(this._numberOfRaidsMadeByFaction1, this._numberOfRaidsMadeByFaction2, GameTexts.FindText("str_war_successful_raids", null), this._faction1Color, this._faction2Color, 10, null, null));
			int num2 = (int)(this._warProgressOfFaction1.ResultNumber * 100f / this._warProgressOfFaction1.LimitMaxValue);
			int num3 = (int)(this._warProgressOfFaction2.ResultNumber * 100f / this._warProgressOfFaction2.LimitMaxValue);
			int faction1Stat = MathF.Max(0, num2 - num3);
			int faction2Stat = MathF.Max(0, num3 - num2);
			base.Stats.Add(new KingdomWarComparableStatVM(faction1Stat, faction2Stat, new TextObject("{=8qbkS5D2}War Progress", null), this._faction1Color, this._faction2Color, 100, new BasicTooltipViewModel(() => CampaignUIHelper.GetNormalizedWarProgressTooltip(this._warProgressOfFaction1, this._warProgressOfFaction2, this._warProgressOfFaction1.LimitMaxValue, this.Faction1.Name, this.Faction2.Name)), new BasicTooltipViewModel(() => CampaignUIHelper.GetNormalizedWarProgressTooltip(this._warProgressOfFaction2, this._warProgressOfFaction1, this._warProgressOfFaction2.LimitMaxValue, this.Faction2.Name, this.Faction1.Name))));
		}

		// Token: 0x170002BF RID: 703
		// (get) Token: 0x06000953 RID: 2387 RVA: 0x000298F0 File Offset: 0x00027AF0
		// (set) Token: 0x06000954 RID: 2388 RVA: 0x000298F8 File Offset: 0x00027AF8
		[DataSourceProperty]
		public string WarName
		{
			get
			{
				return this._warName;
			}
			set
			{
				if (value != this._warName)
				{
					this._warName = value;
					base.OnPropertyChangedWithValue<string>(value, "WarName");
				}
			}
		}

		// Token: 0x170002C0 RID: 704
		// (get) Token: 0x06000955 RID: 2389 RVA: 0x0002991B File Offset: 0x00027B1B
		// (set) Token: 0x06000956 RID: 2390 RVA: 0x00029923 File Offset: 0x00027B23
		[DataSourceProperty]
		public string NumberOfDaysSinceWarBegan
		{
			get
			{
				return this._numberOfDaysSinceWarBegan;
			}
			set
			{
				if (value != this._numberOfDaysSinceWarBegan)
				{
					this._numberOfDaysSinceWarBegan = value;
					base.OnPropertyChangedWithValue<string>(value, "NumberOfDaysSinceWarBegan");
				}
			}
		}

		// Token: 0x170002C1 RID: 705
		// (get) Token: 0x06000957 RID: 2391 RVA: 0x00029946 File Offset: 0x00027B46
		// (set) Token: 0x06000958 RID: 2392 RVA: 0x0002994E File Offset: 0x00027B4E
		[DataSourceProperty]
		public bool IsBehaviorSelectionEnabled
		{
			get
			{
				return this._isBehaviorSelectionEnabled;
			}
			set
			{
				if (value != this._isBehaviorSelectionEnabled)
				{
					this._isBehaviorSelectionEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsBehaviorSelectionEnabled");
				}
			}
		}

		// Token: 0x170002C2 RID: 706
		// (get) Token: 0x06000959 RID: 2393 RVA: 0x0002996C File Offset: 0x00027B6C
		// (set) Token: 0x0600095A RID: 2394 RVA: 0x00029974 File Offset: 0x00027B74
		[DataSourceProperty]
		public int Score
		{
			get
			{
				return this._score;
			}
			set
			{
				if (value != this._score)
				{
					this._score = value;
					base.OnPropertyChangedWithValue(value, "Score");
				}
			}
		}

		// Token: 0x170002C3 RID: 707
		// (get) Token: 0x0600095B RID: 2395 RVA: 0x00029992 File Offset: 0x00027B92
		// (set) Token: 0x0600095C RID: 2396 RVA: 0x0002999A File Offset: 0x00027B9A
		[DataSourceProperty]
		public int CasualtiesOfFaction1
		{
			get
			{
				return this._casualtiesOfFaction1;
			}
			set
			{
				if (value != this._casualtiesOfFaction1)
				{
					this._casualtiesOfFaction1 = value;
					base.OnPropertyChangedWithValue(value, "CasualtiesOfFaction1");
				}
			}
		}

		// Token: 0x170002C4 RID: 708
		// (get) Token: 0x0600095D RID: 2397 RVA: 0x000299B8 File Offset: 0x00027BB8
		// (set) Token: 0x0600095E RID: 2398 RVA: 0x000299C0 File Offset: 0x00027BC0
		[DataSourceProperty]
		public int CasualtiesOfFaction2
		{
			get
			{
				return this._casualtiesOfFaction2;
			}
			set
			{
				if (value != this._casualtiesOfFaction2)
				{
					this._casualtiesOfFaction2 = value;
					base.OnPropertyChangedWithValue(value, "CasualtiesOfFaction2");
				}
			}
		}

		// Token: 0x170002C5 RID: 709
		// (get) Token: 0x0600095F RID: 2399 RVA: 0x000299DE File Offset: 0x00027BDE
		// (set) Token: 0x06000960 RID: 2400 RVA: 0x000299E6 File Offset: 0x00027BE6
		[DataSourceProperty]
		public MBBindingList<KingdomWarLogItemVM> WarLog
		{
			get
			{
				return this._warLog;
			}
			set
			{
				if (value != this._warLog)
				{
					this._warLog = value;
					base.OnPropertyChangedWithValue<MBBindingList<KingdomWarLogItemVM>>(value, "WarLog");
				}
			}
		}

		// Token: 0x04000411 RID: 1041
		private readonly Action<KingdomWarItemVM> _onSelect;

		// Token: 0x04000412 RID: 1042
		private readonly StanceLink _war;

		// Token: 0x04000413 RID: 1043
		private ExplainedNumber _warProgressOfFaction1;

		// Token: 0x04000414 RID: 1044
		private ExplainedNumber _warProgressOfFaction2;

		// Token: 0x04000415 RID: 1045
		private int _numberOfTownsCapturedByFaction1;

		// Token: 0x04000416 RID: 1046
		private int _numberOfTownsCapturedByFaction2;

		// Token: 0x04000417 RID: 1047
		private int _numberOfCastlesCapturedByFaction1;

		// Token: 0x04000418 RID: 1048
		private int _numberOfCastlesCapturedByFaction2;

		// Token: 0x04000419 RID: 1049
		private int _numberOfRaidsMadeByFaction1;

		// Token: 0x0400041A RID: 1050
		private int _numberOfRaidsMadeByFaction2;

		// Token: 0x0400041B RID: 1051
		private string _warName;

		// Token: 0x0400041C RID: 1052
		private string _numberOfDaysSinceWarBegan;

		// Token: 0x0400041D RID: 1053
		private int _score;

		// Token: 0x0400041E RID: 1054
		private bool _isBehaviorSelectionEnabled;

		// Token: 0x0400041F RID: 1055
		private int _casualtiesOfFaction1;

		// Token: 0x04000420 RID: 1056
		private int _casualtiesOfFaction2;

		// Token: 0x04000421 RID: 1057
		private MBBindingList<KingdomWarLogItemVM> _warLog;
	}
}
