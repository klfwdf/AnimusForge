using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager
{
	// Token: 0x02000056 RID: 86
	[NullableContext(1)]
	[Nullable(0)]
	internal class ApplicationVersionRange : IEquatable<ApplicationVersionRange>
	{
		// Token: 0x170000E6 RID: 230
		// (get) Token: 0x060002FB RID: 763 RVA: 0x0000AAD5 File Offset: 0x00008CD5
		[CompilerGenerated]
		protected virtual Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ApplicationVersionRange);
			}
		}

		// Token: 0x170000E7 RID: 231
		// (get) Token: 0x060002FC RID: 764 RVA: 0x0000AAE1 File Offset: 0x00008CE1
		public static ApplicationVersionRange Empty
		{
			get
			{
				return new ApplicationVersionRange();
			}
		}

		// Token: 0x170000E8 RID: 232
		// (get) Token: 0x060002FD RID: 765 RVA: 0x0000AAE8 File Offset: 0x00008CE8
		// (set) Token: 0x060002FE RID: 766 RVA: 0x0000AAF0 File Offset: 0x00008CF0
		public ApplicationVersion Min { get; set; }

		// Token: 0x170000E9 RID: 233
		// (get) Token: 0x060002FF RID: 767 RVA: 0x0000AAF9 File Offset: 0x00008CF9
		// (set) Token: 0x06000300 RID: 768 RVA: 0x0000AB01 File Offset: 0x00008D01
		public ApplicationVersion Max { get; set; }

		// Token: 0x06000301 RID: 769 RVA: 0x0000AB0A File Offset: 0x00008D0A
		public ApplicationVersionRange()
		{
			this.Min = ApplicationVersion.Empty;
			this.Max = ApplicationVersion.Empty;
			base..ctor();
		}

		// Token: 0x06000302 RID: 770 RVA: 0x0000AB28 File Offset: 0x00008D28
		public ApplicationVersionRange(ApplicationVersion min, ApplicationVersion max)
		{
			this.Min = ApplicationVersion.Empty;
			this.Max = ApplicationVersion.Empty;
			base..ctor();
			this.Max = max;
			this.Min = min;
		}

		// Token: 0x06000303 RID: 771 RVA: 0x0000AB54 File Offset: 0x00008D54
		[NullableContext(2)]
		public bool IsSame(ApplicationVersionRange other)
		{
			return this.Min.IsSame((other != null) ? other.Min : null) && this.Max.IsSame((other != null) ? other.Max : null);
		}

		// Token: 0x06000304 RID: 772 RVA: 0x0000AB88 File Offset: 0x00008D88
		[NullableContext(2)]
		public bool IsSameWithChangeSet(ApplicationVersionRange other)
		{
			return this.Min.IsSameWithChangeSet((other != null) ? other.Min : null) && this.Max.IsSameWithChangeSet((other != null) ? other.Max : null);
		}

		// Token: 0x06000305 RID: 773 RVA: 0x0000ABBC File Offset: 0x00008DBC
		public override string ToString()
		{
			return string.Format("{0} - {1}", this.Min, this.Max);
		}

		// Token: 0x06000306 RID: 774 RVA: 0x0000ABD4 File Offset: 0x00008DD4
		public static bool TryParse(string versionRangeAsString, out ApplicationVersionRange versionRange)
		{
			versionRange = ApplicationVersionRange.Empty;
			if (string.IsNullOrWhiteSpace(versionRangeAsString))
			{
				return false;
			}
			versionRangeAsString = versionRangeAsString.Replace(" ", string.Empty);
			int index = versionRangeAsString.IndexOf('-');
			if (index < 0)
			{
				return false;
			}
			string minAsString = versionRangeAsString.Substring(0, index);
			string maxAsString = versionRangeAsString.Substring(index + 1, versionRangeAsString.Length - 1 - index);
			ApplicationVersion min;
			ApplicationVersion max;
			if (ApplicationVersion.TryParse(minAsString, out min, true) && ApplicationVersion.TryParse(maxAsString, out max, false))
			{
				versionRange = new ApplicationVersionRange
				{
					Min = min,
					Max = max
				};
				return true;
			}
			return false;
		}

		// Token: 0x06000307 RID: 775 RVA: 0x0000AC5F File Offset: 0x00008E5F
		[CompilerGenerated]
		protected virtual bool PrintMembers(StringBuilder builder)
		{
			RuntimeHelpers.EnsureSufficientExecutionStack();
			builder.Append("Min = ");
			builder.Append(this.Min);
			builder.Append(", Max = ");
			builder.Append(this.Max);
			return true;
		}

		// Token: 0x06000308 RID: 776 RVA: 0x0000AC99 File Offset: 0x00008E99
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ApplicationVersionRange left, ApplicationVersionRange right)
		{
			return !(left == right);
		}

		// Token: 0x06000309 RID: 777 RVA: 0x0000ACA5 File Offset: 0x00008EA5
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ApplicationVersionRange left, ApplicationVersionRange right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x0600030A RID: 778 RVA: 0x0000ACB9 File Offset: 0x00008EB9
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return (EqualityComparer<Type>.Default.GetHashCode(this.EqualityContract) * -1521134295 + EqualityComparer<ApplicationVersion>.Default.GetHashCode(this.<Min>k__BackingField)) * -1521134295 + EqualityComparer<ApplicationVersion>.Default.GetHashCode(this.<Max>k__BackingField);
		}

		// Token: 0x0600030B RID: 779 RVA: 0x0000ACF9 File Offset: 0x00008EF9
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ApplicationVersionRange);
		}

		// Token: 0x0600030C RID: 780 RVA: 0x0000AD08 File Offset: 0x00008F08
		[NullableContext(2)]
		[CompilerGenerated]
		public virtual bool Equals(ApplicationVersionRange other)
		{
			return this == other || (other != null && this.EqualityContract == other.EqualityContract && EqualityComparer<ApplicationVersion>.Default.Equals(this.<Min>k__BackingField, other.<Min>k__BackingField) && EqualityComparer<ApplicationVersion>.Default.Equals(this.<Max>k__BackingField, other.<Max>k__BackingField));
		}

		// Token: 0x0600030E RID: 782 RVA: 0x0000AD69 File Offset: 0x00008F69
		[CompilerGenerated]
		protected ApplicationVersionRange(ApplicationVersionRange original)
		{
			this.Min = original.<Min>k__BackingField;
			this.Max = original.<Max>k__BackingField;
		}
	}
}
