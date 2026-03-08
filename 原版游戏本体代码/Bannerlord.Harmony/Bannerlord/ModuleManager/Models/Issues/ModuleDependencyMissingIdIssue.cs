using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x0200003C RID: 60
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleDependencyMissingIdIssue : ModuleIssueV2, IEquatable<ModuleDependencyMissingIdIssue>
	{
		// Token: 0x0600032E RID: 814 RVA: 0x0000B7CF File Offset: 0x000099CF
		public ModuleDependencyMissingIdIssue(ModuleInfoExtended Module)
			: base(Module)
		{
		}

		// Token: 0x17000090 RID: 144
		// (get) Token: 0x0600032F RID: 815 RVA: 0x0000B7D9 File Offset: 0x000099D9
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleDependencyMissingIdIssue);
			}
		}

		// Token: 0x06000330 RID: 816 RVA: 0x0000B7E5 File Offset: 0x000099E5
		public override string ToString()
		{
			return "Module '" + base.Module.Id + "' has a dependency entry missing its Id field";
		}

		// Token: 0x06000331 RID: 817 RVA: 0x0000B801 File Offset: 0x00009A01
		public override ModuleIssue ToLegacy()
		{
			return new ModuleIssue(base.Module, "UNKNOWN", ModuleIssueType.DependencyMissingModuleId, this.ToString(), ApplicationVersionRange.Empty);
		}

		// Token: 0x06000332 RID: 818 RVA: 0x0000B820 File Offset: 0x00009A20
		[CompilerGenerated]
		protected override bool PrintMembers(StringBuilder builder)
		{
			return base.PrintMembers(builder);
		}

		// Token: 0x06000333 RID: 819 RVA: 0x0000B829 File Offset: 0x00009A29
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleDependencyMissingIdIssue left, ModuleDependencyMissingIdIssue right)
		{
			return !(left == right);
		}

		// Token: 0x06000334 RID: 820 RVA: 0x0000B835 File Offset: 0x00009A35
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleDependencyMissingIdIssue left, ModuleDependencyMissingIdIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x06000335 RID: 821 RVA: 0x0000B84B File Offset: 0x00009A4B
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x06000336 RID: 822 RVA: 0x0000B853 File Offset: 0x00009A53
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleDependencyMissingIdIssue);
		}

		// Token: 0x06000337 RID: 823 RVA: 0x0000B861 File Offset: 0x00009A61
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleIssueV2 other)
		{
			return this.Equals(other);
		}

		// Token: 0x06000338 RID: 824 RVA: 0x0000B86A File Offset: 0x00009A6A
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleDependencyMissingIdIssue other)
		{
			return this == other || base.Equals(other);
		}

		// Token: 0x0600033A RID: 826 RVA: 0x0000B882 File Offset: 0x00009A82
		[CompilerGenerated]
		private ModuleDependencyMissingIdIssue(ModuleDependencyMissingIdIssue original)
			: base(original)
		{
		}

		// Token: 0x0600033B RID: 827 RVA: 0x0000B88D File Offset: 0x00009A8D
		[CompilerGenerated]
		public new void Deconstruct(out ModuleInfoExtended Module)
		{
			Module = base.Module;
		}
	}
}
