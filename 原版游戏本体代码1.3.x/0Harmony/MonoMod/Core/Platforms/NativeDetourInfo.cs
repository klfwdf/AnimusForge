using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace MonoMod.Core.Platforms
{
	// Token: 0x020004FA RID: 1274
	[NullableContext(1)]
	[Nullable(0)]
	internal readonly struct NativeDetourInfo : IEquatable<NativeDetourInfo>
	{
		// Token: 0x06001C69 RID: 7273 RVA: 0x0005AE26 File Offset: 0x00059026
		public NativeDetourInfo(IntPtr From, IntPtr To, INativeDetourKind InternalKind, [Nullable(2)] IDisposable InternalData)
		{
			this.From = From;
			this.To = To;
			this.InternalKind = InternalKind;
			this.InternalData = InternalData;
		}

		// Token: 0x17000621 RID: 1569
		// (get) Token: 0x06001C6A RID: 7274 RVA: 0x0005AE45 File Offset: 0x00059045
		// (set) Token: 0x06001C6B RID: 7275 RVA: 0x0005AE4D File Offset: 0x0005904D
		public IntPtr From { get; set; }

		// Token: 0x17000622 RID: 1570
		// (get) Token: 0x06001C6C RID: 7276 RVA: 0x0005AE56 File Offset: 0x00059056
		// (set) Token: 0x06001C6D RID: 7277 RVA: 0x0005AE5E File Offset: 0x0005905E
		public IntPtr To { get; set; }

		// Token: 0x17000623 RID: 1571
		// (get) Token: 0x06001C6E RID: 7278 RVA: 0x0005AE67 File Offset: 0x00059067
		// (set) Token: 0x06001C6F RID: 7279 RVA: 0x0005AE6F File Offset: 0x0005906F
		public INativeDetourKind InternalKind { get; set; }

		// Token: 0x17000624 RID: 1572
		// (get) Token: 0x06001C70 RID: 7280 RVA: 0x0005AE78 File Offset: 0x00059078
		// (set) Token: 0x06001C71 RID: 7281 RVA: 0x0005AE80 File Offset: 0x00059080
		[Nullable(2)]
		public IDisposable InternalData
		{
			[NullableContext(2)]
			get;
			[NullableContext(2)]
			set;
		}

		// Token: 0x17000625 RID: 1573
		// (get) Token: 0x06001C72 RID: 7282 RVA: 0x0005AE89 File Offset: 0x00059089
		public int Size
		{
			get
			{
				return this.InternalKind.Size;
			}
		}

		// Token: 0x06001C73 RID: 7283 RVA: 0x0005AE98 File Offset: 0x00059098
		[NullableContext(0)]
		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("NativeDetourInfo");
			stringBuilder.Append(" { ");
			if (this.PrintMembers(stringBuilder))
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}

		// Token: 0x06001C74 RID: 7284 RVA: 0x0005AEE4 File Offset: 0x000590E4
		[NullableContext(0)]
		[CompilerGenerated]
		private bool PrintMembers(StringBuilder builder)
		{
			builder.Append("From = ");
			builder.Append(this.From.ToString());
			builder.Append(", To = ");
			builder.Append(this.To.ToString());
			builder.Append(", InternalKind = ");
			builder.Append(this.InternalKind);
			builder.Append(", InternalData = ");
			builder.Append(this.InternalData);
			builder.Append(", Size = ");
			builder.Append(this.Size.ToString());
			return true;
		}

		// Token: 0x06001C75 RID: 7285 RVA: 0x0005AF99 File Offset: 0x00059199
		[CompilerGenerated]
		public static bool operator !=(NativeDetourInfo left, NativeDetourInfo right)
		{
			return !(left == right);
		}

		// Token: 0x06001C76 RID: 7286 RVA: 0x0005AFA5 File Offset: 0x000591A5
		[CompilerGenerated]
		public static bool operator ==(NativeDetourInfo left, NativeDetourInfo right)
		{
			return left.Equals(right);
		}

		// Token: 0x06001C77 RID: 7287 RVA: 0x0005AFB0 File Offset: 0x000591B0
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return ((EqualityComparer<IntPtr>.Default.GetHashCode(this.<From>k__BackingField) * -1521134295 + EqualityComparer<IntPtr>.Default.GetHashCode(this.<To>k__BackingField)) * -1521134295 + EqualityComparer<INativeDetourKind>.Default.GetHashCode(this.<InternalKind>k__BackingField)) * -1521134295 + EqualityComparer<IDisposable>.Default.GetHashCode(this.<InternalData>k__BackingField);
		}

		// Token: 0x06001C78 RID: 7288 RVA: 0x0005B012 File Offset: 0x00059212
		[NullableContext(0)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return obj is NativeDetourInfo && this.Equals((NativeDetourInfo)obj);
		}

		// Token: 0x06001C79 RID: 7289 RVA: 0x0005B02C File Offset: 0x0005922C
		[CompilerGenerated]
		public bool Equals(NativeDetourInfo other)
		{
			return EqualityComparer<IntPtr>.Default.Equals(this.<From>k__BackingField, other.<From>k__BackingField) && EqualityComparer<IntPtr>.Default.Equals(this.<To>k__BackingField, other.<To>k__BackingField) && EqualityComparer<INativeDetourKind>.Default.Equals(this.<InternalKind>k__BackingField, other.<InternalKind>k__BackingField) && EqualityComparer<IDisposable>.Default.Equals(this.<InternalData>k__BackingField, other.<InternalData>k__BackingField);
		}

		// Token: 0x06001C7A RID: 7290 RVA: 0x0005B099 File Offset: 0x00059299
		[CompilerGenerated]
		public void Deconstruct(out IntPtr From, out IntPtr To, out INativeDetourKind InternalKind, [Nullable(2)] out IDisposable InternalData)
		{
			From = this.From;
			To = this.To;
			InternalKind = this.InternalKind;
			InternalData = this.InternalData;
		}
	}
}
