using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Core.Platforms
{
	// Token: 0x020004F6 RID: 1270
	internal readonly struct FeatureFlags : IEquatable<FeatureFlags>
	{
		// Token: 0x17000619 RID: 1561
		// (get) Token: 0x06001C50 RID: 7248 RVA: 0x0005ACD3 File Offset: 0x00058ED3
		public ArchitectureFeature Architecture { get; }

		// Token: 0x1700061A RID: 1562
		// (get) Token: 0x06001C51 RID: 7249 RVA: 0x0005ACDB File Offset: 0x00058EDB
		public SystemFeature System { get; }

		// Token: 0x1700061B RID: 1563
		// (get) Token: 0x06001C52 RID: 7250 RVA: 0x0005ACE3 File Offset: 0x00058EE3
		public RuntimeFeature Runtime { get; }

		// Token: 0x06001C53 RID: 7251 RVA: 0x0005ACEB File Offset: 0x00058EEB
		public FeatureFlags(ArchitectureFeature archFlags, SystemFeature sysFlags, RuntimeFeature runtimeFlags)
		{
			this.Runtime = runtimeFlags;
			this.Architecture = archFlags;
			this.System = sysFlags;
		}

		// Token: 0x06001C54 RID: 7252 RVA: 0x0005AD02 File Offset: 0x00058F02
		public bool Has(RuntimeFeature feature)
		{
			return (this.Runtime & feature) == feature;
		}

		// Token: 0x06001C55 RID: 7253 RVA: 0x0005AD0F File Offset: 0x00058F0F
		public bool Has(ArchitectureFeature feature)
		{
			return (this.Architecture & feature) == feature;
		}

		// Token: 0x06001C56 RID: 7254 RVA: 0x0005AD1C File Offset: 0x00058F1C
		public bool Has(SystemFeature feature)
		{
			return (this.System & feature) == feature;
		}

		// Token: 0x06001C57 RID: 7255 RVA: 0x0005AD2C File Offset: 0x00058F2C
		[NullableContext(2)]
		public override bool Equals(object obj)
		{
			if (obj is FeatureFlags)
			{
				FeatureFlags flags = (FeatureFlags)obj;
				return this.Equals(flags);
			}
			return false;
		}

		// Token: 0x06001C58 RID: 7256 RVA: 0x0005AD51 File Offset: 0x00058F51
		public bool Equals(FeatureFlags other)
		{
			return this.Runtime == other.Runtime && this.Architecture == other.Architecture && this.System == other.System;
		}

		// Token: 0x06001C59 RID: 7257 RVA: 0x0005AD82 File Offset: 0x00058F82
		public override int GetHashCode()
		{
			return HashCode.Combine<RuntimeFeature, ArchitectureFeature, SystemFeature>(this.Runtime, this.Architecture, this.System);
		}

		// Token: 0x06001C5A RID: 7258 RVA: 0x0005AD9C File Offset: 0x00058F9C
		[NullableContext(1)]
		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(6, 3);
			defaultInterpolatedStringHandler.AppendLiteral("(");
			defaultInterpolatedStringHandler.AppendFormatted<ArchitectureFeature>(this.Architecture);
			defaultInterpolatedStringHandler.AppendLiteral(")(");
			defaultInterpolatedStringHandler.AppendFormatted<SystemFeature>(this.System);
			defaultInterpolatedStringHandler.AppendLiteral(")(");
			defaultInterpolatedStringHandler.AppendFormatted<RuntimeFeature>(this.Runtime);
			defaultInterpolatedStringHandler.AppendLiteral(")");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x06001C5B RID: 7259 RVA: 0x0005AE10 File Offset: 0x00059010
		public static bool operator ==(FeatureFlags left, FeatureFlags right)
		{
			return left.Equals(right);
		}

		// Token: 0x06001C5C RID: 7260 RVA: 0x0005AE1A File Offset: 0x0005901A
		public static bool operator !=(FeatureFlags left, FeatureFlags right)
		{
			return !(left == right);
		}
	}
}
