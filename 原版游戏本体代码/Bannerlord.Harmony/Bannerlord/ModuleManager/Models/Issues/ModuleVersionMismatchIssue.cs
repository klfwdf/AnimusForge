using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x0200002D RID: 45
	[NullableContext(1)]
	[Nullable(0)]
	internal abstract class ModuleVersionMismatchIssue : ModuleIssueV2, IEquatable<ModuleVersionMismatchIssue>
	{
		// Token: 0x0600024D RID: 589 RVA: 0x0000A38A File Offset: 0x0000858A
		protected ModuleVersionMismatchIssue(ModuleInfoExtended Module, ModuleInfoExtended Dependency)
		{
			this.Dependency = Dependency;
			base..ctor(Module);
		}

		// Token: 0x17000078 RID: 120
		// (get) Token: 0x0600024E RID: 590 RVA: 0x0000A39B File Offset: 0x0000859B
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleVersionMismatchIssue);
			}
		}

		// Token: 0x17000079 RID: 121
		// (get) Token: 0x0600024F RID: 591 RVA: 0x0000A3A7 File Offset: 0x000085A7
		// (set) Token: 0x06000250 RID: 592 RVA: 0x0000A3AF File Offset: 0x000085AF
		public ModuleInfoExtended Dependency { get; set; }

		// Token: 0x06000251 RID: 593 RVA: 0x0000A3B8 File Offset: 0x000085B8
		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("ModuleVersionMismatchIssue");
			stringBuilder.Append(" { ");
			if (this.PrintMembers(stringBuilder))
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}

		// Token: 0x06000252 RID: 594 RVA: 0x0000A404 File Offset: 0x00008604
		[CompilerGenerated]
		protected override bool PrintMembers(StringBuilder builder)
		{
			if (base.PrintMembers(builder))
			{
				builder.Append(", ");
			}
			builder.Append("Dependency = ");
			builder.Append(this.Dependency);
			return true;
		}

		// Token: 0x06000253 RID: 595 RVA: 0x0000A435 File Offset: 0x00008635
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleVersionMismatchIssue left, ModuleVersionMismatchIssue right)
		{
			return !(left == right);
		}

		// Token: 0x06000254 RID: 596 RVA: 0x0000A441 File Offset: 0x00008641
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleVersionMismatchIssue left, ModuleVersionMismatchIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x06000255 RID: 597 RVA: 0x0000A457 File Offset: 0x00008657
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return base.GetHashCode() * -1521134295 + EqualityComparer<ModuleInfoExtended>.Default.GetHashCode(this.<Dependency>k__BackingField);
		}

		// Token: 0x06000256 RID: 598 RVA: 0x0000A476 File Offset: 0x00008676
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleVersionMismatchIssue);
		}

		// Token: 0x06000257 RID: 599 RVA: 0x0000A484 File Offset: 0x00008684
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleIssueV2 other)
		{
			return this.Equals(other);
		}

		// Token: 0x06000258 RID: 600 RVA: 0x0000A48D File Offset: 0x0000868D
		[NullableContext(2)]
		[CompilerGenerated]
		public virtual bool Equals(ModuleVersionMismatchIssue other)
		{
			return this == other || (base.Equals(other) && EqualityComparer<ModuleInfoExtended>.Default.Equals(this.<Dependency>k__BackingField, other.<Dependency>k__BackingField));
		}

		// Token: 0x0600025A RID: 602 RVA: 0x0000A4B8 File Offset: 0x000086B8
		[CompilerGenerated]
		protected ModuleVersionMismatchIssue(ModuleVersionMismatchIssue original)
			: base(original)
		{
			this.Dependency = original.<Dependency>k__BackingField;
		}

		// Token: 0x0600025B RID: 603 RVA: 0x0000A4CF File Offset: 0x000086CF
		[CompilerGenerated]
		public void Deconstruct(out ModuleInfoExtended Module, out ModuleInfoExtended Dependency)
		{
			Module = base.Module;
			Dependency = this.Dependency;
		}
	}
}
