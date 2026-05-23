using System;
using System.Linq;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign
{
	// Token: 0x020000FF RID: 255
	public class CraftingHistoryVM : ViewModel
	{
		// Token: 0x06001710 RID: 5904 RVA: 0x00058EA8 File Offset: 0x000570A8
		public CraftingHistoryVM(Crafting crafting, ICraftingCampaignBehavior craftingBehavior, Func<CraftingOrder> getActiveOrder, Action<WeaponDesignSelectorVM> onDone)
		{
			this._crafting = crafting;
			this._craftingBehavior = craftingBehavior;
			this._getActiveOrder = getActiveOrder;
			this._onDone = onDone;
			this.CraftingHistory = new MBBindingList<WeaponDesignSelectorVM>();
			this.HistoryHint = new HintViewModel(CraftingHistoryVM._craftingHistoryText, null);
			this.HistoryDisabledHint = new HintViewModel(CraftingHistoryVM._noItemsHint, null);
			this.RefreshValues();
		}

		// Token: 0x06001711 RID: 5905 RVA: 0x00058F0C File Offset: 0x0005710C
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.TitleText = CraftingHistoryVM._craftingHistoryText.ToString();
			this.DoneText = GameTexts.FindText("str_done", null).ToString();
			this.CancelText = GameTexts.FindText("str_cancel", null).ToString();
			this.RefreshAvailability();
		}

		// Token: 0x06001712 RID: 5906 RVA: 0x00058F64 File Offset: 0x00057164
		private void RefreshCraftingHistory()
		{
			this.FinalizeHistory();
			CraftingOrder craftingOrder = this._getActiveOrder();
			foreach (WeaponDesign weaponDesign in this._craftingBehavior.CraftingHistory)
			{
				if (craftingOrder == null || weaponDesign.Template.TemplateName.ToString() == craftingOrder.PreCraftedWeaponDesignItem.WeaponDesign.Template.TemplateName.ToString())
				{
					this.CraftingHistory.Add(new WeaponDesignSelectorVM(weaponDesign, new Action<WeaponDesignSelectorVM>(this.ExecuteSelect)));
				}
			}
			this.HasItemsInHistory = this.CraftingHistory.Count > 0;
			this.ExecuteSelect(null);
		}

		// Token: 0x06001713 RID: 5907 RVA: 0x00059030 File Offset: 0x00057230
		private void FinalizeHistory()
		{
			if (this.CraftingHistory.Count > 0)
			{
				foreach (WeaponDesignSelectorVM weaponDesignSelectorVM in this.CraftingHistory)
				{
					weaponDesignSelectorVM.OnFinalize();
				}
			}
			this.CraftingHistory.Clear();
		}

		// Token: 0x06001714 RID: 5908 RVA: 0x00059094 File Offset: 0x00057294
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.FinalizeHistory();
			this.DoneKey.OnFinalize();
			this.CancelKey.OnFinalize();
		}

		// Token: 0x06001715 RID: 5909 RVA: 0x000590B8 File Offset: 0x000572B8
		public void RefreshAvailability()
		{
			CraftingOrder activeOrder = this._getActiveOrder();
			this.HasItemsInHistory = ((activeOrder == null) ? (this._craftingBehavior.CraftingHistory.Count > 0) : this._craftingBehavior.CraftingHistory.Any((WeaponDesign x) => x.Template.StringId == activeOrder.PreCraftedWeaponDesignItem.WeaponDesign.Template.StringId));
		}

		// Token: 0x06001716 RID: 5910 RVA: 0x0005911B File Offset: 0x0005731B
		public void ExecuteOpen()
		{
			this.RefreshCraftingHistory();
			this.IsVisible = true;
		}

		// Token: 0x06001717 RID: 5911 RVA: 0x0005912A File Offset: 0x0005732A
		public void ExecuteCancel()
		{
			this.IsVisible = false;
		}

		// Token: 0x06001718 RID: 5912 RVA: 0x00059133 File Offset: 0x00057333
		public void ExecuteDone()
		{
			Action<WeaponDesignSelectorVM> onDone = this._onDone;
			if (onDone != null)
			{
				onDone(this.SelectedDesign);
			}
			this.ExecuteCancel();
		}

		// Token: 0x06001719 RID: 5913 RVA: 0x00059152 File Offset: 0x00057352
		private void ExecuteSelect(WeaponDesignSelectorVM selector)
		{
			this.IsDoneAvailable = selector != null;
			if (this.SelectedDesign != null)
			{
				this.SelectedDesign.IsSelected = false;
			}
			this.SelectedDesign = selector;
			if (this.SelectedDesign != null)
			{
				this.SelectedDesign.IsSelected = true;
			}
		}

		// Token: 0x170007AE RID: 1966
		// (get) Token: 0x0600171A RID: 5914 RVA: 0x0005918D File Offset: 0x0005738D
		// (set) Token: 0x0600171B RID: 5915 RVA: 0x00059195 File Offset: 0x00057395
		[DataSourceProperty]
		public bool IsDoneAvailable
		{
			get
			{
				return this._isDoneAvailable;
			}
			set
			{
				if (value != this._isDoneAvailable)
				{
					this._isDoneAvailable = value;
					base.OnPropertyChangedWithValue(value, "IsDoneAvailable");
				}
			}
		}

		// Token: 0x170007AF RID: 1967
		// (get) Token: 0x0600171C RID: 5916 RVA: 0x000591B3 File Offset: 0x000573B3
		// (set) Token: 0x0600171D RID: 5917 RVA: 0x000591BB File Offset: 0x000573BB
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

		// Token: 0x170007B0 RID: 1968
		// (get) Token: 0x0600171E RID: 5918 RVA: 0x000591D9 File Offset: 0x000573D9
		// (set) Token: 0x0600171F RID: 5919 RVA: 0x000591E1 File Offset: 0x000573E1
		[DataSourceProperty]
		public bool HasItemsInHistory
		{
			get
			{
				return this._hasItemsInHistory;
			}
			set
			{
				if (value != this._hasItemsInHistory)
				{
					this._hasItemsInHistory = value;
					base.OnPropertyChangedWithValue(value, "HasItemsInHistory");
				}
			}
		}

		// Token: 0x170007B1 RID: 1969
		// (get) Token: 0x06001720 RID: 5920 RVA: 0x000591FF File Offset: 0x000573FF
		// (set) Token: 0x06001721 RID: 5921 RVA: 0x00059207 File Offset: 0x00057407
		[DataSourceProperty]
		public HintViewModel HistoryHint
		{
			get
			{
				return this._historyHint;
			}
			set
			{
				if (value != this._historyHint)
				{
					this._historyHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "HistoryHint");
				}
			}
		}

		// Token: 0x170007B2 RID: 1970
		// (get) Token: 0x06001722 RID: 5922 RVA: 0x00059225 File Offset: 0x00057425
		// (set) Token: 0x06001723 RID: 5923 RVA: 0x0005922D File Offset: 0x0005742D
		[DataSourceProperty]
		public HintViewModel HistoryDisabledHint
		{
			get
			{
				return this._historyDisabledHint;
			}
			set
			{
				if (value != this._historyDisabledHint)
				{
					this._historyDisabledHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "HistoryDisabledHint");
				}
			}
		}

		// Token: 0x170007B3 RID: 1971
		// (get) Token: 0x06001724 RID: 5924 RVA: 0x0005924B File Offset: 0x0005744B
		// (set) Token: 0x06001725 RID: 5925 RVA: 0x00059253 File Offset: 0x00057453
		[DataSourceProperty]
		public MBBindingList<WeaponDesignSelectorVM> CraftingHistory
		{
			get
			{
				return this._craftingHistory;
			}
			set
			{
				if (value != this._craftingHistory)
				{
					this._craftingHistory = value;
					base.OnPropertyChangedWithValue<MBBindingList<WeaponDesignSelectorVM>>(value, "CraftingHistory");
				}
			}
		}

		// Token: 0x170007B4 RID: 1972
		// (get) Token: 0x06001726 RID: 5926 RVA: 0x00059271 File Offset: 0x00057471
		// (set) Token: 0x06001727 RID: 5927 RVA: 0x00059279 File Offset: 0x00057479
		[DataSourceProperty]
		public WeaponDesignSelectorVM SelectedDesign
		{
			get
			{
				return this._selectedDesign;
			}
			set
			{
				if (value != this._selectedDesign)
				{
					this._selectedDesign = value;
					base.OnPropertyChangedWithValue<WeaponDesignSelectorVM>(value, "SelectedDesign");
				}
			}
		}

		// Token: 0x170007B5 RID: 1973
		// (get) Token: 0x06001728 RID: 5928 RVA: 0x00059297 File Offset: 0x00057497
		// (set) Token: 0x06001729 RID: 5929 RVA: 0x0005929F File Offset: 0x0005749F
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

		// Token: 0x170007B6 RID: 1974
		// (get) Token: 0x0600172A RID: 5930 RVA: 0x000592C2 File Offset: 0x000574C2
		// (set) Token: 0x0600172B RID: 5931 RVA: 0x000592CA File Offset: 0x000574CA
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

		// Token: 0x170007B7 RID: 1975
		// (get) Token: 0x0600172C RID: 5932 RVA: 0x000592ED File Offset: 0x000574ED
		// (set) Token: 0x0600172D RID: 5933 RVA: 0x000592F5 File Offset: 0x000574F5
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

		// Token: 0x0600172E RID: 5934 RVA: 0x00059318 File Offset: 0x00057518
		public void SetDoneKey(HotKey hotkey)
		{
			this.DoneKey = InputKeyItemVM.CreateFromHotKey(hotkey, true);
		}

		// Token: 0x0600172F RID: 5935 RVA: 0x00059327 File Offset: 0x00057527
		public void SetCancelKey(HotKey hotkey)
		{
			this.CancelKey = InputKeyItemVM.CreateFromHotKey(hotkey, true);
		}

		// Token: 0x170007B8 RID: 1976
		// (get) Token: 0x06001730 RID: 5936 RVA: 0x00059336 File Offset: 0x00057536
		// (set) Token: 0x06001731 RID: 5937 RVA: 0x0005933E File Offset: 0x0005753E
		public InputKeyItemVM CancelKey
		{
			get
			{
				return this._cancelKey;
			}
			set
			{
				if (value != this._cancelKey)
				{
					this._cancelKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "CancelKey");
				}
			}
		}

		// Token: 0x170007B9 RID: 1977
		// (get) Token: 0x06001732 RID: 5938 RVA: 0x0005935C File Offset: 0x0005755C
		// (set) Token: 0x06001733 RID: 5939 RVA: 0x00059364 File Offset: 0x00057564
		public InputKeyItemVM DoneKey
		{
			get
			{
				return this._doneKey;
			}
			set
			{
				if (value != this._doneKey)
				{
					this._doneKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "DoneKey");
				}
			}
		}

		// Token: 0x04000A8C RID: 2700
		private static TextObject _noItemsHint = new TextObject("{=saHYZKLt}There are no available items in history", null);

		// Token: 0x04000A8D RID: 2701
		private static TextObject _craftingHistoryText = new TextObject("{=xW4BPVLX}Crafting History", null);

		// Token: 0x04000A8E RID: 2702
		private ICraftingCampaignBehavior _craftingBehavior;

		// Token: 0x04000A8F RID: 2703
		private Func<CraftingOrder> _getActiveOrder;

		// Token: 0x04000A90 RID: 2704
		private Action<WeaponDesignSelectorVM> _onDone;

		// Token: 0x04000A91 RID: 2705
		private Crafting _crafting;

		// Token: 0x04000A92 RID: 2706
		private bool _isDoneAvailable;

		// Token: 0x04000A93 RID: 2707
		private bool _isVisible;

		// Token: 0x04000A94 RID: 2708
		private bool _hasItemsInHistory;

		// Token: 0x04000A95 RID: 2709
		private HintViewModel _historyHint;

		// Token: 0x04000A96 RID: 2710
		private HintViewModel _historyDisabledHint;

		// Token: 0x04000A97 RID: 2711
		private MBBindingList<WeaponDesignSelectorVM> _craftingHistory;

		// Token: 0x04000A98 RID: 2712
		private WeaponDesignSelectorVM _selectedDesign;

		// Token: 0x04000A99 RID: 2713
		private string _titleText;

		// Token: 0x04000A9A RID: 2714
		private string _doneText;

		// Token: 0x04000A9B RID: 2715
		private string _cancelText;

		// Token: 0x04000A9C RID: 2716
		private InputKeyItemVM _cancelKey;

		// Token: 0x04000A9D RID: 2717
		private InputKeyItemVM _doneKey;
	}
}
