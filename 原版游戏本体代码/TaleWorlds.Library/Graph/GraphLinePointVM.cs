using System;

namespace TaleWorlds.Library.Graph
{
	// Token: 0x020000B5 RID: 181
	public class GraphLinePointVM : ViewModel
	{
		// Token: 0x060006BD RID: 1725 RVA: 0x00016F59 File Offset: 0x00015159
		public GraphLinePointVM(float horizontalValue, float verticalValue)
		{
			this.HorizontalValue = horizontalValue;
			this.VerticalValue = verticalValue;
		}

		// Token: 0x170000CA RID: 202
		// (get) Token: 0x060006BE RID: 1726 RVA: 0x00016F6F File Offset: 0x0001516F
		// (set) Token: 0x060006BF RID: 1727 RVA: 0x00016F77 File Offset: 0x00015177
		[DataSourceProperty]
		public float HorizontalValue
		{
			get
			{
				return this._horizontalValue;
			}
			set
			{
				if (value != this._horizontalValue)
				{
					this._horizontalValue = value;
					base.OnPropertyChangedWithValue(value, "HorizontalValue");
				}
			}
		}

		// Token: 0x170000CB RID: 203
		// (get) Token: 0x060006C0 RID: 1728 RVA: 0x00016F95 File Offset: 0x00015195
		// (set) Token: 0x060006C1 RID: 1729 RVA: 0x00016F9D File Offset: 0x0001519D
		[DataSourceProperty]
		public float VerticalValue
		{
			get
			{
				return this._verticalValue;
			}
			set
			{
				if (value != this._verticalValue)
				{
					this._verticalValue = value;
					base.OnPropertyChangedWithValue(value, "VerticalValue");
				}
			}
		}

		// Token: 0x0400020E RID: 526
		private float _horizontalValue;

		// Token: 0x0400020F RID: 527
		private float _verticalValue;
	}
}
