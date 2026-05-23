using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Conversation
{
	// Token: 0x02000117 RID: 279
	public class ConversationItemVM : ViewModel
	{
		// Token: 0x1700087E RID: 2174
		// (get) Token: 0x06001958 RID: 6488 RVA: 0x0005FEE8 File Offset: 0x0005E0E8
		private ConversationSentenceOption _option
		{
			get
			{
				List<ConversationSentenceOption> curOptions = Campaign.Current.ConversationManager.CurOptions;
				if (curOptions == null || curOptions.Count <= 0)
				{
					return default(ConversationSentenceOption);
				}
				return Campaign.Current.ConversationManager.CurOptions[this.Index];
			}
		}

		// Token: 0x06001959 RID: 6489 RVA: 0x0005FF3C File Offset: 0x0005E13C
		public ConversationItemVM(Action<int> action, Action onReadyToContinue, Action<ConversationItemVM> setCurrentAnswer, int index)
		{
			this.ActionWihIntIndex = action;
			this.Index = index;
			this._onReadyToContinue = onReadyToContinue;
			this.IsEnabled = this._option.IsClickable;
			this.HasPersuasion = this._option.HasPersuasion;
			this._setCurrentAnswer = setCurrentAnswer;
			this.PersuasionItem = new PersuasionOptionVM(Campaign.Current.ConversationManager, index, new Action(this.OnReadyToContinue));
			this.IsSpecial = this._option.IsSpecial;
			this.RefreshValues();
		}

		// Token: 0x0600195A RID: 6490 RVA: 0x0005FFC8 File Offset: 0x0005E1C8
		private void OnReadyToContinue()
		{
			this._onReadyToContinue.DynamicInvokeWithLog(Array.Empty<object>());
		}

		// Token: 0x0600195B RID: 6491 RVA: 0x0005FFDB File Offset: 0x0005E1DB
		public ConversationItemVM()
		{
			this.Index = 0;
			this.ItemText = "";
			this.IsEnabled = false;
			this.OptionHint = new HintViewModel();
			this.HasPersuasion = false;
			this._setCurrentAnswer = null;
		}

		// Token: 0x0600195C RID: 6492 RVA: 0x00060018 File Offset: 0x0005E218
		public override void RefreshValues()
		{
			base.RefreshValues();
			TextObject text = this._option.Text;
			string text2 = ((text != null) ? text.ToString() : null) ?? "";
			this.OptionHint = new HintViewModel((this._option.HintText != null) ? this._option.HintText : TextObject.GetEmpty(), null);
			PersuasionOptionVM persuasionItem = this.PersuasionItem;
			if (persuasionItem != null)
			{
				persuasionItem.RefreshValues();
			}
			if (this.PersuasionItem != null)
			{
				string persuasionAdditionalText = this.PersuasionItem.GetPersuasionAdditionalText();
				if (!string.IsNullOrEmpty(persuasionAdditionalText))
				{
					GameTexts.SetVariable("STR1", text2);
					GameTexts.SetVariable("STR2", persuasionAdditionalText);
					text2 = GameTexts.FindText("str_STR1_space_STR2", null).ToString();
				}
			}
			this.ItemText = text2;
		}

		// Token: 0x0600195D RID: 6493 RVA: 0x000600D8 File Offset: 0x0005E2D8
		public void ExecuteAction()
		{
			Action<int> actionWihIntIndex = this.ActionWihIntIndex;
			if (actionWihIntIndex == null)
			{
				return;
			}
			actionWihIntIndex(this.Index);
		}

		// Token: 0x0600195E RID: 6494 RVA: 0x000600F0 File Offset: 0x0005E2F0
		public void SetCurrentAnswer()
		{
			Action<ConversationItemVM> setCurrentAnswer = this._setCurrentAnswer;
			if (setCurrentAnswer == null)
			{
				return;
			}
			setCurrentAnswer(this);
		}

		// Token: 0x0600195F RID: 6495 RVA: 0x00060103 File Offset: 0x0005E303
		public void ResetCurrentAnswer()
		{
			this._setCurrentAnswer(null);
		}

		// Token: 0x06001960 RID: 6496 RVA: 0x00060111 File Offset: 0x0005E311
		internal void OnPersuasionProgress(Tuple<PersuasionOptionArgs, PersuasionOptionResult> result)
		{
			PersuasionOptionVM persuasionItem = this.PersuasionItem;
			if (persuasionItem == null)
			{
				return;
			}
			persuasionItem.OnPersuasionProgress(result);
		}

		// Token: 0x1700087F RID: 2175
		// (get) Token: 0x06001961 RID: 6497 RVA: 0x00060124 File Offset: 0x0005E324
		// (set) Token: 0x06001962 RID: 6498 RVA: 0x0006012C File Offset: 0x0005E32C
		[DataSourceProperty]
		public PersuasionOptionVM PersuasionItem
		{
			get
			{
				return this._persuasionItem;
			}
			set
			{
				if (this._persuasionItem != value)
				{
					this._persuasionItem = value;
					base.OnPropertyChangedWithValue<PersuasionOptionVM>(value, "PersuasionItem");
				}
			}
		}

		// Token: 0x17000880 RID: 2176
		// (get) Token: 0x06001963 RID: 6499 RVA: 0x0006014A File Offset: 0x0005E34A
		// (set) Token: 0x06001964 RID: 6500 RVA: 0x00060152 File Offset: 0x0005E352
		[DataSourceProperty]
		public bool HasPersuasion
		{
			get
			{
				return this._hasPersuasion;
			}
			set
			{
				if (this._hasPersuasion != value)
				{
					this._hasPersuasion = value;
					base.OnPropertyChangedWithValue(value, "HasPersuasion");
				}
			}
		}

		// Token: 0x17000881 RID: 2177
		// (get) Token: 0x06001965 RID: 6501 RVA: 0x00060170 File Offset: 0x0005E370
		// (set) Token: 0x06001966 RID: 6502 RVA: 0x00060178 File Offset: 0x0005E378
		[DataSourceProperty]
		public int IconType
		{
			get
			{
				return this._iconType;
			}
			set
			{
				if (this._iconType != value)
				{
					this._iconType = value;
					base.OnPropertyChangedWithValue(value, "IconType");
				}
			}
		}

		// Token: 0x17000882 RID: 2178
		// (get) Token: 0x06001967 RID: 6503 RVA: 0x00060196 File Offset: 0x0005E396
		// (set) Token: 0x06001968 RID: 6504 RVA: 0x0006019E File Offset: 0x0005E39E
		[DataSourceProperty]
		public HintViewModel OptionHint
		{
			get
			{
				return this._optionHint;
			}
			set
			{
				if (this._optionHint != value)
				{
					this._optionHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "OptionHint");
				}
			}
		}

		// Token: 0x17000883 RID: 2179
		// (get) Token: 0x06001969 RID: 6505 RVA: 0x000601BC File Offset: 0x0005E3BC
		// (set) Token: 0x0600196A RID: 6506 RVA: 0x000601C4 File Offset: 0x0005E3C4
		[DataSourceProperty]
		public string ItemText
		{
			get
			{
				return this._itemText;
			}
			set
			{
				if (this._itemText != value)
				{
					this._itemText = value;
					base.OnPropertyChangedWithValue<string>(value, "ItemText");
				}
			}
		}

		// Token: 0x17000884 RID: 2180
		// (get) Token: 0x0600196B RID: 6507 RVA: 0x000601E7 File Offset: 0x0005E3E7
		// (set) Token: 0x0600196C RID: 6508 RVA: 0x000601EF File Offset: 0x0005E3EF
		[DataSourceProperty]
		public bool IsEnabled
		{
			get
			{
				return this._isEnabled;
			}
			set
			{
				if (this._isEnabled != value)
				{
					this._isEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsEnabled");
				}
			}
		}

		// Token: 0x17000885 RID: 2181
		// (get) Token: 0x0600196D RID: 6509 RVA: 0x0006020D File Offset: 0x0005E40D
		// (set) Token: 0x0600196E RID: 6510 RVA: 0x00060215 File Offset: 0x0005E415
		[DataSourceProperty]
		public bool IsSpecial
		{
			get
			{
				return this._isSpecial;
			}
			set
			{
				if (this._isSpecial != value)
				{
					this._isSpecial = value;
					base.OnPropertyChangedWithValue(value, "IsSpecial");
				}
			}
		}

		// Token: 0x04000BA7 RID: 2983
		public Action<int> ActionWihIntIndex;

		// Token: 0x04000BA8 RID: 2984
		public Action<ConversationItemVM> _setCurrentAnswer;

		// Token: 0x04000BA9 RID: 2985
		public int Index;

		// Token: 0x04000BAA RID: 2986
		private Action _onReadyToContinue;

		// Token: 0x04000BAB RID: 2987
		private bool _hasPersuasion;

		// Token: 0x04000BAC RID: 2988
		private bool _isSpecial;

		// Token: 0x04000BAD RID: 2989
		private string _itemText;

		// Token: 0x04000BAE RID: 2990
		private int _iconType;

		// Token: 0x04000BAF RID: 2991
		private bool _isEnabled;

		// Token: 0x04000BB0 RID: 2992
		private PersuasionOptionVM _persuasionItem;

		// Token: 0x04000BB1 RID: 2993
		private HintViewModel _optionHint;
	}
}
