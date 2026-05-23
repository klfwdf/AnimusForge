using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x0200003A RID: 58
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleMissingNameIssue : ModuleIssueV2, IEquatable<ModuleMissingNameIssue>
	{
		// Token: 0x06000312 RID: 786 RVA: 0x0000B639 File Offset: 0x00009839
		public ModuleMissingNameIssue(ModuleInfoExtended Module)
			: base(Module)
		{
		}

		// Token: 0x1700008E RID: 142
		// (get) Token: 0x06000313 RID: 787 RVA: 0x0000B643 File Offset: 0x00009843
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleMissingNameIssue);
			}
		}

		// Token: 0x06000314 RID: 788 RVA: 0x0000B64F File Offset: 0x0000984F
		public override string ToString()
		{
			return "Module with Id '" + base.Module.Id + "' is missing its Name field";
		}

		// Token: 0x06000315 RID: 789 RVA: 0x0000B66B File Offset: 0x0000986B
		public override ModuleIssue ToLegacy()
		{
			return new ModuleIssue(base.Module, base.Module.Id, ModuleIssueType.MissingModuleName, this.ToString(), ApplicationVersionRange.Empty);
		}

		// Token: 0x06000316 RID: 790 RVA: 0x0000B690 File Offset: 0x00009890
		[CompilerGenerated]
		protected override bool PrintMembers(StringBuilder builder)
		{
			return base.PrintMembers(builder);
		}

		// Token: 0x06000317 RID: 791 RVA: 0x0000B699 File Offset: 0x00009899
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleMissingNameIssue left, ModuleMissingNameIssue right)
		{
			return !(left == right);
		}

		// Token: 0x06000318 RID: 792 RVA: 0x0000B6A5 File Offset: 0x000098A5
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleMissingNameIssue left, ModuleMissingNameIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x06000319 RID: 793 RVA: 0x0000B6BB File Offset: 0x000098BB
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x0600031A RID: 794 RVA: 0x0000B6C3 File Offset: 0x000098C3
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleMissingNameIssue);
		}

		// Token: 0x0600031B RID: 795 RVA: 0x0000B6D1 File Offset: 0x000098D1
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleIssueV2 other)
		{
			return this.Equals(other);
		}

		// Token: 0x0600031C RID: 796 RVA: 0x0000B6DA File Offset: 0x000098DA
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleMissingNameIssue other)
		{
			return this == other || base.Equals(other);
		}

		// Token: 0x0600031E RID: 798 RVA: 0x0000B6F2 File Offset: 0x000098F2
		[CompilerGenerated]
		private ModuleMissingNameIssue(ModuleMissingNameIssue original)
			: base(original)
		{
		}

		// Token: 0x0600031F RID: 799 RVA: 0x0000B6FD File Offset: 0x000098FD
		[CompilerGenerated]
		public new void Deconstruct(out ModuleInfoExtended Module)
		{
			Module = base.Module;
		}
	}
}
