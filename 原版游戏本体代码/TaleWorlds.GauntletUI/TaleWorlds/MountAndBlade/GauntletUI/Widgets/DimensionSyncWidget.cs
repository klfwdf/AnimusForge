using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets
{
	// Token: 0x02000007 RID: 7
	public class DimensionSyncWidget : Widget
	{
		// Token: 0x0600003D RID: 61 RVA: 0x00002212 File Offset: 0x00000412
		public DimensionSyncWidget(UIContext context)
			: base(context)
		{
			base.EventManager.AddLateUpdateAction(this, new Action<float>(this.UpdateDimensions), 5);
		}

		// Token: 0x0600003E RID: 62 RVA: 0x00002234 File Offset: 0x00000434
		private void UpdateDimensions(float dt)
		{
			if (this.DimensionToSync != DimensionSyncWidget.Dimensions.None && this.WidgetToCopyHeightFrom != null)
			{
				if (this._isLayoutDirty)
				{
					if (base.IsRecursivelyVisible())
					{
						this._isLayoutDirty = false;
					}
				}
				else
				{
					if (this.DimensionToSync == DimensionSyncWidget.Dimensions.Horizontal || this.DimensionToSync == DimensionSyncWidget.Dimensions.HorizontalAndVertical)
					{
						base.ScaledSuggestedWidth = this.WidgetToCopyHeightFrom.Size.X + (float)this.PaddingAmount * base._scaleToUse;
					}
					if (this.DimensionToSync == DimensionSyncWidget.Dimensions.Vertical || this.DimensionToSync == DimensionSyncWidget.Dimensions.HorizontalAndVertical)
					{
						base.ScaledSuggestedHeight = this.WidgetToCopyHeightFrom.Size.Y + (float)this.PaddingAmount * base._scaleToUse;
					}
				}
			}
			base.EventManager.AddLateUpdateAction(this, new Action<float>(this.UpdateDimensions), 5);
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000022F8 File Offset: 0x000004F8
		protected override void OnLayoutUpdated()
		{
			base.OnLayoutUpdated();
			this._isLayoutDirty = true;
			if (this.DimensionToSync == DimensionSyncWidget.Dimensions.Horizontal || this.DimensionToSync == DimensionSyncWidget.Dimensions.HorizontalAndVertical)
			{
				base.ScaledSuggestedWidth = 0f;
			}
			if (this.DimensionToSync == DimensionSyncWidget.Dimensions.Vertical || this.DimensionToSync == DimensionSyncWidget.Dimensions.HorizontalAndVertical)
			{
				base.ScaledSuggestedHeight = 0f;
			}
		}

		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000040 RID: 64 RVA: 0x0000234C File Offset: 0x0000054C
		// (set) Token: 0x06000041 RID: 65 RVA: 0x00002354 File Offset: 0x00000554
		public Widget WidgetToCopyHeightFrom
		{
			get
			{
				return this._widgetToCopyHeightFrom;
			}
			set
			{
				if (this._widgetToCopyHeightFrom != value)
				{
					this._widgetToCopyHeightFrom = value;
					base.OnPropertyChanged<Widget>(value, "WidgetToCopyHeightFrom");
				}
			}
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000042 RID: 66 RVA: 0x00002372 File Offset: 0x00000572
		// (set) Token: 0x06000043 RID: 67 RVA: 0x0000237A File Offset: 0x0000057A
		public int PaddingAmount
		{
			get
			{
				return this._paddingAmount;
			}
			set
			{
				if (this._paddingAmount != value)
				{
					this._paddingAmount = value;
				}
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000044 RID: 68 RVA: 0x0000238C File Offset: 0x0000058C
		// (set) Token: 0x06000045 RID: 69 RVA: 0x00002394 File Offset: 0x00000594
		public DimensionSyncWidget.Dimensions DimensionToSync
		{
			get
			{
				return this._dimensionToSync;
			}
			set
			{
				if (this._dimensionToSync != value)
				{
					this._dimensionToSync = value;
				}
			}
		}

		// Token: 0x04000004 RID: 4
		private bool _isLayoutDirty;

		// Token: 0x04000005 RID: 5
		private Widget _widgetToCopyHeightFrom;

		// Token: 0x04000006 RID: 6
		private DimensionSyncWidget.Dimensions _dimensionToSync;

		// Token: 0x04000007 RID: 7
		private int _paddingAmount;

		// Token: 0x02000070 RID: 112
		public enum Dimensions
		{
			// Token: 0x040003E5 RID: 997
			None,
			// Token: 0x040003E6 RID: 998
			Horizontal,
			// Token: 0x040003E7 RID: 999
			Vertical,
			// Token: 0x040003E8 RID: 1000
			HorizontalAndVertical
		}
	}
}
