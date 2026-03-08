using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x0200003B RID: 59
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleDependencyNullIssue : ModuleIssueV2, IEquatable<ModuleDependencyNullIssue>
	{
		// Token: 0x06000320 RID: 800 RVA: 0x0000B707 File Offset: 0x00009907
		public ModuleDependencyNullIssue(ModuleInfoExtended Module)
			: base(Module)
		{
		}

		// Token: 0x1700008F RID: 143
		// (get) Token: 0x06000321 RID: 801 RVA: 0x0000B711 File Offset: 0x00009911
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleDependencyNullIssue);
			}
		}

		// Token: 0x06000322 RID: 802 RVA: 0x0000B71D File Offset: 0x0000991D
		public override string ToString()
		{
			return "Module '" + base.Module.Id + "' has a null dependency entry";
		}

		// Token: 0x06000323 RID: 803 RVA: 0x0000B739 File Offset: 0x00009939
		public override ModuleIssue ToLegacy()
		{
			return new ModuleIssue(base.Module, "UNKNOWN", ModuleIssueType.DependencyIsNull, this.ToString(), ApplicationVersionRange.Empty);
		}

		// Token: 0x06000324 RID: 804 RVA: 0x0000B758 File Offset: 0x00009958
		[CompilerGenerated]
		protected override bool PrintMembers(StringBuilder builder)
		{
			return base.PrintMembers(builder);
		}

		// Token: 0x06000325 RID: 805 RVA: 0x0000B761 File Offset: 0x00009961
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleDependencyNullIssue left, ModuleDependencyNullIssue right)
		{
			return !(left == right);
		}

		// Token: 0x06000326 RID: 806 RVA: 0x0000B76D File Offset: 0x0000996D
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleDependencyNullIssue left, ModuleDependencyNullIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x06000327 RID: 807 RVA: 0x0000B783 File Offset: 0x00009983
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x06000328 RID: 808 RVA: 0x0000B78B File Offset: 0x0000998B
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleDependencyNullIssue);
		}

		// Token: 0x06000329 RID: 809 RVA: 0x0000B799 File Offset: 0x00009999
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleIssueV2 other)
		{
			return this.Equals(other);
		}

		// Token: 0x0600032A RID: 810 RVA: 0x0000B7A2 File Offset: 0x000099A2
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleDependencyNullIssue other)
		{
			return this == other || base.Equals(other);
		}

		// Token: 0x0600032C RID: 812 RVA: 0x0000B7BA File Offset: 0x000099BA
		[CompilerGenerated]
		private ModuleDependencyNullIssue(ModuleDependencyNullIssue original)
			: base(original)
		{
		}

		// Token: 0x0600032D RID: 813 RVA: 0x0000B7C5 File Offset: 0x000099C5
		[CompilerGenerated]
		public new void Deconstruct(out ModuleInfoExtended Module)
		{
			Module = base.Module;
		}
	}
}
