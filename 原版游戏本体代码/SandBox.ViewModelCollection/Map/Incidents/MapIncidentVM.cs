using System;
using System.Collections.Generic;
using SandBox.ViewModelCollection.Input;
using TaleWorlds.CampaignSystem.Incidents;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Map.Incidents
{
	// Token: 0x0200004D RID: 77
	public class MapIncidentVM : ViewModel
	{
		// Token: 0x060004B5 RID: 1205 RVA: 0x0001241C File Offset: 0x0001061C
		public MapIncidentVM(Incident incident, Action onClose)
		{
			this._incident = incident;
			this._onClose = onClose;
			this.IncidentType = incident.Type.ToString();
			this.ConfirmHint = new HintViewModel();
			this.Options = new MBBindingList<MapIncidentOptionVM>();
			this.PopulateOptions();
			this.RefreshValues();
			this.UpdateCanConfirm();
		}

		// Token: 0x060004B6 RID: 1206 RVA: 0x00012480 File Offset: 0x00010680
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Title = this._incident.Title.ToString();
			this.Description = this._incident.Description.ToString();
			this.ConfirmText = new TextObject("{=WiNRdfsm}Done", null).ToString();
			this.Options.ApplyActionOnAllItems(delegate(MapIncidentOptionVM o)
			{
				o.RefreshValues();
			});
		}

		// Token: 0x060004B7 RID: 1207 RVA: 0x000124FF File Offset: 0x000106FF
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.Options.ApplyActionOnAllItems(delegate(MapIncidentOptionVM o)
			{
				o.OnFinalize();
			});
		}

		// Token: 0x060004B8 RID: 1208 RVA: 0x00012534 File Offset: 0x00010734
		public void ExecuteConfirm()
		{
			if (this.SelectedOption != null)
			{
				int index = this.SelectedOption.Index;
				if (index >= 0 && index < this._incident.NumOfOptions)
				{
					using (List<TextObject>.Enumerator enumerator = this._incident.InvokeOption(index).GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							TextObject message = enumerator.Current;
							MBInformationManager.AddQuickInformation(message, 0, null, null, "");
						}
						goto IL_95;
					}
				}
				Debug.FailedAssert("Selected incident option is out of bounds. Action won't be invoked", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\Map\\Incidents\\MapIncidentVM.cs", "ExecuteConfirm", 69);
			}
			else
			{
				Debug.FailedAssert("An incident option must be selected before confirm", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\Map\\Incidents\\MapIncidentVM.cs", "ExecuteConfirm", 74);
			}
			IL_95:
			this._onClose();
		}

		// Token: 0x060004B9 RID: 1209 RVA: 0x000125F4 File Offset: 0x000107F4
		private void PopulateOptions()
		{
			this.Options.Clear();
			for (int i = 0; i < this._incident.NumOfOptions; i++)
			{
				TextObject optionText = this._incident.GetOptionText(i);
				List<TextObject> optionHint = this._incident.GetOptionHint(i);
				MapIncidentOptionVM item = new MapIncidentOptionVM(optionText, optionHint, i, new Action<MapIncidentOptionVM>(this.OnOptionSelected), new Action<MapIncidentOptionVM>(this.OnOptionFocused));
				this.Options.Add(item);
			}
		}

		// Token: 0x060004BA RID: 1210 RVA: 0x00012667 File Offset: 0x00010867
		private void OnOptionSelected(MapIncidentOptionVM option)
		{
			this.SelectedOption = option;
			this.UpdateActiveHint();
			this.UpdateCanConfirm();
		}

		// Token: 0x060004BB RID: 1211 RVA: 0x0001267C File Offset: 0x0001087C
		private void OnOptionFocused(MapIncidentOptionVM option)
		{
			this.FocusedOption = option;
			this.UpdateActiveHint();
		}

		// Token: 0x060004BC RID: 1212 RVA: 0x0001268C File Offset: 0x0001088C
		private void UpdateActiveHint()
		{
			if (this.FocusedOption != null)
			{
				this.ActiveHint = this.FocusedOption.Hint;
				return;
			}
			if (this.SelectedOption != null)
			{
				this.ActiveHint = this.SelectedOption.Hint;
				return;
			}
			this.ActiveHint = string.Empty;
		}

		// Token: 0x060004BD RID: 1213 RVA: 0x000126D8 File Offset: 0x000108D8
		private void UpdateCanConfirm()
		{
			if (this.SelectedOption == null)
			{
				this.CanConfirm = false;
				this.ConfirmHint.HintText = new TextObject("{=R3Zn7x07}You must select an option", null);
				return;
			}
			this.CanConfirm = true;
			this.ConfirmHint.HintText = TextObject.GetEmpty();
		}

		// Token: 0x1700015F RID: 351
		// (get) Token: 0x060004BE RID: 1214 RVA: 0x00012717 File Offset: 0x00010917
		// (set) Token: 0x060004BF RID: 1215 RVA: 0x0001271F File Offset: 0x0001091F
		[DataSourceProperty]
		public bool CanConfirm
		{
			get
			{
				return this._canConfirm;
			}
			set
			{
				if (value != this._canConfirm)
				{
					this._canConfirm = value;
					base.OnPropertyChangedWithValue(value, "CanConfirm");
				}
			}
		}

		// Token: 0x17000160 RID: 352
		// (get) Token: 0x060004C0 RID: 1216 RVA: 0x0001273D File Offset: 0x0001093D
		// (set) Token: 0x060004C1 RID: 1217 RVA: 0x00012745 File Offset: 0x00010945
		[DataSourceProperty]
		public bool HasFocusedOption
		{
			get
			{
				return this._hasFocusedOption;
			}
			set
			{
				if (value != this._hasFocusedOption)
				{
					this._hasFocusedOption = value;
					base.OnPropertyChangedWithValue(value, "HasFocusedOption");
				}
			}
		}

		// Token: 0x17000161 RID: 353
		// (get) Token: 0x060004C2 RID: 1218 RVA: 0x00012763 File Offset: 0x00010963
		// (set) Token: 0x060004C3 RID: 1219 RVA: 0x0001276B File Offset: 0x0001096B
		[DataSourceProperty]
		public bool HasSelectedOption
		{
			get
			{
				return this._hasSelectedOption;
			}
			set
			{
				if (value != this._hasSelectedOption)
				{
					this._hasSelectedOption = value;
					base.OnPropertyChangedWithValue(value, "HasSelectedOption");
				}
			}
		}

		// Token: 0x17000162 RID: 354
		// (get) Token: 0x060004C4 RID: 1220 RVA: 0x00012789 File Offset: 0x00010989
		// (set) Token: 0x060004C5 RID: 1221 RVA: 0x00012791 File Offset: 0x00010991
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

		// Token: 0x17000163 RID: 355
		// (get) Token: 0x060004C6 RID: 1222 RVA: 0x000127B4 File Offset: 0x000109B4
		// (set) Token: 0x060004C7 RID: 1223 RVA: 0x000127BC File Offset: 0x000109BC
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

		// Token: 0x17000164 RID: 356
		// (get) Token: 0x060004C8 RID: 1224 RVA: 0x000127DF File Offset: 0x000109DF
		// (set) Token: 0x060004C9 RID: 1225 RVA: 0x000127E7 File Offset: 0x000109E7
		[DataSourceProperty]
		public string ConfirmText
		{
			get
			{
				return this._confirmText;
			}
			set
			{
				if (value != this._confirmText)
				{
					this._confirmText = value;
					base.OnPropertyChangedWithValue<string>(value, "ConfirmText");
				}
			}
		}

		// Token: 0x17000165 RID: 357
		// (get) Token: 0x060004CA RID: 1226 RVA: 0x0001280A File Offset: 0x00010A0A
		// (set) Token: 0x060004CB RID: 1227 RVA: 0x00012812 File Offset: 0x00010A12
		[DataSourceProperty]
		public string IncidentType
		{
			get
			{
				return this._incidentType;
			}
			set
			{
				if (value != this._incidentType)
				{
					this._incidentType = value;
					base.OnPropertyChangedWithValue<string>(value, "IncidentType");
				}
			}
		}

		// Token: 0x17000166 RID: 358
		// (get) Token: 0x060004CC RID: 1228 RVA: 0x00012835 File Offset: 0x00010A35
		// (set) Token: 0x060004CD RID: 1229 RVA: 0x0001283D File Offset: 0x00010A3D
		[DataSourceProperty]
		public string ActiveHint
		{
			get
			{
				return this._activeHint;
			}
			set
			{
				if (value != this._activeHint)
				{
					this._activeHint = value;
					base.OnPropertyChangedWithValue<string>(value, "ActiveHint");
				}
			}
		}

		// Token: 0x17000167 RID: 359
		// (get) Token: 0x060004CE RID: 1230 RVA: 0x00012860 File Offset: 0x00010A60
		// (set) Token: 0x060004CF RID: 1231 RVA: 0x00012868 File Offset: 0x00010A68
		[DataSourceProperty]
		public HintViewModel ConfirmHint
		{
			get
			{
				return this._confirmHint;
			}
			set
			{
				if (value != this._confirmHint)
				{
					this._confirmHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ConfirmHint");
				}
			}
		}

		// Token: 0x17000168 RID: 360
		// (get) Token: 0x060004D0 RID: 1232 RVA: 0x00012886 File Offset: 0x00010A86
		// (set) Token: 0x060004D1 RID: 1233 RVA: 0x00012890 File Offset: 0x00010A90
		[DataSourceProperty]
		public MapIncidentOptionVM FocusedOption
		{
			get
			{
				return this._focusedOption;
			}
			set
			{
				if (value != this._focusedOption)
				{
					if (this._focusedOption != null)
					{
						this._focusedOption.IsFocused = false;
					}
					this._focusedOption = value;
					base.OnPropertyChangedWithValue<MapIncidentOptionVM>(value, "FocusedOption");
					if (this._focusedOption != null)
					{
						this._focusedOption.IsFocused = true;
					}
					this.HasFocusedOption = value != null;
				}
			}
		}

		// Token: 0x17000169 RID: 361
		// (get) Token: 0x060004D2 RID: 1234 RVA: 0x000128EB File Offset: 0x00010AEB
		// (set) Token: 0x060004D3 RID: 1235 RVA: 0x000128F4 File Offset: 0x00010AF4
		[DataSourceProperty]
		public MapIncidentOptionVM SelectedOption
		{
			get
			{
				return this._selectedOption;
			}
			set
			{
				if (value != this._selectedOption)
				{
					if (this._selectedOption != null)
					{
						this._selectedOption.IsSelected = false;
					}
					this._selectedOption = value;
					base.OnPropertyChangedWithValue<MapIncidentOptionVM>(value, "SelectedOption");
					if (this._selectedOption != null)
					{
						this._selectedOption.IsSelected = true;
					}
					this.HasSelectedOption = value != null;
				}
			}
		}

		// Token: 0x1700016A RID: 362
		// (get) Token: 0x060004D4 RID: 1236 RVA: 0x0001294F File Offset: 0x00010B4F
		// (set) Token: 0x060004D5 RID: 1237 RVA: 0x00012957 File Offset: 0x00010B57
		[DataSourceProperty]
		public MBBindingList<MapIncidentOptionVM> Options
		{
			get
			{
				return this._options;
			}
			set
			{
				if (value != this._options)
				{
					this._options = value;
					base.OnPropertyChangedWithValue<MBBindingList<MapIncidentOptionVM>>(value, "Options");
				}
			}
		}

		// Token: 0x060004D6 RID: 1238 RVA: 0x00012975 File Offset: 0x00010B75
		public void SetDoneInputKey(HotKey hotKey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x1700016B RID: 363
		// (get) Token: 0x060004D7 RID: 1239 RVA: 0x00012984 File Offset: 0x00010B84
		// (set) Token: 0x060004D8 RID: 1240 RVA: 0x0001298C File Offset: 0x00010B8C
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

		// Token: 0x04000255 RID: 597
		private readonly Incident _incident;

		// Token: 0x04000256 RID: 598
		private readonly Action _onClose;

		// Token: 0x04000257 RID: 599
		private bool _canConfirm;

		// Token: 0x04000258 RID: 600
		private bool _hasFocusedOption;

		// Token: 0x04000259 RID: 601
		private bool _hasSelectedOption;

		// Token: 0x0400025A RID: 602
		private string _title;

		// Token: 0x0400025B RID: 603
		private string _description;

		// Token: 0x0400025C RID: 604
		private string _confirmText;

		// Token: 0x0400025D RID: 605
		private string _incidentType;

		// Token: 0x0400025E RID: 606
		private string _activeHint;

		// Token: 0x0400025F RID: 607
		private HintViewModel _confirmHint;

		// Token: 0x04000260 RID: 608
		private MapIncidentOptionVM _focusedOption;

		// Token: 0x04000261 RID: 609
		private MapIncidentOptionVM _selectedOption;

		// Token: 0x04000262 RID: 610
		private MBBindingList<MapIncidentOptionVM> _options;

		// Token: 0x04000263 RID: 611
		private InputKeyItemVM _doneInputKey;
	}
}
