using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x02000026 RID: 38
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleMissingIssue : ModuleIssueV2, IEquatable<ModuleMissingIssue>
	{
		// Token: 0x060001DD RID: 477 RVA: 0x000098CE File Offset: 0x00007ACE
		public ModuleMissingIssue(ModuleInfoExtended Module, ApplicationVersionRange SourceVersion)
		{
			this.SourceVersion = SourceVersion;
			base..ctor(Module);
		}

		// Token: 0x1700006A RID: 106
		// (get) Token: 0x060001DE RID: 478 RVA: 0x000098DF File Offset: 0x00007ADF
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleMissingIssue);
			}
		}

		// Token: 0x1700006B RID: 107
		// (get) Token: 0x060001DF RID: 479 RVA: 0x000098EB File Offset: 0x00007AEB
		// (set) Token: 0x060001E0 RID: 480 RVA: 0x000098F3 File Offset: 0x00007AF3
		public ApplicationVersionRange SourceVersion { get; set; }

		// Token: 0x060001E1 RID: 481 RVA: 0x000098FC File Offset: 0x00007AFC
		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(39, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Module '");
			defaultInterpolatedStringHandler.AppendFormatted(base.Module.Id);
			defaultInterpolatedStringHandler.AppendLiteral("' ");
			defaultInterpolatedStringHandler.AppendFormatted<ApplicationVersion>(base.Module.Version);
			defaultInterpolatedStringHandler.AppendLiteral(" is missing from modules list");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x060001E2 RID: 482 RVA: 0x00009967 File Offset: 0x00007B67
		public override ModuleIssue ToLegacy()
		{
			return new ModuleIssue(base.Module, base.Module.Id, ModuleIssueType.Missing, this.ToString(), this.SourceVersion);
		}

		// Token: 0x060001E3 RID: 483 RVA: 0x0000998C File Offset: 0x00007B8C
		[CompilerGenerated]
		protected override bool PrintMembers(StringBuilder builder)
		{
			if (base.PrintMembers(builder))
			{
				builder.Append(", ");
			}
			builder.Append("SourceVersion = ");
			builder.Append(this.SourceVersion);
			return true;
		}

		// Token: 0x060001E4 RID: 484 RVA: 0x000099BD File Offset: 0x00007BBD
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleMissingIssue left, ModuleMissingIssue right)
		{
			return !(left == right);
		}

		// Token: 0x060001E5 RID: 485 RVA: 0x000099C9 File Offset: 0x00007BC9
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleMissingIssue left, ModuleMissingIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x060001E6 RID: 486 RVA: 0x000099DF File Offset: 0x00007BDF
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return base.GetHashCode() * -1521134295 + EqualityComparer<ApplicationVersionRange>.Default.GetHashCode(this.<SourceVersion>k__BackingField);
		}

		// Token: 0x060001E7 RID: 487 RVA: 0x000099FE File Offset: 0x00007BFE
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleMissingIssue);
		}

		// Token: 0x060001E8 RID: 488 RVA: 0x00009A0C File Offset: 0x00007C0C
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleIssueV2 other)
		{
			return this.Equals(other);
		}

		// Token: 0x060001E9 RID: 489 RVA: 0x00009A15 File Offset: 0x00007C15
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleMissingIssue other)
		{
			return this == other || (base.Equals(other) && EqualityComparer<ApplicationVersionRange>.Default.Equals(this.<SourceVersion>k__BackingField, other.<SourceVersion>k__BackingField));
		}

		// Token: 0x060001EB RID: 491 RVA: 0x00009A48 File Offset: 0x00007C48
		[CompilerGenerated]
		private ModuleMissingIssue(ModuleMissingIssue original)
			: base(original)
		{
			this.SourceVersion = original.<SourceVersion>k__BackingField;
		}

		// Token: 0x060001EC RID: 492 RVA: 0x00009A5F File Offset: 0x00007C5F
		[CompilerGenerated]
		public void Deconstruct(out ModuleInfoExtended Module, out ApplicationVersionRange SourceVersion)
		{
			Module = base.Module;
			SourceVersion = this.SourceVersion;
		}
	}
}
