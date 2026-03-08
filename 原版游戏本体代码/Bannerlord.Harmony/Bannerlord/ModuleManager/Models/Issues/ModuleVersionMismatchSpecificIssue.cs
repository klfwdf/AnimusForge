using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x0200002E RID: 46
	[NullableContext(1)]
	[Nullable(0)]
	internal abstract class ModuleVersionMismatchSpecificIssue : ModuleVersionMismatchIssue, IEquatable<ModuleVersionMismatchSpecificIssue>
	{
		// Token: 0x0600025C RID: 604 RVA: 0x0000A4E1 File Offset: 0x000086E1
		protected ModuleVersionMismatchSpecificIssue(ModuleInfoExtended Module, ModuleInfoExtended Dependency, ApplicationVersion Version)
		{
			this.Version = Version;
			base..ctor(Module, Dependency);
		}

		// Token: 0x1700007A RID: 122
		// (get) Token: 0x0600025D RID: 605 RVA: 0x0000A4F3 File Offset: 0x000086F3
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleVersionMismatchSpecificIssue);
			}
		}

		// Token: 0x1700007B RID: 123
		// (get) Token: 0x0600025E RID: 606 RVA: 0x0000A4FF File Offset: 0x000086FF
		// (set) Token: 0x0600025F RID: 607 RVA: 0x0000A507 File Offset: 0x00008707
		public ApplicationVersion Version { get; set; }

		// Token: 0x06000260 RID: 608 RVA: 0x0000A510 File Offset: 0x00008710
		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("ModuleVersionMismatchSpecificIssue");
			stringBuilder.Append(" { ");
			if (this.PrintMembers(stringBuilder))
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}

		// Token: 0x06000261 RID: 609 RVA: 0x0000A55C File Offset: 0x0000875C
		[CompilerGenerated]
		protected override bool PrintMembers(StringBuilder builder)
		{
			if (base.PrintMembers(builder))
			{
				builder.Append(", ");
			}
			builder.Append("Version = ");
			builder.Append(this.Version);
			return true;
		}

		// Token: 0x06000262 RID: 610 RVA: 0x0000A58D File Offset: 0x0000878D
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleVersionMismatchSpecificIssue left, ModuleVersionMismatchSpecificIssue right)
		{
			return !(left == right);
		}

		// Token: 0x06000263 RID: 611 RVA: 0x0000A599 File Offset: 0x00008799
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleVersionMismatchSpecificIssue left, ModuleVersionMismatchSpecificIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x06000264 RID: 612 RVA: 0x0000A5AF File Offset: 0x000087AF
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return base.GetHashCode() * -1521134295 + EqualityComparer<ApplicationVersion>.Default.GetHashCode(this.<Version>k__BackingField);
		}

		// Token: 0x06000265 RID: 613 RVA: 0x0000A5CE File Offset: 0x000087CE
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleVersionMismatchSpecificIssue);
		}

		// Token: 0x06000266 RID: 614 RVA: 0x0000A5DC File Offset: 0x000087DC
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleVersionMismatchIssue other)
		{
			return this.Equals(other);
		}

		// Token: 0x06000267 RID: 615 RVA: 0x0000A5E5 File Offset: 0x000087E5
		[NullableContext(2)]
		[CompilerGenerated]
		public virtual bool Equals(ModuleVersionMismatchSpecificIssue other)
		{
			return this == other || (base.Equals(other) && EqualityComparer<ApplicationVersion>.Default.Equals(this.<Version>k__BackingField, other.<Version>k__BackingField));
		}

		// Token: 0x06000269 RID: 617 RVA: 0x0000A610 File Offset: 0x00008810
		[CompilerGenerated]
		protected ModuleVersionMismatchSpecificIssue(ModuleVersionMismatchSpecificIssue original)
			: base(original)
		{
			this.Version = original.<Version>k__BackingField;
		}

		// Token: 0x0600026A RID: 618 RVA: 0x0000A627 File Offset: 0x00008827
		[CompilerGenerated]
		public void Deconstruct(out ModuleInfoExtended Module, out ModuleInfoExtended Dependency, out ApplicationVersion Version)
		{
			Module = base.Module;
			Dependency = base.Dependency;
			Version = this.Version;
		}
	}
}
