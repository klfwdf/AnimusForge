using System;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TroopSelection
{
	// Token: 0x0200009F RID: 159
	public class GameMenuTroopSelectionVM : ViewModel
	{
		// Token: 0x06000F5A RID: 3930 RVA: 0x0003FC6C File Offset: 0x0003DE6C
		public GameMenuTroopSelectionVM(TroopRoster fullRoster, TroopRoster initialSelections, Func<CharacterObject, bool> canChangeChangeStatusOfTroop, Action<TroopRoster> onDone, int maxSelectableTroopCount, int minSelectableTroopCount)
		{
			this._canChangeChangeStatusOfTroop = canChangeChangeStatusOfTroop;
			this._onDone = onDone;
			this._fullRoster = fullRoster;
			this._initialSelections = initialSelections;
			this._maxSelectableTroopCount = maxSelectableTroopCount;
			this._minSelectableTroopCount = minSelectableTroopCount;
			this.DoneHint = new HintViewModel();
			this.InitList();
			this.RefreshValues();
			this.OnCurrentSelectedAmountChange();
		}

		// Token: 0x06000F5B RID: 3931 RVA: 0x0003FCEC File Offset: 0x0003DEEC
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.TitleText = this._titleTextObject.ToString();
			this.CurrentSelectedAmountTitle = this._chosenTitleTextObject.ToString();
			this.DoneText = GameTexts.FindText("str_done", null).ToString();
			this.CancelText = GameTexts.FindText("str_cancel", null).ToString();
			this.ClearSelectionText = new TextObject("{=QMNWbmao}Clear Selection", null).ToString();
			this.RefreshDoneHint();
		}

		// Token: 0x06000F5C RID: 3932 RVA: 0x0003FD6C File Offset: 0x0003DF6C
		private void RefreshDoneHint()
		{
			if (this.IsDoneEnabled)
			{
				this.DoneHint.HintText = TextObject.GetEmpty();
				return;
			}
			if (this._currentTotalSelectedTroopCount < this._minSelectableTroopCount)
			{
				this.DoneHint.HintText = new TextObject("{=LlV29O9B}You must select at least {TROOP_COUNT} troops", null).SetTextVariable("TROOP_COUNT", this._minSelectableTroopCount);
				return;
			}
			this.DoneHint.HintText = new TextObject("{=TdWQM7QZ}You must select less than {TROOP_COUNT} troops", null).SetTextVariable("TROOP_COUNT", this._maxSelectableTroopCount);
		}

		// Token: 0x06000F5D RID: 3933 RVA: 0x0003FDF0 File Offset: 0x0003DFF0
		private void InitList()
		{
			this.Troops = new MBBindingList<TroopSelectionItemVM>();
			this._currentTotalSelectedTroopCount = 0;
			foreach (TroopRosterElement troopRosterElement in this._fullRoster.GetTroopRoster())
			{
				TroopSelectionItemVM troopSelectionItemVM = new TroopSelectionItemVM(troopRosterElement, new Action<TroopSelectionItemVM>(this.OnAddCount), new Action<TroopSelectionItemVM>(this.OnRemoveCount));
				troopSelectionItemVM.IsLocked = !this._canChangeChangeStatusOfTroop(troopRosterElement.Character) || troopRosterElement.Number - troopRosterElement.WoundedNumber <= 0;
				this.Troops.Add(troopSelectionItemVM);
				int troopCount = this._initialSelections.GetTroopCount(troopRosterElement.Character);
				if (troopCount > 0)
				{
					troopSelectionItemVM.CurrentAmount = troopCount;
					this._currentTotalSelectedTroopCount += troopCount;
				}
			}
			this.Troops.Sort(new TroopItemComparer());
		}

		// Token: 0x06000F5E RID: 3934 RVA: 0x0003FEF0 File Offset: 0x0003E0F0
		private void OnRemoveCount(TroopSelectionItemVM troopItem)
		{
			if (troopItem.CurrentAmount > 0)
			{
				int num = 1;
				if (this.IsEntireStackModifierActive)
				{
					num = troopItem.CurrentAmount;
				}
				else if (this.IsFiveStackModifierActive)
				{
					num = MathF.Min(troopItem.CurrentAmount, 5);
				}
				troopItem.CurrentAmount -= num;
				this._currentTotalSelectedTroopCount -= num;
			}
			this.OnCurrentSelectedAmountChange();
		}

		// Token: 0x06000F5F RID: 3935 RVA: 0x0003FF50 File Offset: 0x0003E150
		private void OnAddCount(TroopSelectionItemVM troopItem)
		{
			if (troopItem.CurrentAmount < troopItem.MaxAmount && this._currentTotalSelectedTroopCount < this._maxSelectableTroopCount)
			{
				int num = 1;
				if (this.IsEntireStackModifierActive)
				{
					num = MathF.Min(troopItem.MaxAmount - troopItem.CurrentAmount, this._maxSelectableTroopCount - this._currentTotalSelectedTroopCount);
				}
				else if (this.IsFiveStackModifierActive)
				{
					num = MathF.Min(MathF.Min(troopItem.MaxAmount - troopItem.CurrentAmount, this._maxSelectableTroopCount - this._currentTotalSelectedTroopCount), 5);
				}
				troopItem.CurrentAmount += num;
				this._currentTotalSelectedTroopCount += num;
			}
			this.OnCurrentSelectedAmountChange();
		}

		// Token: 0x06000F60 RID: 3936 RVA: 0x0003FFF8 File Offset: 0x0003E1F8
		private void OnCurrentSelectedAmountChange()
		{
			foreach (TroopSelectionItemVM troopSelectionItemVM in this.Troops)
			{
				troopSelectionItemVM.IsRosterFull = this._currentTotalSelectedTroopCount >= this._maxSelectableTroopCount;
			}
			GameTexts.SetVariable("LEFT", this._currentTotalSelectedTroopCount);
			GameTexts.SetVariable("RIGHT", this._maxSelectableTroopCount);
			this.CurrentSelectedAmountText = GameTexts.FindText("str_LEFT_over_RIGHT_in_paranthesis", null).ToString();
			this.IsDoneEnabled = this._currentTotalSelectedTroopCount <= this._maxSelectableTroopCount && this._currentTotalSelectedTroopCount >= this._minSelectableTroopCount;
			this.RefreshDoneHint();
		}

		// Token: 0x06000F61 RID: 3937 RVA: 0x000400B8 File Offset: 0x0003E2B8
		private void OnDone()
		{
			TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
			foreach (TroopSelectionItemVM troopSelectionItemVM in this.Troops)
			{
				if (troopSelectionItemVM.CurrentAmount > 0)
				{
					troopRoster.AddToCounts(troopSelectionItemVM.Troop.Character, troopSelectionItemVM.CurrentAmount, false, 0, 0, true, -1);
				}
			}
			this.IsEnabled = false;
			this._onDone.DynamicInvokeWithLog(new object[] { troopRoster });
		}

		// Token: 0x06000F62 RID: 3938 RVA: 0x00040148 File Offset: 0x0003E348
		public void ExecuteDone()
		{
			if (this.GetAvailableSelectableTroopCount() > 0)
			{
				string text = new TextObject("{=z2Slmx4N}There are still some room for more soldiers. Do you want to proceed?", null).ToString();
				InformationManager.ShowInquiry(new InquiryData(this.TitleText, text, true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), new Action(this.OnDone), null, "", 0f, null, null, null), false, false);
				return;
			}
			this.OnDone();
		}

		// Token: 0x06000F63 RID: 3939 RVA: 0x000401C8 File Offset: 0x0003E3C8
		private int GetAvailableSelectableTroopCount()
		{
			int num = 0;
			foreach (TroopSelectionItemVM troopSelectionItemVM in this.Troops)
			{
				if (!troopSelectionItemVM.IsLocked && troopSelectionItemVM.CurrentAmount < troopSelectionItemVM.MaxAmount)
				{
					num += troopSelectionItemVM.MaxAmount - troopSelectionItemVM.CurrentAmount;
				}
			}
			if (this._currentTotalSelectedTroopCount + num > this._maxSelectableTroopCount)
			{
				num = this._maxSelectableTroopCount - this._currentTotalSelectedTroopCount;
			}
			return num;
		}

		// Token: 0x06000F64 RID: 3940 RVA: 0x00040258 File Offset: 0x0003E458
		public void ExecuteCancel()
		{
			this.IsEnabled = false;
		}

		// Token: 0x06000F65 RID: 3941 RVA: 0x00040261 File Offset: 0x0003E461
		public void ExecuteReset()
		{
			this.InitList();
			this.OnCurrentSelectedAmountChange();
		}

		// Token: 0x06000F66 RID: 3942 RVA: 0x0004026F File Offset: 0x0003E46F
		public void ExecuteClearSelection()
		{
			this.Troops.ApplyActionOnAllItems(delegate(TroopSelectionItemVM troopItem)
			{
				if (this._canChangeChangeStatusOfTroop(troopItem.Troop.Character))
				{
					int currentAmount = troopItem.CurrentAmount;
					for (int i = 0; i < currentAmount; i++)
					{
						troopItem.ExecuteRemove();
					}
				}
			});
		}

		// Token: 0x06000F67 RID: 3943 RVA: 0x00040288 File Offset: 0x0003E488
		public override void OnFinalize()
		{
			base.OnFinalize();
			InputKeyItemVM cancelInputKey = this.CancelInputKey;
			if (cancelInputKey != null)
			{
				cancelInputKey.OnFinalize();
			}
			InputKeyItemVM doneInputKey = this.DoneInputKey;
			if (doneInputKey != null)
			{
				doneInputKey.OnFinalize();
			}
			InputKeyItemVM resetInputKey = this.ResetInputKey;
			if (resetInputKey == null)
			{
				return;
			}
			resetInputKey.OnFinalize();
		}

		// Token: 0x06000F68 RID: 3944 RVA: 0x000402C2 File Offset: 0x0003E4C2
		public void SetCancelInputKey(HotKey hotkey)
		{
			this.CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, true);
		}

		// Token: 0x06000F69 RID: 3945 RVA: 0x000402D1 File Offset: 0x0003E4D1
		public void SetDoneInputKey(HotKey hotkey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, true);
		}

		// Token: 0x06000F6A RID: 3946 RVA: 0x000402E0 File Offset: 0x0003E4E0
		public void SetResetInputKey(HotKey hotkey)
		{
			this.ResetInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, true);
		}

		// Token: 0x170004F5 RID: 1269
		// (get) Token: 0x06000F6B RID: 3947 RVA: 0x000402EF File Offset: 0x0003E4EF
		// (set) Token: 0x06000F6C RID: 3948 RVA: 0x000402F7 File Offset: 0x0003E4F7
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

		// Token: 0x170004F6 RID: 1270
		// (get) Token: 0x06000F6D RID: 3949 RVA: 0x00040315 File Offset: 0x0003E515
		// (set) Token: 0x06000F6E RID: 3950 RVA: 0x0004031D File Offset: 0x0003E51D
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

		// Token: 0x170004F7 RID: 1271
		// (get) Token: 0x06000F6F RID: 3951 RVA: 0x0004033B File Offset: 0x0003E53B
		// (set) Token: 0x06000F70 RID: 3952 RVA: 0x00040343 File Offset: 0x0003E543
		[DataSourceProperty]
		public InputKeyItemVM ResetInputKey
		{
			get
			{
				return this._resetInputKey;
			}
			set
			{
				if (value != this._resetInputKey)
				{
					this._resetInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "ResetInputKey");
				}
			}
		}

		// Token: 0x170004F8 RID: 1272
		// (get) Token: 0x06000F71 RID: 3953 RVA: 0x00040361 File Offset: 0x0003E561
		// (set) Token: 0x06000F72 RID: 3954 RVA: 0x00040369 File Offset: 0x0003E569
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

		// Token: 0x170004F9 RID: 1273
		// (get) Token: 0x06000F73 RID: 3955 RVA: 0x00040387 File Offset: 0x0003E587
		// (set) Token: 0x06000F74 RID: 3956 RVA: 0x0004038F File Offset: 0x0003E58F
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

		// Token: 0x170004FA RID: 1274
		// (get) Token: 0x06000F75 RID: 3957 RVA: 0x000403AD File Offset: 0x0003E5AD
		// (set) Token: 0x06000F76 RID: 3958 RVA: 0x000403B5 File Offset: 0x0003E5B5
		[DataSourceProperty]
		public HintViewModel DoneHint
		{
			get
			{
				return this._doneHint;
			}
			set
			{
				if (value != this._doneHint)
				{
					this._doneHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "DoneHint");
				}
			}
		}

		// Token: 0x170004FB RID: 1275
		// (get) Token: 0x06000F77 RID: 3959 RVA: 0x000403D3 File Offset: 0x0003E5D3
		// (set) Token: 0x06000F78 RID: 3960 RVA: 0x000403DB File Offset: 0x0003E5DB
		[DataSourceProperty]
		public MBBindingList<TroopSelectionItemVM> Troops
		{
			get
			{
				return this._troops;
			}
			set
			{
				if (value != this._troops)
				{
					this._troops = value;
					base.OnPropertyChangedWithValue<MBBindingList<TroopSelectionItemVM>>(value, "Troops");
				}
			}
		}

		// Token: 0x170004FC RID: 1276
		// (get) Token: 0x06000F79 RID: 3961 RVA: 0x000403F9 File Offset: 0x0003E5F9
		// (set) Token: 0x06000F7A RID: 3962 RVA: 0x00040401 File Offset: 0x0003E601
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

		// Token: 0x170004FD RID: 1277
		// (get) Token: 0x06000F7B RID: 3963 RVA: 0x00040424 File Offset: 0x0003E624
		// (set) Token: 0x06000F7C RID: 3964 RVA: 0x0004042C File Offset: 0x0003E62C
		[DataSourceProperty]
		public string CancelText
		{
			get
			{
				return this._cancelText;
			}
			set
			{
				if (value != this._cancelText)
				{
					this._cancelText = value;
					base.OnPropertyChangedWithValue<string>(value, "CancelText");
				}
			}
		}

		// Token: 0x170004FE RID: 1278
		// (get) Token: 0x06000F7D RID: 3965 RVA: 0x0004044F File Offset: 0x0003E64F
		// (set) Token: 0x06000F7E RID: 3966 RVA: 0x00040457 File Offset: 0x0003E657
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

		// Token: 0x170004FF RID: 1279
		// (get) Token: 0x06000F7F RID: 3967 RVA: 0x0004047A File Offset: 0x0003E67A
		// (set) Token: 0x06000F80 RID: 3968 RVA: 0x00040482 File Offset: 0x0003E682
		[DataSourceProperty]
		public string ClearSelectionText
		{
			get
			{
				return this._clearSelectionText;
			}
			set
			{
				if (value != this._clearSelectionText)
				{
					this._clearSelectionText = value;
					base.OnPropertyChangedWithValue<string>(value, "ClearSelectionText");
				}
			}
		}

		// Token: 0x17000500 RID: 1280
		// (get) Token: 0x06000F81 RID: 3969 RVA: 0x000404A5 File Offset: 0x0003E6A5
		// (set) Token: 0x06000F82 RID: 3970 RVA: 0x000404AD File Offset: 0x0003E6AD
		[DataSourceProperty]
		public string CurrentSelectedAmountText
		{
			get
			{
				return this._currentSelectedAmountText;
			}
			set
			{
				if (value != this._currentSelectedAmountText)
				{
					this._currentSelectedAmountText = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentSelectedAmountText");
				}
			}
		}

		// Token: 0x17000501 RID: 1281
		// (get) Token: 0x06000F83 RID: 3971 RVA: 0x000404D0 File Offset: 0x0003E6D0
		// (set) Token: 0x06000F84 RID: 3972 RVA: 0x000404D8 File Offset: 0x0003E6D8
		[DataSourceProperty]
		public string CurrentSelectedAmountTitle
		{
			get
			{
				return this._currentSelectedAmountTitle;
			}
			set
			{
				if (value != this._currentSelectedAmountTitle)
				{
					this._currentSelectedAmountTitle = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentSelectedAmountTitle");
				}
			}
		}

		// Token: 0x04000702 RID: 1794
		private readonly Action<TroopRoster> _onDone;

		// Token: 0x04000703 RID: 1795
		private readonly TroopRoster _fullRoster;

		// Token: 0x04000704 RID: 1796
		private readonly TroopRoster _initialSelections;

		// Token: 0x04000705 RID: 1797
		private readonly Func<CharacterObject, bool> _canChangeChangeStatusOfTroop;

		// Token: 0x04000706 RID: 1798
		private readonly int _maxSelectableTroopCount;

		// Token: 0x04000707 RID: 1799
		private readonly int _minSelectableTroopCount;

		// Token: 0x04000708 RID: 1800
		private readonly TextObject _titleTextObject = new TextObject("{=uQgNPJnc}Manage Troops", null);

		// Token: 0x04000709 RID: 1801
		private readonly TextObject _chosenTitleTextObject = new TextObject("{=InqmgBiF}Chosen Crew", null);

		// Token: 0x0400070A RID: 1802
		private int _currentTotalSelectedTroopCount;

		// Token: 0x0400070B RID: 1803
		public bool IsFiveStackModifierActive;

		// Token: 0x0400070C RID: 1804
		public bool IsEntireStackModifierActive;

		// Token: 0x0400070D RID: 1805
		private InputKeyItemVM _doneInputKey;

		// Token: 0x0400070E RID: 1806
		private InputKeyItemVM _cancelInputKey;

		// Token: 0x0400070F RID: 1807
		private InputKeyItemVM _resetInputKey;

		// Token: 0x04000710 RID: 1808
		private bool _isEnabled;

		// Token: 0x04000711 RID: 1809
		private bool _isDoneEnabled;

		// Token: 0x04000712 RID: 1810
		private HintViewModel _doneHint;

		// Token: 0x04000713 RID: 1811
		private string _doneText;

		// Token: 0x04000714 RID: 1812
		private string _cancelText;

		// Token: 0x04000715 RID: 1813
		private string _titleText;

		// Token: 0x04000716 RID: 1814
		private string _clearSelectionText;

		// Token: 0x04000717 RID: 1815
		private string _currentSelectedAmountText;

		// Token: 0x04000718 RID: 1816
		private string _currentSelectedAmountTitle;

		// Token: 0x04000719 RID: 1817
		private MBBindingList<TroopSelectionItemVM> _troops;
	}
}
