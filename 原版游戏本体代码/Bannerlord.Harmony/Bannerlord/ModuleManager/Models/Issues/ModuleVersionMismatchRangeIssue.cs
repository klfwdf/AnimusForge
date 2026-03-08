using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x0200002F RID: 47
	[NullableContext(1)]
	[Nullable(0)]
	internal abstract class ModuleVersionMismatchRangeIssue : ModuleVersionMismatchIssue, IEquatable<ModuleVersionMismatchRangeIssue>
	{
		// Token: 0x0600026B RID: 619 RVA: 0x0000A641 File Offset: 0x00008841
		protected ModuleVersionMismatchRangeIssue(ModuleInfoExtended Module, ModuleInfoExtended Dependency, ApplicationVersionRange VersionRange)
		{
			this.VersionRange = VersionRange;
			base..ctor(Module, Dependency);
		}

		// Token: 0x1700007C RID: 124
		// (get) Token: 0x0600026C RID: 620 RVA: 0x0000A653 File Offset: 0x00008853
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleVersionMismatchRangeIssue);
			}
		}

		// Token: 0x1700007D RID: 125
		// (get) Token: 0x0600026D RID: 621 RVA: 0x0000A65F File Offset: 0x0000885F
		// (set) Token: 0x0600026E RID: 622 RVA: 0x0000A667 File Offset: 0x00008867
		public ApplicationVersionRange VersionRange { get; set; }

		// Token: 0x0600026F RID: 623 RVA: 0x0000A670 File Offset: 0x00008870
		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("ModuleVersionMismatchRangeIssue");
			stringBuilder.Append(" { ");
			if (this.PrintMembers(stringBuilder))
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}

		// Token: 0x06000270 RID: 624 RVA: 0x0000A6BC File Offset: 0x000088BC
		[CompilerGenerated]
		protected override bool PrintMembers(StringBuilder builder)
		{
			if (base.PrintMembers(builder))
			{
				builder.Append(", ");
			}
			builder.Append("VersionRange = ");
			builder.Append(this.VersionRange);
			return true;
		}

		// Token: 0x06000271 RID: 625 RVA: 0x0000A6ED File Offset: 0x000088ED
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleVersionMismatchRangeIssue left, ModuleVersionMismatchRangeIssue right)
		{
			return !(left == right);
		}

		// Token: 0x06000272 RID: 626 RVA: 0x0000A6F9 File Offset: 0x000088F9
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleVersionMismatchRangeIssue left, ModuleVersionMismatchRangeIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x06000273 RID: 627 RVA: 0x0000A70F File Offset: 0x0000890F
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return base.GetHashCode() * -1521134295 + EqualityComparer<ApplicationVersionRange>.Default.GetHashCode(this.<VersionRange>k__BackingField);
		}

		// Token: 0x06000274 RID: 628 RVA: 0x0000A72E File Offset: 0x0000892E
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleVersionMismatchRangeIssue);
		}

		// Token: 0x06000275 RID: 629 RVA: 0x0000A73C File Offset: 0x0000893C
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleVersionMismatchIssue other)
		{
			return this.Equals(other);
		}

		// Token: 0x06000276 RID: 630 RVA: 0x0000A745 File Offset: 0x00008945
		[NullableContext(2)]
		[CompilerGenerated]
		public virtual bool Equals(ModuleVersionMismatchRangeIssue other)
		{
			return this == other || (base.Equals(other) && EqualityComparer<ApplicationVersionRange>.Default.Equals(this.<VersionRange>k__BackingField, other.<VersionRange>k__BackingField));
		}

		// Token: 0x06000278 RID: 632 RVA: 0x0000A770 File Offset: 0x00008970
		[CompilerGenerated]
		protected ModuleVersionMismatchRangeIssue(ModuleVersionMismatchRangeIssue original)
			: base(original)
		{
			this.VersionRange = original.<VersionRange>k__BackingField;
		}

		// Token: 0x06000279 RID: 633 RVA: 0x0000A787 File Offset: 0x00008987
		[CompilerGenerated]
		public void Deconstruct(out ModuleInfoExtended Module, out ModuleInfoExtended Dependency, out ApplicationVersionRange VersionRange)
		{
			Module = base.Module;
			Dependency = base.Dependency;
			VersionRange = this.VersionRange;
		}
	}
}
