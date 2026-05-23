using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TournamentLeaderboard
{
	// Token: 0x020000AF RID: 175
	public class TournamentLeaderboardVM : ViewModel
	{
		// Token: 0x060010D2 RID: 4306 RVA: 0x00043BBC File Offset: 0x00041DBC
		public TournamentLeaderboardVM()
		{
			this.Entries = new MBBindingList<TournamentLeaderboardEntryItemVM>();
			List<KeyValuePair<Hero, int>> leaderboard = Campaign.Current.TournamentManager.GetLeaderboard();
			for (int i = 0; i < leaderboard.Count; i++)
			{
				this.Entries.Add(new TournamentLeaderboardEntryItemVM(leaderboard[i].Key, leaderboard[i].Value, i + 1));
			}
			this.SortController = new TournamentLeaderboardSortControllerVM(ref this._entries);
			this.RefreshValues();
		}

		// Token: 0x060010D3 RID: 4307 RVA: 0x00043C44 File Offset: 0x00041E44
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.DoneText = GameTexts.FindText("str_done", null).ToString();
			this.Entries.ApplyActionOnAllItems(delegate(TournamentLeaderboardEntryItemVM x)
			{
				x.RefreshValues();
			});
			this.HeroText = GameTexts.FindText("str_hero", null).ToString();
			this.VictoriesText = GameTexts.FindText("str_leaderboard_victories", null).ToString();
			this.RankText = GameTexts.FindText("str_rank_sign", null).ToString();
			this.TitleText = GameTexts.FindText("str_leaderboard_title", null).ToString();
		}

		// Token: 0x060010D4 RID: 4308 RVA: 0x00043CEF File Offset: 0x00041EEF
		public override void OnFinalize()
		{
			base.OnFinalize();
			InputKeyItemVM doneInputKey = this.DoneInputKey;
			if (doneInputKey == null)
			{
				return;
			}
			doneInputKey.OnFinalize();
		}

		// Token: 0x060010D5 RID: 4309 RVA: 0x00043D07 File Offset: 0x00041F07
		public void ExecuteDone()
		{
			this.IsEnabled = false;
		}

		// Token: 0x060010D6 RID: 4310 RVA: 0x00043D10 File Offset: 0x00041F10
		public void SetDoneInputKey(HotKey hotKey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x1700057A RID: 1402
		// (get) Token: 0x060010D7 RID: 4311 RVA: 0x00043D1F File Offset: 0x00041F1F
		// (set) Token: 0x060010D8 RID: 4312 RVA: 0x00043D27 File Offset: 0x00041F27
		[DataSourceProperty]
		public InputKeyItemVM DoneInputKey
		{
			get
			{
				return this._doneInputKey;
			}
			set
			{
				if (value != this._doneInputKey)
				{
					this._doneInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "DoneInputKey");
				}
			}
		}

		// Token: 0x1700057B RID: 1403
		// (get) Token: 0x060010D9 RID: 4313 RVA: 0x00043D45 File Offset: 0x00041F45
		// (set) Token: 0x060010DA RID: 4314 RVA: 0x00043D4D File Offset: 0x00041F4D
		[DataSourceProperty]
		public bool IsEnabled
		{
			get
			{
				return this._isEnabled;
			}
			set
			{
				if (value != this._isEnabled)
				{
					this._isEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsEnabled");
				}
			}
		}

		// Token: 0x1700057C RID: 1404
		// (get) Token: 0x060010DB RID: 4315 RVA: 0x00043D6B File Offset: 0x00041F6B
		// (set) Token: 0x060010DC RID: 4316 RVA: 0x00043D73 File Offset: 0x00041F73
		[DataSourceProperty]
		public TournamentLeaderboardSortControllerVM SortController
		{
			get
			{
				return this._sortController;
			}
			set
			{
				if (value != this._sortController)
				{
					this._sortController = value;
					base.OnPropertyChangedWithValue<TournamentLeaderboardSortControllerVM>(value, "SortController");
				}
			}
		}

		// Token: 0x1700057D RID: 1405
		// (get) Token: 0x060010DD RID: 4317 RVA: 0x00043D91 File Offset: 0x00041F91
		// (set) Token: 0x060010DE RID: 4318 RVA: 0x00043D99 File Offset: 0x00041F99
		[DataSourceProperty]
		public MBBindingList<TournamentLeaderboardEntryItemVM> Entries
		{
			get
			{
				return this._entries;
			}
			set
			{
				if (value != this._entries)
				{
					this._entries = value;
					base.OnPropertyChangedWithValue<MBBindingList<TournamentLeaderboardEntryItemVM>>(value, "Entries");
				}
			}
		}

		// Token: 0x1700057E RID: 1406
		// (get) Token: 0x060010DF RID: 4319 RVA: 0x00043DB7 File Offset: 0x00041FB7
		// (set) Token: 0x060010E0 RID: 4320 RVA: 0x00043DBF File Offset: 0x00041FBF
		[DataSourceProperty]
		public string DoneText
		{
			get
			{
				return this._doneText;
			}
			set
			{
				if (value != this._doneText)
				{
					this._doneText = value;
					base.OnPropertyChangedWithValue<string>(value, "DoneText");
				}
			}
		}

		// Token: 0x1700057F RID: 1407
		// (get) Token: 0x060010E1 RID: 4321 RVA: 0x00043DE2 File Offset: 0x00041FE2
		// (set) Token: 0x060010E2 RID: 4322 RVA: 0x00043DEA File Offset: 0x00041FEA
		[DataSourceProperty]
		public string TitleText
		{
			get
			{
				return this._titleText;
			}
			set
			{
				if (value != this._titleText)
				{
					this._titleText = value;
					base.OnPropertyChangedWithValue<string>(value, "TitleText");
				}
			}
		}

		// Token: 0x17000580 RID: 1408
		// (get) Token: 0x060010E3 RID: 4323 RVA: 0x00043E0D File Offset: 0x0004200D
		// (set) Token: 0x060010E4 RID: 4324 RVA: 0x00043E15 File Offset: 0x00042015
		[DataSourceProperty]
		public string HeroText
		{
			get
			{
				return this._heroText;
			}
			set
			{
				if (value != this._heroText)
				{
					this._heroText = value;
					base.OnPropertyChangedWithValue<string>(value, "HeroText");
				}
			}
		}

		// Token: 0x17000581 RID: 1409
		// (get) Token: 0x060010E5 RID: 4325 RVA: 0x00043E38 File Offset: 0x00042038
		// (set) Token: 0x060010E6 RID: 4326 RVA: 0x00043E40 File Offset: 0x00042040
		[DataSourceProperty]
		public string VictoriesText
		{
			get
			{
				return this._victoriesText;
			}
			set
			{
				if (value != this._victoriesText)
				{
					this._victoriesText = value;
					base.OnPropertyChangedWithValue<string>(value, "VictoriesText");
				}
			}
		}

		// Token: 0x17000582 RID: 1410
		// (get) Token: 0x060010E7 RID: 4327 RVA: 0x00043E63 File Offset: 0x00042063
		// (set) Token: 0x060010E8 RID: 4328 RVA: 0x00043E6B File Offset: 0x0004206B
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

		// Token: 0x040007AF RID: 1967
		private InputKeyItemVM _doneInputKey;

		// Token: 0x040007B0 RID: 1968
		private bool _isEnabled;

		// Token: 0x040007B1 RID: 1969
		private string _doneText;

		// Token: 0x040007B2 RID: 1970
		private string _heroText;

		// Token: 0x040007B3 RID: 1971
		private string _victoriesText;

		// Token: 0x040007B4 RID: 1972
		private string _rankText;

		// Token: 0x040007B5 RID: 1973
		private string _titleText;

		// Token: 0x040007B6 RID: 1974
		private MBBindingList<TournamentLeaderboardEntryItemVM> _entries;

		// Token: 0x040007B7 RID: 1975
		private TournamentLeaderboardSortControllerVM _sortController;
	}
}
