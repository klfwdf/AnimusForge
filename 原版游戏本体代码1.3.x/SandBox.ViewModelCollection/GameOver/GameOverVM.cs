using System;
using SandBox.ViewModelCollection.Input;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.GameOver
{
	// Token: 0x0200005D RID: 93
	public class GameOverVM : ViewModel
	{
		// Token: 0x06000593 RID: 1427 RVA: 0x00014BD8 File Offset: 0x00012DD8
		public GameOverVM(GameOverState.GameOverReason reason, Action onClose)
		{
			this._onClose = onClose;
			this._reason = reason;
			this._statsProvider = new GameOverStatsProvider();
			this.Categories = new MBBindingList<GameOverStatCategoryVM>();
			this.IsPositiveGameOver = this._reason == GameOverState.GameOverReason.Victory;
			this.ClanBanner = new BannerImageIdentifierVM(Hero.MainHero.ClanBanner, true);
			this.ReasonAsString = Enum.GetName(typeof(GameOverState.GameOverReason), this._reason);
			this.RefreshValues();
		}

		// Token: 0x06000594 RID: 1428 RVA: 0x00014C5C File Offset: 0x00012E5C
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.CloseText = (this.IsPositiveGameOver ? new TextObject("{=AdgAJbAP}Return To The Map", null).ToString() : GameTexts.FindText("str_main_menu", null).ToString());
			this.TitleText = GameTexts.FindText("str_game_over_title", this.ReasonAsString).ToString();
			this.StatisticsTitle = GameTexts.FindText("str_statistics", null).ToString();
			this.Categories.Clear();
			foreach (StatCategory category in this._statsProvider.GetGameOverStats())
			{
				this.Categories.Add(new GameOverStatCategoryVM(category, new Action<GameOverStatCategoryVM>(this.OnCategorySelection)));
			}
			this.OnCategorySelection(this.Categories[0]);
		}

		// Token: 0x06000595 RID: 1429 RVA: 0x00014D48 File Offset: 0x00012F48
		private void OnCategorySelection(GameOverStatCategoryVM newCategory)
		{
			if (this._currentCategory != null)
			{
				this._currentCategory.IsSelected = false;
			}
			this._currentCategory = newCategory;
			if (this._currentCategory != null)
			{
				this._currentCategory.IsSelected = true;
			}
		}

		// Token: 0x06000596 RID: 1430 RVA: 0x00014D79 File Offset: 0x00012F79
		public void ExecuteClose()
		{
			Action onClose = this._onClose;
			if (onClose == null)
			{
				return;
			}
			onClose.DynamicInvokeWithLog(Array.Empty<object>());
		}

		// Token: 0x06000597 RID: 1431 RVA: 0x00014D91 File Offset: 0x00012F91
		public void SetCloseInputKey(HotKey hotKey)
		{
			this.CloseInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x06000598 RID: 1432 RVA: 0x00014DA0 File Offset: 0x00012FA0
		public override void OnFinalize()
		{
			base.OnFinalize();
			InputKeyItemVM closeInputKey = this.CloseInputKey;
			if (closeInputKey == null)
			{
				return;
			}
			closeInputKey.OnFinalize();
		}

		// Token: 0x170001A6 RID: 422
		// (get) Token: 0x06000599 RID: 1433 RVA: 0x00014DB8 File Offset: 0x00012FB8
		// (set) Token: 0x0600059A RID: 1434 RVA: 0x00014DC0 File Offset: 0x00012FC0
		[DataSourceProperty]
		public string CloseText
		{
			get
			{
				return this._closeText;
			}
			set
			{
				if (value != this._closeText)
				{
					this._closeText = value;
					base.OnPropertyChangedWithValue<string>(value, "CloseText");
				}
			}
		}

		// Token: 0x170001A7 RID: 423
		// (get) Token: 0x0600059B RID: 1435 RVA: 0x00014DE3 File Offset: 0x00012FE3
		// (set) Token: 0x0600059C RID: 1436 RVA: 0x00014DEB File Offset: 0x00012FEB
		[DataSourceProperty]
		public string StatisticsTitle
		{
			get
			{
				return this._statisticsTitle;
			}
			set
			{
				if (value != this._statisticsTitle)
				{
					this._statisticsTitle = value;
					base.OnPropertyChangedWithValue<string>(value, "StatisticsTitle");
				}
			}
		}

		// Token: 0x170001A8 RID: 424
		// (get) Token: 0x0600059D RID: 1437 RVA: 0x00014E0E File Offset: 0x0001300E
		// (set) Token: 0x0600059E RID: 1438 RVA: 0x00014E16 File Offset: 0x00013016
		[DataSourceProperty]
		public string ReasonAsString
		{
			get
			{
				return this._reasonAsString;
			}
			set
			{
				if (value != this._reasonAsString)
				{
					this._reasonAsString = value;
					base.OnPropertyChangedWithValue<string>(value, "ReasonAsString");
				}
			}
		}

		// Token: 0x170001A9 RID: 425
		// (get) Token: 0x0600059F RID: 1439 RVA: 0x00014E39 File Offset: 0x00013039
		// (set) Token: 0x060005A0 RID: 1440 RVA: 0x00014E41 File Offset: 0x00013041
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

		// Token: 0x170001AA RID: 426
		// (get) Token: 0x060005A1 RID: 1441 RVA: 0x00014E64 File Offset: 0x00013064
		// (set) Token: 0x060005A2 RID: 1442 RVA: 0x00014E6C File Offset: 0x0001306C
		[DataSourceProperty]
		public BannerImageIdentifierVM ClanBanner
		{
			get
			{
				return this._clanBanner;
			}
			set
			{
				if (value != this._clanBanner)
				{
					this._clanBanner = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "ClanBanner");
				}
			}
		}

		// Token: 0x170001AB RID: 427
		// (get) Token: 0x060005A3 RID: 1443 RVA: 0x00014E8A File Offset: 0x0001308A
		// (set) Token: 0x060005A4 RID: 1444 RVA: 0x00014E92 File Offset: 0x00013092
		[DataSourceProperty]
		public bool IsPositiveGameOver
		{
			get
			{
				return this._isPositiveGameOver;
			}
			set
			{
				if (value != this._isPositiveGameOver)
				{
					this._isPositiveGameOver = value;
					base.OnPropertyChangedWithValue(value, "IsPositiveGameOver");
				}
			}
		}

		// Token: 0x170001AC RID: 428
		// (get) Token: 0x060005A5 RID: 1445 RVA: 0x00014EB0 File Offset: 0x000130B0
		// (set) Token: 0x060005A6 RID: 1446 RVA: 0x00014EB8 File Offset: 0x000130B8
		[DataSourceProperty]
		public InputKeyItemVM CloseInputKey
		{
			get
			{
				return this._closeInputKey;
			}
			set
			{
				if (value != this._closeInputKey)
				{
					this._closeInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "CloseInputKey");
				}
			}
		}

		// Token: 0x170001AD RID: 429
		// (get) Token: 0x060005A7 RID: 1447 RVA: 0x00014ED6 File Offset: 0x000130D6
		// (set) Token: 0x060005A8 RID: 1448 RVA: 0x00014EDE File Offset: 0x000130DE
		[DataSourceProperty]
		public MBBindingList<GameOverStatCategoryVM> Categories
		{
			get
			{
				return this._categories;
			}
			set
			{
				if (value != this._categories)
				{
					this._categories = value;
					base.OnPropertyChangedWithValue<MBBindingList<GameOverStatCategoryVM>>(value, "Categories");
				}
			}
		}

		// Token: 0x040002C1 RID: 705
		private readonly Action _onClose;

		// Token: 0x040002C2 RID: 706
		private readonly GameOverStatsProvider _statsProvider;

		// Token: 0x040002C3 RID: 707
		private readonly GameOverState.GameOverReason _reason;

		// Token: 0x040002C4 RID: 708
		private GameOverStatCategoryVM _currentCategory;

		// Token: 0x040002C5 RID: 709
		private string _closeText;

		// Token: 0x040002C6 RID: 710
		private string _titleText;

		// Token: 0x040002C7 RID: 711
		private string _reasonAsString;

		// Token: 0x040002C8 RID: 712
		private string _statisticsTitle;

		// Token: 0x040002C9 RID: 713
		private bool _isPositiveGameOver;

		// Token: 0x040002CA RID: 714
		private InputKeyItemVM _closeInputKey;

		// Token: 0x040002CB RID: 715
		private BannerImageIdentifierVM _clanBanner;

		// Token: 0x040002CC RID: 716
		private MBBindingList<GameOverStatCategoryVM> _categories;
	}
}
