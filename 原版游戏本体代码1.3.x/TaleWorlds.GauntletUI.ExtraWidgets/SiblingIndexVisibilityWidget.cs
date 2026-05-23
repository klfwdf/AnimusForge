using System;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.ExtraWidgets
{
	// Token: 0x02000011 RID: 17
	public class SiblingIndexVisibilityWidget : Widget
	{
		// Token: 0x17000066 RID: 102
		// (get) Token: 0x06000109 RID: 265 RVA: 0x000065D7 File Offset: 0x000047D7
		// (set) Token: 0x0600010A RID: 266 RVA: 0x000065DF File Offset: 0x000047DF
		public SiblingIndexVisibilityWidget.WatchTypes WatchType { get; set; }

		// Token: 0x0600010B RID: 267 RVA: 0x000065E8 File Offset: 0x000047E8
		public SiblingIndexVisibilityWidget(UIContext context)
			: base(context)
		{
		}

		// Token: 0x0600010C RID: 268 RVA: 0x000065F1 File Offset: 0x000047F1
		protected override void OnLateUpdate(float dt)
		{
			base.OnLateUpdate(dt);
			this.UpdateVisibility();
		}

		// Token: 0x0600010D RID: 269 RVA: 0x00006600 File Offset: 0x00004800
		private void UpdateVisibility()
		{
			Widget widget = this.WidgetToWatch ?? this;
			if (((widget != null) ? widget.ParentWidget : null) != null)
			{
				switch (this.WatchType)
				{
				case SiblingIndexVisibilityWidget.WatchTypes.Equal:
					base.IsVisible = widget.GetSiblingIndex() == this.IndexToBeVisible;
					return;
				case SiblingIndexVisibilityWidget.WatchTypes.BiggerThan:
					base.IsVisible = widget.GetSiblingIndex() > this.IndexToBeVisible;
					return;
				case SiblingIndexVisibilityWidget.WatchTypes.BiggerThanEqual:
					base.IsVisible = widget.GetSiblingIndex() >= this.IndexToBeVisible;
					return;
				case SiblingIndexVisibilityWidget.WatchTypes.LessThan:
					base.IsVisible = widget.GetSiblingIndex() < this.IndexToBeVisible;
					return;
				case SiblingIndexVisibilityWidget.WatchTypes.LessThanEqual:
					base.IsVisible = widget.GetSiblingIndex() <= this.IndexToBeVisible;
					return;
				case SiblingIndexVisibilityWidget.WatchTypes.Odd:
					base.IsVisible = widget.GetSiblingIndex() % 2 == 1;
					return;
				case SiblingIndexVisibilityWidget.WatchTypes.Even:
					base.IsVisible = widget.GetSiblingIndex() % 2 == 0;
					break;
				default:
					return;
				}
			}
		}

		// Token: 0x0600010E RID: 270 RVA: 0x000066E6 File Offset: 0x000048E6
		private void OnWidgetToWatchParentEventFired(Widget arg1, string arg2, object[] arg3)
		{
			if (arg2 == "ItemAdd" || arg2 == "ItemRemove")
			{
				this.UpdateVisibility();
			}
		}

		// Token: 0x17000067 RID: 103
		// (get) Token: 0x0600010F RID: 271 RVA: 0x00006708 File Offset: 0x00004908
		// (set) Token: 0x06000110 RID: 272 RVA: 0x00006710 File Offset: 0x00004910
		[Editor(false)]
		public int IndexToBeVisible
		{
			get
			{
				return this._indexToBeVisible;
			}
			set
			{
				if (this._indexToBeVisible != value)
				{
					this._indexToBeVisible = value;
					base.OnPropertyChanged(value, "IndexToBeVisible");
				}
			}
		}

		// Token: 0x17000068 RID: 104
		// (get) Token: 0x06000111 RID: 273 RVA: 0x0000672E File Offset: 0x0000492E
		// (set) Token: 0x06000112 RID: 274 RVA: 0x00006736 File Offset: 0x00004936
		[Editor(false)]
		public Widget WidgetToWatch
		{
			get
			{
				return this._widgetToWatch;
			}
			set
			{
				if (this._widgetToWatch != value)
				{
					this._widgetToWatch = value;
					base.OnPropertyChanged<Widget>(value, "WidgetToWatch");
					value.ParentWidget.EventFire += this.OnWidgetToWatchParentEventFired;
					this.UpdateVisibility();
				}
			}
		}

		// Token: 0x0400007E RID: 126
		private Widget _widgetToWatch;

		// Token: 0x0400007F RID: 127
		private int _indexToBeVisible;

		// Token: 0x0200001D RID: 29
		public enum WatchTypes
		{
			// Token: 0x040000BE RID: 190
			Equal,
			// Token: 0x040000BF RID: 191
			BiggerThan,
			// Token: 0x040000C0 RID: 192
			BiggerThanEqual,
			// Token: 0x040000C1 RID: 193
			LessThan,
			// Token: 0x040000C2 RID: 194
			LessThanEqual,
			// Token: 0x040000C3 RID: 195
			Odd,
			// Token: 0x040000C4 RID: 196
			Even
		}
	}
}
