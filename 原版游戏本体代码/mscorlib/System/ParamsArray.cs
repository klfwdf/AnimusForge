using System;

namespace System
{
	// Token: 0x02000120 RID: 288
	internal struct ParamsArray
	{
		// Token: 0x060010DC RID: 4316 RVA: 0x00032CD7 File Offset: 0x00030ED7
		public ParamsArray(object arg0)
		{
			this.arg0 = arg0;
			this.arg1 = null;
			this.arg2 = null;
			this.args = ParamsArray.oneArgArray;
		}

		// Token: 0x060010DD RID: 4317 RVA: 0x00032CF9 File Offset: 0x00030EF9
		public ParamsArray(object arg0, object arg1)
		{
			this.arg0 = arg0;
			this.arg1 = arg1;
			this.arg2 = null;
			this.args = ParamsArray.twoArgArray;
		}

		// Token: 0x060010DE RID: 4318 RVA: 0x00032D1B File Offset: 0x00030F1B
		public ParamsArray(object arg0, object arg1, object arg2)
		{
			this.arg0 = arg0;
			this.arg1 = arg1;
			this.arg2 = arg2;
			this.args = ParamsArray.threeArgArray;
		}

		// Token: 0x060010DF RID: 4319 RVA: 0x00032D40 File Offset: 0x00030F40
		public ParamsArray(object[] args)
		{
			int num = args.Length;
			this.arg0 = ((num > 0) ? args[0] : null);
			this.arg1 = ((num > 1) ? args[1] : null);
			this.arg2 = ((num > 2) ? args[2] : null);
			this.args = args;
		}

		// Token: 0x170001C7 RID: 455
		// (get) Token: 0x060010E0 RID: 4320 RVA: 0x00032D88 File Offset: 0x00030F88
		public int Length
		{
			get
			{
				return this.args.Length;
			}
		}

		// Token: 0x170001C8 RID: 456
		public object this[int index]
		{
			get
			{
				if (index != 0)
				{
					return this.GetAtSlow(index);
				}
				return this.arg0;
			}
		}

		// Token: 0x060010E2 RID: 4322 RVA: 0x00032DA5 File Offset: 0x00030FA5
		private object GetAtSlow(int index)
		{
			if (index == 1)
			{
				return this.arg1;
			}
			if (index == 2)
			{
				return this.arg2;
			}
			return this.args[index];
		}

		// Token: 0x040005D4 RID: 1492
		private static readonly object[] oneArgArray = new object[1];

		// Token: 0x040005D5 RID: 1493
		private static readonly object[] twoArgArray = new object[2];

		// Token: 0x040005D6 RID: 1494
		private static readonly object[] threeArgArray = new object[3];

		// Token: 0x040005D7 RID: 1495
		private readonly object arg0;

		// Token: 0x040005D8 RID: 1496
		private readonly object arg1;

		// Token: 0x040005D9 RID: 1497
		private readonly object arg2;

		// Token: 0x040005DA RID: 1498
		private readonly object[] args;
	}
}
