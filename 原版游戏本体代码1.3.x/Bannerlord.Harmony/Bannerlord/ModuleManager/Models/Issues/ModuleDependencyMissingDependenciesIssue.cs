using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x0200002B RID: 43
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleDependencyMissingDependenciesIssue : ModuleIssueV2, IEquatable<ModuleDependencyMissingDependenciesIssue>
	{
		// Token: 0x0600022D RID: 557 RVA: 0x0000A05A File Offset: 0x0000825A
		public ModuleDependencyMissingDependenciesIssue(ModuleInfoExtended Module, string DependencyId)
		{
			this.DependencyId = DependencyId;
			base..ctor(Module);
		}

		// Token: 0x17000074 RID: 116
		// (get) Token: 0x0600022E RID: 558 RVA: 0x0000A06B File Offset: 0x0000826B
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleDependencyMissingDependenciesIssue);
			}
		}

		// Token: 0x17000075 RID: 117
		// (get) Token: 0x0600022F RID: 559 RVA: 0x0000A077 File Offset: 0x00008277
		// (set) Token: 0x06000230 RID: 560 RVA: 0x0000A07F File Offset: 0x0000827F
		public string DependencyId { get; set; }

		// Token: 0x06000231 RID: 561 RVA: 0x0000A088 File Offset: 0x00008288
		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(65, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Module '");
			defaultInterpolatedStringHandler.AppendFormatted(base.Module.Id);
			defaultInterpolatedStringHandler.AppendLiteral("': Required dependency '");
			defaultInterpolatedStringHandler.AppendFormatted(this.DependencyId);
			defaultInterpolatedStringHandler.AppendLiteral("' is missing its own dependencies");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x06000232 RID: 562 RVA: 0x0000A0EE File Offset: 0x000082EE
		public override ModuleIssue ToLegacy()
		{
			return new ModuleIssue(base.Module, this.DependencyId, ModuleIssueType.DependencyMissingDependencies, this.ToString(), ApplicationVersionRange.Empty);
		}

		// Token: 0x06000233 RID: 563 RVA: 0x0000A10D File Offset: 0x0000830D
		[CompilerGenerated]
		protected override bool PrintMembers(StringBuilder builder)
		{
			if (base.PrintMembers(builder))
			{
				builder.Append(", ");
			}
			builder.Append("DependencyId = ");
			builder.Append(this.DependencyId);
			return true;
		}

		// Token: 0x06000234 RID: 564 RVA: 0x0000A13E File Offset: 0x0000833E
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleDependencyMissingDependenciesIssue left, ModuleDependencyMissingDependenciesIssue right)
		{
			return !(left == right);
		}

		// Token: 0x06000235 RID: 565 RVA: 0x0000A14A File Offset: 0x0000834A
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleDependencyMissingDependenciesIssue left, ModuleDependencyMissingDependenciesIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x06000236 RID: 566 RVA: 0x0000A160 File Offset: 0x00008360
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return base.GetHashCode() * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.<DependencyId>k__BackingField);
		}

		// Token: 0x06000237 RID: 567 RVA: 0x0000A17F File Offset: 0x0000837F
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleDependencyMissingDependenciesIssue);
		}

		// Token: 0x06000238 RID: 568 RVA: 0x0000A18D File Offset: 0x0000838D
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleIssueV2 other)
		{
			return this.Equals(other);
		}

		// Token: 0x06000239 RID: 569 RVA: 0x0000A196 File Offset: 0x00008396
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleDependencyMissingDependenciesIssue other)
		{
			return this == other || (base.Equals(other) && EqualityComparer<string>.Default.Equals(this.<DependencyId>k__BackingField, other.<DependencyId>k__BackingField));
		}

		// Token: 0x0600023B RID: 571 RVA: 0x0000A1C9 File Offset: 0x000083C9
		[CompilerGenerated]
		private ModuleDependencyMissingDependenciesIssue(ModuleDependencyMissingDependenciesIssue original)
			: base(original)
		{
			this.DependencyId = original.<DependencyId>k__BackingField;
		}

		// Token: 0x0600023C RID: 572 RVA: 0x0000A1E0 File Offset: 0x000083E0
		[CompilerGenerated]
		public void Deconstruct(out ModuleInfoExtended Module, out string DependencyId)
		{
			Module = base.Module;
			DependencyId = this.DependencyId;
		}
	}
}
