using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x02000033 RID: 51
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleIncompatibleIssue : ModuleIssueV2, IEquatable<ModuleIncompatibleIssue>
	{
		// Token: 0x060002A4 RID: 676 RVA: 0x0000ABF2 File Offset: 0x00008DF2
		public ModuleIncompatibleIssue(ModuleInfoExtended Module, ModuleInfoExtended IncompatibleModule)
		{
			this.IncompatibleModule = IncompatibleModule;
			base..ctor(Module);
		}

		// Token: 0x17000081 RID: 129
		// (get) Token: 0x060002A5 RID: 677 RVA: 0x0000AC03 File Offset: 0x00008E03
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleIncompatibleIssue);
			}
		}

		// Token: 0x17000082 RID: 130
		// (get) Token: 0x060002A6 RID: 678 RVA: 0x0000AC0F File Offset: 0x00008E0F
		// (set) Token: 0x060002A7 RID: 679 RVA: 0x0000AC17 File Offset: 0x00008E17
		public ModuleInfoExtended IncompatibleModule { get; set; }

		// Token: 0x060002A8 RID: 680 RVA: 0x0000AC20 File Offset: 0x00008E20
		public override string ToString()
		{
			return "'" + this.IncompatibleModule.Id + "' is incompatible with this module";
		}

		// Token: 0x060002A9 RID: 681 RVA: 0x0000AC3C File Offset: 0x00008E3C
		public override ModuleIssue ToLegacy()
		{
			return new ModuleIssue(base.Module, this.IncompatibleModule.Id, ModuleIssueType.Incompatible, this.ToString(), ApplicationVersionRange.Empty);
		}

		// Token: 0x060002AA RID: 682 RVA: 0x0000AC60 File Offset: 0x00008E60
		[CompilerGenerated]
		protected override bool PrintMembers(StringBuilder builder)
		{
			if (base.PrintMembers(builder))
			{
				builder.Append(", ");
			}
			builder.Append("IncompatibleModule = ");
			builder.Append(this.IncompatibleModule);
			return true;
		}

		// Token: 0x060002AB RID: 683 RVA: 0x0000AC91 File Offset: 0x00008E91
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleIncompatibleIssue left, ModuleIncompatibleIssue right)
		{
			return !(left == right);
		}

		// Token: 0x060002AC RID: 684 RVA: 0x0000AC9D File Offset: 0x00008E9D
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleIncompatibleIssue left, ModuleIncompatibleIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x060002AD RID: 685 RVA: 0x0000ACB3 File Offset: 0x00008EB3
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return base.GetHashCode() * -1521134295 + EqualityComparer<ModuleInfoExtended>.Default.GetHashCode(this.<IncompatibleModule>k__BackingField);
		}

		// Token: 0x060002AE RID: 686 RVA: 0x0000ACD2 File Offset: 0x00008ED2
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleIncompatibleIssue);
		}

		// Token: 0x060002AF RID: 687 RVA: 0x0000ACE0 File Offset: 0x00008EE0
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleIssueV2 other)
		{
			return this.Equals(other);
		}

		// Token: 0x060002B0 RID: 688 RVA: 0x0000ACE9 File Offset: 0x00008EE9
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleIncompatibleIssue other)
		{
			return this == other || (base.Equals(other) && EqualityComparer<ModuleInfoExtended>.Default.Equals(this.<IncompatibleModule>k__BackingField, other.<IncompatibleModule>k__BackingField));
		}

		// Token: 0x060002B2 RID: 690 RVA: 0x0000AD1C File Offset: 0x00008F1C
		[CompilerGenerated]
		private ModuleIncompatibleIssue(ModuleIncompatibleIssue original)
			: base(original)
		{
			this.IncompatibleModule = original.<IncompatibleModule>k__BackingField;
		}

		// Token: 0x060002B3 RID: 691 RVA: 0x0000AD33 File Offset: 0x00008F33
		[CompilerGenerated]
		public void Deconstruct(out ModuleInfoExtended Module, out ModuleInfoExtended IncompatibleModule)
		{
			Module = base.Module;
			IncompatibleModule = this.IncompatibleModule;
		}
	}
}
