using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x02000037 RID: 55
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleDependencyNotLoadedBeforeIssue : ModuleIssueV2, IEquatable<ModuleDependencyNotLoadedBeforeIssue>
	{
		// Token: 0x060002E4 RID: 740 RVA: 0x0000B229 File Offset: 0x00009429
		public ModuleDependencyNotLoadedBeforeIssue(ModuleInfoExtended Module, DependentModuleMetadata Dependency)
		{
			this.Dependency = Dependency;
			base..ctor(Module);
		}

		// Token: 0x17000089 RID: 137
		// (get) Token: 0x060002E5 RID: 741 RVA: 0x0000B23A File Offset: 0x0000943A
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleDependencyNotLoadedBeforeIssue);
			}
		}

		// Token: 0x1700008A RID: 138
		// (get) Token: 0x060002E6 RID: 742 RVA: 0x0000B246 File Offset: 0x00009446
		// (set) Token: 0x060002E7 RID: 743 RVA: 0x0000B24E File Offset: 0x0000944E
		public DependentModuleMetadata Dependency { get; set; }

		// Token: 0x060002E8 RID: 744 RVA: 0x0000B258 File Offset: 0x00009458
		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 2);
			defaultInterpolatedStringHandler.AppendLiteral("'");
			defaultInterpolatedStringHandler.AppendFormatted(this.Dependency.Id);
			defaultInterpolatedStringHandler.AppendLiteral("' should be loaded before '");
			defaultInterpolatedStringHandler.AppendFormatted(base.Module.Id);
			defaultInterpolatedStringHandler.AppendLiteral("'");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x060002E9 RID: 745 RVA: 0x0000B2C3 File Offset: 0x000094C3
		public override ModuleIssue ToLegacy()
		{
			return new ModuleIssue(base.Module, this.Dependency.Id, ModuleIssueType.DependencyNotLoadedBeforeThis, this.ToString(), ApplicationVersionRange.Empty);
		}

		// Token: 0x060002EA RID: 746 RVA: 0x0000B2E8 File Offset: 0x000094E8
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

		// Token: 0x060002EB RID: 747 RVA: 0x0000B319 File Offset: 0x00009519
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleDependencyNotLoadedBeforeIssue left, ModuleDependencyNotLoadedBeforeIssue right)
		{
			return !(left == right);
		}

		// Token: 0x060002EC RID: 748 RVA: 0x0000B325 File Offset: 0x00009525
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleDependencyNotLoadedBeforeIssue left, ModuleDependencyNotLoadedBeforeIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x060002ED RID: 749 RVA: 0x0000B33B File Offset: 0x0000953B
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return base.GetHashCode() * -1521134295 + EqualityComparer<DependentModuleMetadata>.Default.GetHashCode(this.<Dependency>k__BackingField);
		}

		// Token: 0x060002EE RID: 750 RVA: 0x0000B35A File Offset: 0x0000955A
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleDependencyNotLoadedBeforeIssue);
		}

		// Token: 0x060002EF RID: 751 RVA: 0x0000B368 File Offset: 0x00009568
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleIssueV2 other)
		{
			return this.Equals(other);
		}

		// Token: 0x060002F0 RID: 752 RVA: 0x0000B371 File Offset: 0x00009571
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleDependencyNotLoadedBeforeIssue other)
		{
			return this == other || (base.Equals(other) && EqualityComparer<DependentModuleMetadata>.Default.Equals(this.<Dependency>k__BackingField, other.<Dependency>k__BackingField));
		}

		// Token: 0x060002F2 RID: 754 RVA: 0x0000B3A4 File Offset: 0x000095A4
		[CompilerGenerated]
		private ModuleDependencyNotLoadedBeforeIssue(ModuleDependencyNotLoadedBeforeIssue original)
			: base(original)
		{
			this.Dependency = original.<Dependency>k__BackingField;
		}

		// Token: 0x060002F3 RID: 755 RVA: 0x0000B3BB File Offset: 0x000095BB
		[CompilerGenerated]
		public void Deconstruct(out ModuleInfoExtended Module, out DependentModuleMetadata Dependency)
		{
			Module = base.Module;
			Dependency = this.Dependency;
		}
	}
}
