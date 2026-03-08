using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace MonoMod.Core.Platforms
{
	// Token: 0x020004FD RID: 1277
	internal readonly struct AllocationRequest : IEquatable<AllocationRequest>
	{
		// Token: 0x06001C81 RID: 7297 RVA: 0x0005B0BC File Offset: 0x000592BC
		public AllocationRequest(int Size)
		{
			this.Executable = false;
			this.Size = Size;
			this.Alignment = 8;
		}

		// Token: 0x17000629 RID: 1577
		// (get) Token: 0x06001C82 RID: 7298 RVA: 0x0005B0D3 File Offset: 0x000592D3
		// (set) Token: 0x06001C83 RID: 7299 RVA: 0x0005B0DB File Offset: 0x000592DB
		public int Size { get; set; }

		// Token: 0x1700062A RID: 1578
		// (get) Token: 0x06001C84 RID: 7300 RVA: 0x0005B0E4 File Offset: 0x000592E4
		// (set) Token: 0x06001C85 RID: 7301 RVA: 0x0005B0EC File Offset: 0x000592EC
		public int Alignment { get; set; }

		// Token: 0x1700062B RID: 1579
		// (get) Token: 0x06001C86 RID: 7302 RVA: 0x0005B0F5 File Offset: 0x000592F5
		// (set) Token: 0x06001C87 RID: 7303 RVA: 0x0005B0FD File Offset: 0x000592FD
		public bool Executable { get; set; }

		// Token: 0x06001C88 RID: 7304 RVA: 0x0005B108 File Offset: 0x00059308
		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("AllocationRequest");
			stringBuilder.Append(" { ");
			if (this.PrintMembers(stringBuilder))
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}

		// Token: 0x06001C89 RID: 7305 RVA: 0x0005B154 File Offset: 0x00059354
		[CompilerGenerated]
		private bool PrintMembers(StringBuilder builder)
		{
			builder.Append("Size = ");
			builder.Append(this.Size.ToString());
			builder.Append(", Alignment = ");
			builder.Append(this.Alignment.ToString());
			builder.Append(", Executable = ");
			builder.Append(this.Executable.ToString());
			return true;
		}

		// Token: 0x06001C8A RID: 7306 RVA: 0x0005B1D7 File Offset: 0x000593D7
		[CompilerGenerated]
		public static bool operator !=(AllocationRequest left, AllocationRequest right)
		{
			return !(left == right);
		}

		// Token: 0x06001C8B RID: 7307 RVA: 0x0005B1E3 File Offset: 0x000593E3
		[CompilerGenerated]
		public static bool operator ==(AllocationRequest left, AllocationRequest right)
		{
			return left.Equals(right);
		}

		// Token: 0x06001C8C RID: 7308 RVA: 0x0005B1ED File Offset: 0x000593ED
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return (EqualityComparer<int>.Default.GetHashCode(this.<Size>k__BackingField) * -1521134295 + EqualityComparer<int>.Default.GetHashCode(this.<Alignment>k__BackingField)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(this.<Executable>k__BackingField);
		}

		// Token: 0x06001C8D RID: 7309 RVA: 0x0005B22D File Offset: 0x0005942D
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return obj is AllocationRequest && this.Equals((AllocationRequest)obj);
		}

		// Token: 0x06001C8E RID: 7310 RVA: 0x0005B248 File Offset: 0x00059448
		[CompilerGenerated]
		public bool Equals(AllocationRequest other)
		{
			return EqualityComparer<int>.Default.Equals(this.<Size>k__BackingField, other.<Size>k__BackingField) && EqualityComparer<int>.Default.Equals(this.<Alignment>k__BackingField, other.<Alignment>k__BackingField) && EqualityComparer<bool>.Default.Equals(this.<Executable>k__BackingField, other.<Executable>k__BackingField);
		}

		// Token: 0x06001C8F RID: 7311 RVA: 0x0005B29D File Offset: 0x0005949D
		[CompilerGenerated]
		public void Deconstruct(out int Size)
		{
			Size = this.Size;
		}
	}
}
