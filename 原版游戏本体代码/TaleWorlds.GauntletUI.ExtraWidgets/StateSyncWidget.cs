using System;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.ExtraWidgets
{
	// Token: 0x02000013 RID: 19
	public class StateSyncWidget : BrushWidget
	{
		// Token: 0x0600011C RID: 284 RVA: 0x00006A4C File Offset: 0x00004C4C
		public StateSyncWidget(UIContext context)
			: base(context)
		{
		}

		// Token: 0x0600011D RID: 285 RVA: 0x00006A55 File Offset: 0x00004C55
		protected override void OnUpdate(float dt)
		{
			base.OnUpdate(dt);
			Widget widget = this.TargetWidget ?? this;
			Widget sourceWidget = this.SourceWidget;
			widget.SetState(((sourceWidget != null) ? sourceWidget.CurrentState : null) ?? "Default");
		}

		// Token: 0x1700006C RID: 108
		// (get) Token: 0x0600011E RID: 286 RVA: 0x00006A89 File Offset: 0x00004C89
		// (set) Token: 0x0600011F RID: 287 RVA: 0x00006A91 File Offset: 0x00004C91
		[Editor(false)]
		public Widget SourceWidget
		{
			get
			{
				return this._sourceWidget;
			}
			set
			{
				if (this._sourceWidget != value)
				{
					this._sourceWidget = value;
					base.OnPropertyChanged<Widget>(value, "SourceWidget");
				}
			}
		}

		// Token: 0x1700006D RID: 109
		// (get) Token: 0x06000120 RID: 288 RVA: 0x00006AAF File Offset: 0x00004CAF
		// (set) Token: 0x06000121 RID: 289 RVA: 0x00006AB7 File Offset: 0x00004CB7
		[Editor(false)]
		public Widget TargetWidget
		{
			get
			{
				return this._targetWidget;
			}
			set
			{
				if (this._targetWidget != value)
				{
					this._targetWidget = value;
					base.OnPropertyChanged<Widget>(value, "TargetWidget");
				}
			}
		}

		// Token: 0x04000085 RID: 133
		private Widget _sourceWidget;

		// Token: 0x04000086 RID: 134
		private Widget _targetWidget;
	}
}
