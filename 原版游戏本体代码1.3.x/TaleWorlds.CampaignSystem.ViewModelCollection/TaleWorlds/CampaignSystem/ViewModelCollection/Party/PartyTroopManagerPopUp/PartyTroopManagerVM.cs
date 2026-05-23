using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party.PartyTroopManagerPopUp
{
	// Token: 0x02000034 RID: 52
	public abstract class PartyTroopManagerVM : ViewModel
	{
		// Token: 0x06000507 RID: 1287 RVA: 0x0001C2F7 File Offset: 0x0001A4F7
		public virtual void ExecuteItemPrimaryAction()
		{
		}

		// Token: 0x06000508 RID: 1288 RVA: 0x0001C2F9 File Offset: 0x0001A4F9
		public virtual void ExecuteItemSecondaryAction()
		{
		}

		// Token: 0x06000509 RID: 1289 RVA: 0x0001C2FB File Offset: 0x0001A4FB
		public virtual void ExecuteItemTertiaryAction()
		{
		}

		// Token: 0x0600050A RID: 1290 RVA: 0x0001C300 File Offset: 0x0001A500
		public PartyTroopManagerVM(PartyVM partyVM)
		{
			this._partyVM = partyVM;
			this.Troops = new MBBindingList<PartyTroopManagerItemVM>();
			this.OpenButtonHint = new HintViewModel();
			this.UsedHorsesHint = new BasicTooltipViewModel();
			this.RefreshValues();
		}

		// Token: 0x0600050B RID: 1291 RVA: 0x0001C378 File Offset: 0x0001A578
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.AvatarText = new TextObject("{=5tbWdY1j}Avatar", null).ToString();
			this.NameText = new TextObject("{=PDdh1sBj}Name", null).ToString();
			this.CountText = new TextObject("{=zFDoDbNj}Count", null).ToString();
			this.DoneLbl = GameTexts.FindText("str_done", null).ToString();
			this.CancelLbl = GameTexts.FindText("str_cancel", null).ToString();
		}

		// Token: 0x0600050C RID: 1292 RVA: 0x0001C3FC File Offset: 0x0001A5FC
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.DoneInputKey.OnFinalize();
			this.CancelInputKey.OnFinalize();
			InputKeyItemVM primaryActionInputKey = this.PrimaryActionInputKey;
			if (primaryActionInputKey != null)
			{
				primaryActionInputKey.OnFinalize();
			}
			InputKeyItemVM secondaryActionInputKey = this.SecondaryActionInputKey;
			if (secondaryActionInputKey != null)
			{
				secondaryActionInputKey.OnFinalize();
			}
			InputKeyItemVM tertiaryActionInputKey = this.TertiaryActionInputKey;
			if (tertiaryActionInputKey == null)
			{
				return;
			}
			tertiaryActionInputKey.OnFinalize();
		}

		// Token: 0x0600050D RID: 1293 RVA: 0x0001C458 File Offset: 0x0001A658
		public virtual void OpenPopUp()
		{
			this._partyVM.PartyScreenLogic.SavePartyScreenData();
			this._initialGoldChange = this._partyVM.PartyScreenLogic.CurrentData.PartyGoldChangeAmount;
			this._initialHorseChange = this._partyVM.PartyScreenLogic.CurrentData.PartyHorseChangeAmount;
			this._initialMoraleChange = this._partyVM.PartyScreenLogic.CurrentData.PartyMoraleChangeAmount;
			this._initialUsedUpgradeHorsesHistory.Clear();
			foreach (Tuple<EquipmentElement, int> item in this._partyVM.PartyScreenLogic.CurrentData.UsedUpgradeHorsesHistory)
			{
				this._initialUsedUpgradeHorsesHistory.Add(item);
			}
			this.UpdateLabels();
			this._hasMadeChanges = false;
			this.IsOpen = true;
		}

		// Token: 0x0600050E RID: 1294 RVA: 0x0001C540 File Offset: 0x0001A740
		public virtual void ExecuteDone()
		{
			this.IsOpen = false;
		}

		// Token: 0x0600050F RID: 1295 RVA: 0x0001C549 File Offset: 0x0001A749
		protected virtual void ConfirmCancel()
		{
			this._partyVM.PartyScreenLogic.ResetToLastSavedPartyScreenData(false);
			this.IsOpen = false;
		}

		// Token: 0x06000510 RID: 1296 RVA: 0x0001C564 File Offset: 0x0001A764
		public void UpdateOpenButtonHint(bool isDisabled, bool isIrrelevant, bool isUpgradesDisabled)
		{
			TextObject hintText;
			if (isIrrelevant)
			{
				hintText = this._openButtonIrrelevantScreenHint;
			}
			else if (isUpgradesDisabled)
			{
				hintText = this._openButtonUpgradesDisabledHint;
			}
			else if (isDisabled)
			{
				hintText = this._openButtonNoTroopsHint;
			}
			else
			{
				hintText = this._openButtonEnabledHint;
			}
			this.OpenButtonHint.HintText = hintText;
		}

		// Token: 0x06000511 RID: 1297
		public abstract void ExecuteCancel();

		// Token: 0x06000512 RID: 1298 RVA: 0x0001C5AC File Offset: 0x0001A7AC
		protected void ShowCancelInquiry(Action confirmCancel)
		{
			if (this._hasMadeChanges)
			{
				string text = new TextObject("{=a8NoW1Q2}Are you sure you want to cancel your changes?", null).ToString();
				InformationManager.ShowInquiry(new InquiryData("", text, true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), delegate()
				{
					confirmCancel();
				}, null, "", 0f, null, null, null), false, false);
				return;
			}
			confirmCancel();
		}

		// Token: 0x06000513 RID: 1299 RVA: 0x0001C63C File Offset: 0x0001A83C
		protected void UpdateLabels()
		{
			MBTextManager.SetTextVariable("PAY_OR_GET", 0);
			int num = this._partyVM.PartyScreenLogic.CurrentData.PartyGoldChangeAmount - this._initialGoldChange;
			int num2 = this._partyVM.PartyScreenLogic.CurrentData.PartyHorseChangeAmount - this._initialHorseChange;
			int num3 = this._partyVM.PartyScreenLogic.CurrentData.PartyMoraleChangeAmount - this._initialMoraleChange;
			MBTextManager.SetTextVariable("LABEL_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">", false);
			MBTextManager.SetTextVariable("TRADE_AMOUNT", MathF.Abs(num));
			this.GoldChangeText = ((num == 0) ? "" : GameTexts.FindText("str_party_generic_label", null).ToString());
			MBTextManager.SetTextVariable("LABEL_ICON", "{=!}<img src=\"StdAssets\\ItemIcons\\Mount\" extend=\"16\">", false);
			MBTextManager.SetTextVariable("TRADE_AMOUNT", MathF.Abs(num2));
			this.HorseChangeText = ((num2 == 0) ? "" : GameTexts.FindText("str_party_generic_label", null).ToString());
			MBTextManager.SetTextVariable("LABEL_ICON", "{=!}<img src=\"General\\Icons\\Morale@2x\" extend=\"8\">", false);
			MBTextManager.SetTextVariable("TRADE_AMOUNT", MathF.Abs(num3));
			this.MoraleChangeText = ((num3 == 0) ? "" : GameTexts.FindText("str_party_generic_label", null).ToString());
		}

		// Token: 0x06000514 RID: 1300 RVA: 0x0001C76C File Offset: 0x0001A96C
		protected void SetFocusedCharacter(PartyTroopManagerItemVM troop)
		{
			this.FocusedTroop = troop;
			this.IsFocusedOnACharacter = troop != null;
			if (this.FocusedTroop == null)
			{
				this.IsPrimaryActionAvailable = false;
				this.IsSecondaryActionAvailable = false;
				this.IsTertiaryActionAvailable = false;
				return;
			}
			if (this.IsUpgradePopUp)
			{
				MBBindingList<UpgradeTargetVM> upgrades = this.FocusedTroop.PartyCharacter.Upgrades;
				this.IsPrimaryActionAvailable = upgrades.Count > 0 && upgrades[0].IsAvailable && !upgrades[0].IsInsufficient;
				this.IsSecondaryActionAvailable = upgrades.Count > 1 && upgrades[1].IsAvailable && !upgrades[1].IsInsufficient;
				this.IsTertiaryActionAvailable = upgrades.Count > 2 && upgrades[2].IsAvailable && !upgrades[2].IsInsufficient;
				return;
			}
			this.IsPrimaryActionAvailable = this.FocusedTroop.IsTroopRecruitable;
			this.IsSecondaryActionAvailable = false;
			this.IsTertiaryActionAvailable = false;
		}

		// Token: 0x06000515 RID: 1301 RVA: 0x0001C871 File Offset: 0x0001AA71
		public void SetDoneInputKey(HotKey hotKey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x06000516 RID: 1302 RVA: 0x0001C880 File Offset: 0x0001AA80
		public void SetCancelInputKey(HotKey hotKey)
		{
			this.CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x06000517 RID: 1303 RVA: 0x0001C88F File Offset: 0x0001AA8F
		public void SetPrimaryActionInputKey(HotKey hotKey)
		{
			this.PrimaryActionInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x06000518 RID: 1304 RVA: 0x0001C89E File Offset: 0x0001AA9E
		public void SetSecondaryActionInputKey(HotKey hotKey)
		{
			this.SecondaryActionInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x06000519 RID: 1305 RVA: 0x0001C8AD File Offset: 0x0001AAAD
		public void SetTertiaryActionInputKey(HotKey hotKey)
		{
			this.TertiaryActionInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x1700016B RID: 363
		// (get) Token: 0x0600051A RID: 1306 RVA: 0x0001C8BC File Offset: 0x0001AABC
		// (set) Token: 0x0600051B RID: 1307 RVA: 0x0001C8C4 File Offset: 0x0001AAC4
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

		// Token: 0x1700016C RID: 364
		// (get) Token: 0x0600051C RID: 1308 RVA: 0x0001C8E2 File Offset: 0x0001AAE2
		// (set) Token: 0x0600051D RID: 1309 RVA: 0x0001C8EA File Offset: 0x0001AAEA
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

		// Token: 0x1700016D RID: 365
		// (get) Token: 0x0600051E RID: 1310 RVA: 0x0001C908 File Offset: 0x0001AB08
		// (set) Token: 0x0600051F RID: 1311 RVA: 0x0001C910 File Offset: 0x0001AB10
		[DataSourceProperty]
		public InputKeyItemVM PrimaryActionInputKey
		{
			get
			{
				return this._primaryActionInputKey;
			}
			set
			{
				if (value != this._primaryActionInputKey)
				{
					this._primaryActionInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "PrimaryActionInputKey");
				}
			}
		}

		// Token: 0x1700016E RID: 366
		// (get) Token: 0x06000520 RID: 1312 RVA: 0x0001C92E File Offset: 0x0001AB2E
		// (set) Token: 0x06000521 RID: 1313 RVA: 0x0001C936 File Offset: 0x0001AB36
		[DataSourceProperty]
		public InputKeyItemVM SecondaryActionInputKey
		{
			get
			{
				return this._secondaryActionInputKey;
			}
			set
			{
				if (value != this._secondaryActionInputKey)
				{
					this._secondaryActionInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "SecondaryActionInputKey");
				}
			}
		}

		// Token: 0x1700016F RID: 367
		// (get) Token: 0x06000522 RID: 1314 RVA: 0x0001C954 File Offset: 0x0001AB54
		// (set) Token: 0x06000523 RID: 1315 RVA: 0x0001C95C File Offset: 0x0001AB5C
		[DataSourceProperty]
		public InputKeyItemVM TertiaryActionInputKey
		{
			get
			{
				return this._tertiaryActionInputKey;
			}
			set
			{
				if (value != this._tertiaryActionInputKey)
				{
					this._tertiaryActionInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "TertiaryActionInputKey");
				}
			}
		}

		// Token: 0x17000170 RID: 368
		// (get) Token: 0x06000524 RID: 1316 RVA: 0x0001C97A File Offset: 0x0001AB7A
		// (set) Token: 0x06000525 RID: 1317 RVA: 0x0001C982 File Offset: 0x0001AB82
		[DataSourceProperty]
		public bool IsFocusedOnACharacter
		{
			get
			{
				return this._isFocusedOnACharacter;
			}
			set
			{
				if (value != this._isFocusedOnACharacter)
				{
					this._isFocusedOnACharacter = value;
					base.OnPropertyChangedWithValue(value, "IsFocusedOnACharacter");
				}
			}
		}

		// Token: 0x17000171 RID: 369
		// (get) Token: 0x06000526 RID: 1318 RVA: 0x0001C9A0 File Offset: 0x0001ABA0
		// (set) Token: 0x06000527 RID: 1319 RVA: 0x0001C9A8 File Offset: 0x0001ABA8
		[DataSourceProperty]
		public bool IsOpen
		{
			get
			{
				return this._isOpen;
			}
			set
			{
				if (value != this._isOpen)
				{
					this._isOpen = value;
					base.OnPropertyChangedWithValue(value, "IsOpen");
				}
			}
		}

		// Token: 0x17000172 RID: 370
		// (get) Token: 0x06000528 RID: 1320 RVA: 0x0001C9C6 File Offset: 0x0001ABC6
		// (set) Token: 0x06000529 RID: 1321 RVA: 0x0001C9CE File Offset: 0x0001ABCE
		[DataSourceProperty]
		public bool IsUpgradePopUp
		{
			get
			{
				return this._isUpgradePopUp;
			}
			set
			{
				if (value != this._isUpgradePopUp)
				{
					this._isUpgradePopUp = value;
					base.OnPropertyChangedWithValue(value, "IsUpgradePopUp");
				}
			}
		}

		// Token: 0x17000173 RID: 371
		// (get) Token: 0x0600052A RID: 1322 RVA: 0x0001C9EC File Offset: 0x0001ABEC
		// (set) Token: 0x0600052B RID: 1323 RVA: 0x0001C9F4 File Offset: 0x0001ABF4
		[DataSourceProperty]
		public bool IsPrimaryActionAvailable
		{
			get
			{
				return this._isPrimaryActionAvailable;
			}
			set
			{
				if (value != this._isPrimaryActionAvailable)
				{
					this._isPrimaryActionAvailable = value;
					base.OnPropertyChangedWithValue(value, "IsPrimaryActionAvailable");
				}
			}
		}

		// Token: 0x17000174 RID: 372
		// (get) Token: 0x0600052C RID: 1324 RVA: 0x0001CA12 File Offset: 0x0001AC12
		// (set) Token: 0x0600052D RID: 1325 RVA: 0x0001CA1A File Offset: 0x0001AC1A
		[DataSourceProperty]
		public bool IsSecondaryActionAvailable
		{
			get
			{
				return this._isSecondaryActionAvailable;
			}
			set
			{
				if (value != this._isSecondaryActionAvailable)
				{
					this._isSecondaryActionAvailable = value;
					base.OnPropertyChangedWithValue(value, "IsSecondaryActionAvailable");
				}
			}
		}

		// Token: 0x17000175 RID: 373
		// (get) Token: 0x0600052E RID: 1326 RVA: 0x0001CA38 File Offset: 0x0001AC38
		// (set) Token: 0x0600052F RID: 1327 RVA: 0x0001CA40 File Offset: 0x0001AC40
		[DataSourceProperty]
		public bool IsTertiaryActionAvailable
		{
			get
			{
				return this._isTertiaryActionAvailable;
			}
			set
			{
				if (value != this._isTertiaryActionAvailable)
				{
					this._isTertiaryActionAvailable = value;
					base.OnPropertyChangedWithValue(value, "IsTertiaryActionAvailable");
				}
			}
		}

		// Token: 0x17000176 RID: 374
		// (get) Token: 0x06000530 RID: 1328 RVA: 0x0001CA5E File Offset: 0x0001AC5E
		// (set) Token: 0x06000531 RID: 1329 RVA: 0x0001CA66 File Offset: 0x0001AC66
		[DataSourceProperty]
		public PartyTroopManagerItemVM FocusedTroop
		{
			get
			{
				return this._focusedTroop;
			}
			set
			{
				if (value != this._focusedTroop)
				{
					this._focusedTroop = value;
					base.OnPropertyChangedWithValue<PartyTroopManagerItemVM>(value, "FocusedTroop");
				}
			}
		}

		// Token: 0x17000177 RID: 375
		// (get) Token: 0x06000532 RID: 1330 RVA: 0x0001CA84 File Offset: 0x0001AC84
		// (set) Token: 0x06000533 RID: 1331 RVA: 0x0001CA8C File Offset: 0x0001AC8C
		[DataSourceProperty]
		public MBBindingList<PartyTroopManagerItemVM> Troops
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
					base.OnPropertyChangedWithValue<MBBindingList<PartyTroopManagerItemVM>>(value, "Troops");
				}
			}
		}

		// Token: 0x17000178 RID: 376
		// (get) Token: 0x06000534 RID: 1332 RVA: 0x0001CAAA File Offset: 0x0001ACAA
		// (set) Token: 0x06000535 RID: 1333 RVA: 0x0001CAB2 File Offset: 0x0001ACB2
		[DataSourceProperty]
		public HintViewModel OpenButtonHint
		{
			get
			{
				return this._openButtonHint;
			}
			set
			{
				if (value != this._openButtonHint)
				{
					this._openButtonHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "OpenButtonHint");
				}
			}
		}

		// Token: 0x17000179 RID: 377
		// (get) Token: 0x06000536 RID: 1334 RVA: 0x0001CAD0 File Offset: 0x0001ACD0
		// (set) Token: 0x06000537 RID: 1335 RVA: 0x0001CAD8 File Offset: 0x0001ACD8
		[DataSourceProperty]
		public BasicTooltipViewModel UsedHorsesHint
		{
			get
			{
				return this._usedHorsesHint;
			}
			set
			{
				if (value != this._usedHorsesHint)
				{
					this._usedHorsesHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "UsedHorsesHint");
				}
			}
		}

		// Token: 0x1700017A RID: 378
		// (get) Token: 0x06000538 RID: 1336 RVA: 0x0001CAF6 File Offset: 0x0001ACF6
		// (set) Token: 0x06000539 RID: 1337 RVA: 0x0001CAFE File Offset: 0x0001ACFE
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

		// Token: 0x1700017B RID: 379
		// (get) Token: 0x0600053A RID: 1338 RVA: 0x0001CB21 File Offset: 0x0001AD21
		// (set) Token: 0x0600053B RID: 1339 RVA: 0x0001CB29 File Offset: 0x0001AD29
		[DataSourceProperty]
		public string AvatarText
		{
			get
			{
				return this._avatarText;
			}
			set
			{
				if (value != this._avatarText)
				{
					this._avatarText = value;
					base.OnPropertyChangedWithValue<string>(value, "AvatarText");
				}
			}
		}

		// Token: 0x1700017C RID: 380
		// (get) Token: 0x0600053C RID: 1340 RVA: 0x0001CB4C File Offset: 0x0001AD4C
		// (set) Token: 0x0600053D RID: 1341 RVA: 0x0001CB54 File Offset: 0x0001AD54
		[DataSourceProperty]
		public string NameText
		{
			get
			{
				return this._nameText;
			}
			set
			{
				if (value != this._nameText)
				{
					this._nameText = value;
					base.OnPropertyChangedWithValue<string>(value, "NameText");
				}
			}
		}

		// Token: 0x1700017D RID: 381
		// (get) Token: 0x0600053E RID: 1342 RVA: 0x0001CB77 File Offset: 0x0001AD77
		// (set) Token: 0x0600053F RID: 1343 RVA: 0x0001CB7F File Offset: 0x0001AD7F
		[DataSourceProperty]
		public string CountText
		{
			get
			{
				return this._countText;
			}
			set
			{
				if (value != this._countText)
				{
					this._countText = value;
					base.OnPropertyChangedWithValue<string>(value, "CountText");
				}
			}
		}

		// Token: 0x1700017E RID: 382
		// (get) Token: 0x06000540 RID: 1344 RVA: 0x0001CBA2 File Offset: 0x0001ADA2
		// (set) Token: 0x06000541 RID: 1345 RVA: 0x0001CBAA File Offset: 0x0001ADAA
		[DataSourceProperty]
		public string GoldChangeText
		{
			get
			{
				return this._goldChangeText;
			}
			set
			{
				if (value != this._goldChangeText)
				{
					this._goldChangeText = value;
					base.OnPropertyChangedWithValue<string>(value, "GoldChangeText");
				}
			}
		}

		// Token: 0x1700017F RID: 383
		// (get) Token: 0x06000542 RID: 1346 RVA: 0x0001CBCD File Offset: 0x0001ADCD
		// (set) Token: 0x06000543 RID: 1347 RVA: 0x0001CBD5 File Offset: 0x0001ADD5
		[DataSourceProperty]
		public string HorseChangeText
		{
			get
			{
				return this._horseChangeText;
			}
			set
			{
				if (value != this._horseChangeText)
				{
					this._horseChangeText = value;
					base.OnPropertyChangedWithValue<string>(value, "HorseChangeText");
				}
			}
		}

		// Token: 0x17000180 RID: 384
		// (get) Token: 0x06000544 RID: 1348 RVA: 0x0001CBF8 File Offset: 0x0001ADF8
		// (set) Token: 0x06000545 RID: 1349 RVA: 0x0001CC00 File Offset: 0x0001AE00
		[DataSourceProperty]
		public string MoraleChangeText
		{
			get
			{
				return this._moraleChangeText;
			}
			set
			{
				if (value != this._moraleChangeText)
				{
					this._moraleChangeText = value;
					base.OnPropertyChangedWithValue<string>(value, "MoraleChangeText");
				}
			}
		}

		// Token: 0x17000181 RID: 385
		// (get) Token: 0x06000546 RID: 1350 RVA: 0x0001CC23 File Offset: 0x0001AE23
		// (set) Token: 0x06000547 RID: 1351 RVA: 0x0001CC2B File Offset: 0x0001AE2B
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

		// Token: 0x17000182 RID: 386
		// (get) Token: 0x06000548 RID: 1352 RVA: 0x0001CC4E File Offset: 0x0001AE4E
		// (set) Token: 0x06000549 RID: 1353 RVA: 0x0001CC56 File Offset: 0x0001AE56
		[DataSourceProperty]
		public string CancelLbl
		{
			get
			{
				return this._cancelLbl;
			}
			set
			{
				if (value != this._cancelLbl)
				{
					this._cancelLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "CancelLbl");
				}
			}
		}

		// Token: 0x04000225 RID: 549
		protected PartyVM _partyVM;

		// Token: 0x04000226 RID: 550
		protected bool _hasMadeChanges;

		// Token: 0x04000227 RID: 551
		protected TextObject _openButtonEnabledHint = TextObject.GetEmpty();

		// Token: 0x04000228 RID: 552
		protected TextObject _openButtonNoTroopsHint = TextObject.GetEmpty();

		// Token: 0x04000229 RID: 553
		protected TextObject _openButtonIrrelevantScreenHint = TextObject.GetEmpty();

		// Token: 0x0400022A RID: 554
		protected TextObject _openButtonUpgradesDisabledHint = TextObject.GetEmpty();

		// Token: 0x0400022B RID: 555
		private int _initialGoldChange;

		// Token: 0x0400022C RID: 556
		private int _initialHorseChange;

		// Token: 0x0400022D RID: 557
		private int _initialMoraleChange;

		// Token: 0x0400022E RID: 558
		protected List<Tuple<EquipmentElement, int>> _initialUsedUpgradeHorsesHistory = new List<Tuple<EquipmentElement, int>>();

		// Token: 0x0400022F RID: 559
		private InputKeyItemVM _doneInputKey;

		// Token: 0x04000230 RID: 560
		private InputKeyItemVM _cancelInputKey;

		// Token: 0x04000231 RID: 561
		private InputKeyItemVM _primaryActionInputKey;

		// Token: 0x04000232 RID: 562
		private InputKeyItemVM _secondaryActionInputKey;

		// Token: 0x04000233 RID: 563
		private InputKeyItemVM _tertiaryActionInputKey;

		// Token: 0x04000234 RID: 564
		private bool _isFocusedOnACharacter;

		// Token: 0x04000235 RID: 565
		private bool _isOpen;

		// Token: 0x04000236 RID: 566
		private bool _isUpgradePopUp;

		// Token: 0x04000237 RID: 567
		private bool _isPrimaryActionAvailable;

		// Token: 0x04000238 RID: 568
		private bool _isSecondaryActionAvailable;

		// Token: 0x04000239 RID: 569
		private bool _isTertiaryActionAvailable;

		// Token: 0x0400023A RID: 570
		private PartyTroopManagerItemVM _focusedTroop;

		// Token: 0x0400023B RID: 571
		private MBBindingList<PartyTroopManagerItemVM> _troops;

		// Token: 0x0400023C RID: 572
		private HintViewModel _openButtonHint;

		// Token: 0x0400023D RID: 573
		private BasicTooltipViewModel _usedHorsesHint;

		// Token: 0x0400023E RID: 574
		private string _titleText;

		// Token: 0x0400023F RID: 575
		private string _avatarText;

		// Token: 0x04000240 RID: 576
		private string _nameText;

		// Token: 0x04000241 RID: 577
		private string _countText;

		// Token: 0x04000242 RID: 578
		private string _goldChangeText;

		// Token: 0x04000243 RID: 579
		private string _horseChangeText;

		// Token: 0x04000244 RID: 580
		private string _moraleChangeText;

		// Token: 0x04000245 RID: 581
		private string _doneLbl;

		// Token: 0x04000246 RID: 582
		private string _cancelLbl;
	}
}
