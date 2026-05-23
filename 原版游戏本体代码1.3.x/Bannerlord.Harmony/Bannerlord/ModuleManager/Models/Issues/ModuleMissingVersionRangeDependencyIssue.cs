using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x0200002A RID: 42
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleMissingVersionRangeDependencyIssue : ModuleIssueV2, IEquatable<ModuleMissingVersionRangeDependencyIssue>
	{
		// Token: 0x0600021D RID: 541 RVA: 0x00009EB1 File Offset: 0x000080B1
		public ModuleMissingVersionRangeDependencyIssue(ModuleInfoExtended Module, DependentModuleMetadata Dependency)
		{
			this.Dependency = Dependency;
			base..ctor(Module);
		}

		// Token: 0x17000072 RID: 114
		// (get) Token: 0x0600021E RID: 542 RVA: 0x00009EC2 File Offset: 0x000080C2
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleMissingVersionRangeDependencyIssue);
			}
		}

		// Token: 0x17000073 RID: 115
		// (get) Token: 0x0600021F RID: 543 RVA: 0x00009ECE File Offset: 0x000080CE
		// (set) Token: 0x06000220 RID: 544 RVA: 0x00009ED6 File Offset: 0x000080D6
		public DependentModuleMetadata Dependency { get; set; }

		// Token: 0x06000221 RID: 545 RVA: 0x00009EE0 File Offset: 0x000080E0
		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(41, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Missing '");
			defaultInterpolatedStringHandler.AppendFormatted(this.Dependency.Id);
			defaultInterpolatedStringHandler.AppendLiteral("' with required version range [");
			defaultInterpolatedStringHandler.AppendFormatted<ApplicationVersionRange>(this.Dependency.VersionRange);
			defaultInterpolatedStringHandler.AppendLiteral("]");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x06000222 RID: 546 RVA: 0x00009F4B File Offset: 0x0000814B
		public override ModuleIssue ToLegacy()
		{
			return new ModuleIssue(base.Module, this.Dependency.Id, ModuleIssueType.MissingDependencies, this.ToString(), this.Dependency.VersionRange);
		}

		// Token: 0x06000223 RID: 547 RVA: 0x00009F75 File Offset: 0x00008175
		[CompilerGenerated]
		protected override bool PrintMembers(StringBuilder builder)
		{
			if (base.PrintMembers(builder))
			{
				builder.Append(", ");
			}
			builder.Append("Dependency = ");
			builder.Append(this.Dependency);
			return true;
		}

		// Token: 0x06000224 RID: 548 RVA: 0x00009FA6 File Offset: 0x000081A6
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleMissingVersionRangeDependencyIssue left, ModuleMissingVersionRangeDependencyIssue right)
		{
			return !(left == right);
		}

		// Token: 0x06000225 RID: 549 RVA: 0x00009FB2 File Offset: 0x000081B2
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleMissingVersionRangeDependencyIssue left, ModuleMissingVersionRangeDependencyIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x06000226 RID: 550 RVA: 0x00009FC8 File Offset: 0x000081C8
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return base.GetHashCode() * -1521134295 + EqualityComparer<DependentModuleMetadata>.Default.GetHashCode(this.<Dependency>k__BackingField);
		}

		// Token: 0x06000227 RID: 551 RVA: 0x00009FE7 File Offset: 0x000081E7
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleMissingVersionRangeDependencyIssue);
		}

		// Token: 0x06000228 RID: 552 RVA: 0x00009FF5 File Offset: 0x000081F5
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleIssueV2 other)
		{
			return this.Equals(other);
		}

		// Token: 0x06000229 RID: 553 RVA: 0x00009FFE File Offset: 0x000081FE
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleMissingVersionRangeDependencyIssue other)
		{
			return this == other || (base.Equals(other) && EqualityComparer<DependentModuleMetadata>.Default.Equals(this.<Dependency>k__BackingField, other.<Dependency>k__BackingField));
		}

		// Token: 0x0600022B RID: 555 RVA: 0x0000A031 File Offset: 0x00008231
		[CompilerGenerated]
		private ModuleMissingVersionRangeDependencyIssue(ModuleMissingVersionRangeDependencyIssue original)
			: base(original)
		{
			this.Dependency = original.<Dependency>k__BackingField;
		}

		// Token: 0x0600022C RID: 556 RVA: 0x0000A048 File Offset: 0x00008248
		[CompilerGenerated]
		public void Deconstruct(out ModuleInfoExtended Module, out DependentModuleMetadata Dependency)
		{
			Module = base.Module;
			Dependency = this.Dependency;
		}
	}
}
