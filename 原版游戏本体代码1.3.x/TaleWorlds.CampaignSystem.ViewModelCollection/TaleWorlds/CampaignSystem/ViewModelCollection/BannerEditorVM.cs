using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.BannerEditor;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection
{
	// Token: 0x02000006 RID: 6
	public class BannerEditorVM : ViewModel
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x0600001C RID: 28 RVA: 0x000022DE File Offset: 0x000004DE
		public BasicCharacterObject Character { get; }

		// Token: 0x0600001D RID: 29 RVA: 0x000022E8 File Offset: 0x000004E8
		public BannerEditorVM(BasicCharacterObject character, Banner banner, Action<bool> onExit, Action refresh, int currentStageIndex, int totalStagesCount, int furthestIndex, Action<int> goToIndex)
		{
			this.Character = character;
			this._initialBanner = banner.Serialize();
			this._banner = banner;
			this.IconsList = new MBBindingList<BannerIconVM>();
			this.PrimaryColorList = new MBBindingList<BannerColorVM>();
			this.SigilColorList = new MBBindingList<BannerColorVM>();
			this._refresh = refresh;
			this.OnExit = onExit;
			this.BannerVM = new BannerViewModel(banner);
			this.CanChangeBackgroundColor = true;
			this._shield = this.FindShield();
			if (this._shield != null)
			{
				this.ShieldRosterElement = new ItemRosterElement(this._shield, 1, null);
			}
			else
			{
				Debug.FailedAssert("Banner Editor couldn't find a shield to show", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\BannerEditorVM.cs", ".ctor", 55);
			}
			this._goToIndex = goToIndex;
			this.TotalStageCount = totalStagesCount;
			this.CurrentStageIndex = currentStageIndex;
			this.FurthestIndex = furthestIndex;
			this.CameraControlKeys = new MBBindingList<InputKeyItemVM>();
			this.MinIconSize = 100;
			this.MaxIconSize = 700;
			this.RefreshValues();
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00002410 File Offset: 0x00000610
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Title = GameTexts.FindText("str_banner_editor", null).ToString();
			this.CancelText = GameTexts.FindText("str_cancel", null).ToString();
			this.DoneText = GameTexts.FindText("str_done", null).ToString();
			this.ResetHint = new HintViewModel(GameTexts.FindText("str_reset_icon", null), null);
			this.RandomizeHint = new HintViewModel(GameTexts.FindText("str_randomize", null), null);
			this.UndoHint = new HintViewModel(GameTexts.FindText("str_undo", null), null);
			this.RedoHint = new HintViewModel(GameTexts.FindText("str_redo", null), null);
			this.PrimaryColorText = new TextObject("{=xwRWjlar}Background Color:", null).ToString();
			this.SigilColorText = new TextObject("{=7tBOCHm6}Sigil Color:", null).ToString();
			this.SizeText = new TextObject("{=OkWLI5C8}Size:", null).ToString();
			this.CategoryNames = new MBBindingList<HintViewModel>();
			foreach (BannerIconGroup bannerIconGroup in BannerManager.Instance.BannerIconGroups)
			{
				if (!bannerIconGroup.IsPattern)
				{
					foreach (KeyValuePair<int, BannerIconData> keyValuePair in bannerIconGroup.AvailableIcons)
					{
						BannerIconVM bannerIconVM = new BannerIconVM(keyValuePair.Key, new Action<BannerIconVM>(this.OnIconSelection));
						this.IconsList.Add(bannerIconVM);
						bannerIconVM.IsSelected = bannerIconVM.IconID == this._banner.GetIconMeshId();
					}
					this.CategoryNames.Add(new HintViewModel(bannerIconGroup.Name, "banner_group_hint_" + bannerIconGroup.Id));
				}
			}
			bool flag = this.IsColorsSwitched();
			foreach (KeyValuePair<int, BannerColor> keyValuePair2 in BannerManager.Instance.ReadOnlyColorPalette)
			{
				bool flag2 = (flag ? keyValuePair2.Value.PlayerCanChooseForSigil : keyValuePair2.Value.PlayerCanChooseForBackground);
				bool flag3 = (flag ? keyValuePair2.Value.PlayerCanChooseForBackground : keyValuePair2.Value.PlayerCanChooseForSigil);
				if (flag2)
				{
					BannerColorVM bannerColorVM = new BannerColorVM(keyValuePair2.Key, keyValuePair2.Value.Color, new Action<BannerColorVM>(this.OnPrimaryColorSelection));
					if (bannerColorVM.ColorID == this._banner.GetPrimaryColorId())
					{
						bannerColorVM.IsSelected = true;
						this._currentSelectedPrimaryColor = bannerColorVM;
					}
					this.PrimaryColorList.Add(bannerColorVM);
				}
				if (flag3)
				{
					BannerColorVM bannerColorVM2 = new BannerColorVM(keyValuePair2.Key, keyValuePair2.Value.Color, new Action<BannerColorVM>(this.OnSigilColorSelection));
					if (bannerColorVM2.ColorID == this._banner.GetIconColorId())
					{
						bannerColorVM2.IsSelected = true;
						this._currentSelectedSigilColor = bannerColorVM2;
					}
					this.SigilColorList.Add(bannerColorVM2);
				}
			}
			this.CurrentIconSize = (int)this._banner.GetIconSize().X;
			this._initialized = true;
		}

		// Token: 0x0600001F RID: 31 RVA: 0x000027A8 File Offset: 0x000009A8
		public void RefreshSelectedColorsAndSigils()
		{
			int iconMeshId = this.BannerVM.Banner.GetIconMeshId();
			int primaryColorId = this._banner.GetPrimaryColorId();
			int iconColorId = this._banner.GetIconColorId();
			for (int i = 0; i < this.IconsList.Count; i++)
			{
				BannerIconVM bannerIconVM = this.IconsList[i];
				bannerIconVM.IsSelected = bannerIconVM.IconID == iconMeshId;
			}
			if (!BannerEditorVM.DoesListHaveColor(this.PrimaryColorList, primaryColorId) || !BannerEditorVM.DoesListHaveColor(this.SigilColorList, iconColorId))
			{
				this.ExecuteSwitchColors();
			}
			if (!BannerEditorVM.DoesListHaveColor(this.PrimaryColorList, primaryColorId) || !BannerEditorVM.DoesListHaveColor(this.SigilColorList, iconColorId))
			{
				Debug.FailedAssert("Color lists do not contain banner colors", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\BannerEditorVM.cs", "RefreshSelectedColorsAndSigils", 160);
				return;
			}
			for (int j = 0; j < this.PrimaryColorList.Count; j++)
			{
				BannerColorVM bannerColorVM = this.PrimaryColorList[j];
				bannerColorVM.IsSelected = bannerColorVM.ColorID == primaryColorId;
			}
			for (int k = 0; k < this.SigilColorList.Count; k++)
			{
				BannerColorVM bannerColorVM2 = this.SigilColorList[k];
				bannerColorVM2.IsSelected = bannerColorVM2.ColorID == iconColorId;
			}
		}

		// Token: 0x06000020 RID: 32 RVA: 0x000028D0 File Offset: 0x00000AD0
		private static bool DoesListHaveColor(MBBindingList<BannerColorVM> list, int color)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].ColorID == color)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002900 File Offset: 0x00000B00
		private bool IsColorsSwitched()
		{
			foreach (KeyValuePair<int, BannerColor> keyValuePair in BannerManager.Instance.ReadOnlyColorPalette)
			{
				if (keyValuePair.Value.PlayerCanChooseForBackground && keyValuePair.Key == this._banner.GetPrimaryColorId())
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06000022 RID: 34 RVA: 0x0000297C File Offset: 0x00000B7C
		public void SetClanRelatedRules(bool canChangeBackgroundColor)
		{
			this.CanChangeBackgroundColor = canChangeBackgroundColor;
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002988 File Offset: 0x00000B88
		private void OnIconSelection(BannerIconVM icon)
		{
			if (icon != this._currentSelectedIcon)
			{
				if (this._currentSelectedIcon != null)
				{
					this._currentSelectedIcon.IsSelected = false;
				}
				this._currentSelectedIcon = icon;
				icon.IsSelected = true;
				this.BannerVM.Banner.SetIconMeshId(icon.IconID);
				this._refresh();
			}
		}

		// Token: 0x06000024 RID: 36 RVA: 0x000029E4 File Offset: 0x00000BE4
		public void ExecuteSwitchColors()
		{
			if (this._currentSelectedPrimaryColor == null || this._currentSelectedSigilColor == null)
			{
				Debug.FailedAssert("Couldn't find current player clan colors in the list of selectable banner editor colors.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\BannerEditorVM.cs", "ExecuteSwitchColors", 230);
				return;
			}
			MBBindingList<BannerColorVM> primaryColorList = this.PrimaryColorList;
			this.PrimaryColorList = this.SigilColorList;
			this.SigilColorList = primaryColorList;
			this.PrimaryColorList.ApplyActionOnAllItems(delegate(BannerColorVM x)
			{
				x.SetOnSelectionAction(new Action<BannerColorVM>(this.OnPrimaryColorSelection));
			});
			this.SigilColorList.ApplyActionOnAllItems(delegate(BannerColorVM x)
			{
				x.SetOnSelectionAction(new Action<BannerColorVM>(this.OnSigilColorSelection));
			});
			BannerColorVM currentSelectedPrimaryColor = this._currentSelectedPrimaryColor;
			this._currentSelectedPrimaryColor = this._currentSelectedSigilColor;
			this._currentSelectedSigilColor = currentSelectedPrimaryColor;
			this._currentSelectedPrimaryColor.IsSelected = true;
			this._currentSelectedSigilColor.IsSelected = true;
			this.BannerVM.Banner.SetPrimaryColorId(this._currentSelectedPrimaryColor.ColorID);
			this.BannerVM.Banner.SetSecondaryColorId(this._currentSelectedPrimaryColor.ColorID);
			this.BannerVM.Banner.SetIconColorId(this._currentSelectedSigilColor.ColorID);
			this._refresh();
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002AF4 File Offset: 0x00000CF4
		private void OnPrimaryColorSelection(BannerColorVM color)
		{
			if (color != this._currentSelectedPrimaryColor)
			{
				if (this._currentSelectedPrimaryColor != null)
				{
					this._currentSelectedPrimaryColor.IsSelected = false;
				}
				this._currentSelectedPrimaryColor = color;
				color.IsSelected = true;
				this.BannerVM.Banner.SetPrimaryColorId(color.ColorID);
				this.BannerVM.Banner.SetSecondaryColorId(color.ColorID);
				this._refresh();
			}
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002B64 File Offset: 0x00000D64
		private void OnSigilColorSelection(BannerColorVM color)
		{
			if (color != this._currentSelectedSigilColor)
			{
				if (this._currentSelectedSigilColor != null)
				{
					this._currentSelectedSigilColor.IsSelected = false;
				}
				this._currentSelectedSigilColor = color;
				color.IsSelected = true;
				this.BannerVM.Banner.SetIconColorId(color.ColorID);
				this._refresh();
			}
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00002BBD File Offset: 0x00000DBD
		private void OnBannerIconSizeChange(int newSize)
		{
			this.BannerVM.Banner.SetIconSize(newSize);
			this._refresh();
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00002BDB File Offset: 0x00000DDB
		public void ExecuteDone()
		{
			this.OnExit(false);
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00002BE9 File Offset: 0x00000DE9
		public void ExecuteCancel()
		{
			this._banner.Deserialize(this._initialBanner);
			this.OnExit(true);
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00002C08 File Offset: 0x00000E08
		private ItemObject FindShield()
		{
			for (int i = 0; i < 4; i++)
			{
				EquipmentElement equipmentFromSlot = this.Character.Equipment.GetEquipmentFromSlot((EquipmentIndex)i);
				ItemObject item = equipmentFromSlot.Item;
				if (((item != null) ? item.PrimaryWeapon : null) != null && equipmentFromSlot.Item.PrimaryWeapon.IsShield && equipmentFromSlot.Item.IsUsingTableau)
				{
					return equipmentFromSlot.Item;
				}
			}
			foreach (ItemObject itemObject in Game.Current.ObjectManager.GetObjectTypeList<ItemObject>())
			{
				if (itemObject.PrimaryWeapon != null && itemObject.PrimaryWeapon.IsShield && itemObject.IsUsingTableau)
				{
					return itemObject;
				}
			}
			return null;
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00002CE0 File Offset: 0x00000EE0
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
			for (int i = 0; i < this.CameraControlKeys.Count; i++)
			{
				this.CameraControlKeys[i].OnFinalize();
			}
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00002D3C File Offset: 0x00000F3C
		public void SetCancelInputKey(HotKey hotKey)
		{
			this.CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00002D4B File Offset: 0x00000F4B
		public void SetDoneInputKey(HotKey hotKey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x0600002E RID: 46 RVA: 0x00002D5C File Offset: 0x00000F5C
		public void AddCameraControlInputKey(HotKey hotKey)
		{
			InputKeyItemVM item = InputKeyItemVM.CreateFromHotKey(hotKey, true);
			this.CameraControlKeys.Add(item);
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00002D80 File Offset: 0x00000F80
		public void AddCameraControlInputKey(GameKey gameKey)
		{
			InputKeyItemVM item = InputKeyItemVM.CreateFromGameKey(gameKey, true);
			this.CameraControlKeys.Add(item);
		}

		// Token: 0x06000030 RID: 48 RVA: 0x00002DA4 File Offset: 0x00000FA4
		public void AddCameraControlInputKey(GameAxisKey gameAxisKey, TextObject keyName)
		{
			InputKeyItemVM item = InputKeyItemVM.CreateFromForcedID(gameAxisKey.AxisKey.ToString(), keyName, true);
			this.CameraControlKeys.Add(item);
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000031 RID: 49 RVA: 0x00002DD0 File Offset: 0x00000FD0
		// (set) Token: 0x06000032 RID: 50 RVA: 0x00002DD8 File Offset: 0x00000FD8
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

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000033 RID: 51 RVA: 0x00002DF6 File Offset: 0x00000FF6
		// (set) Token: 0x06000034 RID: 52 RVA: 0x00002DFE File Offset: 0x00000FFE
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

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000035 RID: 53 RVA: 0x00002E1C File Offset: 0x0000101C
		// (set) Token: 0x06000036 RID: 54 RVA: 0x00002E24 File Offset: 0x00001024
		[DataSourceProperty]
		public MBBindingList<InputKeyItemVM> CameraControlKeys
		{
			get
			{
				return this._cameraControlKeys;
			}
			set
			{
				if (value != this._cameraControlKeys)
				{
					this._cameraControlKeys = value;
					base.OnPropertyChangedWithValue<MBBindingList<InputKeyItemVM>>(value, "CameraControlKeys");
				}
			}
		}

		// Token: 0x06000037 RID: 55 RVA: 0x00002E42 File Offset: 0x00001042
		public void ExecuteGoToIndex(int index)
		{
			this._goToIndex(index);
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000038 RID: 56 RVA: 0x00002E50 File Offset: 0x00001050
		// (set) Token: 0x06000039 RID: 57 RVA: 0x00002E58 File Offset: 0x00001058
		[DataSourceProperty]
		public MBBindingList<HintViewModel> CategoryNames
		{
			get
			{
				return this._categoryNames;
			}
			set
			{
				if (value != this._categoryNames)
				{
					this._categoryNames = value;
					base.OnPropertyChangedWithValue<MBBindingList<HintViewModel>>(value, "CategoryNames");
				}
			}
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600003A RID: 58 RVA: 0x00002E76 File Offset: 0x00001076
		// (set) Token: 0x0600003B RID: 59 RVA: 0x00002E7E File Offset: 0x0000107E
		[DataSourceProperty]
		public MBBindingList<BannerIconVM> IconsList
		{
			get
			{
				return this._iconsList;
			}
			set
			{
				if (value != this._iconsList)
				{
					this._iconsList = value;
					base.OnPropertyChangedWithValue<MBBindingList<BannerIconVM>>(value, "IconsList");
				}
			}
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x0600003C RID: 60 RVA: 0x00002E9C File Offset: 0x0000109C
		// (set) Token: 0x0600003D RID: 61 RVA: 0x00002EA4 File Offset: 0x000010A4
		[DataSourceProperty]
		public MBBindingList<BannerColorVM> PrimaryColorList
		{
			get
			{
				return this._primaryColorList;
			}
			set
			{
				if (value != this._primaryColorList)
				{
					this._primaryColorList = value;
					base.OnPropertyChangedWithValue<MBBindingList<BannerColorVM>>(value, "PrimaryColorList");
				}
			}
		}

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x0600003E RID: 62 RVA: 0x00002EC2 File Offset: 0x000010C2
		// (set) Token: 0x0600003F RID: 63 RVA: 0x00002ECA File Offset: 0x000010CA
		[DataSourceProperty]
		public MBBindingList<BannerColorVM> SigilColorList
		{
			get
			{
				return this._sigilColorList;
			}
			set
			{
				if (value != this._sigilColorList)
				{
					this._sigilColorList = value;
					base.OnPropertyChangedWithValue<MBBindingList<BannerColorVM>>(value, "SigilColorList");
				}
			}
		}

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000040 RID: 64 RVA: 0x00002EE8 File Offset: 0x000010E8
		// (set) Token: 0x06000041 RID: 65 RVA: 0x00002EF0 File Offset: 0x000010F0
		[DataSourceProperty]
		public HintViewModel RandomizeHint
		{
			get
			{
				return this._randomizeHint;
			}
			set
			{
				if (value != this._randomizeHint)
				{
					this._randomizeHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "RandomizeHint");
				}
			}
		}

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000042 RID: 66 RVA: 0x00002F0E File Offset: 0x0000110E
		// (set) Token: 0x06000043 RID: 67 RVA: 0x00002F16 File Offset: 0x00001116
		[DataSourceProperty]
		public HintViewModel UndoHint
		{
			get
			{
				return this._undoHint;
			}
			set
			{
				if (value != this._undoHint)
				{
					this._undoHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "UndoHint");
				}
			}
		}

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000044 RID: 68 RVA: 0x00002F34 File Offset: 0x00001134
		// (set) Token: 0x06000045 RID: 69 RVA: 0x00002F3C File Offset: 0x0000113C
		[DataSourceProperty]
		public HintViewModel RedoHint
		{
			get
			{
				return this._redoHint;
			}
			set
			{
				if (value != this._redoHint)
				{
					this._redoHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "RedoHint");
				}
			}
		}

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000046 RID: 70 RVA: 0x00002F5A File Offset: 0x0000115A
		// (set) Token: 0x06000047 RID: 71 RVA: 0x00002F62 File Offset: 0x00001162
		[DataSourceProperty]
		public HintViewModel ResetHint
		{
			get
			{
				return this._resetHint;
			}
			set
			{
				if (value != this._resetHint)
				{
					this._resetHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ResetHint");
				}
			}
		}

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x06000048 RID: 72 RVA: 0x00002F80 File Offset: 0x00001180
		// (set) Token: 0x06000049 RID: 73 RVA: 0x00002F88 File Offset: 0x00001188
		[DataSourceProperty]
		public string CurrentShieldName
		{
			get
			{
				return this._currentShieldName;
			}
			set
			{
				if (value != this._currentShieldName)
				{
					this._currentShieldName = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentShieldName");
				}
			}
		}

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x0600004A RID: 74 RVA: 0x00002FAB File Offset: 0x000011AB
		// (set) Token: 0x0600004B RID: 75 RVA: 0x00002FB3 File Offset: 0x000011B3
		[DataSourceProperty]
		public int MinIconSize
		{
			get
			{
				return this._minIconSize;
			}
			set
			{
				if (value != this._minIconSize)
				{
					this._minIconSize = value;
					base.OnPropertyChangedWithValue(value, "MinIconSize");
				}
			}
		}

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x0600004C RID: 76 RVA: 0x00002FD1 File Offset: 0x000011D1
		// (set) Token: 0x0600004D RID: 77 RVA: 0x00002FD9 File Offset: 0x000011D9
		[DataSourceProperty]
		public int MaxIconSize
		{
			get
			{
				return this._maxIconSize;
			}
			set
			{
				if (value != this._maxIconSize)
				{
					this._maxIconSize = value;
					base.OnPropertyChangedWithValue(value, "MaxIconSize");
				}
			}
		}

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x0600004E RID: 78 RVA: 0x00002FF7 File Offset: 0x000011F7
		// (set) Token: 0x0600004F RID: 79 RVA: 0x00002FFF File Offset: 0x000011FF
		[DataSourceProperty]
		public int CurrentIconSize
		{
			get
			{
				return this._currentIconSize;
			}
			set
			{
				if (value != this._currentIconSize)
				{
					this._currentIconSize = value;
					base.OnPropertyChangedWithValue(value, "CurrentIconSize");
					if (this._initialized)
					{
						this.OnBannerIconSizeChange(value);
					}
				}
			}
		}

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000050 RID: 80 RVA: 0x0000302C File Offset: 0x0000122C
		// (set) Token: 0x06000051 RID: 81 RVA: 0x00003034 File Offset: 0x00001234
		[DataSourceProperty]
		public string PrimaryColorText
		{
			get
			{
				return this._primaryColorText;
			}
			set
			{
				if (value != this._primaryColorText)
				{
					this._primaryColorText = value;
					base.OnPropertyChangedWithValue<string>(value, "PrimaryColorText");
				}
			}
		}

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000052 RID: 82 RVA: 0x00003057 File Offset: 0x00001257
		// (set) Token: 0x06000053 RID: 83 RVA: 0x0000305F File Offset: 0x0000125F
		[DataSourceProperty]
		public string SizeText
		{
			get
			{
				return this._sizeText;
			}
			set
			{
				if (value != this._sizeText)
				{
					this._sizeText = value;
					base.OnPropertyChangedWithValue<string>(value, "SizeText");
				}
			}
		}

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x06000054 RID: 84 RVA: 0x00003082 File Offset: 0x00001282
		// (set) Token: 0x06000055 RID: 85 RVA: 0x0000308A File Offset: 0x0000128A
		[DataSourceProperty]
		public string SigilColorText
		{
			get
			{
				return this._sigilColorText;
			}
			set
			{
				if (value != this._sigilColorText)
				{
					this._sigilColorText = value;
					base.OnPropertyChangedWithValue<string>(value, "SigilColorText");
				}
			}
		}

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x06000056 RID: 86 RVA: 0x000030AD File Offset: 0x000012AD
		// (set) Token: 0x06000057 RID: 87 RVA: 0x000030B5 File Offset: 0x000012B5
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

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x06000058 RID: 88 RVA: 0x000030D8 File Offset: 0x000012D8
		// (set) Token: 0x06000059 RID: 89 RVA: 0x000030E0 File Offset: 0x000012E0
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

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x0600005A RID: 90 RVA: 0x00003103 File Offset: 0x00001303
		// (set) Token: 0x0600005B RID: 91 RVA: 0x0000310B File Offset: 0x0000130B
		[DataSourceProperty]
		public BannerViewModel BannerVM
		{
			get
			{
				return this._bannerVM;
			}
			set
			{
				if (value != this._bannerVM)
				{
					this._bannerVM = value;
					base.OnPropertyChangedWithValue<BannerViewModel>(value, "BannerVM");
				}
			}
		}

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x0600005C RID: 92 RVA: 0x00003129 File Offset: 0x00001329
		// (set) Token: 0x0600005D RID: 93 RVA: 0x00003131 File Offset: 0x00001331
		[DataSourceProperty]
		public string IconCodes
		{
			get
			{
				return this._iconCodes;
			}
			set
			{
				if (value != this._iconCodes)
				{
					this._iconCodes = value;
					base.OnPropertyChangedWithValue<string>(value, "IconCodes");
				}
			}
		}

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x0600005E RID: 94 RVA: 0x00003154 File Offset: 0x00001354
		// (set) Token: 0x0600005F RID: 95 RVA: 0x0000315C File Offset: 0x0000135C
		[DataSourceProperty]
		public string ColorCodes
		{
			get
			{
				return this._colorCodes;
			}
			set
			{
				if (value != this._colorCodes)
				{
					this._colorCodes = value;
					base.OnPropertyChangedWithValue<string>(value, "ColorCodes");
				}
			}
		}

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x06000060 RID: 96 RVA: 0x0000317F File Offset: 0x0000137F
		// (set) Token: 0x06000061 RID: 97 RVA: 0x00003187 File Offset: 0x00001387
		[DataSourceProperty]
		public bool CanChangeBackgroundColor
		{
			get
			{
				return this._canChangeBackgroundColor;
			}
			set
			{
				if (value != this._canChangeBackgroundColor)
				{
					this._canChangeBackgroundColor = value;
					base.OnPropertyChangedWithValue(value, "CanChangeBackgroundColor");
				}
			}
		}

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x06000062 RID: 98 RVA: 0x000031A5 File Offset: 0x000013A5
		// (set) Token: 0x06000063 RID: 99 RVA: 0x000031AD File Offset: 0x000013AD
		[DataSourceProperty]
		public bool CharacterGamepadControlsEnabled
		{
			get
			{
				return this._characterGamepadControlsEnabled;
			}
			set
			{
				if (value != this._characterGamepadControlsEnabled)
				{
					this._characterGamepadControlsEnabled = value;
					base.OnPropertyChangedWithValue(value, "CharacterGamepadControlsEnabled");
				}
			}
		}

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x06000064 RID: 100 RVA: 0x000031CB File Offset: 0x000013CB
		// (set) Token: 0x06000065 RID: 101 RVA: 0x000031D3 File Offset: 0x000013D3
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

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x06000066 RID: 102 RVA: 0x000031F6 File Offset: 0x000013F6
		// (set) Token: 0x06000067 RID: 103 RVA: 0x000031FE File Offset: 0x000013FE
		[DataSourceProperty]
		public string Description
		{
			get
			{
				return this._description;
			}
			set
			{
				if (value != this._description)
				{
					this._description = value;
					base.OnPropertyChangedWithValue<string>(value, "Description");
				}
			}
		}

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000068 RID: 104 RVA: 0x00003221 File Offset: 0x00001421
		// (set) Token: 0x06000069 RID: 105 RVA: 0x00003229 File Offset: 0x00001429
		[DataSourceProperty]
		public int TotalStageCount
		{
			get
			{
				return this._totalStageCount;
			}
			set
			{
				if (value != this._totalStageCount)
				{
					this._totalStageCount = value;
					base.OnPropertyChangedWithValue(value, "TotalStageCount");
				}
			}
		}

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x0600006A RID: 106 RVA: 0x00003247 File Offset: 0x00001447
		// (set) Token: 0x0600006B RID: 107 RVA: 0x0000324F File Offset: 0x0000144F
		[DataSourceProperty]
		public int CurrentStageIndex
		{
			get
			{
				return this._currentStageIndex;
			}
			set
			{
				if (value != this._currentStageIndex)
				{
					this._currentStageIndex = value;
					base.OnPropertyChangedWithValue(value, "CurrentStageIndex");
				}
			}
		}

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x0600006C RID: 108 RVA: 0x0000326D File Offset: 0x0000146D
		// (set) Token: 0x0600006D RID: 109 RVA: 0x00003275 File Offset: 0x00001475
		[DataSourceProperty]
		public int FurthestIndex
		{
			get
			{
				return this._furthestIndex;
			}
			set
			{
				if (value != this._furthestIndex)
				{
					this._furthestIndex = value;
					base.OnPropertyChangedWithValue(value, "FurthestIndex");
				}
			}
		}

		// Token: 0x04000009 RID: 9
		private readonly string _initialBanner;

		// Token: 0x0400000A RID: 10
		public int ShieldSlotIndex = 3;

		// Token: 0x0400000B RID: 11
		public int CurrentShieldIndex;

		// Token: 0x0400000D RID: 13
		public ItemRosterElement ShieldRosterElement;

		// Token: 0x0400000E RID: 14
		private readonly Action<bool> OnExit;

		// Token: 0x0400000F RID: 15
		private readonly Action _refresh;

		// Token: 0x04000010 RID: 16
		private readonly ItemObject _shield;

		// Token: 0x04000011 RID: 17
		private readonly Banner _banner;

		// Token: 0x04000012 RID: 18
		private BannerIconVM _currentSelectedIcon;

		// Token: 0x04000013 RID: 19
		private BannerColorVM _currentSelectedPrimaryColor;

		// Token: 0x04000014 RID: 20
		private BannerColorVM _currentSelectedSigilColor;

		// Token: 0x04000015 RID: 21
		private readonly Action<int> _goToIndex;

		// Token: 0x04000016 RID: 22
		private InputKeyItemVM _cancelInputKey;

		// Token: 0x04000017 RID: 23
		private InputKeyItemVM _doneInputKey;

		// Token: 0x04000018 RID: 24
		private MBBindingList<InputKeyItemVM> _cameraControlKeys;

		// Token: 0x04000019 RID: 25
		private string _iconCodes;

		// Token: 0x0400001A RID: 26
		private string _colorCodes;

		// Token: 0x0400001B RID: 27
		private BannerViewModel _bannerVM;

		// Token: 0x0400001C RID: 28
		private MBBindingList<BannerIconVM> _iconsList;

		// Token: 0x0400001D RID: 29
		private MBBindingList<BannerColorVM> _primaryColorList;

		// Token: 0x0400001E RID: 30
		private MBBindingList<BannerColorVM> _sigilColorList;

		// Token: 0x0400001F RID: 31
		private string _cancelText;

		// Token: 0x04000020 RID: 32
		private string _doneText;

		// Token: 0x04000021 RID: 33
		private string _sizeText;

		// Token: 0x04000022 RID: 34
		private string _primaryColorText;

		// Token: 0x04000023 RID: 35
		private string _sigilColorText;

		// Token: 0x04000024 RID: 36
		private string _currentShieldName;

		// Token: 0x04000025 RID: 37
		private bool _canChangeBackgroundColor;

		// Token: 0x04000026 RID: 38
		private int _currentIconSize;

		// Token: 0x04000027 RID: 39
		private int _minIconSize;

		// Token: 0x04000028 RID: 40
		private int _maxIconSize;

		// Token: 0x04000029 RID: 41
		private HintViewModel _resetHint;

		// Token: 0x0400002A RID: 42
		private HintViewModel _randomizeHint;

		// Token: 0x0400002B RID: 43
		private HintViewModel _undoHint;

		// Token: 0x0400002C RID: 44
		private HintViewModel _redoHint;

		// Token: 0x0400002D RID: 45
		private MBBindingList<HintViewModel> _categoryNames;

		// Token: 0x0400002E RID: 46
		private bool _characterGamepadControlsEnabled;

		// Token: 0x0400002F RID: 47
		private string _title = "";

		// Token: 0x04000030 RID: 48
		private string _description = "";

		// Token: 0x04000031 RID: 49
		private int _totalStageCount = -1;

		// Token: 0x04000032 RID: 50
		private int _currentStageIndex = -1;

		// Token: 0x04000033 RID: 51
		private int _furthestIndex = -1;

		// Token: 0x04000034 RID: 52
		private bool _initialized;
	}
}
