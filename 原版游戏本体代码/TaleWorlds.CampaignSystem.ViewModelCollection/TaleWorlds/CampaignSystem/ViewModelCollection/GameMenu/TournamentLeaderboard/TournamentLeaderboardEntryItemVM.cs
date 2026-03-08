using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TournamentLeaderboard
{
	// Token: 0x020000AD RID: 173
	public class TournamentLeaderboardEntryItemVM : ViewModel
	{
		// Token: 0x17000567 RID: 1383
		// (get) Token: 0x060010A4 RID: 4260 RVA: 0x000435D0 File Offset: 0x000417D0
		// (set) Token: 0x060010A5 RID: 4261 RVA: 0x000435D8 File Offset: 0x000417D8
		public int Rank { get; private set; }

		// Token: 0x17000568 RID: 1384
		// (get) Token: 0x060010A6 RID: 4262 RVA: 0x000435E1 File Offset: 0x000417E1
		// (set) Token: 0x060010A7 RID: 4263 RVA: 0x000435E9 File Offset: 0x000417E9
		public float PrizeValue { get; private set; }

		// Token: 0x060010A8 RID: 4264 RVA: 0x000435F4 File Offset: 0x000417F4
		public TournamentLeaderboardEntryItemVM(Hero hero, int victories, int placement)
		{
			this._heroObj = hero;
			this.PrizeStr = "-";
			this.Rank = placement;
			this.PlacementOnLeaderboard = placement;
			this.IsChampion = placement == 1;
			this.Victories = victories;
			float prizeValue;
			if (float.TryParse(this.PrizeStr, out prizeValue))
			{
				this.PrizeValue = prizeValue;
			}
			this.IsMainHero = hero == TaleWorlds.CampaignSystem.Hero.MainHero;
			this.Hero = new HeroVM(hero, false);
			this.ChampionRewardsHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTournamentChampionRewardsTooltip(hero, null));
			this.RefreshValues();
		}

		// Token: 0x060010A9 RID: 4265 RVA: 0x000436A4 File Offset: 0x000418A4
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = this._heroObj.Name.ToString();
			GameTexts.SetVariable("RANK", this.Rank);
			this.RankText = GameTexts.FindText("str_leaderboard_rank", null).ToString();
			HeroVM hero = this.Hero;
			if (hero == null)
			{
				return;
			}
			hero.RefreshValues();
		}

		// Token: 0x17000569 RID: 1385
		// (get) Token: 0x060010AA RID: 4266 RVA: 0x00043703 File Offset: 0x00041903
		// (set) Token: 0x060010AB RID: 4267 RVA: 0x0004370B File Offset: 0x0004190B
		[DataSourceProperty]
		public BasicTooltipViewModel ChampionRewardsHint
		{
			get
			{
				return this._championRewardsHint;
			}
			set
			{
				if (value != this._championRewardsHint)
				{
					this._championRewardsHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "ChampionRewardsHint");
				}
			}
		}

		// Token: 0x1700056A RID: 1386
		// (get) Token: 0x060010AC RID: 4268 RVA: 0x00043729 File Offset: 0x00041929
		// (set) Token: 0x060010AD RID: 4269 RVA: 0x00043731 File Offset: 0x00041931
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

		// Token: 0x1700056B RID: 1387
		// (get) Token: 0x060010AE RID: 4270 RVA: 0x00043754 File Offset: 0x00041954
		// (set) Token: 0x060010AF RID: 4271 RVA: 0x0004375C File Offset: 0x0004195C
		[DataSourceProperty]
		public string RankText
		{
			get
			{
				return this._rankText;
			}
			set
			{
				if (value != this._rankText)
				{
					this._rankText = value;
					base.OnPropertyChangedWithValue<string>(value, "RankText");
				}
			}
		}

		// Token: 0x1700056C RID: 1388
		// (get) Token: 0x060010B0 RID: 4272 RVA: 0x0004377F File Offset: 0x0004197F
		// (set) Token: 0x060010B1 RID: 4273 RVA: 0x00043787 File Offset: 0x00041987
		[DataSourceProperty]
		public int Victories
		{
			get
			{
				return this._victories;
			}
			set
			{
				if (value != this._victories)
				{
					this._victories = value;
					base.OnPropertyChangedWithValue(value, "Victories");
				}
			}
		}

		// Token: 0x1700056D RID: 1389
		// (get) Token: 0x060010B2 RID: 4274 RVA: 0x000437A5 File Offset: 0x000419A5
		// (set) Token: 0x060010B3 RID: 4275 RVA: 0x000437AD File Offset: 0x000419AD
		[DataSourceProperty]
		public bool IsChampion
		{
			get
			{
				return this._isChampion;
			}
			set
			{
				if (value != this._isChampion)
				{
					this._isChampion = value;
					base.OnPropertyChangedWithValue(value, "IsChampion");
				}
			}
		}

		// Token: 0x1700056E RID: 1390
		// (get) Token: 0x060010B4 RID: 4276 RVA: 0x000437CB File Offset: 0x000419CB
		// (set) Token: 0x060010B5 RID: 4277 RVA: 0x000437D3 File Offset: 0x000419D3
		[DataSourceProperty]
		public bool IsMainHero
		{
			get
			{
				return this._isMainHero;
			}
			set
			{
				if (value != this._isMainHero)
				{
					this._isMainHero = value;
					base.OnPropertyChangedWithValue(value, "IsMainHero");
				}
			}
		}

		// Token: 0x1700056F RID: 1391
		// (get) Token: 0x060010B6 RID: 4278 RVA: 0x000437F1 File Offset: 0x000419F1
		// (set) Token: 0x060010B7 RID: 4279 RVA: 0x000437F9 File Offset: 0x000419F9
		[DataSourceProperty]
		public HeroVM Hero
		{
			get
			{
				return this._hero;
			}
			set
			{
				if (value != this._hero)
				{
					this._hero = value;
					base.OnPropertyChangedWithValue<HeroVM>(value, "Hero");
				}
			}
		}

		// Token: 0x17000570 RID: 1392
		// (get) Token: 0x060010B8 RID: 4280 RVA: 0x00043817 File Offset: 0x00041A17
		// (set) Token: 0x060010B9 RID: 4281 RVA: 0x0004381F File Offset: 0x00041A1F
		[DataSourceProperty]
		public string PrizeStr
		{
			get
			{
				return this._prizeStr;
			}
			set
			{
				if (value != this._prizeStr)
				{
					this._prizeStr = value;
					base.OnPropertyChangedWithValue<string>(value, "PrizeStr");
				}
			}
		}

		// Token: 0x17000571 RID: 1393
		// (get) Token: 0x060010BA RID: 4282 RVA: 0x00043842 File Offset: 0x00041A42
		// (set) Token: 0x060010BB RID: 4283 RVA: 0x0004384A File Offset: 0x00041A4A
		[DataSourceProperty]
		public int PlacementOnLeaderboard
		{
			get
			{
				return this._placementOnLeaderboard;
			}
			set
			{
				if (value != this._placementOnLeaderboard)
				{
					this._placementOnLeaderboard = value;
					base.OnPropertyChangedWithValue(value, "PlacementOnLeaderboard");
				}
			}
		}

		// Token: 0x04000798 RID: 1944
		private readonly Hero _heroObj;

		// Token: 0x04000799 RID: 1945
		private int _placementOnLeaderboard;

		// Token: 0x0400079A RID: 1946
		private int _victories;

		// Token: 0x0400079B RID: 1947
		private bool _isMainHero;

		// Token: 0x0400079C RID: 1948
		private bool _isChampion;

		// Token: 0x0400079D RID: 1949
		private string _name;

		// Token: 0x0400079E RID: 1950
		private string _rankText;

		// Token: 0x0400079F RID: 1951
		private string _prizeStr;

		// Token: 0x040007A0 RID: 1952
		private HeroVM _hero;

		// Token: 0x040007A1 RID: 1953
		private BasicTooltipViewModel _championRewardsHint;
	}
}
