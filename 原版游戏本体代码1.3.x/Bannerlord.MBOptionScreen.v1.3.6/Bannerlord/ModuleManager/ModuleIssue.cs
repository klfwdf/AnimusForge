using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager
{
	// Token: 0x0200005D RID: 93
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleIssue : IEquatable<ModuleIssue>
	{
		// Token: 0x17000104 RID: 260
		// (get) Token: 0x06000369 RID: 873 RVA: 0x0000C8FE File Offset: 0x0000AAFE
		[CompilerGenerated]
		private Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleIssue);
			}
		}

		// Token: 0x17000105 RID: 261
		// (get) Token: 0x0600036A RID: 874 RVA: 0x0000C90A File Offset: 0x0000AB0A
		// (set) Token: 0x0600036B RID: 875 RVA: 0x0000C912 File Offset: 0x0000AB12
		public ModuleInfoExtended Target { get; set; }

		// Token: 0x17000106 RID: 262
		// (get) Token: 0x0600036C RID: 876 RVA: 0x0000C91B File Offset: 0x0000AB1B
		// (set) Token: 0x0600036D RID: 877 RVA: 0x0000C923 File Offset: 0x0000AB23
		public string SourceId { get; set; }

		// Token: 0x17000107 RID: 263
		// (get) Token: 0x0600036E RID: 878 RVA: 0x0000C92C File Offset: 0x0000AB2C
		// (set) Token: 0x0600036F RID: 879 RVA: 0x0000C934 File Offset: 0x0000AB34
		public ModuleIssueType Type { get; set; }

		// Token: 0x17000108 RID: 264
		// (get) Token: 0x06000370 RID: 880 RVA: 0x0000C93D File Offset: 0x0000AB3D
		// (set) Token: 0x06000371 RID: 881 RVA: 0x0000C945 File Offset: 0x0000AB45
		public string Reason { get; set; }

		// Token: 0x17000109 RID: 265
		// (get) Token: 0x06000372 RID: 882 RVA: 0x0000C94E File Offset: 0x0000AB4E
		// (set) Token: 0x06000373 RID: 883 RVA: 0x0000C956 File Offset: 0x0000AB56
		public ApplicationVersionRange SourceVersion { get; set; }

		// Token: 0x06000374 RID: 884 RVA: 0x0000C95F File Offset: 0x0000AB5F
		public ModuleIssue()
		{
			this.Target = new ModuleInfoExtended();
			this.SourceId = string.Empty;
			this.Type = ModuleIssueType.NONE;
			this.Reason = string.Empty;
			this.SourceVersion = ApplicationVersionRange.Empty;
		}

		// Token: 0x06000375 RID: 885 RVA: 0x0000C99A File Offset: 0x0000AB9A
		public ModuleIssue(ModuleInfoExtended target, string sourceId, ModuleIssueType type)
		{
			this.Target = target;
			this.SourceId = sourceId;
			this.Type = type;
			this.Reason = string.Empty;
			this.SourceVersion = ApplicationVersionRange.Empty;
		}

		// Token: 0x06000376 RID: 886 RVA: 0x0000C9CD File Offset: 0x0000ABCD
		public ModuleIssue(ModuleInfoExtended target, string sourceId, ModuleIssueType type, string reason, ApplicationVersionRange sourceVersion)
			: this(target, sourceId, type)
		{
			this.Reason = reason;
			this.SourceVersion = sourceVersion;
		}

		// Token: 0x06000377 RID: 887 RVA: 0x0000C9E8 File Offset: 0x0000ABE8
		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("ModuleIssue");
			stringBuilder.Append(" { ");
			if (this.PrintMembers(stringBuilder))
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}

		// Token: 0x06000378 RID: 888 RVA: 0x0000CA34 File Offset: 0x0000AC34
		[CompilerGenerated]
		private bool PrintMembers(StringBuilder builder)
		{
			RuntimeHelpers.EnsureSufficientExecutionStack();
			builder.Append("Target = ");
			builder.Append(this.Target);
			builder.Append(", SourceId = ");
			builder.Append(this.SourceId);
			builder.Append(", Type = ");
			builder.Append(this.Type.ToString());
			builder.Append(", Reason = ");
			builder.Append(this.Reason);
			builder.Append(", SourceVersion = ");
			builder.Append(this.SourceVersion);
			return true;
		}

		// Token: 0x06000379 RID: 889 RVA: 0x0000CAD2 File Offset: 0x0000ACD2
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleIssue left, ModuleIssue right)
		{
			return !(left == right);
		}

		// Token: 0x0600037A RID: 890 RVA: 0x0000CADE File Offset: 0x0000ACDE
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleIssue left, ModuleIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x0600037B RID: 891 RVA: 0x0000CAF4 File Offset: 0x0000ACF4
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return ((((EqualityComparer<System.Type>.Default.GetHashCode(this.EqualityContract) * -1521134295 + EqualityComparer<ModuleInfoExtended>.Default.GetHashCode(this.<Target>k__BackingField)) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.<SourceId>k__BackingField)) * -1521134295 + EqualityComparer<ModuleIssueType>.Default.GetHashCode(this.<Type>k__BackingField)) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.<Reason>k__BackingField)) * -1521134295 + EqualityComparer<ApplicationVersionRange>.Default.GetHashCode(this.<SourceVersion>k__BackingField);
		}

		// Token: 0x0600037C RID: 892 RVA: 0x0000CB84 File Offset: 0x0000AD84
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleIssue);
		}

		// Token: 0x0600037D RID: 893 RVA: 0x0000CB94 File Offset: 0x0000AD94
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleIssue other)
		{
			return this == other || (other != null && this.EqualityContract == other.EqualityContract && EqualityComparer<ModuleInfoExtended>.Default.Equals(this.<Target>k__BackingField, other.<Target>k__BackingField) && EqualityComparer<string>.Default.Equals(this.<SourceId>k__BackingField, other.<SourceId>k__BackingField) && EqualityComparer<ModuleIssueType>.Default.Equals(this.<Type>k__BackingField, other.<Type>k__BackingField) && EqualityComparer<string>.Default.Equals(this.<Reason>k__BackingField, other.<Reason>k__BackingField) && EqualityComparer<ApplicationVersionRange>.Default.Equals(this.<SourceVersion>k__BackingField, other.<SourceVersion>k__BackingField));
		}

		// Token: 0x0600037F RID: 895 RVA: 0x0000CC44 File Offset: 0x0000AE44
		[CompilerGenerated]
		private ModuleIssue(ModuleIssue original)
		{
			this.Target = original.<Target>k__BackingField;
			this.SourceId = original.<SourceId>k__BackingField;
			this.Type = original.<Type>k__BackingField;
			this.Reason = original.<Reason>k__BackingField;
			this.SourceVersion = original.<SourceVersion>k__BackingField;
		}
	}
}
