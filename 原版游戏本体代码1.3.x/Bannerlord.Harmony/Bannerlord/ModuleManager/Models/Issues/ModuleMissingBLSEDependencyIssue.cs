using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x02000028 RID: 40
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleMissingBLSEDependencyIssue : ModuleIssueV2, IEquatable<ModuleMissingBLSEDependencyIssue>
	{
		// Token: 0x060001FD RID: 509 RVA: 0x00009BC4 File Offset: 0x00007DC4
		public ModuleMissingBLSEDependencyIssue(ModuleInfoExtended Module, DependentModuleMetadata Dependency)
		{
			this.Dependency = Dependency;
			base..ctor(Module);
		}

		// Token: 0x1700006E RID: 110
		// (get) Token: 0x060001FE RID: 510 RVA: 0x00009BD5 File Offset: 0x00007DD5
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleMissingBLSEDependencyIssue);
			}
		}

		// Token: 0x1700006F RID: 111
		// (get) Token: 0x060001FF RID: 511 RVA: 0x00009BE1 File Offset: 0x00007DE1
		// (set) Token: 0x06000200 RID: 512 RVA: 0x00009BE9 File Offset: 0x00007DE9
		public DependentModuleMetadata Dependency { get; set; }

		// Token: 0x06000201 RID: 513 RVA: 0x00009BF2 File Offset: 0x00007DF2
		public override string ToString()
		{
			return "Missing Bannerlord Software Extender";
		}

		// Token: 0x06000202 RID: 514 RVA: 0x00009BF9 File Offset: 0x00007DF9
		public override ModuleIssue ToLegacy()
		{
			return new ModuleIssue(base.Module, this.Dependency.Id, ModuleIssueType.MissingBLSE, this.ToString(), ApplicationVersionRange.Empty);
		}

		// Token: 0x06000203 RID: 515 RVA: 0x00009C1E File Offset: 0x00007E1E
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

		// Token: 0x06000204 RID: 516 RVA: 0x00009C4F File Offset: 0x00007E4F
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleMissingBLSEDependencyIssue left, ModuleMissingBLSEDependencyIssue right)
		{
			return !(left == right);
		}

		// Token: 0x06000205 RID: 517 RVA: 0x00009C5B File Offset: 0x00007E5B
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleMissingBLSEDependencyIssue left, ModuleMissingBLSEDependencyIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x06000206 RID: 518 RVA: 0x00009C71 File Offset: 0x00007E71
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return base.GetHashCode() * -1521134295 + EqualityComparer<DependentModuleMetadata>.Default.GetHashCode(this.<Dependency>k__BackingField);
		}

		// Token: 0x06000207 RID: 519 RVA: 0x00009C90 File Offset: 0x00007E90
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleMissingBLSEDependencyIssue);
		}

		// Token: 0x06000208 RID: 520 RVA: 0x00009C9E File Offset: 0x00007E9E
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleIssueV2 other)
		{
			return this.Equals(other);
		}

		// Token: 0x06000209 RID: 521 RVA: 0x00009CA7 File Offset: 0x00007EA7
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleMissingBLSEDependencyIssue other)
		{
			return this == other || (base.Equals(other) && EqualityComparer<DependentModuleMetadata>.Default.Equals(this.<Dependency>k__BackingField, other.<Dependency>k__BackingField));
		}

		// Token: 0x0600020B RID: 523 RVA: 0x00009CDA File Offset: 0x00007EDA
		[CompilerGenerated]
		private ModuleMissingBLSEDependencyIssue(ModuleMissingBLSEDependencyIssue original)
			: base(original)
		{
			this.Dependency = original.<Dependency>k__BackingField;
		}

		// Token: 0x0600020C RID: 524 RVA: 0x00009CF1 File Offset: 0x00007EF1
		[CompilerGenerated]
		public void Deconstruct(out ModuleInfoExtended Module, out DependentModuleMetadata Dependency)
		{
			Module = base.Module;
			Dependency = this.Dependency;
		}
	}
}
