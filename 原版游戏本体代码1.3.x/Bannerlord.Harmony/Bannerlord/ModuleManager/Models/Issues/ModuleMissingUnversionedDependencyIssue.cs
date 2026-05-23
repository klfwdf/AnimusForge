using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x02000027 RID: 39
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleMissingUnversionedDependencyIssue : ModuleIssueV2, IEquatable<ModuleMissingUnversionedDependencyIssue>
	{
		// Token: 0x060001ED RID: 493 RVA: 0x00009A71 File Offset: 0x00007C71
		public ModuleMissingUnversionedDependencyIssue(ModuleInfoExtended Module, DependentModuleMetadata Dependency)
		{
			this.Dependency = Dependency;
			base..ctor(Module);
		}

		// Token: 0x1700006C RID: 108
		// (get) Token: 0x060001EE RID: 494 RVA: 0x00009A82 File Offset: 0x00007C82
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleMissingUnversionedDependencyIssue);
			}
		}

		// Token: 0x1700006D RID: 109
		// (get) Token: 0x060001EF RID: 495 RVA: 0x00009A8E File Offset: 0x00007C8E
		// (set) Token: 0x060001F0 RID: 496 RVA: 0x00009A96 File Offset: 0x00007C96
		public DependentModuleMetadata Dependency { get; set; }

		// Token: 0x060001F1 RID: 497 RVA: 0x00009A9F File Offset: 0x00007C9F
		public override string ToString()
		{
			return "Missing '" + this.Dependency.Id + "' module";
		}

		// Token: 0x060001F2 RID: 498 RVA: 0x00009ABB File Offset: 0x00007CBB
		public override ModuleIssue ToLegacy()
		{
			return new ModuleIssue(base.Module, this.Dependency.Id, ModuleIssueType.MissingDependencies, this.ToString(), ApplicationVersionRange.Empty);
		}

		// Token: 0x060001F3 RID: 499 RVA: 0x00009ADF File Offset: 0x00007CDF
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

		// Token: 0x060001F4 RID: 500 RVA: 0x00009B10 File Offset: 0x00007D10
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleMissingUnversionedDependencyIssue left, ModuleMissingUnversionedDependencyIssue right)
		{
			return !(left == right);
		}

		// Token: 0x060001F5 RID: 501 RVA: 0x00009B1C File Offset: 0x00007D1C
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleMissingUnversionedDependencyIssue left, ModuleMissingUnversionedDependencyIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x060001F6 RID: 502 RVA: 0x00009B32 File Offset: 0x00007D32
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return base.GetHashCode() * -1521134295 + EqualityComparer<DependentModuleMetadata>.Default.GetHashCode(this.<Dependency>k__BackingField);
		}

		// Token: 0x060001F7 RID: 503 RVA: 0x00009B51 File Offset: 0x00007D51
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleMissingUnversionedDependencyIssue);
		}

		// Token: 0x060001F8 RID: 504 RVA: 0x00009B5F File Offset: 0x00007D5F
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleIssueV2 other)
		{
			return this.Equals(other);
		}

		// Token: 0x060001F9 RID: 505 RVA: 0x00009B68 File Offset: 0x00007D68
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleMissingUnversionedDependencyIssue other)
		{
			return this == other || (base.Equals(other) && EqualityComparer<DependentModuleMetadata>.Default.Equals(this.<Dependency>k__BackingField, other.<Dependency>k__BackingField));
		}

		// Token: 0x060001FB RID: 507 RVA: 0x00009B9B File Offset: 0x00007D9B
		[CompilerGenerated]
		private ModuleMissingUnversionedDependencyIssue(ModuleMissingUnversionedDependencyIssue original)
			: base(original)
		{
			this.Dependency = original.<Dependency>k__BackingField;
		}

		// Token: 0x060001FC RID: 508 RVA: 0x00009BB2 File Offset: 0x00007DB2
		[CompilerGenerated]
		public void Deconstruct(out ModuleInfoExtended Module, out DependentModuleMetadata Dependency)
		{
			Module = base.Module;
			Dependency = this.Dependency;
		}
	}
}
