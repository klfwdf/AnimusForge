using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.MapSiege
{
	// Token: 0x02000054 RID: 84
	public class MapSiegeProductionMachineVM : ViewModel
	{
		// Token: 0x1700018D RID: 397
		// (get) Token: 0x0600053B RID: 1339 RVA: 0x00013DF3 File Offset: 0x00011FF3
		public SiegeEngineType Engine { get; }

		// Token: 0x0600053C RID: 1340 RVA: 0x00013DFB File Offset: 0x00011FFB
		public MapSiegeProductionMachineVM(SiegeEngineType engineType, int number, Action<MapSiegeProductionMachineVM> onSelection)
		{
			this._onSelection = onSelection;
			this.Engine = engineType;
			this.NumberOfMachines = number;
			this.MachineID = engineType.StringId;
			this.IsReserveOption = false;
		}

		// Token: 0x0600053D RID: 1341 RVA: 0x00013E2B File Offset: 0x0001202B
		public MapSiegeProductionMachineVM(Action<MapSiegeProductionMachineVM> onSelection, bool isCancel)
		{
			this._onSelection = onSelection;
			this.Engine = null;
			this.NumberOfMachines = 0;
			this.MachineID = "reserve";
			this.IsReserveOption = true;
			this._isCancel = isCancel;
			this.RefreshValues();
		}

		// Token: 0x0600053E RID: 1342 RVA: 0x00013E67 File Offset: 0x00012067
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.ActionText = (this._isCancel ? GameTexts.FindText("str_cancel", null).ToString() : GameTexts.FindText("str_siege_move_to_reserve", null).ToString());
		}

		// Token: 0x0600053F RID: 1343 RVA: 0x00013E9F File Offset: 0x0001209F
		public void OnSelection()
		{
			this._onSelection(this);
		}

		// Token: 0x06000540 RID: 1344 RVA: 0x00013EAD File Offset: 0x000120AD
		public void ExecuteShowTooltip()
		{
			if (this.Engine != null)
			{
				InformationManager.ShowTooltip(typeof(List<TooltipProperty>), new object[] { SandBoxUIHelper.GetSiegeEngineTooltip(this.Engine) });
			}
		}

		// Token: 0x06000541 RID: 1345 RVA: 0x00013EDA File Offset: 0x000120DA
		public void ExecuteHideTooltip()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x1700018E RID: 398
		// (get) Token: 0x06000542 RID: 1346 RVA: 0x00013EE1 File Offset: 0x000120E1
		// (set) Token: 0x06000543 RID: 1347 RVA: 0x00013EE9 File Offset: 0x000120E9
		[DataSourceProperty]
		public int MachineType
		{
			get
			{
				return this._machineType;
			}
			set
			{
				if (value != this._machineType)
				{
					this._machineType = value;
					base.OnPropertyChangedWithValue(value, "MachineType");
				}
			}
		}

		// Token: 0x1700018F RID: 399
		// (get) Token: 0x06000544 RID: 1348 RVA: 0x00013F07 File Offset: 0x00012107
		// (set) Token: 0x06000545 RID: 1349 RVA: 0x00013F0F File Offset: 0x0001210F
		[DataSourceProperty]
		public string MachineID
		{
			get
			{
				return this._machineID;
			}
			set
			{
				if (value != this._machineID)
				{
					this._machineID = value;
					base.OnPropertyChangedWithValue<string>(value, "MachineID");
				}
			}
		}

		// Token: 0x17000190 RID: 400
		// (get) Token: 0x06000546 RID: 1350 RVA: 0x00013F32 File Offset: 0x00012132
		// (set) Token: 0x06000547 RID: 1351 RVA: 0x00013F3A File Offset: 0x0001213A
		[DataSourceProperty]
		public int NumberOfMachines
		{
			get
			{
				return this._numberOfMachines;
			}
			set
			{
				if (value != this._numberOfMachines)
				{
					this._numberOfMachines = value;
					base.OnPropertyChangedWithValue(value, "NumberOfMachines");
				}
			}
		}

		// Token: 0x17000191 RID: 401
		// (get) Token: 0x06000548 RID: 1352 RVA: 0x00013F58 File Offset: 0x00012158
		// (set) Token: 0x06000549 RID: 1353 RVA: 0x00013F60 File Offset: 0x00012160
		[DataSourceProperty]
		public string ActionText
		{
			get
			{
				return this._actionText;
			}
			set
			{
				if (value != this._actionText)
				{
					this._actionText = value;
					base.OnPropertyChangedWithValue<string>(value, "ActionText");
				}
			}
		}

		// Token: 0x17000192 RID: 402
		// (get) Token: 0x0600054A RID: 1354 RVA: 0x00013F83 File Offset: 0x00012183
		// (set) Token: 0x0600054B RID: 1355 RVA: 0x00013F8B File Offset: 0x0001218B
		[DataSourceProperty]
		public bool IsReserveOption
		{
			get
			{
				return this._isReserveOption;
			}
			set
			{
				if (value != this._isReserveOption)
				{
					this._isReserveOption = value;
					base.OnPropertyChangedWithValue(value, "IsReserveOption");
				}
			}
		}

		// Token: 0x04000298 RID: 664
		private Action<MapSiegeProductionMachineVM> _onSelection;

		// Token: 0x0400029A RID: 666
		private bool _isCancel;

		// Token: 0x0400029B RID: 667
		private int _machineType;

		// Token: 0x0400029C RID: 668
		private int _numberOfMachines;

		// Token: 0x0400029D RID: 669
		private string _machineID;

		// Token: 0x0400029E RID: 670
		private bool _isReserveOption;

		// Token: 0x0400029F RID: 671
		private string _actionText;
	}
}
