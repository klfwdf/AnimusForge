using System;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu
{
	// Token: 0x0200009B RID: 155
	public class GameMenuItemProgressVM : ViewModel
	{
		// Token: 0x06000EFB RID: 3835 RVA: 0x0003E2F2 File Offset: 0x0003C4F2
		public void InitializeWith(MenuContext context, int virtualIndex)
		{
			this._context = context;
			this._virtualIndex = virtualIndex;
			this._gameMenuManager = Campaign.Current.GameMenuManager;
			this.RefreshValues();
		}

		// Token: 0x06000EFC RID: 3836 RVA: 0x0003E318 File Offset: 0x0003C518
		public override void RefreshValues()
		{
			base.RefreshValues();
			this._text1 = Campaign.Current.GameMenuManager.GetVirtualMenuOptionText(this._context, this._virtualIndex).ToString();
			this._text2 = Campaign.Current.GameMenuManager.GetVirtualMenuOptionText2(this._context, this._virtualIndex).ToString();
			this.Refresh();
		}

		// Token: 0x06000EFD RID: 3837 RVA: 0x0003E380 File Offset: 0x0003C580
		private void Refresh()
		{
			switch (this._gameMenuManager.GetVirtualMenuAndOptionType(this._context))
			{
			case GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption:
			{
				float virtualMenuTargetWaitHours = Campaign.Current.GameMenuManager.GetVirtualMenuTargetWaitHours(this._context);
				if (virtualMenuTargetWaitHours > 1f)
				{
					GameTexts.SetVariable("PLURAL_HOURS", 1);
				}
				else
				{
					GameTexts.SetVariable("PLURAL_HOURS", 0);
				}
				GameTexts.SetVariable("HOUR", MathF.Round(virtualMenuTargetWaitHours).ToString("0.0"));
				this.ProgressText = GameTexts.FindText("str_hours", null).ToString();
				goto IL_C3;
			}
			case GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption:
				this.ProgressText = "";
				goto IL_C3;
			}
			Debug.FailedAssert("Shouldn't create game menu progress for normal options", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\GameMenu\\GameMenuItemProgressVM.cs", "Refresh", 68);
			return;
			IL_C3:
			this.Text = (Campaign.Current.GameMenuManager.GetVirtualMenuIsWaitActive(this._context) ? this._text2 : this._text1);
			float virtualMenuProgress = Campaign.Current.GameMenuManager.GetVirtualMenuProgress(this._context);
			this.Progress = (float)MathF.Round(virtualMenuProgress * 100f);
		}

		// Token: 0x06000EFE RID: 3838 RVA: 0x0003E4A6 File Offset: 0x0003C6A6
		public void OnTick()
		{
			this.Refresh();
		}

		// Token: 0x170004D4 RID: 1236
		// (get) Token: 0x06000EFF RID: 3839 RVA: 0x0003E4AE File Offset: 0x0003C6AE
		// (set) Token: 0x06000F00 RID: 3840 RVA: 0x0003E4B6 File Offset: 0x0003C6B6
		[DataSourceProperty]
		public string Text
		{
			get
			{
				return this._text;
			}
			set
			{
				if (value != this._text)
				{
					this._text = value;
					base.OnPropertyChangedWithValue<string>(value, "Text");
				}
			}
		}

		// Token: 0x170004D5 RID: 1237
		// (get) Token: 0x06000F01 RID: 3841 RVA: 0x0003E4D9 File Offset: 0x0003C6D9
		// (set) Token: 0x06000F02 RID: 3842 RVA: 0x0003E4E1 File Offset: 0x0003C6E1
		[DataSourceProperty]
		public string ProgressText
		{
			get
			{
				return this._progressText;
			}
			set
			{
				if (value != this._progressText)
				{
					this._progressText = value;
					base.OnPropertyChangedWithValue<string>(value, "ProgressText");
				}
			}
		}

		// Token: 0x170004D6 RID: 1238
		// (get) Token: 0x06000F03 RID: 3843 RVA: 0x0003E504 File Offset: 0x0003C704
		// (set) Token: 0x06000F04 RID: 3844 RVA: 0x0003E50C File Offset: 0x0003C70C
		[DataSourceProperty]
		public float Progress
		{
			get
			{
				return this._progress;
			}
			set
			{
				if (value != this._progress)
				{
					this._progress = value;
					base.OnPropertyChangedWithValue(value, "Progress");
				}
			}
		}

		// Token: 0x040006C0 RID: 1728
		private MenuContext _context;

		// Token: 0x040006C1 RID: 1729
		private GameMenuManager _gameMenuManager;

		// Token: 0x040006C2 RID: 1730
		private int _virtualIndex;

		// Token: 0x040006C3 RID: 1731
		private string _text1 = "";

		// Token: 0x040006C4 RID: 1732
		private string _text2 = "";

		// Token: 0x040006C5 RID: 1733
		private string _text;

		// Token: 0x040006C6 RID: 1734
		private string _progressText;

		// Token: 0x040006C7 RID: 1735
		private float _progress;
	}
}
