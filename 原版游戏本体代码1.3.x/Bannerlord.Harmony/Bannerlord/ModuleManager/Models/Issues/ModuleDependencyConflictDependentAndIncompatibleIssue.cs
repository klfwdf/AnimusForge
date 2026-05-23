using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x02000034 RID: 52
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleDependencyConflictDependentAndIncompatibleIssue : ModuleIssueV2, IEquatable<ModuleDependencyConflictDependentAndIncompatibleIssue>
	{
		// Token: 0x060002B4 RID: 692 RVA: 0x0000AD45 File Offset: 0x00008F45
		public ModuleDependencyConflictDependentAndIncompatibleIssue(ModuleInfoExtended Module, string ConflictingModuleId)
		{
			this.ConflictingModuleId = ConflictingModuleId;
			base..ctor(Module);
		}

		// Token: 0x17000083 RID: 131
		// (get) Token: 0x060002B5 RID: 693 RVA: 0x0000AD56 File Offset: 0x00008F56
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleDependencyConflictDependentAndIncompatibleIssue);
			}
		}

		// Token: 0x17000084 RID: 132
		// (get) Token: 0x060002B6 RID: 694 RVA: 0x0000AD62 File Offset: 0x00008F62
		// (set) Token: 0x060002B7 RID: 695 RVA: 0x0000AD6A File Offset: 0x00008F6A
		public string ConflictingModuleId { get; set; }

		// Token: 0x060002B8 RID: 696 RVA: 0x0000AD74 File Offset: 0x00008F74
		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(87, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Module '");
			defaultInterpolatedStringHandler.AppendFormatted(base.Module.Id);
			defaultInterpolatedStringHandler.AppendLiteral("' has conflicting configuration: '");
			defaultInterpolatedStringHandler.AppendFormatted(this.ConflictingModuleId);
			defaultInterpolatedStringHandler.AppendLiteral("' is marked as both required and incompatible");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x060002B9 RID: 697 RVA: 0x0000ADDA File Offset: 0x00008FDA
		public override ModuleIssue ToLegacy()
		{
			return new ModuleIssue(base.Module, this.ConflictingModuleId, ModuleIssueType.DependencyConflictDependentAndIncompatible, this.ToString(), ApplicationVersionRange.Empty);
		}

		// Token: 0x060002BA RID: 698 RVA: 0x0000ADFA File Offset: 0x00008FFA
		[CompilerGenerated]
		protected override bool PrintMembers(StringBuilder builder)
		{
			if (base.PrintMembers(builder))
			{
				builder.Append(", ");
			}
			builder.Append("ConflictingModuleId = ");
			builder.Append(this.ConflictingModuleId);
			return true;
		}

		// Token: 0x060002BB RID: 699 RVA: 0x0000AE2B File Offset: 0x0000902B
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleDependencyConflictDependentAndIncompatibleIssue left, ModuleDependencyConflictDependentAndIncompatibleIssue right)
		{
			return !(left == right);
		}

		// Token: 0x060002BC RID: 700 RVA: 0x0000AE37 File Offset: 0x00009037
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleDependencyConflictDependentAndIncompatibleIssue left, ModuleDependencyConflictDependentAndIncompatibleIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x060002BD RID: 701 RVA: 0x0000AE4D File Offset: 0x0000904D
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return base.GetHashCode() * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.<ConflictingModuleId>k__BackingField);
		}

		// Token: 0x060002BE RID: 702 RVA: 0x0000AE6C File Offset: 0x0000906C
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleDependencyConflictDependentAndIncompatibleIssue);
		}

		// Token: 0x060002BF RID: 703 RVA: 0x0000AE7A File Offset: 0x0000907A
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleIssueV2 other)
		{
			return this.Equals(other);
		}

		// Token: 0x060002C0 RID: 704 RVA: 0x0000AE83 File Offset: 0x00009083
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleDependencyConflictDependentAndIncompatibleIssue other)
		{
			return this == other || (base.Equals(other) && EqualityComparer<string>.Default.Equals(this.<ConflictingModuleId>k__BackingField, other.<ConflictingModuleId>k__BackingField));
		}

		// Token: 0x060002C2 RID: 706 RVA: 0x0000AEB6 File Offset: 0x000090B6
		[CompilerGenerated]
		private ModuleDependencyConflictDependentAndIncompatibleIssue(ModuleDependencyConflictDependentAndIncompatibleIssue original)
			: base(original)
		{
			this.ConflictingModuleId = original.<ConflictingModuleId>k__BackingField;
		}

		// Token: 0x060002C3 RID: 707 RVA: 0x0000AECD File Offset: 0x000090CD
		[CompilerGenerated]
		public void Deconstruct(out ModuleInfoExtended Module, out string ConflictingModuleId)
		{
			Module = base.Module;
			ConflictingModuleId = this.ConflictingModuleId;
		}
	}
}
