using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement
{
	// Token: 0x02000121 RID: 289
	public class ClanCardSelectionPopupVM : ViewModel
	{
		// Token: 0x06001A34 RID: 6708 RVA: 0x00062B1C File Offset: 0x00060D1C
		public ClanCardSelectionPopupVM()
		{
			this._titleText = TextObject.GetEmpty();
			this.Items = new MBBindingList<ClanCardSelectionPopupItemVM>();
			this.DisabledHint = new HintViewModel();
		}

		// Token: 0x06001A35 RID: 6709 RVA: 0x00062B48 File Offset: 0x00060D48
		public override void RefreshValues()
		{
			base.RefreshValues();
			if (!this._isMultiSelection)
			{
				ClanCardSelectionPopupItemVM lastSelectedItem = this._lastSelectedItem;
				string text;
				if (lastSelectedItem == null)
				{
					text = null;
				}
				else
				{
					TextObject actionResultText = lastSelectedItem.ActionResultText;
					text = ((actionResultText != null) ? actionResultText.ToString() : null);
				}
				this.ActionResult = text ?? string.Empty;
			}
			this.DoneLbl = GameTexts.FindText("str_done", null).ToString();
			TextObject titleText = this._titleText;
			this.Title = ((titleText != null) ? titleText.ToString() : null) ?? string.Empty;
			this.Items.ApplyActionOnAllItems(delegate(ClanCardSelectionPopupItemVM x)
			{
				x.RefreshValues();
			});
			this.RefreshHintText();
		}

		// Token: 0x06001A36 RID: 6710 RVA: 0x00062BF8 File Offset: 0x00060DF8
		private void RefreshHintText()
		{
			TextObject textObject = TextObject.GetEmpty();
			if (this._isMultiSelection)
			{
				if (this._maximumSelection > 0 && this._selectedItemCount > this._maximumSelection)
				{
					textObject = new TextObject("{=lIGdkJGm}You must choose less than {NUMBER} {?NUMBER>1}items{?}item{\\?}", null);
					textObject.SetTextVariable("NUMBER", this._maximumSelection);
				}
				else if (this._selectedItemCount < this._minimumSelection)
				{
					textObject = new TextObject("{=woD234nb}You must choose more than {NUMBER} {?NUMBER>1}items{?}item{\\?}", null);
					textObject.SetTextVariable("NUMBER", this._minimumSelection);
				}
			}
			else if (this._selectedItemCount != 1)
			{
				textObject = new TextObject("{=aYm5Ehv1}You must choose an item", null);
			}
			this.DisabledHint.HintText = textObject;
		}

		// Token: 0x06001A37 RID: 6711 RVA: 0x00062C99 File Offset: 0x00060E99
		public override void OnFinalize()
		{
			base.OnFinalize();
			InputKeyItemVM doneInputKey = this.DoneInputKey;
			if (doneInputKey != null)
			{
				doneInputKey.OnFinalize();
			}
			InputKeyItemVM cancelInputKey = this.CancelInputKey;
			if (cancelInputKey == null)
			{
				return;
			}
			cancelInputKey.OnFinalize();
		}

		// Token: 0x06001A38 RID: 6712 RVA: 0x00062CC2 File Offset: 0x00060EC2
		public void SetDoneInputKey(HotKey hotKey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x06001A39 RID: 6713 RVA: 0x00062CD1 File Offset: 0x00060ED1
		public void SetCancelInputKey(HotKey hotKey)
		{
			this.CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x06001A3A RID: 6714 RVA: 0x00062CE0 File Offset: 0x00060EE0
		public void Open(ClanCardSelectionInfo info)
		{
			this._isMultiSelection = info.IsMultiSelection;
			this._minimumSelection = info.MinimumSelection;
			this._maximumSelection = info.MaximumSelection;
			this._titleText = info.Title;
			this._onClosed = info.OnClosedAction;
			foreach (ClanCardSelectionItemInfo clanCardSelectionItemInfo in info.Items)
			{
				this.Items.Add(new ClanCardSelectionPopupItemVM(ref clanCardSelectionItemInfo, new Action<ClanCardSelectionPopupItemVM>(this.OnItemSelected)));
			}
			this._selectedItemCount = 0;
			this.RefreshValues();
			this.IsVisible = true;
			this.UpdateIsDoneEnabled();
		}

		// Token: 0x06001A3B RID: 6715 RVA: 0x00062D9C File Offset: 0x00060F9C
		public void ExecuteCancel()
		{
			Action<List<object>, Action> onClosed = this._onClosed;
			if (onClosed != null)
			{
				onClosed(new List<object>(), null);
			}
			this.Close();
		}

		// Token: 0x06001A3C RID: 6716 RVA: 0x00062DBC File Offset: 0x00060FBC
		public void ExecuteDone()
		{
			List<object> selectedItems = new List<object>();
			this.Items.ApplyActionOnAllItems(delegate(ClanCardSelectionPopupItemVM x)
			{
				if (x.IsSelected)
				{
					selectedItems.Add(x.Identifier);
				}
			});
			Action<List<object>, Action> onClosed = this._onClosed;
			if (onClosed == null)
			{
				return;
			}
			onClosed(selectedItems, new Action(this.Close));
		}

		// Token: 0x06001A3D RID: 6717 RVA: 0x00062E14 File Offset: 0x00061014
		private void Close()
		{
			this.IsVisible = false;
			this._lastSelectedItem = null;
			this._titleText = TextObject.GetEmpty();
			this.ActionResult = string.Empty;
			this.Title = string.Empty;
			this._onClosed = null;
			this.Items.Clear();
		}

		// Token: 0x06001A3E RID: 6718 RVA: 0x00062E64 File Offset: 0x00061064
		private void OnItemSelected(ClanCardSelectionPopupItemVM item)
		{
			if (this._isMultiSelection)
			{
				item.IsSelected = !item.IsSelected;
				if (item.IsSelected)
				{
					this._selectedItemCount++;
				}
				else
				{
					this._selectedItemCount--;
				}
			}
			else if (item != this._lastSelectedItem)
			{
				if (this._lastSelectedItem != null)
				{
					this._lastSelectedItem.IsSelected = false;
				}
				item.IsSelected = true;
				TextObject actionResultText = item.ActionResultText;
				this.ActionResult = ((actionResultText != null) ? actionResultText.ToString() : null) ?? string.Empty;
				this._selectedItemCount = 1;
			}
			this._lastSelectedItem = item;
			this.UpdateIsDoneEnabled();
			this.RefreshHintText();
		}

		// Token: 0x06001A3F RID: 6719 RVA: 0x00062F10 File Offset: 0x00061110
		private void UpdateIsDoneEnabled()
		{
			if (this._isMultiSelection)
			{
				this.IsDoneEnabled = this._selectedItemCount >= this._minimumSelection && (this._maximumSelection <= 0 || this._selectedItemCount <= this._maximumSelection);
				return;
			}
			this.IsDoneEnabled = this._selectedItemCount == 1;
		}

		// Token: 0x170008D1 RID: 2257
		// (get) Token: 0x06001A40 RID: 6720 RVA: 0x00062F69 File Offset: 0x00061169
		// (set) Token: 0x06001A41 RID: 6721 RVA: 0x00062F71 File Offset: 0x00061171
		[DataSourceProperty]
		public MBBindingList<ClanCardSelectionPopupItemVM> Items
		{
			get
			{
				return this._items;
			}
			set
			{
				if (value != this._items)
				{
					this._items = value;
					base.OnPropertyChangedWithValue<MBBindingList<ClanCardSelectionPopupItemVM>>(value, "Items");
				}
			}
		}

		// Token: 0x170008D2 RID: 2258
		// (get) Token: 0x06001A42 RID: 6722 RVA: 0x00062F8F File Offset: 0x0006118F
		// (set) Token: 0x06001A43 RID: 6723 RVA: 0x00062F97 File Offset: 0x00061197
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

		// Token: 0x170008D3 RID: 2259
		// (get) Token: 0x06001A44 RID: 6724 RVA: 0x00062FB5 File Offset: 0x000611B5
		// (set) Token: 0x06001A45 RID: 6725 RVA: 0x00062FBD File Offset: 0x000611BD
		[DataSourceProperty]
		public InputKeyItemVM CancelInputKey
		{
			get
			{
				return this._cancelInputKey;
			}
			set
			{
				if (value != this._cancelInputKey)
				{
					this._cancelInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "CancelInputKey");
				}
			}
		}

		// Token: 0x170008D4 RID: 2260
		// (get) Token: 0x06001A46 RID: 6726 RVA: 0x00062FDB File Offset: 0x000611DB
		// (set) Token: 0x06001A47 RID: 6727 RVA: 0x00062FE3 File Offset: 0x000611E3
		[DataSourceProperty]
		public string Title
		{
			get
			{
				return this._title;
			}
			set
			{
				if (value != this._title)
				{
					this._title = value;
					base.OnPropertyChangedWithValue<string>(value, "Title");
				}
			}
		}

		// Token: 0x170008D5 RID: 2261
		// (get) Token: 0x06001A48 RID: 6728 RVA: 0x00063006 File Offset: 0x00061206
		// (set) Token: 0x06001A49 RID: 6729 RVA: 0x0006300E File Offset: 0x0006120E
		[DataSourceProperty]
		public string ActionResult
		{
			get
			{
				return this._actionResult;
			}
			set
			{
				if (value != this._actionResult)
				{
					this._actionResult = value;
					base.OnPropertyChangedWithValue<string>(value, "ActionResult");
				}
			}
		}

		// Token: 0x170008D6 RID: 2262
		// (get) Token: 0x06001A4A RID: 6730 RVA: 0x00063031 File Offset: 0x00061231
		// (set) Token: 0x06001A4B RID: 6731 RVA: 0x00063039 File Offset: 0x00061239
		[DataSourceProperty]
		public string DoneLbl
		{
			get
			{
				return this._doneLbl;
			}
			set
			{
				if (value != this._doneLbl)
				{
					this._doneLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "DoneLbl");
				}
			}
		}

		// Token: 0x170008D7 RID: 2263
		// (get) Token: 0x06001A4C RID: 6732 RVA: 0x0006305C File Offset: 0x0006125C
		// (set) Token: 0x06001A4D RID: 6733 RVA: 0x00063064 File Offset: 0x00061264
		[DataSourceProperty]
		public bool IsVisible
		{
			get
			{
				return this._isVisible;
			}
			set
			{
				if (value != this._isVisible)
				{
					this._isVisible = value;
					base.OnPropertyChangedWithValue(value, "IsVisible");
				}
			}
		}

		// Token: 0x170008D8 RID: 2264
		// (get) Token: 0x06001A4E RID: 6734 RVA: 0x00063082 File Offset: 0x00061282
		// (set) Token: 0x06001A4F RID: 6735 RVA: 0x0006308A File Offset: 0x0006128A
		[DataSourceProperty]
		public bool IsDoneEnabled
		{
			get
			{
				return this._isDoneEnabled;
			}
			set
			{
				if (value != this._isDoneEnabled)
				{
					this._isDoneEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsDoneEnabled");
				}
			}
		}

		// Token: 0x170008D9 RID: 2265
		// (get) Token: 0x06001A50 RID: 6736 RVA: 0x000630A8 File Offset: 0x000612A8
		// (set) Token: 0x06001A51 RID: 6737 RVA: 0x000630B0 File Offset: 0x000612B0
		[DataSourceProperty]
		public HintViewModel DisabledHint
		{
			get
			{
				return this._disabledHint;
			}
			set
			{
				if (value != this._disabledHint)
				{
					this._disabledHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "DisabledHint");
				}
			}
		}

		// Token: 0x04000C29 RID: 3113
		private TextObject _titleText;

		// Token: 0x04000C2A RID: 3114
		private bool _isMultiSelection;

		// Token: 0x04000C2B RID: 3115
		private int _minimumSelection;

		// Token: 0x04000C2C RID: 3116
		private int _maximumSelection;

		// Token: 0x04000C2D RID: 3117
		private ClanCardSelectionPopupItemVM _lastSelectedItem;

		// Token: 0x04000C2E RID: 3118
		private int _selectedItemCount;

		// Token: 0x04000C2F RID: 3119
		private Action<List<object>, Action> _onClosed;

		// Token: 0x04000C30 RID: 3120
		private MBBindingList<ClanCardSelectionPopupItemVM> _items;

		// Token: 0x04000C31 RID: 3121
		private InputKeyItemVM _doneInputKey;

		// Token: 0x04000C32 RID: 3122
		private InputKeyItemVM _cancelInputKey;

		// Token: 0x04000C33 RID: 3123
		private string _title;

		// Token: 0x04000C34 RID: 3124
		private string _actionResult;

		// Token: 0x04000C35 RID: 3125
		private string _doneLbl;

		// Token: 0x04000C36 RID: 3126
		private bool _isVisible;

		// Token: 0x04000C37 RID: 3127
		private bool _isDoneEnabled;

		// Token: 0x04000C38 RID: 3128
		private HintViewModel _disabledHint;
	}
}
