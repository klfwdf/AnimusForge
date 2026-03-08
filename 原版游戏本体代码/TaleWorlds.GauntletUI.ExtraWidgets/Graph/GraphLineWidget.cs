using System;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.ExtraWidgets.Graph
{
	// Token: 0x0200001A RID: 26
	public class GraphLineWidget : Widget
	{
		// Token: 0x1700007D RID: 125
		// (get) Token: 0x0600014E RID: 334 RVA: 0x00007596 File Offset: 0x00005796
		// (set) Token: 0x0600014F RID: 335 RVA: 0x0000759E File Offset: 0x0000579E
		public string LineBrushStateName { get; set; }

		// Token: 0x06000150 RID: 336 RVA: 0x000075A7 File Offset: 0x000057A7
		public GraphLineWidget(UIContext context)
			: base(context)
		{
		}

		// Token: 0x06000151 RID: 337 RVA: 0x000075B0 File Offset: 0x000057B0
		private void OnPointContainerEventFire(Widget widget, string eventName, object[] eventArgs)
		{
			GraphLinePointWidget arg;
			if (eventName == "ItemAdd" && eventArgs.Length != 0 && (arg = eventArgs[0] as GraphLinePointWidget) != null)
			{
				Action<GraphLineWidget, GraphLinePointWidget> onPointAdded = this.OnPointAdded;
				if (onPointAdded == null)
				{
					return;
				}
				onPointAdded(this, arg);
			}
		}

		// Token: 0x1700007E RID: 126
		// (get) Token: 0x06000152 RID: 338 RVA: 0x000075EC File Offset: 0x000057EC
		// (set) Token: 0x06000153 RID: 339 RVA: 0x000075F4 File Offset: 0x000057F4
		public Widget PointContainerWidget
		{
			get
			{
				return this._pointContainerWidget;
			}
			set
			{
				if (value != this._pointContainerWidget)
				{
					if (this._pointContainerWidget != null)
					{
						this._pointContainerWidget.EventFire -= this.OnPointContainerEventFire;
					}
					this._pointContainerWidget = value;
					if (this._pointContainerWidget != null)
					{
						this._pointContainerWidget.EventFire += this.OnPointContainerEventFire;
					}
					base.OnPropertyChanged<Widget>(value, "PointContainerWidget");
				}
			}
		}

		// Token: 0x040000A1 RID: 161
		public Action<GraphLineWidget, GraphLinePointWidget> OnPointAdded;

		// Token: 0x040000A2 RID: 162
		private Widget _pointContainerWidget;
	}
}
