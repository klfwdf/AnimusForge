using System;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.ExtraWidgets
{
	// Token: 0x02000018 RID: 24
	public class ValueBasedVisibilityWidget : Widget
	{
		// Token: 0x17000076 RID: 118
		// (get) Token: 0x0600013D RID: 317 RVA: 0x000073F9 File Offset: 0x000055F9
		// (set) Token: 0x0600013E RID: 318 RVA: 0x00007401 File Offset: 0x00005601
		public ValueBasedVisibilityWidget.WatchTypes WatchType
		{
			get
			{
				return this._watchType;
			}
			set
			{
				if (value != this._watchType)
				{
					this._watchType = value;
					this.UpdateIsVisible();
				}
			}
		}

		// Token: 0x0600013F RID: 319 RVA: 0x00007419 File Offset: 0x00005619
		public ValueBasedVisibilityWidget(UIContext context)
			: base(context)
		{
			this.UpdateIsVisible();
		}

		// Token: 0x06000140 RID: 320 RVA: 0x00007434 File Offset: 0x00005634
		private void UpdateIsVisible()
		{
			switch (this.WatchType)
			{
			case ValueBasedVisibilityWidget.WatchTypes.Equal:
				base.IsVisible = this.IndexToWatchFloat == this.IndexToBeVisibleFloat;
				return;
			case ValueBasedVisibilityWidget.WatchTypes.BiggerThan:
				base.IsVisible = this.IndexToWatchFloat > this.IndexToBeVisibleFloat;
				return;
			case ValueBasedVisibilityWidget.WatchTypes.BiggerThanEqual:
				base.IsVisible = this.IndexToWatchFloat >= this.IndexToBeVisibleFloat;
				return;
			case ValueBasedVisibilityWidget.WatchTypes.LessThan:
				base.IsVisible = this.IndexToWatchFloat < this.IndexToBeVisibleFloat;
				return;
			case ValueBasedVisibilityWidget.WatchTypes.LessThanEqual:
				base.IsVisible = this.IndexToWatchFloat <= this.IndexToBeVisibleFloat;
				return;
			case ValueBasedVisibilityWidget.WatchTypes.NotEqual:
				base.IsVisible = this.IndexToWatchFloat != this.IndexToBeVisibleFloat;
				return;
			default:
				return;
			}
		}

		// Token: 0x17000077 RID: 119
		// (get) Token: 0x06000141 RID: 321 RVA: 0x000074ED File Offset: 0x000056ED
		// (set) Token: 0x06000142 RID: 322 RVA: 0x000074F6 File Offset: 0x000056F6
		[Editor(false)]
		public int IndexToWatch
		{
			get
			{
				return (int)this.IndexToWatchFloat;
			}
			set
			{
				this.IndexToWatchFloat = (float)value;
			}
		}

		// Token: 0x17000078 RID: 120
		// (get) Token: 0x06000143 RID: 323 RVA: 0x00007500 File Offset: 0x00005700
		// (set) Token: 0x06000144 RID: 324 RVA: 0x00007508 File Offset: 0x00005708
		[Editor(false)]
		public float IndexToWatchFloat
		{
			get
			{
				return this._indexToWatchFloat;
			}
			set
			{
				if (this._indexToWatchFloat != value)
				{
					this._indexToWatchFloat = value;
					base.OnPropertyChanged(value, "IndexToWatchFloat");
					this.UpdateIsVisible();
				}
			}
		}

		// Token: 0x17000079 RID: 121
		// (get) Token: 0x06000145 RID: 325 RVA: 0x0000752C File Offset: 0x0000572C
		// (set) Token: 0x06000146 RID: 326 RVA: 0x00007535 File Offset: 0x00005735
		[Editor(false)]
		public int IndexToBeVisible
		{
			get
			{
				return (int)this.IndexToBeVisibleFloat;
			}
			set
			{
				this.IndexToBeVisibleFloat = (float)value;
			}
		}

		// Token: 0x1700007A RID: 122
		// (get) Token: 0x06000147 RID: 327 RVA: 0x0000753F File Offset: 0x0000573F
		// (set) Token: 0x06000148 RID: 328 RVA: 0x00007547 File Offset: 0x00005747
		[Editor(false)]
		public float IndexToBeVisibleFloat
		{
			get
			{
				return this._indexToBeVisibleFloat;
			}
			set
			{
				if (this._indexToBeVisibleFloat != value)
				{
					this._indexToBeVisibleFloat = value;
					base.OnPropertyChanged(value, "IndexToBeVisibleFloat");
					this.UpdateIsVisible();
				}
			}
		}

		// Token: 0x0400009B RID: 155
		private ValueBasedVisibilityWidget.WatchTypes _watchType;

		// Token: 0x0400009C RID: 156
		private float _indexToBeVisibleFloat;

		// Token: 0x0400009D RID: 157
		private float _indexToWatchFloat = -1f;

		// Token: 0x02000020 RID: 32
		public enum WatchTypes
		{
			// Token: 0x040000CD RID: 205
			Equal,
			// Token: 0x040000CE RID: 206
			BiggerThan,
			// Token: 0x040000CF RID: 207
			BiggerThanEqual,
			// Token: 0x040000D0 RID: 208
			LessThan,
			// Token: 0x040000D1 RID: 209
			LessThanEqual,
			// Token: 0x040000D2 RID: 210
			NotEqual
		}
	}
}
