using System;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.BaseTypes
{
	// Token: 0x02000069 RID: 105
	public class TabControl : Widget
	{
		// Token: 0x1400000F RID: 15
		// (add) Token: 0x06000731 RID: 1841 RVA: 0x0001EEB4 File Offset: 0x0001D0B4
		// (remove) Token: 0x06000732 RID: 1842 RVA: 0x0001EEEC File Offset: 0x0001D0EC
		public event OnActiveTabChangeEvent OnActiveTabChange;

		// Token: 0x06000733 RID: 1843 RVA: 0x0001EF21 File Offset: 0x0001D121
		public TabControl(UIContext context)
			: base(context)
		{
		}

		// Token: 0x06000734 RID: 1844 RVA: 0x0001EF2A File Offset: 0x0001D12A
		protected override void OnBeforeChildRemoved(Widget child)
		{
			base.OnBeforeChildRemoved(child);
			if (child == this.ActiveTab)
			{
				this.ActiveTab = null;
			}
		}

		// Token: 0x1700020B RID: 523
		// (get) Token: 0x06000735 RID: 1845 RVA: 0x0001EF43 File Offset: 0x0001D143
		// (set) Token: 0x06000736 RID: 1846 RVA: 0x0001EF4B File Offset: 0x0001D14B
		[Editor(false)]
		public Widget ActiveTab
		{
			get
			{
				return this._activeTab;
			}
			private set
			{
				if (this._activeTab != value)
				{
					this._activeTab = value;
					OnActiveTabChangeEvent onActiveTabChange = this.OnActiveTabChange;
					if (onActiveTabChange == null)
					{
						return;
					}
					onActiveTabChange();
				}
			}
		}

		// Token: 0x06000737 RID: 1847 RVA: 0x0001EF70 File Offset: 0x0001D170
		private void SetActiveTab(int index)
		{
			Widget child = base.GetChild(index);
			this.SetActiveTab(child);
		}

		// Token: 0x06000738 RID: 1848 RVA: 0x0001EF8C File Offset: 0x0001D18C
		public void SetActiveTab(string tabName)
		{
			Widget activeTab = base.FindChild(tabName);
			this.SetActiveTab(activeTab);
		}

		// Token: 0x06000739 RID: 1849 RVA: 0x0001EFA8 File Offset: 0x0001D1A8
		private void SetActiveTab(Widget newTab)
		{
			if (this.ActiveTab != newTab && newTab != null)
			{
				if (this.ActiveTab != null)
				{
					this.ActiveTab.IsVisible = false;
				}
				this.ActiveTab = newTab;
				this.ActiveTab.IsVisible = true;
				this.SelectedIndex = base.GetChildIndex(this.ActiveTab);
			}
		}

		// Token: 0x0600073A RID: 1850 RVA: 0x0001EFFC File Offset: 0x0001D1FC
		protected override void OnUpdate(float dt)
		{
			base.OnUpdate(dt);
			if (this.ActiveTab != null && this.ActiveTab.ParentWidget == null)
			{
				this.ActiveTab = null;
			}
			if (this.ActiveTab == null || this.ActiveTab.IsDisabled)
			{
				for (int i = 0; i < base.ChildCount; i++)
				{
					Widget child = base.GetChild(i);
					if (child.IsEnabled && !string.IsNullOrEmpty(child.Id))
					{
						this.ActiveTab = child;
						break;
					}
				}
			}
			for (int j = 0; j < base.ChildCount; j++)
			{
				Widget child2 = base.GetChild(j);
				if (this.ActiveTab != child2 && (child2.IsEnabled || child2.IsVisible))
				{
					child2.IsVisible = false;
				}
				if (this.ActiveTab == child2)
				{
					child2.IsVisible = true;
				}
			}
		}

		// Token: 0x1700020C RID: 524
		// (get) Token: 0x0600073B RID: 1851 RVA: 0x0001F0C1 File Offset: 0x0001D2C1
		// (set) Token: 0x0600073C RID: 1852 RVA: 0x0001F0C9 File Offset: 0x0001D2C9
		[DataSourceProperty]
		public int SelectedIndex
		{
			get
			{
				return this._selectedIndex;
			}
			set
			{
				if (this._selectedIndex != value)
				{
					this._selectedIndex = value;
					this.SetActiveTab(this._selectedIndex);
					base.OnPropertyChanged(value, "SelectedIndex");
				}
			}
		}

		// Token: 0x0400035C RID: 860
		private Widget _activeTab;

		// Token: 0x0400035D RID: 861
		private int _selectedIndex;
	}
}
