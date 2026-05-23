using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace MonoMod.Core.Platforms
{
	// Token: 0x020004FE RID: 1278
	internal readonly struct PositionedAllocationRequest : IEquatable<PositionedAllocationRequest>
	{
		// Token: 0x06001C90 RID: 7312 RVA: 0x0005B2A7 File Offset: 0x000594A7
		public PositionedAllocationRequest(IntPtr Target, IntPtr LowBound, IntPtr HighBound, AllocationRequest Base)
		{
			this.Target = Target;
			this.LowBound = LowBound;
			this.HighBound = HighBound;
			this.Base = Base;
		}

		// Token: 0x1700062C RID: 1580
		// (get) Token: 0x06001C91 RID: 7313 RVA: 0x0005B2C6 File Offset: 0x000594C6
		// (set) Token: 0x06001C92 RID: 7314 RVA: 0x0005B2CE File Offset: 0x000594CE
		public IntPtr Target { get; set; }

		// Token: 0x1700062D RID: 1581
		// (get) Token: 0x06001C93 RID: 7315 RVA: 0x0005B2D7 File Offset: 0x000594D7
		// (set) Token: 0x06001C94 RID: 7316 RVA: 0x0005B2DF File Offset: 0x000594DF
		public IntPtr LowBound { get; set; }

		// Token: 0x1700062E RID: 1582
		// (get) Token: 0x06001C95 RID: 7317 RVA: 0x0005B2E8 File Offset: 0x000594E8
		// (set) Token: 0x06001C96 RID: 7318 RVA: 0x0005B2F0 File Offset: 0x000594F0
		public IntPtr HighBound { get; set; }

		// Token: 0x1700062F RID: 1583
		// (get) Token: 0x06001C97 RID: 7319 RVA: 0x0005B2F9 File Offset: 0x000594F9
		// (set) Token: 0x06001C98 RID: 7320 RVA: 0x0005B301 File Offset: 0x00059501
		public AllocationRequest Base { get; set; }

		// Token: 0x06001C99 RID: 7321 RVA: 0x0005B30C File Offset: 0x0005950C
		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("PositionedAllocationRequest");
			stringBuilder.Append(" { ");
			if (this.PrintMembers(stringBuilder))
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}

		// Token: 0x06001C9A RID: 7322 RVA: 0x0005B358 File Offset: 0x00059558
		[CompilerGenerated]
		private bool PrintMembers(StringBuilder builder)
		{
			builder.Append("Target = ");
			builder.Append(this.Target.ToString());
			builder.Append(", LowBound = ");
			builder.Append(this.LowBound.ToString());
			builder.Append(", HighBound = ");
			builder.Append(this.HighBound.ToString());
			builder.Append(", Base = ");
			builder.Append(this.Base.ToString());
			return true;
		}

		// Token: 0x06001C9B RID: 7323 RVA: 0x0005B402 File Offset: 0x00059602
		[CompilerGenerated]
		public static bool operator !=(PositionedAllocationRequest left, PositionedAllocationRequest right)
		{
			return !(left == right);
		}

		// Token: 0x06001C9C RID: 7324 RVA: 0x0005B40E File Offset: 0x0005960E
		[CompilerGenerated]
		public static bool operator ==(PositionedAllocationRequest left, PositionedAllocationRequest right)
		{
			return left.Equals(right);
		}

		// Token: 0x06001C9D RID: 7325 RVA: 0x0005B418 File Offset: 0x00059618
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return ((EqualityComparer<IntPtr>.Default.GetHashCode(this.<Target>k__BackingField) * -1521134295 + EqualityComparer<IntPtr>.Default.GetHashCode(this.<LowBound>k__BackingField)) * -1521134295 + EqualityComparer<IntPtr>.Default.GetHashCode(this.<HighBound>k__BackingField)) * -1521134295 + EqualityComparer<AllocationRequest>.Default.GetHashCode(this.<Base>k__BackingField);
		}

		// Token: 0x06001C9E RID: 7326 RVA: 0x0005B47A File Offset: 0x0005967A
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return obj is PositionedAllocationRequest && this.Equals((PositionedAllocationRequest)obj);
		}

		// Token: 0x06001C9F RID: 7327 RVA: 0x0005B494 File Offset: 0x00059694
		[CompilerGenerated]
		public bool Equals(PositionedAllocationRequest other)
		{
			return EqualityComparer<IntPtr>.Default.Equals(this.<Target>k__BackingField, other.<Target>k__BackingField) && EqualityComparer<IntPtr>.Default.Equals(this.<LowBound>k__BackingField, other.<LowBound>k__BackingField) && EqualityComparer<IntPtr>.Default.Equals(this.<HighBound>k__BackingField, other.<HighBound>k__BackingField) && EqualityComparer<AllocationRequest>.Default.Equals(this.<Base>k__BackingField, other.<Base>k__BackingField);
		}

		// Token: 0x06001CA0 RID: 7328 RVA: 0x0005B501 File Offset: 0x00059701
		[CompilerGenerated]
		public void Deconstruct(out IntPtr Target, out IntPtr LowBound, out IntPtr HighBound, out AllocationRequest Base)
		{
			Target = this.Target;
			LowBound = this.LowBound;
			HighBound = this.HighBound;
			Base = this.Base;
		}
	}
}
