using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager
{
	// Token: 0x0200001B RID: 27
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleIssue : IEquatable<ModuleIssue>
	{
		// Token: 0x17000059 RID: 89
		// (get) Token: 0x06000161 RID: 353 RVA: 0x000079FA File Offset: 0x00005BFA
		[CompilerGenerated]
		private Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleIssue);
			}
		}

		// Token: 0x1700005A RID: 90
		// (get) Token: 0x06000162 RID: 354 RVA: 0x00007A06 File Offset: 0x00005C06
		// (set) Token: 0x06000163 RID: 355 RVA: 0x00007A0E File Offset: 0x00005C0E
		public ModuleInfoExtended Target { get; set; }

		// Token: 0x1700005B RID: 91
		// (get) Token: 0x06000164 RID: 356 RVA: 0x00007A17 File Offset: 0x00005C17
		// (set) Token: 0x06000165 RID: 357 RVA: 0x00007A1F File Offset: 0x00005C1F
		public string SourceId { get; set; }

		// Token: 0x1700005C RID: 92
		// (get) Token: 0x06000166 RID: 358 RVA: 0x00007A28 File Offset: 0x00005C28
		// (set) Token: 0x06000167 RID: 359 RVA: 0x00007A30 File Offset: 0x00005C30
		public ModuleIssueType Type { get; set; }

		// Token: 0x1700005D RID: 93
		// (get) Token: 0x06000168 RID: 360 RVA: 0x00007A39 File Offset: 0x00005C39
		// (set) Token: 0x06000169 RID: 361 RVA: 0x00007A41 File Offset: 0x00005C41
		public string Reason { get; set; }

		// Token: 0x1700005E RID: 94
		// (get) Token: 0x0600016A RID: 362 RVA: 0x00007A4A File Offset: 0x00005C4A
		// (set) Token: 0x0600016B RID: 363 RVA: 0x00007A52 File Offset: 0x00005C52
		public ApplicationVersionRange SourceVersion { get; set; }

		// Token: 0x0600016C RID: 364 RVA: 0x00007A5C File Offset: 0x00005C5C
		public ModuleIssue()
		{
			this.Target = new ModuleInfoExtended();
			this.SourceId = string.Empty;
			this.Type = ModuleIssueType.NONE;
			this.Reason = string.Empty;
			this.SourceVersion = ApplicationVersionRange.Empty;
		}

		// Token: 0x0600016D RID: 365 RVA: 0x00007AA9 File Offset: 0x00005CA9
		public ModuleIssue(ModuleInfoExtended target, string sourceId, ModuleIssueType type)
		{
			this.Target = target;
			this.SourceId = sourceId;
			this.Type = type;
			this.Reason = string.Empty;
			this.SourceVersion = ApplicationVersionRange.Empty;
		}

		// Token: 0x0600016E RID: 366 RVA: 0x00007AE3 File Offset: 0x00005CE3
		public ModuleIssue(ModuleInfoExtended target, string sourceId, ModuleIssueType type, string reason, ApplicationVersionRange sourceVersion)
			: this(target, sourceId, type)
		{
			this.Reason = reason;
			this.SourceVersion = sourceVersion;
		}

		// Token: 0x0600016F RID: 367 RVA: 0x00007B04 File Offset: 0x00005D04
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

		// Token: 0x06000170 RID: 368 RVA: 0x00007B50 File Offset: 0x00005D50
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

		// Token: 0x06000171 RID: 369 RVA: 0x00007BEE File Offset: 0x00005DEE
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleIssue left, ModuleIssue right)
		{
			return !(left == right);
		}

		// Token: 0x06000172 RID: 370 RVA: 0x00007BFA File Offset: 0x00005DFA
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleIssue left, ModuleIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x06000173 RID: 371 RVA: 0x00007C10 File Offset: 0x00005E10
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return ((((EqualityComparer<System.Type>.Default.GetHashCode(this.EqualityContract) * -1521134295 + EqualityComparer<ModuleInfoExtended>.Default.GetHashCode(this.<Target>k__BackingField)) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.<SourceId>k__BackingField)) * -1521134295 + EqualityComparer<ModuleIssueType>.Default.GetHashCode(this.<Type>k__BackingField)) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.<Reason>k__BackingField)) * -1521134295 + EqualityComparer<ApplicationVersionRange>.Default.GetHashCode(this.<SourceVersion>k__BackingField);
		}

		// Token: 0x06000174 RID: 372 RVA: 0x00007CA0 File Offset: 0x00005EA0
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleIssue);
		}

		// Token: 0x06000175 RID: 373 RVA: 0x00007CB0 File Offset: 0x00005EB0
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleIssue other)
		{
			return this == other || (other != null && this.EqualityContract == other.EqualityContract && EqualityComparer<ModuleInfoExtended>.Default.Equals(this.<Target>k__BackingField, other.<Target>k__BackingField) && EqualityComparer<string>.Default.Equals(this.<SourceId>k__BackingField, other.<SourceId>k__BackingField) && EqualityComparer<ModuleIssueType>.Default.Equals(this.<Type>k__BackingField, other.<Type>k__BackingField) && EqualityComparer<string>.Default.Equals(this.<Reason>k__BackingField, other.<Reason>k__BackingField) && EqualityComparer<ApplicationVersionRange>.Default.Equals(this.<SourceVersion>k__BackingField, other.<SourceVersion>k__BackingField));
		}

		// Token: 0x06000177 RID: 375 RVA: 0x00007D64 File Offset: 0x00005F64
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
