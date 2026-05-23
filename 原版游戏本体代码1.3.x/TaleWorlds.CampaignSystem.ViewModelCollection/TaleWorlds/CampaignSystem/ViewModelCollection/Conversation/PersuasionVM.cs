using System;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Conversation
{
	// Token: 0x0200011A RID: 282
	public class PersuasionVM : ViewModel
	{
		// Token: 0x060019EE RID: 6638 RVA: 0x0006207F File Offset: 0x0006027F
		public PersuasionVM(ConversationManager manager)
		{
			this.PersuasionProgress = new MBBindingList<BoolItemWithActionVM>();
			this._manager = manager;
		}

		// Token: 0x060019EF RID: 6639 RVA: 0x0006209C File Offset: 0x0006029C
		public void OnPersuasionProgress(Tuple<PersuasionOptionArgs, PersuasionOptionResult> selectedOption)
		{
			this.ProgressText = "";
			string newValue = null;
			string text = null;
			switch (selectedOption.Item2)
			{
			case PersuasionOptionResult.CriticalFailure:
				newValue = new TextObject("{=ocSW4WA2}Critical Fail!", null).ToString();
				text = "<a style=\"Conversation.Persuasion.Negative\"><b>{TEXT}</b></a>";
				break;
			case PersuasionOptionResult.Failure:
			case PersuasionOptionResult.Miss:
				newValue = new TextObject("{=JYOcl7Ox}Ineffective!", null).ToString();
				text = "<a style=\"Conversation.Persuasion.Neutral\"><b>{TEXT}</b></a>";
				break;
			case PersuasionOptionResult.Success:
				newValue = new TextObject("{=3F0y3ugx}Success!", null).ToString();
				text = "<a style=\"Conversation.Persuasion.Positive\"><b>{TEXT}</b></a>";
				break;
			case PersuasionOptionResult.CriticalSuccess:
				newValue = new TextObject("{=4U9EnZt5}Critical Success!", null).ToString();
				text = "<a style=\"Conversation.Persuasion.Positive\"><b>{TEXT}</b></a>";
				break;
			}
			this.ProgressText = text.Replace("{TEXT}", newValue);
		}

		// Token: 0x060019F0 RID: 6640 RVA: 0x0006214F File Offset: 0x0006034F
		public override void RefreshValues()
		{
			base.RefreshValues();
			PersuasionOptionVM currentPersuasionOption = this.CurrentPersuasionOption;
			if (currentPersuasionOption == null)
			{
				return;
			}
			currentPersuasionOption.RefreshValues();
		}

		// Token: 0x060019F1 RID: 6641 RVA: 0x00062167 File Offset: 0x00060367
		public void SetCurrentOption(PersuasionOptionVM option)
		{
			if (this.CurrentPersuasionOption != option)
			{
				this.CurrentPersuasionOption = option;
			}
		}

		// Token: 0x060019F2 RID: 6642 RVA: 0x0006217C File Offset: 0x0006037C
		public void RefreshPersusasion()
		{
			this.CurrentCritFailChance = 0;
			this.CurrentFailChance = 0;
			this.CurrentCritSuccessChance = 0;
			this.CurrentSuccessChance = 0;
			this.IsPersuasionActive = ConversationManager.GetPersuasionIsActive();
			this.PersuasionProgress.Clear();
			this.PersuasionHint = new BasicTooltipViewModel();
			if (this.IsPersuasionActive)
			{
				int num = (int)ConversationManager.GetPersuasionProgress();
				int num2 = (int)ConversationManager.GetPersuasionGoalValue();
				for (int i = 1; i <= num2; i++)
				{
					bool isActive = i <= num;
					this.PersuasionProgress.Add(new BoolItemWithActionVM(null, isActive, null));
				}
				if (this.CurrentPersuasionOption != null)
				{
					this.CurrentCritFailChance = this._currentPersuasionOption.CritFailChance;
					this.CurrentFailChance = this._currentPersuasionOption.FailChance;
					this.CurrentCritSuccessChance = this._currentPersuasionOption.CritSuccessChance;
					this.CurrentSuccessChance = this._currentPersuasionOption.SuccessChance;
				}
				this.PersuasionHint = new BasicTooltipViewModel(() => this.GetPersuasionTooltip());
			}
		}

		// Token: 0x060019F3 RID: 6643 RVA: 0x00062269 File Offset: 0x00060469
		private string GetPersuasionTooltip()
		{
			if (ConversationManager.GetPersuasionIsActive())
			{
				GameTexts.SetVariable("CURRENT_PROGRESS", (int)ConversationManager.GetPersuasionProgress());
				GameTexts.SetVariable("TARGET_PROGRESS", (int)ConversationManager.GetPersuasionGoalValue());
				return GameTexts.FindText("str_persuasion_tooltip", null).ToString();
			}
			return "";
		}

		// Token: 0x060019F4 RID: 6644 RVA: 0x000622A8 File Offset: 0x000604A8
		private void RefreshChangeValues()
		{
			float num;
			float num2;
			float num3;
			this._manager.GetPersuasionChanceValues(out num, out num2, out num3);
		}

		// Token: 0x170008B7 RID: 2231
		// (get) Token: 0x060019F5 RID: 6645 RVA: 0x000622C6 File Offset: 0x000604C6
		// (set) Token: 0x060019F6 RID: 6646 RVA: 0x000622CE File Offset: 0x000604CE
		[DataSourceProperty]
		public BasicTooltipViewModel PersuasionHint
		{
			get
			{
				return this._persuasionHint;
			}
			set
			{
				if (this._persuasionHint != value)
				{
					this._persuasionHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "PersuasionHint");
				}
			}
		}

		// Token: 0x170008B8 RID: 2232
		// (get) Token: 0x060019F7 RID: 6647 RVA: 0x000622EC File Offset: 0x000604EC
		// (set) Token: 0x060019F8 RID: 6648 RVA: 0x000622F4 File Offset: 0x000604F4
		[DataSourceProperty]
		public string ProgressText
		{
			get
			{
				return this._progressText;
			}
			set
			{
				if (this._progressText != value)
				{
					this._progressText = value;
					base.OnPropertyChangedWithValue<string>(value, "ProgressText");
				}
			}
		}

		// Token: 0x170008B9 RID: 2233
		// (get) Token: 0x060019F9 RID: 6649 RVA: 0x00062317 File Offset: 0x00060517
		// (set) Token: 0x060019FA RID: 6650 RVA: 0x0006231F File Offset: 0x0006051F
		[DataSourceProperty]
		public MBBindingList<BoolItemWithActionVM> PersuasionProgress
		{
			get
			{
				return this._persuasionProgress;
			}
			set
			{
				if (value != this._persuasionProgress)
				{
					this._persuasionProgress = value;
					base.OnPropertyChangedWithValue<MBBindingList<BoolItemWithActionVM>>(value, "PersuasionProgress");
				}
			}
		}

		// Token: 0x170008BA RID: 2234
		// (get) Token: 0x060019FB RID: 6651 RVA: 0x0006233D File Offset: 0x0006053D
		// (set) Token: 0x060019FC RID: 6652 RVA: 0x00062345 File Offset: 0x00060545
		[DataSourceProperty]
		public bool IsPersuasionActive
		{
			get
			{
				return this._isPersuasionActive;
			}
			set
			{
				if (value != this._isPersuasionActive)
				{
					if (value)
					{
						this.RefreshChangeValues();
					}
					this._isPersuasionActive = value;
					base.OnPropertyChangedWithValue(value, "IsPersuasionActive");
				}
			}
		}

		// Token: 0x170008BB RID: 2235
		// (get) Token: 0x060019FD RID: 6653 RVA: 0x0006236C File Offset: 0x0006056C
		// (set) Token: 0x060019FE RID: 6654 RVA: 0x00062374 File Offset: 0x00060574
		[DataSourceProperty]
		public int CurrentSuccessChance
		{
			get
			{
				return this._currentSuccessChance;
			}
			set
			{
				if (this._currentSuccessChance != value)
				{
					this._currentSuccessChance = value;
					base.OnPropertyChangedWithValue(value, "CurrentSuccessChance");
				}
			}
		}

		// Token: 0x170008BC RID: 2236
		// (get) Token: 0x060019FF RID: 6655 RVA: 0x00062392 File Offset: 0x00060592
		// (set) Token: 0x06001A00 RID: 6656 RVA: 0x0006239A File Offset: 0x0006059A
		[DataSourceProperty]
		public PersuasionOptionVM CurrentPersuasionOption
		{
			get
			{
				return this._currentPersuasionOption;
			}
			set
			{
				if (this._currentPersuasionOption != value)
				{
					this._currentPersuasionOption = value;
					base.OnPropertyChangedWithValue<PersuasionOptionVM>(value, "CurrentPersuasionOption");
				}
			}
		}

		// Token: 0x170008BD RID: 2237
		// (get) Token: 0x06001A01 RID: 6657 RVA: 0x000623B8 File Offset: 0x000605B8
		// (set) Token: 0x06001A02 RID: 6658 RVA: 0x000623C0 File Offset: 0x000605C0
		[DataSourceProperty]
		public int CurrentFailChance
		{
			get
			{
				return this._currentFailChance;
			}
			set
			{
				if (this._currentFailChance != value)
				{
					this._currentFailChance = value;
					base.OnPropertyChangedWithValue(value, "CurrentFailChance");
				}
			}
		}

		// Token: 0x170008BE RID: 2238
		// (get) Token: 0x06001A03 RID: 6659 RVA: 0x000623DE File Offset: 0x000605DE
		// (set) Token: 0x06001A04 RID: 6660 RVA: 0x000623E6 File Offset: 0x000605E6
		[DataSourceProperty]
		public int CurrentCritSuccessChance
		{
			get
			{
				return this._currentCritSuccessChance;
			}
			set
			{
				if (this._currentCritSuccessChance != value)
				{
					this._currentCritSuccessChance = value;
					base.OnPropertyChangedWithValue(value, "CurrentCritSuccessChance");
				}
			}
		}

		// Token: 0x170008BF RID: 2239
		// (get) Token: 0x06001A05 RID: 6661 RVA: 0x00062404 File Offset: 0x00060604
		// (set) Token: 0x06001A06 RID: 6662 RVA: 0x0006240C File Offset: 0x0006060C
		[DataSourceProperty]
		public int CurrentCritFailChance
		{
			get
			{
				return this._currentCritFailChance;
			}
			set
			{
				if (this._currentCritFailChance != value)
				{
					this._currentCritFailChance = value;
					base.OnPropertyChangedWithValue(value, "CurrentCritFailChance");
				}
			}
		}

		// Token: 0x04000BED RID: 3053
		internal const string PositiveText = "<a style=\"Conversation.Persuasion.Positive\"><b>{TEXT}</b></a>";

		// Token: 0x04000BEE RID: 3054
		internal const string NegativeText = "<a style=\"Conversation.Persuasion.Negative\"><b>{TEXT}</b></a>";

		// Token: 0x04000BEF RID: 3055
		internal const string NeutralText = "<a style=\"Conversation.Persuasion.Neutral\"><b>{TEXT}</b></a>";

		// Token: 0x04000BF0 RID: 3056
		private ConversationManager _manager;

		// Token: 0x04000BF1 RID: 3057
		private MBBindingList<BoolItemWithActionVM> _persuasionProgress;

		// Token: 0x04000BF2 RID: 3058
		private bool _isPersuasionActive;

		// Token: 0x04000BF3 RID: 3059
		private int _currentCritFailChance;

		// Token: 0x04000BF4 RID: 3060
		private int _currentFailChance;

		// Token: 0x04000BF5 RID: 3061
		private int _currentSuccessChance;

		// Token: 0x04000BF6 RID: 3062
		private int _currentCritSuccessChance;

		// Token: 0x04000BF7 RID: 3063
		private string _progressText;

		// Token: 0x04000BF8 RID: 3064
		private PersuasionOptionVM _currentPersuasionOption;

		// Token: 0x04000BF9 RID: 3065
		private BasicTooltipViewModel _persuasionHint;
	}
}
