using System;

namespace TaleWorlds.GauntletUI.BaseTypes
{
	// Token: 0x0200006A RID: 106
	public class TabToggleWidget : ButtonWidget
	{
		// Token: 0x1700020D RID: 525
		// (get) Token: 0x0600073D RID: 1853 RVA: 0x0001F0F3 File Offset: 0x0001D2F3
		// (set) Token: 0x0600073E RID: 1854 RVA: 0x0001F0FB File Offset: 0x0001D2FB
		public TabControl TabControlWidget { get; set; }

		// Token: 0x0600073F RID: 1855 RVA: 0x0001F104 File Offset: 0x0001D304
		public TabToggleWidget(UIContext context)
			: base(context)
		{
		}

		// Token: 0x06000740 RID: 1856 RVA: 0x0001F10D File Offset: 0x0001D30D
		protected override void HandleClick()
		{
			base.HandleClick();
			if (this.TabControlWidget != null && !string.IsNullOrEmpty(this.TabName))
			{
				this.TabControlWidget.SetActiveTab(this.TabName);
			}
		}

		// Token: 0x06000741 RID: 1857 RVA: 0x0001F13C File Offset: 0x0001D33C
		protected override void OnLateUpdate(float dt)
		{
			base.OnLateUpdate(dt);
			bool isDisabled = false;
			if (this.TabControlWidget == null || string.IsNullOrEmpty(this.TabName))
			{
				isDisabled = true;
			}
			else
			{
				Widget widget = this.TabControlWidget.FindChild(this.TabName);
				if (widget == null || widget.IsDisabled)
				{
					isDisabled = true;
				}
			}
			base.IsDisabled = isDisabled;
			base.IsSelected = this.DetermineIfIsSelected();
		}

		// Token: 0x06000742 RID: 1858 RVA: 0x0001F1A0 File Offset: 0x0001D3A0
		private bool DetermineIfIsSelected()
		{
			TabControl tabControlWidget = this.TabControlWidget;
			return ((tabControlWidget != null) ? tabControlWidget.ActiveTab : null) != null && !string.IsNullOrEmpty(this.TabName) && this.TabControlWidget.ActiveTab.Id == this.TabName && base.IsVisible;
		}

		// Token: 0x1700020E RID: 526
		// (get) Token: 0x06000743 RID: 1859 RVA: 0x0001F1F3 File Offset: 0x0001D3F3
		// (set) Token: 0x06000744 RID: 1860 RVA: 0x0001F1FB File Offset: 0x0001D3FB
		[Editor(false)]
		public string TabName
		{
			get
			{
				return this._tabName;
			}
			set
			{
				if (this._tabName != value)
				{
					this._tabName = value;
					base.OnPropertyChanged<string>(value, "TabName");
				}
			}
		}

		// Token: 0x0400035F RID: 863
		private string _tabName;
	}
}
