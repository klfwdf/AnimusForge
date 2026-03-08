using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace MonoMod.Utils
{
	// Token: 0x020008A0 RID: 2208
	internal struct DynamicReferenceCell : IEquatable<DynamicReferenceCell>
	{
		// Token: 0x17000840 RID: 2112
		// (get) Token: 0x06002D4E RID: 11598 RVA: 0x000985E1 File Offset: 0x000967E1
		// (set) Token: 0x06002D4F RID: 11599 RVA: 0x000985E9 File Offset: 0x000967E9
		public int Index { readonly get; internal set; }

		// Token: 0x17000841 RID: 2113
		// (get) Token: 0x06002D50 RID: 11600 RVA: 0x000985F2 File Offset: 0x000967F2
		// (set) Token: 0x06002D51 RID: 11601 RVA: 0x000985FA File Offset: 0x000967FA
		public int Hash { readonly get; internal set; }

		// Token: 0x06002D52 RID: 11602 RVA: 0x00098603 File Offset: 0x00096803
		public DynamicReferenceCell(int idx, int hash)
		{
			this.Index = idx;
			this.Hash = hash;
		}

		// Token: 0x06002D53 RID: 11603 RVA: 0x00098614 File Offset: 0x00096814
		[CompilerGenerated]
		public override readonly string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("DynamicReferenceCell");
			stringBuilder.Append(" { ");
			if (this.PrintMembers(stringBuilder))
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}

		// Token: 0x06002D54 RID: 11604 RVA: 0x00098660 File Offset: 0x00096860
		[CompilerGenerated]
		private readonly bool PrintMembers(StringBuilder builder)
		{
			builder.Append("Index = ");
			builder.Append(this.Index.ToString());
			builder.Append(", Hash = ");
			builder.Append(this.Hash.ToString());
			return true;
		}

		// Token: 0x06002D55 RID: 11605 RVA: 0x000986BC File Offset: 0x000968BC
		[CompilerGenerated]
		public static bool operator !=(DynamicReferenceCell left, DynamicReferenceCell right)
		{
			return !(left == right);
		}

		// Token: 0x06002D56 RID: 11606 RVA: 0x000986C8 File Offset: 0x000968C8
		[CompilerGenerated]
		public static bool operator ==(DynamicReferenceCell left, DynamicReferenceCell right)
		{
			return left.Equals(right);
		}

		// Token: 0x06002D57 RID: 11607 RVA: 0x000986D2 File Offset: 0x000968D2
		[CompilerGenerated]
		public override readonly int GetHashCode()
		{
			return EqualityComparer<int>.Default.GetHashCode(this.<Index>k__BackingField) * -1521134295 + EqualityComparer<int>.Default.GetHashCode(this.<Hash>k__BackingField);
		}

		// Token: 0x06002D58 RID: 11608 RVA: 0x000986FB File Offset: 0x000968FB
		[CompilerGenerated]
		public override readonly bool Equals(object obj)
		{
			return obj is DynamicReferenceCell && this.Equals((DynamicReferenceCell)obj);
		}

		// Token: 0x06002D59 RID: 11609 RVA: 0x00098713 File Offset: 0x00096913
		[CompilerGenerated]
		public readonly bool Equals(DynamicReferenceCell other)
		{
			return EqualityComparer<int>.Default.Equals(this.<Index>k__BackingField, other.<Index>k__BackingField) && EqualityComparer<int>.Default.Equals(this.<Hash>k__BackingField, other.<Hash>k__BackingField);
		}
	}
}
