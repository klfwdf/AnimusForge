using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x02000031 RID: 49
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleVersionMismatchLessThanRangeIssue : ModuleVersionMismatchRangeIssue, IEquatable<ModuleVersionMismatchLessThanRangeIssue>
	{
		// Token: 0x06000288 RID: 648 RVA: 0x0000A919 File Offset: 0x00008B19
		public ModuleVersionMismatchLessThanRangeIssue(ModuleInfoExtended Module, ModuleInfoExtended Dependency, ApplicationVersionRange VersionRange)
			: base(Module, Dependency, VersionRange)
		{
		}

		// Token: 0x1700007F RID: 127
		// (get) Token: 0x06000289 RID: 649 RVA: 0x0000A925 File Offset: 0x00008B25
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleVersionMismatchLessThanRangeIssue);
			}
		}

		// Token: 0x0600028A RID: 650 RVA: 0x0000A934 File Offset: 0x00008B34
		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(77, 4);
			defaultInterpolatedStringHandler.AppendLiteral("The module '");
			defaultInterpolatedStringHandler.AppendFormatted(base.Module.Id);
			defaultInterpolatedStringHandler.AppendLiteral("' requires '");
			defaultInterpolatedStringHandler.AppendFormatted(base.Dependency.Id);
			defaultInterpolatedStringHandler.AppendLiteral("' version ");
			defaultInterpolatedStringHandler.AppendFormatted<ApplicationVersionRange>(base.VersionRange);
			defaultInterpolatedStringHandler.AppendLiteral(", but version ");
			defaultInterpolatedStringHandler.AppendFormatted<ApplicationVersion>(base.Dependency.Version);
			defaultInterpolatedStringHandler.AppendLiteral(" is installed (below minimum)");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x0600028B RID: 651 RVA: 0x0000A9DA File Offset: 0x00008BDA
		public override ModuleIssue ToLegacy()
		{
			return new ModuleIssue(base.Module, base.Dependency.Id, ModuleIssueType.VersionMismatchLessThan, this.ToString(), base.VersionRange);
		}

		// Token: 0x0600028C RID: 652 RVA: 0x0000A9FF File Offset: 0x00008BFF
		[CompilerGenerated]
		protected override bool PrintMembers(StringBuilder builder)
		{
			return base.PrintMembers(builder);
		}

		// Token: 0x0600028D RID: 653 RVA: 0x0000AA08 File Offset: 0x00008C08
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleVersionMismatchLessThanRangeIssue left, ModuleVersionMismatchLessThanRangeIssue right)
		{
			return !(left == right);
		}

		// Token: 0x0600028E RID: 654 RVA: 0x0000AA14 File Offset: 0x00008C14
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleVersionMismatchLessThanRangeIssue left, ModuleVersionMismatchLessThanRangeIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x0600028F RID: 655 RVA: 0x0000AA2A File Offset: 0x00008C2A
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x06000290 RID: 656 RVA: 0x0000AA32 File Offset: 0x00008C32
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleVersionMismatchLessThanRangeIssue);
		}

		// Token: 0x06000291 RID: 657 RVA: 0x0000AA40 File Offset: 0x00008C40
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleVersionMismatchRangeIssue other)
		{
			return this.Equals(other);
		}

		// Token: 0x06000292 RID: 658 RVA: 0x0000AA49 File Offset: 0x00008C49
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleVersionMismatchLessThanRangeIssue other)
		{
			return this == other || base.Equals(other);
		}

		// Token: 0x06000294 RID: 660 RVA: 0x0000AA61 File Offset: 0x00008C61
		[CompilerGenerated]
		private ModuleVersionMismatchLessThanRangeIssue(ModuleVersionMismatchLessThanRangeIssue original)
			: base(original)
		{
		}

		// Token: 0x06000295 RID: 661 RVA: 0x0000AA6C File Offset: 0x00008C6C
		[CompilerGenerated]
		public new void Deconstruct(out ModuleInfoExtended Module, out ModuleInfoExtended Dependency, out ApplicationVersionRange VersionRange)
		{
			Module = base.Module;
			Dependency = base.Dependency;
			VersionRange = base.VersionRange;
		}
	}
}
