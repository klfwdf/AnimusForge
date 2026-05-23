using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x02000029 RID: 41
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleMissingExactVersionDependencyIssue : ModuleIssueV2, IEquatable<ModuleMissingExactVersionDependencyIssue>
	{
		// Token: 0x0600020D RID: 525 RVA: 0x00009D03 File Offset: 0x00007F03
		public ModuleMissingExactVersionDependencyIssue(ModuleInfoExtended Module, DependentModuleMetadata Dependency)
		{
			this.Dependency = Dependency;
			base..ctor(Module);
		}

		// Token: 0x17000070 RID: 112
		// (get) Token: 0x0600020E RID: 526 RVA: 0x00009D14 File Offset: 0x00007F14
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleMissingExactVersionDependencyIssue);
			}
		}

		// Token: 0x17000071 RID: 113
		// (get) Token: 0x0600020F RID: 527 RVA: 0x00009D20 File Offset: 0x00007F20
		// (set) Token: 0x06000210 RID: 528 RVA: 0x00009D28 File Offset: 0x00007F28
		public DependentModuleMetadata Dependency { get; set; }

		// Token: 0x06000211 RID: 529 RVA: 0x00009D34 File Offset: 0x00007F34
		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(33, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Missing '");
			defaultInterpolatedStringHandler.AppendFormatted(this.Dependency.Id);
			defaultInterpolatedStringHandler.AppendLiteral("' with required version ");
			defaultInterpolatedStringHandler.AppendFormatted<ApplicationVersion>(this.Dependency.Version);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x06000212 RID: 530 RVA: 0x00009D92 File Offset: 0x00007F92
		public override ModuleIssue ToLegacy()
		{
			return new ModuleIssue(base.Module, this.Dependency.Id, ModuleIssueType.MissingDependencies, this.ToString(), new ApplicationVersionRange(this.Dependency.Version, this.Dependency.Version));
		}

		// Token: 0x06000213 RID: 531 RVA: 0x00009DCC File Offset: 0x00007FCC
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

		// Token: 0x06000214 RID: 532 RVA: 0x00009DFD File Offset: 0x00007FFD
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleMissingExactVersionDependencyIssue left, ModuleMissingExactVersionDependencyIssue right)
		{
			return !(left == right);
		}

		// Token: 0x06000215 RID: 533 RVA: 0x00009E09 File Offset: 0x00008009
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleMissingExactVersionDependencyIssue left, ModuleMissingExactVersionDependencyIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x06000216 RID: 534 RVA: 0x00009E1F File Offset: 0x0000801F
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return base.GetHashCode() * -1521134295 + EqualityComparer<DependentModuleMetadata>.Default.GetHashCode(this.<Dependency>k__BackingField);
		}

		// Token: 0x06000217 RID: 535 RVA: 0x00009E3E File Offset: 0x0000803E
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleMissingExactVersionDependencyIssue);
		}

		// Token: 0x06000218 RID: 536 RVA: 0x00009E4C File Offset: 0x0000804C
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleIssueV2 other)
		{
			return this.Equals(other);
		}

		// Token: 0x06000219 RID: 537 RVA: 0x00009E55 File Offset: 0x00008055
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleMissingExactVersionDependencyIssue other)
		{
			return this == other || (base.Equals(other) && EqualityComparer<DependentModuleMetadata>.Default.Equals(this.<Dependency>k__BackingField, other.<Dependency>k__BackingField));
		}

		// Token: 0x0600021B RID: 539 RVA: 0x00009E88 File Offset: 0x00008088
		[CompilerGenerated]
		private ModuleMissingExactVersionDependencyIssue(ModuleMissingExactVersionDependencyIssue original)
			: base(original)
		{
			this.Dependency = original.<Dependency>k__BackingField;
		}

		// Token: 0x0600021C RID: 540 RVA: 0x00009E9F File Offset: 0x0000809F
		[CompilerGenerated]
		public void Deconstruct(out ModuleInfoExtended Module, out DependentModuleMetadata Dependency)
		{
			Module = base.Module;
			Dependency = this.Dependency;
		}
	}
}
