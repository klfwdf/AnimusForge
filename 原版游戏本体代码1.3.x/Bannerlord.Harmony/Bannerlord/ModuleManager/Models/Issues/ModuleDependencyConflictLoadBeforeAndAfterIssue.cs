using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x02000035 RID: 53
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleDependencyConflictLoadBeforeAndAfterIssue : ModuleIssueV2, IEquatable<ModuleDependencyConflictLoadBeforeAndAfterIssue>
	{
		// Token: 0x060002C4 RID: 708 RVA: 0x0000AEDF File Offset: 0x000090DF
		public ModuleDependencyConflictLoadBeforeAndAfterIssue(ModuleInfoExtended Module, DependentModuleMetadata ConflictingModule)
		{
			this.ConflictingModule = ConflictingModule;
			base..ctor(Module);
		}

		// Token: 0x17000085 RID: 133
		// (get) Token: 0x060002C5 RID: 709 RVA: 0x0000AEF0 File Offset: 0x000090F0
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleDependencyConflictLoadBeforeAndAfterIssue);
			}
		}

		// Token: 0x17000086 RID: 134
		// (get) Token: 0x060002C6 RID: 710 RVA: 0x0000AEFC File Offset: 0x000090FC
		// (set) Token: 0x060002C7 RID: 711 RVA: 0x0000AF04 File Offset: 0x00009104
		public DependentModuleMetadata ConflictingModule { get; set; }

		// Token: 0x060002C8 RID: 712 RVA: 0x0000AF10 File Offset: 0x00009110
		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(89, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Module '");
			defaultInterpolatedStringHandler.AppendFormatted(base.Module.Id);
			defaultInterpolatedStringHandler.AppendLiteral("' has conflicting load order requirements with '");
			defaultInterpolatedStringHandler.AppendFormatted(this.ConflictingModule.Id);
			defaultInterpolatedStringHandler.AppendLiteral("' (both LoadBefore and LoadAfter)");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x060002C9 RID: 713 RVA: 0x0000AF7B File Offset: 0x0000917B
		public override ModuleIssue ToLegacy()
		{
			return new ModuleIssue(base.Module, this.ConflictingModule.Id, ModuleIssueType.DependencyConflictDependentLoadBeforeAndAfter, this.ToString(), ApplicationVersionRange.Empty);
		}

		// Token: 0x060002CA RID: 714 RVA: 0x0000AFA0 File Offset: 0x000091A0
		[CompilerGenerated]
		protected override bool PrintMembers(StringBuilder builder)
		{
			if (base.PrintMembers(builder))
			{
				builder.Append(", ");
			}
			builder.Append("ConflictingModule = ");
			builder.Append(this.ConflictingModule);
			return true;
		}

		// Token: 0x060002CB RID: 715 RVA: 0x0000AFD1 File Offset: 0x000091D1
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleDependencyConflictLoadBeforeAndAfterIssue left, ModuleDependencyConflictLoadBeforeAndAfterIssue right)
		{
			return !(left == right);
		}

		// Token: 0x060002CC RID: 716 RVA: 0x0000AFDD File Offset: 0x000091DD
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleDependencyConflictLoadBeforeAndAfterIssue left, ModuleDependencyConflictLoadBeforeAndAfterIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x060002CD RID: 717 RVA: 0x0000AFF3 File Offset: 0x000091F3
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return base.GetHashCode() * -1521134295 + EqualityComparer<DependentModuleMetadata>.Default.GetHashCode(this.<ConflictingModule>k__BackingField);
		}

		// Token: 0x060002CE RID: 718 RVA: 0x0000B012 File Offset: 0x00009212
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleDependencyConflictLoadBeforeAndAfterIssue);
		}

		// Token: 0x060002CF RID: 719 RVA: 0x0000B020 File Offset: 0x00009220
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleIssueV2 other)
		{
			return this.Equals(other);
		}

		// Token: 0x060002D0 RID: 720 RVA: 0x0000B029 File Offset: 0x00009229
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleDependencyConflictLoadBeforeAndAfterIssue other)
		{
			return this == other || (base.Equals(other) && EqualityComparer<DependentModuleMetadata>.Default.Equals(this.<ConflictingModule>k__BackingField, other.<ConflictingModule>k__BackingField));
		}

		// Token: 0x060002D2 RID: 722 RVA: 0x0000B05C File Offset: 0x0000925C
		[CompilerGenerated]
		private ModuleDependencyConflictLoadBeforeAndAfterIssue(ModuleDependencyConflictLoadBeforeAndAfterIssue original)
			: base(original)
		{
			this.ConflictingModule = original.<ConflictingModule>k__BackingField;
		}

		// Token: 0x060002D3 RID: 723 RVA: 0x0000B073 File Offset: 0x00009273
		[CompilerGenerated]
		public void Deconstruct(out ModuleInfoExtended Module, out DependentModuleMetadata ConflictingModule)
		{
			Module = base.Module;
			ConflictingModule = this.ConflictingModule;
		}
	}
}
