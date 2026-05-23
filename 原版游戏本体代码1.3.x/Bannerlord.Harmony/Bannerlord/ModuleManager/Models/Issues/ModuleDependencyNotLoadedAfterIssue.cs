using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x02000038 RID: 56
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleDependencyNotLoadedAfterIssue : ModuleIssueV2, IEquatable<ModuleDependencyNotLoadedAfterIssue>
	{
		// Token: 0x060002F4 RID: 756 RVA: 0x0000B3CD File Offset: 0x000095CD
		public ModuleDependencyNotLoadedAfterIssue(ModuleInfoExtended Module, DependentModuleMetadata Dependency)
		{
			this.Dependency = Dependency;
			base..ctor(Module);
		}

		// Token: 0x1700008B RID: 139
		// (get) Token: 0x060002F5 RID: 757 RVA: 0x0000B3DE File Offset: 0x000095DE
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleDependencyNotLoadedAfterIssue);
			}
		}

		// Token: 0x1700008C RID: 140
		// (get) Token: 0x060002F6 RID: 758 RVA: 0x0000B3EA File Offset: 0x000095EA
		// (set) Token: 0x060002F7 RID: 759 RVA: 0x0000B3F2 File Offset: 0x000095F2
		public DependentModuleMetadata Dependency { get; set; }

		// Token: 0x060002F8 RID: 760 RVA: 0x0000B3FC File Offset: 0x000095FC
		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(28, 2);
			defaultInterpolatedStringHandler.AppendLiteral("'");
			defaultInterpolatedStringHandler.AppendFormatted(this.Dependency.Id);
			defaultInterpolatedStringHandler.AppendLiteral("' should be loaded after '");
			defaultInterpolatedStringHandler.AppendFormatted(base.Module.Id);
			defaultInterpolatedStringHandler.AppendLiteral("'");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x060002F9 RID: 761 RVA: 0x0000B467 File Offset: 0x00009667
		public override ModuleIssue ToLegacy()
		{
			return new ModuleIssue(base.Module, this.Dependency.Id, ModuleIssueType.DependencyNotLoadedAfterThis, this.ToString(), ApplicationVersionRange.Empty);
		}

		// Token: 0x060002FA RID: 762 RVA: 0x0000B48C File Offset: 0x0000968C
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

		// Token: 0x060002FB RID: 763 RVA: 0x0000B4BD File Offset: 0x000096BD
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleDependencyNotLoadedAfterIssue left, ModuleDependencyNotLoadedAfterIssue right)
		{
			return !(left == right);
		}

		// Token: 0x060002FC RID: 764 RVA: 0x0000B4C9 File Offset: 0x000096C9
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleDependencyNotLoadedAfterIssue left, ModuleDependencyNotLoadedAfterIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x060002FD RID: 765 RVA: 0x0000B4DF File Offset: 0x000096DF
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return base.GetHashCode() * -1521134295 + EqualityComparer<DependentModuleMetadata>.Default.GetHashCode(this.<Dependency>k__BackingField);
		}

		// Token: 0x060002FE RID: 766 RVA: 0x0000B4FE File Offset: 0x000096FE
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleDependencyNotLoadedAfterIssue);
		}

		// Token: 0x060002FF RID: 767 RVA: 0x0000B50C File Offset: 0x0000970C
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleIssueV2 other)
		{
			return this.Equals(other);
		}

		// Token: 0x06000300 RID: 768 RVA: 0x0000B515 File Offset: 0x00009715
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleDependencyNotLoadedAfterIssue other)
		{
			return this == other || (base.Equals(other) && EqualityComparer<DependentModuleMetadata>.Default.Equals(this.<Dependency>k__BackingField, other.<Dependency>k__BackingField));
		}

		// Token: 0x06000302 RID: 770 RVA: 0x0000B548 File Offset: 0x00009748
		[CompilerGenerated]
		private ModuleDependencyNotLoadedAfterIssue(ModuleDependencyNotLoadedAfterIssue original)
			: base(original)
		{
			this.Dependency = original.<Dependency>k__BackingField;
		}

		// Token: 0x06000303 RID: 771 RVA: 0x0000B55F File Offset: 0x0000975F
		[CompilerGenerated]
		public void Deconstruct(out ModuleInfoExtended Module, out DependentModuleMetadata Dependency)
		{
			Module = base.Module;
			Dependency = this.Dependency;
		}
	}
}
