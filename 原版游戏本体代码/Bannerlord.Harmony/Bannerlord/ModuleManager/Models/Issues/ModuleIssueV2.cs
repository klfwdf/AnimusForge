using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x02000025 RID: 37
	[NullableContext(1)]
	[Nullable(0)]
	internal abstract class ModuleIssueV2 : IEquatable<ModuleIssueV2>
	{
		// Token: 0x060001CE RID: 462 RVA: 0x00009780 File Offset: 0x00007980
		protected ModuleIssueV2(ModuleInfoExtended Module)
		{
			this.Module = Module;
			base..ctor();
		}

		// Token: 0x17000068 RID: 104
		// (get) Token: 0x060001CF RID: 463 RVA: 0x00009790 File Offset: 0x00007990
		[CompilerGenerated]
		protected virtual Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleIssueV2);
			}
		}

		// Token: 0x17000069 RID: 105
		// (get) Token: 0x060001D0 RID: 464 RVA: 0x0000979C File Offset: 0x0000799C
		// (set) Token: 0x060001D1 RID: 465 RVA: 0x000097A4 File Offset: 0x000079A4
		public ModuleInfoExtended Module { get; set; }

		// Token: 0x060001D2 RID: 466
		public abstract ModuleIssue ToLegacy();

		// Token: 0x060001D3 RID: 467 RVA: 0x000097B0 File Offset: 0x000079B0
		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("ModuleIssueV2");
			stringBuilder.Append(" { ");
			if (this.PrintMembers(stringBuilder))
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}

		// Token: 0x060001D4 RID: 468 RVA: 0x000097FC File Offset: 0x000079FC
		[CompilerGenerated]
		protected virtual bool PrintMembers(StringBuilder builder)
		{
			RuntimeHelpers.EnsureSufficientExecutionStack();
			builder.Append("Module = ");
			builder.Append(this.Module);
			return true;
		}

		// Token: 0x060001D5 RID: 469 RVA: 0x0000981D File Offset: 0x00007A1D
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleIssueV2 left, ModuleIssueV2 right)
		{
			return !(left == right);
		}

		// Token: 0x060001D6 RID: 470 RVA: 0x00009829 File Offset: 0x00007A29
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleIssueV2 left, ModuleIssueV2 right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x060001D7 RID: 471 RVA: 0x0000983F File Offset: 0x00007A3F
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return EqualityComparer<Type>.Default.GetHashCode(this.EqualityContract) * -1521134295 + EqualityComparer<ModuleInfoExtended>.Default.GetHashCode(this.<Module>k__BackingField);
		}

		// Token: 0x060001D8 RID: 472 RVA: 0x00009868 File Offset: 0x00007A68
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleIssueV2);
		}

		// Token: 0x060001D9 RID: 473 RVA: 0x00009876 File Offset: 0x00007A76
		[NullableContext(2)]
		[CompilerGenerated]
		public virtual bool Equals(ModuleIssueV2 other)
		{
			return this == other || (other != null && this.EqualityContract == other.EqualityContract && EqualityComparer<ModuleInfoExtended>.Default.Equals(this.<Module>k__BackingField, other.<Module>k__BackingField));
		}

		// Token: 0x060001DB RID: 475 RVA: 0x000098AE File Offset: 0x00007AAE
		[CompilerGenerated]
		protected ModuleIssueV2(ModuleIssueV2 original)
		{
			this.Module = original.<Module>k__BackingField;
		}

		// Token: 0x060001DC RID: 476 RVA: 0x000098C4 File Offset: 0x00007AC4
		[CompilerGenerated]
		public void Deconstruct(out ModuleInfoExtended Module)
		{
			Module = this.Module;
		}
	}
}
