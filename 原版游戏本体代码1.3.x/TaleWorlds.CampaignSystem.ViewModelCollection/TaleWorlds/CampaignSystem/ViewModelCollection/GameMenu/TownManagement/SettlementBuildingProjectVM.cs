using System;
using Helpers;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement
{
	// Token: 0x020000A2 RID: 162
	public class SettlementBuildingProjectVM : SettlementProjectVM
	{
		// Token: 0x06000FA7 RID: 4007 RVA: 0x000409EC File Offset: 0x0003EBEC
		public SettlementBuildingProjectVM(Action<SettlementProjectVM, bool> onSelection, Action<SettlementProjectVM> onSetAsCurrent, Action onResetCurrent, Building building, Settlement settlement)
			: base(onSelection, onSetAsCurrent, onResetCurrent, building, settlement)
		{
			this.Level = building.CurrentLevel;
			this.MaxLevel = 3;
			this.DevelopmentLevelText = building.CurrentLevel.ToString();
			this.CanBuild = this.Level < 3;
			base.IsDaily = false;
			this.RefreshValues();
		}

		// Token: 0x06000FA8 RID: 4008 RVA: 0x00040A52 File Offset: 0x0003EC52
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.AlreadyAtMaxText = new TextObject("{=ybLA7ZXp}Already at Max", null).ToString();
			this.UpdateProjectHints();
		}

		// Token: 0x06000FA9 RID: 4009 RVA: 0x00040A78 File Offset: 0x0003EC78
		private void UpdateProjectHints()
		{
			if (this.AddRemoveHint == null)
			{
				this.AddRemoveHint = new HintViewModel();
			}
			if (this.SetAsActiveHint == null)
			{
				this.SetAsActiveHint = new HintViewModel();
			}
			this.AddRemoveHint.HintText = (this.IsInQueue ? new TextObject("{=faDegful}Remove from queue", null) : new TextObject("{=SFebv4hH}Add to queue", null));
			this.SetAsActiveHint.HintText = ((this.DevelopmentQueueIndex == 0) ? new TextObject("{=cD1HTdYJ}Already active development", null) : new TextObject("{=PcLGc2bM}Set as active development", null));
		}

		// Token: 0x06000FAA RID: 4010 RVA: 0x00040B04 File Offset: 0x0003ED04
		public override void RefreshProductionText()
		{
			base.RefreshProductionText();
			if (this.DevelopmentQueueIndex == 0)
			{
				GameTexts.SetVariable("LEFT", GameTexts.FindText("str_completion", null));
				int daysToComplete = BuildingHelper.GetDaysToComplete(base.Building, this._settlement.Town);
				TextObject textObject;
				if (daysToComplete != -1)
				{
					textObject = new TextObject("{=c5eYzHaM}{DAYS} {?DAY_IS_PLURAL}Days{?}Day{\\?} ({PERCENTAGE}%)", null);
					textObject.SetTextVariable("DAYS", daysToComplete);
					GameTexts.SetVariable("DAY_IS_PLURAL", (daysToComplete > 1) ? 1 : 0);
				}
				else
				{
					textObject = new TextObject("{=0TauthlH}Never ({PERCENTAGE}%)", null);
				}
				textObject.SetTextVariable("PERCENTAGE", (int)(BuildingHelper.GetProgressOfBuilding(base.Building, this._settlement.Town) * 100f));
				GameTexts.SetVariable("RIGHT", textObject);
				base.ProductionText = GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon", null).ToString();
				return;
			}
			if (this.DevelopmentQueueIndex > 0)
			{
				GameTexts.SetVariable("NUMBER", this.DevelopmentQueueIndex);
				base.ProductionText = GameTexts.FindText("str_in_queue_with_number", null).ToString();
				return;
			}
			base.ProductionText = " ";
		}

		// Token: 0x06000FAB RID: 4011 RVA: 0x00040C11 File Offset: 0x0003EE11
		public override void ExecuteAddToQueue()
		{
			if (this._onSelection != null && this.CanBuild)
			{
				this._onSelection(this, false);
			}
		}

		// Token: 0x06000FAC RID: 4012 RVA: 0x00040C30 File Offset: 0x0003EE30
		public override void ExecuteSetAsActiveDevelopment()
		{
			if (this._onSelection != null && this.CanBuild)
			{
				this._onSelection(this, true);
			}
		}

		// Token: 0x06000FAD RID: 4013 RVA: 0x00040C4F File Offset: 0x0003EE4F
		public override void ExecuteSetAsCurrent()
		{
			Action<SettlementProjectVM> onSetAsCurrent = this._onSetAsCurrent;
			if (onSetAsCurrent == null)
			{
				return;
			}
			onSetAsCurrent(this);
		}

		// Token: 0x06000FAE RID: 4014 RVA: 0x00040C62 File Offset: 0x0003EE62
		public override void ExecuteResetCurrent()
		{
			Action onResetCurrent = this._onResetCurrent;
			if (onResetCurrent == null)
			{
				return;
			}
			onResetCurrent();
		}

		// Token: 0x06000FAF RID: 4015 RVA: 0x00040C74 File Offset: 0x0003EE74
		public override void ExecuteToggleSelected()
		{
			if (this.CanBuild)
			{
				this.IsSelected = !this.IsSelected;
			}
		}

		// Token: 0x1700050F RID: 1295
		// (get) Token: 0x06000FB0 RID: 4016 RVA: 0x00040C8D File Offset: 0x0003EE8D
		// (set) Token: 0x06000FB1 RID: 4017 RVA: 0x00040C95 File Offset: 0x0003EE95
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (value != this._isSelected)
				{
					this._isSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSelected");
				}
			}
		}

		// Token: 0x17000510 RID: 1296
		// (get) Token: 0x06000FB2 RID: 4018 RVA: 0x00040CB3 File Offset: 0x0003EEB3
		// (set) Token: 0x06000FB3 RID: 4019 RVA: 0x00040CBB File Offset: 0x0003EEBB
		[DataSourceProperty]
		public string DevelopmentLevelText
		{
			get
			{
				return this._developmentLevelText;
			}
			set
			{
				if (value != this._developmentLevelText)
				{
					this._developmentLevelText = value;
					base.OnPropertyChangedWithValue<string>(value, "DevelopmentLevelText");
				}
			}
		}

		// Token: 0x17000511 RID: 1297
		// (get) Token: 0x06000FB4 RID: 4020 RVA: 0x00040CDE File Offset: 0x0003EEDE
		// (set) Token: 0x06000FB5 RID: 4021 RVA: 0x00040CE6 File Offset: 0x0003EEE6
		[DataSourceProperty]
		public int Level
		{
			get
			{
				return this._level;
			}
			set
			{
				if (value != this._level)
				{
					this._level = value;
					base.OnPropertyChangedWithValue(value, "Level");
				}
			}
		}

		// Token: 0x17000512 RID: 1298
		// (get) Token: 0x06000FB6 RID: 4022 RVA: 0x00040D04 File Offset: 0x0003EF04
		// (set) Token: 0x06000FB7 RID: 4023 RVA: 0x00040D0C File Offset: 0x0003EF0C
		[DataSourceProperty]
		public int MaxLevel
		{
			get
			{
				return this._maxLevel;
			}
			set
			{
				if (value != this._maxLevel)
				{
					this._maxLevel = value;
					base.OnPropertyChangedWithValue(value, "MaxLevel");
				}
			}
		}

		// Token: 0x17000513 RID: 1299
		// (get) Token: 0x06000FB8 RID: 4024 RVA: 0x00040D2A File Offset: 0x0003EF2A
		// (set) Token: 0x06000FB9 RID: 4025 RVA: 0x00040D32 File Offset: 0x0003EF32
		[DataSourceProperty]
		public int DevelopmentQueueIndex
		{
			get
			{
				return this._developmentQueueIndex;
			}
			set
			{
				if (value != this._developmentQueueIndex)
				{
					this._developmentQueueIndex = value;
					base.OnPropertyChangedWithValue(value, "DevelopmentQueueIndex");
					this.UpdateProjectHints();
				}
			}
		}

		// Token: 0x17000514 RID: 1300
		// (get) Token: 0x06000FBA RID: 4026 RVA: 0x00040D56 File Offset: 0x0003EF56
		// (set) Token: 0x06000FBB RID: 4027 RVA: 0x00040D5E File Offset: 0x0003EF5E
		[DataSourceProperty]
		public bool IsInQueue
		{
			get
			{
				return this._isInQueue;
			}
			set
			{
				if (value != this._isInQueue)
				{
					this._isInQueue = value;
					base.OnPropertyChangedWithValue(value, "IsInQueue");
					this.UpdateProjectHints();
				}
			}
		}

		// Token: 0x17000515 RID: 1301
		// (get) Token: 0x06000FBC RID: 4028 RVA: 0x00040D82 File Offset: 0x0003EF82
		// (set) Token: 0x06000FBD RID: 4029 RVA: 0x00040D8A File Offset: 0x0003EF8A
		[DataSourceProperty]
		public string AlreadyAtMaxText
		{
			get
			{
				return this._alreadyAtMaxText;
			}
			set
			{
				if (value != this._alreadyAtMaxText)
				{
					this._alreadyAtMaxText = value;
					base.OnPropertyChangedWithValue<string>(value, "AlreadyAtMaxText");
				}
			}
		}

		// Token: 0x17000516 RID: 1302
		// (get) Token: 0x06000FBE RID: 4030 RVA: 0x00040DAD File Offset: 0x0003EFAD
		// (set) Token: 0x06000FBF RID: 4031 RVA: 0x00040DB5 File Offset: 0x0003EFB5
		[DataSourceProperty]
		public bool CanBuild
		{
			get
			{
				return this._canBuild;
			}
			set
			{
				if (value != this._canBuild)
				{
					this._canBuild = value;
					base.OnPropertyChangedWithValue(value, "CanBuild");
				}
			}
		}

		// Token: 0x17000517 RID: 1303
		// (get) Token: 0x06000FC0 RID: 4032 RVA: 0x00040DD3 File Offset: 0x0003EFD3
		// (set) Token: 0x06000FC1 RID: 4033 RVA: 0x00040DDB File Offset: 0x0003EFDB
		[DataSourceProperty]
		public HintViewModel AddRemoveHint
		{
			get
			{
				return this._addRemoveHint;
			}
			set
			{
				if (value != this._addRemoveHint)
				{
					this._addRemoveHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "AddRemoveHint");
				}
			}
		}

		// Token: 0x17000518 RID: 1304
		// (get) Token: 0x06000FC2 RID: 4034 RVA: 0x00040DF9 File Offset: 0x0003EFF9
		// (set) Token: 0x06000FC3 RID: 4035 RVA: 0x00040E01 File Offset: 0x0003F001
		[DataSourceProperty]
		public HintViewModel SetAsActiveHint
		{
			get
			{
				return this._setAsActiveHint;
			}
			set
			{
				if (value != this._setAsActiveHint)
				{
					this._setAsActiveHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "SetAsActiveHint");
				}
			}
		}

		// Token: 0x04000729 RID: 1833
		private bool _isSelected;

		// Token: 0x0400072A RID: 1834
		private string _alreadyAtMaxText;

		// Token: 0x0400072B RID: 1835
		private string _developmentLevelText;

		// Token: 0x0400072C RID: 1836
		private int _level;

		// Token: 0x0400072D RID: 1837
		private int _maxLevel;

		// Token: 0x0400072E RID: 1838
		private int _developmentQueueIndex = -1;

		// Token: 0x0400072F RID: 1839
		private bool _canBuild;

		// Token: 0x04000730 RID: 1840
		private bool _isInQueue;

		// Token: 0x04000731 RID: 1841
		private HintViewModel _addRemoveHint;

		// Token: 0x04000732 RID: 1842
		private HintViewModel _setAsActiveHint;
	}
}
