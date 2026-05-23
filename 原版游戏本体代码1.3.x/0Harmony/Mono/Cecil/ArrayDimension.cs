using System;

namespace Mono.Cecil
{
	// Token: 0x020001E4 RID: 484
	internal struct ArrayDimension
	{
		// Token: 0x17000225 RID: 549
		// (get) Token: 0x06000948 RID: 2376 RVA: 0x0001E6A9 File Offset: 0x0001C8A9
		// (set) Token: 0x06000949 RID: 2377 RVA: 0x0001E6B1 File Offset: 0x0001C8B1
		public int? LowerBound
		{
			get
			{
				return this.lower_bound;
			}
			set
			{
				this.lower_bound = value;
			}
		}

		// Token: 0x17000226 RID: 550
		// (get) Token: 0x0600094A RID: 2378 RVA: 0x0001E6BA File Offset: 0x0001C8BA
		// (set) Token: 0x0600094B RID: 2379 RVA: 0x0001E6C2 File Offset: 0x0001C8C2
		public int? UpperBound
		{
			get
			{
				return this.upper_bound;
			}
			set
			{
				this.upper_bound = value;
			}
		}

		// Token: 0x17000227 RID: 551
		// (get) Token: 0x0600094C RID: 2380 RVA: 0x0001E6CB File Offset: 0x0001C8CB
		public bool IsSized
		{
			get
			{
				return this.lower_bound != null || this.upper_bound != null;
			}
		}

		// Token: 0x0600094D RID: 2381 RVA: 0x0001E6E7 File Offset: 0x0001C8E7
		public ArrayDimension(int? lowerBound, int? upperBound)
		{
			this.lower_bound = lowerBound;
			this.upper_bound = upperBound;
		}

		// Token: 0x0600094E RID: 2382 RVA: 0x0001E6F8 File Offset: 0x0001C8F8
		public override string ToString()
		{
			if (this.IsSized)
			{
				int? num = this.lower_bound;
				string str = num.ToString();
				string str2 = "...";
				num = this.upper_bound;
				return str + str2 + num.ToString();
			}
			return string.Empty;
		}

		// Token: 0x04000317 RID: 791
		private int? lower_bound;

		// Token: 0x04000318 RID: 792
		private int? upper_bound;
	}
}
