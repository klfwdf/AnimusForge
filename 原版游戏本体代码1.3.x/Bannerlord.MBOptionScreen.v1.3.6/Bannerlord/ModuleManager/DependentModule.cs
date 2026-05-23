using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager
{
	// Token: 0x02000058 RID: 88
	[NullableContext(1)]
	[Nullable(0)]
	internal class DependentModule : IEquatable<DependentModule>
	{
		// Token: 0x170000EA RID: 234
		// (get) Token: 0x0600030F RID: 783 RVA: 0x0000AD89 File Offset: 0x00008F89
		[CompilerGenerated]
		protected virtual Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(DependentModule);
			}
		}

		// Token: 0x170000EB RID: 235
		// (get) Token: 0x06000310 RID: 784 RVA: 0x0000AD95 File Offset: 0x00008F95
		// (set) Token: 0x06000311 RID: 785 RVA: 0x0000AD9D File Offset: 0x00008F9D
		public string Id { get; set; }

		// Token: 0x170000EC RID: 236
		// (get) Token: 0x06000312 RID: 786 RVA: 0x0000ADA6 File Offset: 0x00008FA6
		// (set) Token: 0x06000313 RID: 787 RVA: 0x0000ADAE File Offset: 0x00008FAE
		public ApplicationVersion Version { get; set; }

		// Token: 0x170000ED RID: 237
		// (get) Token: 0x06000314 RID: 788 RVA: 0x0000ADB7 File Offset: 0x00008FB7
		// (set) Token: 0x06000315 RID: 789 RVA: 0x0000ADBF File Offset: 0x00008FBF
		public bool IsOptional { get; set; }

		// Token: 0x06000316 RID: 790 RVA: 0x0000ADC8 File Offset: 0x00008FC8
		public DependentModule()
		{
			this.Id = string.Empty;
			this.Version = ApplicationVersion.Empty;
			base..ctor();
		}

		// Token: 0x06000317 RID: 791 RVA: 0x0000ADE6 File Offset: 0x00008FE6
		public DependentModule(string id, ApplicationVersion version, bool isOptional)
		{
			this.Id = string.Empty;
			this.Version = ApplicationVersion.Empty;
			base..ctor();
			this.Id = id;
			this.Version = version;
			this.IsOptional = isOptional;
		}

		// Token: 0x06000318 RID: 792 RVA: 0x0000AE1C File Offset: 0x0000901C
		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("DependentModule");
			stringBuilder.Append(" { ");
			if (this.PrintMembers(stringBuilder))
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}

		// Token: 0x06000319 RID: 793 RVA: 0x0000AE68 File Offset: 0x00009068
		[CompilerGenerated]
		protected virtual bool PrintMembers(StringBuilder builder)
		{
			RuntimeHelpers.EnsureSufficientExecutionStack();
			builder.Append("Id = ");
			builder.Append(this.Id);
			builder.Append(", Version = ");
			builder.Append(this.Version);
			builder.Append(", IsOptional = ");
			builder.Append(this.IsOptional.ToString());
			return true;
		}

		// Token: 0x0600031A RID: 794 RVA: 0x0000AED4 File Offset: 0x000090D4
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(DependentModule left, DependentModule right)
		{
			return !(left == right);
		}

		// Token: 0x0600031B RID: 795 RVA: 0x0000AEE0 File Offset: 0x000090E0
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(DependentModule left, DependentModule right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x0600031C RID: 796 RVA: 0x0000AEF4 File Offset: 0x000090F4
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return ((EqualityComparer<Type>.Default.GetHashCode(this.EqualityContract) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.<Id>k__BackingField)) * -1521134295 + EqualityComparer<ApplicationVersion>.Default.GetHashCode(this.<Version>k__BackingField)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(this.<IsOptional>k__BackingField);
		}

		// Token: 0x0600031D RID: 797 RVA: 0x0000AF56 File Offset: 0x00009156
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as DependentModule);
		}

		// Token: 0x0600031E RID: 798 RVA: 0x0000AF64 File Offset: 0x00009164
		[NullableContext(2)]
		[CompilerGenerated]
		public virtual bool Equals(DependentModule other)
		{
			return this == other || (other != null && this.EqualityContract == other.EqualityContract && EqualityComparer<string>.Default.Equals(this.<Id>k__BackingField, other.<Id>k__BackingField) && EqualityComparer<ApplicationVersion>.Default.Equals(this.<Version>k__BackingField, other.<Version>k__BackingField) && EqualityComparer<bool>.Default.Equals(this.<IsOptional>k__BackingField, other.<IsOptional>k__BackingField));
		}

		// Token: 0x06000320 RID: 800 RVA: 0x0000AFDD File Offset: 0x000091DD
		[CompilerGenerated]
		protected DependentModule(DependentModule original)
		{
			this.Id = original.<Id>k__BackingField;
			this.Version = original.<Version>k__BackingField;
			this.IsOptional = original.<IsOptional>k__BackingField;
		}
	}
}
