using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x02000032 RID: 50
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleVersionMismatchGreaterThanRangeIssue : ModuleVersionMismatchRangeIssue, IEquatable<ModuleVersionMismatchGreaterThanRangeIssue>
	{
		// Token: 0x06000296 RID: 662 RVA: 0x0000AA86 File Offset: 0x00008C86
		public ModuleVersionMismatchGreaterThanRangeIssue(ModuleInfoExtended Module, ModuleInfoExtended Dependency, ApplicationVersionRange VersionRange)
			: base(Module, Dependency, VersionRange)
		{
		}

		// Token: 0x17000080 RID: 128
		// (get) Token: 0x06000297 RID: 663 RVA: 0x0000AA92 File Offset: 0x00008C92
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleVersionMismatchGreaterThanRangeIssue);
			}
		}

		// Token: 0x06000298 RID: 664 RVA: 0x0000AAA0 File Offset: 0x00008CA0
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
			defaultInterpolatedStringHandler.AppendLiteral(" is installed (above maximum)");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x06000299 RID: 665 RVA: 0x0000AB46 File Offset: 0x00008D46
		public override ModuleIssue ToLegacy()
		{
			return new ModuleIssue(base.Module, base.Dependency.Id, ModuleIssueType.VersionMismatchGreaterThan, this.ToString(), base.VersionRange);
		}

		// Token: 0x0600029A RID: 666 RVA: 0x0000AB6B File Offset: 0x00008D6B
		[CompilerGenerated]
		protected override bool PrintMembers(StringBuilder builder)
		{
			return base.PrintMembers(builder);
		}

		// Token: 0x0600029B RID: 667 RVA: 0x0000AB74 File Offset: 0x00008D74
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleVersionMismatchGreaterThanRangeIssue left, ModuleVersionMismatchGreaterThanRangeIssue right)
		{
			return !(left == right);
		}

		// Token: 0x0600029C RID: 668 RVA: 0x0000AB80 File Offset: 0x00008D80
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleVersionMismatchGreaterThanRangeIssue left, ModuleVersionMismatchGreaterThanRangeIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x0600029D RID: 669 RVA: 0x0000AB96 File Offset: 0x00008D96
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x0600029E RID: 670 RVA: 0x0000AB9E File Offset: 0x00008D9E
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleVersionMismatchGreaterThanRangeIssue);
		}

		// Token: 0x0600029F RID: 671 RVA: 0x0000ABAC File Offset: 0x00008DAC
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleVersionMismatchRangeIssue other)
		{
			return this.Equals(other);
		}

		// Token: 0x060002A0 RID: 672 RVA: 0x0000ABB5 File Offset: 0x00008DB5
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleVersionMismatchGreaterThanRangeIssue other)
		{
			return this == other || base.Equals(other);
		}

		// Token: 0x060002A2 RID: 674 RVA: 0x0000ABCD File Offset: 0x00008DCD
		[CompilerGenerated]
		private ModuleVersionMismatchGreaterThanRangeIssue(ModuleVersionMismatchGreaterThanRangeIssue original)
			: base(original)
		{
		}

		// Token: 0x060002A3 RID: 675 RVA: 0x0000ABD8 File Offset: 0x00008DD8
		[CompilerGenerated]
		public new void Deconstruct(out ModuleInfoExtended Module, out ModuleInfoExtended Dependency, out ApplicationVersionRange VersionRange)
		{
			Module = base.Module;
			Dependency = base.Dependency;
			VersionRange = base.VersionRange;
		}
	}
}
