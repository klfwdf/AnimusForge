using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x02000039 RID: 57
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleMissingIdIssue : ModuleIssueV2, IEquatable<ModuleMissingIdIssue>
	{
		// Token: 0x06000304 RID: 772 RVA: 0x0000B571 File Offset: 0x00009771
		public ModuleMissingIdIssue(ModuleInfoExtended Module)
			: base(Module)
		{
		}

		// Token: 0x1700008D RID: 141
		// (get) Token: 0x06000305 RID: 773 RVA: 0x0000B57B File Offset: 0x0000977B
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleMissingIdIssue);
			}
		}

		// Token: 0x06000306 RID: 774 RVA: 0x0000B587 File Offset: 0x00009787
		public override string ToString()
		{
			return "Module with Name '" + base.Module.Name + "' is missing its Id field";
		}

		// Token: 0x06000307 RID: 775 RVA: 0x0000B5A3 File Offset: 0x000097A3
		public override ModuleIssue ToLegacy()
		{
			return new ModuleIssue(base.Module, "UNKNOWN", ModuleIssueType.MissingModuleId, this.ToString(), ApplicationVersionRange.Empty);
		}

		// Token: 0x06000308 RID: 776 RVA: 0x0000B5C2 File Offset: 0x000097C2
		[CompilerGenerated]
		protected override bool PrintMembers(StringBuilder builder)
		{
			return base.PrintMembers(builder);
		}

		// Token: 0x06000309 RID: 777 RVA: 0x0000B5CB File Offset: 0x000097CB
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleMissingIdIssue left, ModuleMissingIdIssue right)
		{
			return !(left == right);
		}

		// Token: 0x0600030A RID: 778 RVA: 0x0000B5D7 File Offset: 0x000097D7
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleMissingIdIssue left, ModuleMissingIdIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x0600030B RID: 779 RVA: 0x0000B5ED File Offset: 0x000097ED
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x0600030C RID: 780 RVA: 0x0000B5F5 File Offset: 0x000097F5
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleMissingIdIssue);
		}

		// Token: 0x0600030D RID: 781 RVA: 0x0000B603 File Offset: 0x00009803
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleIssueV2 other)
		{
			return this.Equals(other);
		}

		// Token: 0x0600030E RID: 782 RVA: 0x0000B60C File Offset: 0x0000980C
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleMissingIdIssue other)
		{
			return this == other || base.Equals(other);
		}

		// Token: 0x06000310 RID: 784 RVA: 0x0000B624 File Offset: 0x00009824
		[CompilerGenerated]
		private ModuleMissingIdIssue(ModuleMissingIdIssue original)
			: base(original)
		{
		}

		// Token: 0x06000311 RID: 785 RVA: 0x0000B62F File Offset: 0x0000982F
		[CompilerGenerated]
		public new void Deconstruct(out ModuleInfoExtended Module)
		{
			Module = base.Module;
		}
	}
}
