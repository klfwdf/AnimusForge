using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement
{
	// Token: 0x020000A6 RID: 166
	public class SettlementProjectSelectionVM : ViewModel
	{
		// Token: 0x17000521 RID: 1313
		// (get) Token: 0x06000FE3 RID: 4067 RVA: 0x000412E2 File Offset: 0x0003F4E2
		// (set) Token: 0x06000FE4 RID: 4068 RVA: 0x000412EA File Offset: 0x0003F4EA
		public List<Building> LocalDevelopmentList { get; private set; }

		// Token: 0x06000FE5 RID: 4069 RVA: 0x000412F3 File Offset: 0x0003F4F3
		public SettlementProjectSelectionVM(Settlement settlement, Action onAnyChangeInQueue)
		{
			this._settlement = settlement;
			this._town = settlement.Town;
			this._onAnyChangeInQueue = onAnyChangeInQueue;
			this.RefreshValues();
		}

		// Token: 0x06000FE6 RID: 4070 RVA: 0x0004131C File Offset: 0x0003F51C
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.ProjectsText = new TextObject("{=LpsoPtOo}Projects", null).ToString();
			this.DailyDefaultsText = GameTexts.FindText("str_town_management_daily_defaults", null).ToString();
			this.DailyDefaultsExplanationText = GameTexts.FindText("str_town_management_daily_defaults_explanation", null).ToString();
			this.QueueText = GameTexts.FindText("str_town_management_queue", null).ToString();
			this.Refresh();
		}

		// Token: 0x06000FE7 RID: 4071 RVA: 0x00041390 File Offset: 0x0003F590
		public void Refresh()
		{
			this.AvailableProjects = new MBBindingList<SettlementBuildingProjectVM>();
			this.DailyDefaultList = new MBBindingList<SettlementDailyProjectVM>();
			this.LocalDevelopmentList = new List<Building>();
			this.CurrentDevelopmentQueue = new MBBindingList<SettlementBuildingProjectVM>();
			this.AvailableProjects.Clear();
			for (int i = 0; i < this._town.Buildings.Count; i++)
			{
				Building building = this._town.Buildings[i];
				if (!building.BuildingType.IsDailyProject)
				{
					SettlementBuildingProjectVM item = new SettlementBuildingProjectVM(new Action<SettlementProjectVM, bool>(this.OnCurrentProjectSelection), new Action<SettlementProjectVM>(this.OnCurrentProjectSet), new Action(this.OnResetCurrentProject), building, this._settlement);
					this.AvailableProjects.Add(item);
				}
				else
				{
					SettlementDailyProjectVM settlementDailyProjectVM = new SettlementDailyProjectVM(new Action<SettlementProjectVM, bool>(this.OnCurrentProjectSelection), new Action<SettlementProjectVM>(this.OnCurrentProjectSet), new Action(this.OnResetCurrentProject), building, this._settlement);
					this.DailyDefaultList.Add(settlementDailyProjectVM);
					if (settlementDailyProjectVM.Building == this._town.Buildings.FirstOrDefault((Building k) => k.IsCurrentlyDefault))
					{
						this.CurrentDailyDefault = settlementDailyProjectVM;
					}
				}
			}
			foreach (Building item2 in this._town.BuildingsInProgress)
			{
				this.LocalDevelopmentList.Add(item2);
			}
			this.RefreshDevelopmentsQueueIndex();
			this.RefreshCurrentSelectedProject();
		}

		// Token: 0x06000FE8 RID: 4072 RVA: 0x00041530 File Offset: 0x0003F730
		private void OnCurrentProjectSet(SettlementProjectVM selectedItem)
		{
			this.CurrentSelectedProject = selectedItem;
		}

		// Token: 0x06000FE9 RID: 4073 RVA: 0x0004153C File Offset: 0x0003F73C
		private void OnCurrentProjectSelection(SettlementProjectVM selectedItem, bool isSetAsActiveDevelopment)
		{
			if (!selectedItem.IsDaily)
			{
				if (isSetAsActiveDevelopment)
				{
					if (this.LocalDevelopmentList.Exists((Building d) => d == selectedItem.Building))
					{
						int num = this.LocalDevelopmentList.IndexOf(selectedItem.Building) - 1;
						while (0 <= num)
						{
							this.LocalDevelopmentList[num + 1] = this.LocalDevelopmentList[num];
							num--;
						}
						this.LocalDevelopmentList.RemoveAt(0);
					}
					this.LocalDevelopmentList.Insert(0, selectedItem.Building);
				}
				else if (this.LocalDevelopmentList.Exists((Building d) => d == selectedItem.Building))
				{
					this.LocalDevelopmentList.Remove(selectedItem.Building);
				}
				else
				{
					this.LocalDevelopmentList.Add(selectedItem.Building);
				}
			}
			else
			{
				this.CurrentDailyDefault = selectedItem as SettlementDailyProjectVM;
			}
			this.RefreshDevelopmentsQueueIndex();
			this.RefreshCurrentSelectedProject();
			Action onAnyChangeInQueue = this._onAnyChangeInQueue;
			if (onAnyChangeInQueue == null)
			{
				return;
			}
			onAnyChangeInQueue();
		}

		// Token: 0x06000FEA RID: 4074 RVA: 0x0004165B File Offset: 0x0003F85B
		private void OnResetCurrentProject()
		{
			this.RefreshCurrentSelectedProject();
		}

		// Token: 0x06000FEB RID: 4075 RVA: 0x00041664 File Offset: 0x0003F864
		private void RefreshCurrentSelectedProject()
		{
			if (this.LocalDevelopmentList.Count > 0)
			{
				for (int i = 0; i < this.AvailableProjects.Count; i++)
				{
					SettlementBuildingProjectVM settlementBuildingProjectVM = this.AvailableProjects[i];
					if (settlementBuildingProjectVM.Building == this.LocalDevelopmentList[0])
					{
						this.CurrentSelectedProject = settlementBuildingProjectVM;
						return;
					}
				}
				return;
			}
			this.CurrentSelectedProject = this.CurrentDailyDefault;
		}

		// Token: 0x06000FEC RID: 4076 RVA: 0x000416CC File Offset: 0x0003F8CC
		private void RefreshDevelopmentsQueueIndex()
		{
			this.CurrentDevelopmentQueue = new MBBindingList<SettlementBuildingProjectVM>();
			using (IEnumerator<SettlementBuildingProjectVM> enumerator = this.AvailableProjects.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					SettlementBuildingProjectVM item = enumerator.Current;
					item.DevelopmentQueueIndex = -1;
					item.IsInQueue = this.LocalDevelopmentList.Any((Building d) => d.BuildingType == item.Building.BuildingType);
					item.IsCurrentActiveProject = false;
					if (item.IsInQueue)
					{
						int num = this.LocalDevelopmentList.IndexOf(item.Building);
						item.DevelopmentQueueIndex = num;
						if (num == 0)
						{
							item.IsCurrentActiveProject = true;
						}
						this.CurrentDevelopmentQueue.Add(item);
					}
					item.RefreshProductionText();
				}
			}
			Comparer<SettlementBuildingProjectVM> comparer = Comparer<SettlementBuildingProjectVM>.Create((SettlementBuildingProjectVM s1, SettlementBuildingProjectVM s2) => s1.DevelopmentQueueIndex.CompareTo(s2.DevelopmentQueueIndex));
			this.CurrentDevelopmentQueue.Sort(comparer);
		}

		// Token: 0x06000FED RID: 4077 RVA: 0x000417F4 File Offset: 0x0003F9F4
		public void ExecuteChangeQueueOrder(SettlementBuildingProjectVM project, int index, string targetTag)
		{
			if (index == project.DevelopmentQueueIndex || targetTag != "CurrentDevelopmentQueue")
			{
				return;
			}
			this.LocalDevelopmentList.Remove(project.Building);
			if (index > project.DevelopmentQueueIndex)
			{
				this.LocalDevelopmentList.Insert(index - 1, project.Building);
			}
			else
			{
				this.LocalDevelopmentList.Insert(index, project.Building);
			}
			this.RefreshDevelopmentsQueueIndex();
			this.RefreshCurrentSelectedProject();
			Action onAnyChangeInQueue = this._onAnyChangeInQueue;
			if (onAnyChangeInQueue == null)
			{
				return;
			}
			onAnyChangeInQueue();
		}

		// Token: 0x17000522 RID: 1314
		// (get) Token: 0x06000FEE RID: 4078 RVA: 0x00041877 File Offset: 0x0003FA77
		// (set) Token: 0x06000FEF RID: 4079 RVA: 0x0004187F File Offset: 0x0003FA7F
		[DataSourceProperty]
		public string ProjectsText
		{
			get
			{
				return this._projectsText;
			}
			set
			{
				if (value != this._projectsText)
				{
					this._projectsText = value;
					base.OnPropertyChangedWithValue<string>(value, "ProjectsText");
				}
			}
		}

		// Token: 0x17000523 RID: 1315
		// (get) Token: 0x06000FF0 RID: 4080 RVA: 0x000418A2 File Offset: 0x0003FAA2
		// (set) Token: 0x06000FF1 RID: 4081 RVA: 0x000418AA File Offset: 0x0003FAAA
		[DataSourceProperty]
		public string QueueText
		{
			get
			{
				return this._queueText;
			}
			set
			{
				if (value != this._queueText)
				{
					this._queueText = value;
					base.OnPropertyChangedWithValue<string>(value, "QueueText");
				}
			}
		}

		// Token: 0x17000524 RID: 1316
		// (get) Token: 0x06000FF2 RID: 4082 RVA: 0x000418CD File Offset: 0x0003FACD
		// (set) Token: 0x06000FF3 RID: 4083 RVA: 0x000418D5 File Offset: 0x0003FAD5
		[DataSourceProperty]
		public string DailyDefaultsText
		{
			get
			{
				return this._dailyDefaultsText;
			}
			set
			{
				if (value != this._dailyDefaultsText)
				{
					this._dailyDefaultsText = value;
					base.OnPropertyChangedWithValue<string>(value, "DailyDefaultsText");
				}
			}
		}

		// Token: 0x17000525 RID: 1317
		// (get) Token: 0x06000FF4 RID: 4084 RVA: 0x000418F8 File Offset: 0x0003FAF8
		// (set) Token: 0x06000FF5 RID: 4085 RVA: 0x00041900 File Offset: 0x0003FB00
		[DataSourceProperty]
		public string DailyDefaultsExplanationText
		{
			get
			{
				return this._dailyDefaultsExplanationText;
			}
			set
			{
				if (value != this._dailyDefaultsExplanationText)
				{
					this._dailyDefaultsExplanationText = value;
					base.OnPropertyChangedWithValue<string>(value, "DailyDefaultsExplanationText");
				}
			}
		}

		// Token: 0x17000526 RID: 1318
		// (get) Token: 0x06000FF6 RID: 4086 RVA: 0x00041923 File Offset: 0x0003FB23
		// (set) Token: 0x06000FF7 RID: 4087 RVA: 0x0004192B File Offset: 0x0003FB2B
		[DataSourceProperty]
		public SettlementProjectVM CurrentSelectedProject
		{
			get
			{
				return this._currentSelectedProject;
			}
			set
			{
				if (value != this._currentSelectedProject)
				{
					this._currentSelectedProject = value;
					base.OnPropertyChangedWithValue<SettlementProjectVM>(value, "CurrentSelectedProject");
					if (this._currentSelectedProject != null)
					{
						this._currentSelectedProject.RefreshProductionText();
					}
				}
			}
		}

		// Token: 0x17000527 RID: 1319
		// (get) Token: 0x06000FF8 RID: 4088 RVA: 0x0004195C File Offset: 0x0003FB5C
		// (set) Token: 0x06000FF9 RID: 4089 RVA: 0x00041964 File Offset: 0x0003FB64
		[DataSourceProperty]
		public SettlementDailyProjectVM CurrentDailyDefault
		{
			get
			{
				return this._currentDailyDefault;
			}
			set
			{
				if (value != this._currentDailyDefault)
				{
					if (this._currentDailyDefault != null)
					{
						this._currentDailyDefault.IsDefault = false;
					}
					this._currentDailyDefault = value;
					base.OnPropertyChangedWithValue<SettlementDailyProjectVM>(value, "CurrentDailyDefault");
					if (this._currentDailyDefault != null)
					{
						this._currentDailyDefault.IsDefault = true;
					}
				}
			}
		}

		// Token: 0x17000528 RID: 1320
		// (get) Token: 0x06000FFA RID: 4090 RVA: 0x000419B5 File Offset: 0x0003FBB5
		// (set) Token: 0x06000FFB RID: 4091 RVA: 0x000419BD File Offset: 0x0003FBBD
		[DataSourceProperty]
		public MBBindingList<SettlementBuildingProjectVM> AvailableProjects
		{
			get
			{
				return this._availableProjects;
			}
			set
			{
				if (value != this._availableProjects)
				{
					this._availableProjects = value;
					base.OnPropertyChangedWithValue<MBBindingList<SettlementBuildingProjectVM>>(value, "AvailableProjects");
				}
			}
		}

		// Token: 0x17000529 RID: 1321
		// (get) Token: 0x06000FFC RID: 4092 RVA: 0x000419DB File Offset: 0x0003FBDB
		// (set) Token: 0x06000FFD RID: 4093 RVA: 0x000419E3 File Offset: 0x0003FBE3
		[DataSourceProperty]
		public MBBindingList<SettlementBuildingProjectVM> CurrentDevelopmentQueue
		{
			get
			{
				return this._currentDevelopmentQueue;
			}
			set
			{
				if (value != this._currentDevelopmentQueue)
				{
					this._currentDevelopmentQueue = value;
					base.OnPropertyChangedWithValue<MBBindingList<SettlementBuildingProjectVM>>(value, "CurrentDevelopmentQueue");
				}
			}
		}

		// Token: 0x1700052A RID: 1322
		// (get) Token: 0x06000FFE RID: 4094 RVA: 0x00041A01 File Offset: 0x0003FC01
		// (set) Token: 0x06000FFF RID: 4095 RVA: 0x00041A09 File Offset: 0x0003FC09
		[DataSourceProperty]
		public MBBindingList<SettlementDailyProjectVM> DailyDefaultList
		{
			get
			{
				return this._dailyDefaultList;
			}
			set
			{
				if (value != this._dailyDefaultList)
				{
					this._dailyDefaultList = value;
					base.OnPropertyChangedWithValue<MBBindingList<SettlementDailyProjectVM>>(value, "DailyDefaultList");
				}
			}
		}

		// Token: 0x0400073E RID: 1854
		private readonly Town _town;

		// Token: 0x0400073F RID: 1855
		private readonly Settlement _settlement;

		// Token: 0x04000740 RID: 1856
		private readonly Action _onAnyChangeInQueue;

		// Token: 0x04000742 RID: 1858
		private SettlementDailyProjectVM _currentDailyDefault;

		// Token: 0x04000743 RID: 1859
		private SettlementProjectVM _currentSelectedProject;

		// Token: 0x04000744 RID: 1860
		private MBBindingList<SettlementDailyProjectVM> _dailyDefaultList;

		// Token: 0x04000745 RID: 1861
		private MBBindingList<SettlementBuildingProjectVM> _currentDevelopmentQueue;

		// Token: 0x04000746 RID: 1862
		private MBBindingList<SettlementBuildingProjectVM> _availableProjects;

		// Token: 0x04000747 RID: 1863
		private string _projectsText;

		// Token: 0x04000748 RID: 1864
		private string _queueText;

		// Token: 0x04000749 RID: 1865
		private string _dailyDefaultsText;

		// Token: 0x0400074A RID: 1866
		private string _dailyDefaultsExplanationText;
	}
}
