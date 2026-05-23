using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x02000036 RID: 54
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleDependencyConflictCircularIssue : ModuleIssueV2, IEquatable<ModuleDependencyConflictCircularIssue>
	{
		// Token: 0x060002D4 RID: 724 RVA: 0x0000B085 File Offset: 0x00009285
		public ModuleDependencyConflictCircularIssue(ModuleInfoExtended Module, ModuleInfoExtended CircularDependency)
		{
			this.CircularDependency = CircularDependency;
			base..ctor(Module);
		}

		// Token: 0x17000087 RID: 135
		// (get) Token: 0x060002D5 RID: 725 RVA: 0x0000B096 File Offset: 0x00009296
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleDependencyConflictCircularIssue);
			}
		}

		// Token: 0x17000088 RID: 136
		// (get) Token: 0x060002D6 RID: 726 RVA: 0x0000B0A2 File Offset: 0x000092A2
		// (set) Token: 0x060002D7 RID: 727 RVA: 0x0000B0AA File Offset: 0x000092AA
		public ModuleInfoExtended CircularDependency { get; set; }

		// Token: 0x060002D8 RID: 728 RVA: 0x0000B0B4 File Offset: 0x000092B4
		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Module '");
			defaultInterpolatedStringHandler.AppendFormatted(base.Module.Id);
			defaultInterpolatedStringHandler.AppendLiteral("' and '");
			defaultInterpolatedStringHandler.AppendFormatted(this.CircularDependency.Id);
			defaultInterpolatedStringHandler.AppendLiteral("' have circular dependencies");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x060002D9 RID: 729 RVA: 0x0000B11F File Offset: 0x0000931F
		public override ModuleIssue ToLegacy()
		{
			return new ModuleIssue(base.Module, this.CircularDependency.Id, ModuleIssueType.DependencyConflictCircular, this.ToString(), ApplicationVersionRange.Empty);
		}

		// Token: 0x060002DA RID: 730 RVA: 0x0000B144 File Offset: 0x00009344
		[CompilerGenerated]
		protected override bool PrintMembers(StringBuilder builder)
		{
			if (base.PrintMembers(builder))
			{
				builder.Append(", ");
			}
			builder.Append("CircularDependency = ");
			builder.Append(this.CircularDependency);
			return true;
		}

		// Token: 0x060002DB RID: 731 RVA: 0x0000B175 File Offset: 0x00009375
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleDependencyConflictCircularIssue left, ModuleDependencyConflictCircularIssue right)
		{
			return !(left == right);
		}

		// Token: 0x060002DC RID: 732 RVA: 0x0000B181 File Offset: 0x00009381
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleDependencyConflictCircularIssue left, ModuleDependencyConflictCircularIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x060002DD RID: 733 RVA: 0x0000B197 File Offset: 0x00009397
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return base.GetHashCode() * -1521134295 + EqualityComparer<ModuleInfoExtended>.Default.GetHashCode(this.<CircularDependency>k__BackingField);
		}

		// Token: 0x060002DE RID: 734 RVA: 0x0000B1B6 File Offset: 0x000093B6
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleDependencyConflictCircularIssue);
		}

		// Token: 0x060002DF RID: 735 RVA: 0x0000B1C4 File Offset: 0x000093C4
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleIssueV2 other)
		{
			return this.Equals(other);
		}

		// Token: 0x060002E0 RID: 736 RVA: 0x0000B1CD File Offset: 0x000093CD
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleDependencyConflictCircularIssue other)
		{
			return this == other || (base.Equals(other) && EqualityComparer<ModuleInfoExtended>.Default.Equals(this.<CircularDependency>k__BackingField, other.<CircularDependency>k__BackingField));
		}

		// Token: 0x060002E2 RID: 738 RVA: 0x0000B200 File Offset: 0x00009400
		[CompilerGenerated]
		private ModuleDependencyConflictCircularIssue(ModuleDependencyConflictCircularIssue original)
			: base(original)
		{
			this.CircularDependency = original.<CircularDependency>k__BackingField;
		}

		// Token: 0x060002E3 RID: 739 RVA: 0x0000B217 File Offset: 0x00009417
		[CompilerGenerated]
		public void Deconstruct(out ModuleInfoExtended Module, out ModuleInfoExtended CircularDependency)
		{
			Module = base.Module;
			CircularDependency = this.CircularDependency;
		}
	}
}
