using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection
{
	// Token: 0x0200000A RID: 10
	public class CampaignOptionsVM : ViewModel
	{
		// Token: 0x06000096 RID: 150 RVA: 0x000038A4 File Offset: 0x00001AA4
		public CampaignOptionsVM(Action onClose)
		{
			this._onClose = onClose;
			MBBindingList<CampaignOptionItemVM> mbbindingList = new MBBindingList<CampaignOptionItemVM>();
			List<ICampaignOptionData> gameplayCampaignOptions = CampaignOptionsManager.GetGameplayCampaignOptions();
			for (int i = 0; i < gameplayCampaignOptions.Count; i++)
			{
				mbbindingList.Add(new CampaignOptionItemVM(gameplayCampaignOptions[i]));
			}
			this.OptionsController = new CampaignOptionsControllerVM(mbbindingList);
			this.RefreshValues();
		}

		// Token: 0x06000097 RID: 151 RVA: 0x00003900 File Offset: 0x00001B00
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.TitleText = new TextObject("{=PXT6aA4J}Campaign Options", null).ToString();
			this.DoneText = GameTexts.FindText("str_done", null).ToString();
			this.ResetTutorialText = new TextObject("{=oUz16Nav}Reset Tutorial", null).ToString();
			this.OptionsController.RefreshValues();
		}

		// Token: 0x06000098 RID: 152 RVA: 0x00003960 File Offset: 0x00001B60
		public void ExecuteDone()
		{
			Action onClose = this._onClose;
			if (onClose == null)
			{
				return;
			}
			onClose();
		}

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x06000099 RID: 153 RVA: 0x00003972 File Offset: 0x00001B72
		// (set) Token: 0x0600009A RID: 154 RVA: 0x0000397A File Offset: 0x00001B7A
		[DataSourceProperty]
		public CampaignOptionsControllerVM OptionsController
		{
			get
			{
				return this._optionsController;
			}
			set
			{
				if (value != this._optionsController)
				{
					this._optionsController = value;
					base.OnPropertyChangedWithValue<CampaignOptionsControllerVM>(value, "OptionsController");
				}
			}
		}

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x0600009B RID: 155 RVA: 0x00003998 File Offset: 0x00001B98
		// (set) Token: 0x0600009C RID: 156 RVA: 0x000039A0 File Offset: 0x00001BA0
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

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x0600009D RID: 157 RVA: 0x000039C3 File Offset: 0x00001BC3
		// (set) Token: 0x0600009E RID: 158 RVA: 0x000039CB File Offset: 0x00001BCB
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

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x0600009F RID: 159 RVA: 0x000039EE File Offset: 0x00001BEE
		// (set) Token: 0x060000A0 RID: 160 RVA: 0x000039F6 File Offset: 0x00001BF6
		[DataSourceProperty]
		public string ResetTutorialText
		{
			get
			{
				return this._resetTutorialText;
			}
			set
			{
				if (value != this._resetTutorialText)
				{
					this._resetTutorialText = value;
					base.OnPropertyChangedWithValue<string>(value, "ResetTutorialText");
				}
			}
		}

		// Token: 0x0400004E RID: 78
		private readonly Action _onClose;

		// Token: 0x0400004F RID: 79
		private string _titleText;

		// Token: 0x04000050 RID: 80
		private string _doneText;

		// Token: 0x04000051 RID: 81
		private string _resetTutorialText;

		// Token: 0x04000052 RID: 82
		private CampaignOptionsControllerVM _optionsController;
	}
}
