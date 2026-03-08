using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x0200002C RID: 44
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleDependencyValidationIssue : ModuleIssueV2, IEquatable<ModuleDependencyValidationIssue>
	{
		// Token: 0x0600023D RID: 573 RVA: 0x0000A1F2 File Offset: 0x000083F2
		public ModuleDependencyValidationIssue(ModuleInfoExtended Module, string DependencyId)
		{
			this.DependencyId = DependencyId;
			base..ctor(Module);
		}

		// Token: 0x17000076 RID: 118
		// (get) Token: 0x0600023E RID: 574 RVA: 0x0000A203 File Offset: 0x00008403
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleDependencyValidationIssue);
			}
		}

		// Token: 0x17000077 RID: 119
		// (get) Token: 0x0600023F RID: 575 RVA: 0x0000A20F File Offset: 0x0000840F
		// (set) Token: 0x06000240 RID: 576 RVA: 0x0000A217 File Offset: 0x00008417
		public string DependencyId { get; set; }

		// Token: 0x06000241 RID: 577 RVA: 0x0000A220 File Offset: 0x00008420
		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(57, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Module '");
			defaultInterpolatedStringHandler.AppendFormatted(base.Module.Id);
			defaultInterpolatedStringHandler.AppendLiteral("': Dependency '");
			defaultInterpolatedStringHandler.AppendFormatted(this.DependencyId);
			defaultInterpolatedStringHandler.AppendLiteral("' has unresolved validation issues");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x06000242 RID: 578 RVA: 0x0000A286 File Offset: 0x00008486
		public override ModuleIssue ToLegacy()
		{
			return new ModuleIssue(base.Module, this.DependencyId, ModuleIssueType.DependencyValidationError, this.ToString(), ApplicationVersionRange.Empty);
		}

		// Token: 0x06000243 RID: 579 RVA: 0x0000A2A5 File Offset: 0x000084A5
		[CompilerGenerated]
		protected override bool PrintMembers(StringBuilder builder)
		{
			if (base.PrintMembers(builder))
			{
				builder.Append(", ");
			}
			builder.Append("DependencyId = ");
			builder.Append(this.DependencyId);
			return true;
		}

		// Token: 0x06000244 RID: 580 RVA: 0x0000A2D6 File Offset: 0x000084D6
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleDependencyValidationIssue left, ModuleDependencyValidationIssue right)
		{
			return !(left == right);
		}

		// Token: 0x06000245 RID: 581 RVA: 0x0000A2E2 File Offset: 0x000084E2
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleDependencyValidationIssue left, ModuleDependencyValidationIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x06000246 RID: 582 RVA: 0x0000A2F8 File Offset: 0x000084F8
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return base.GetHashCode() * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.<DependencyId>k__BackingField);
		}

		// Token: 0x06000247 RID: 583 RVA: 0x0000A317 File Offset: 0x00008517
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleDependencyValidationIssue);
		}

		// Token: 0x06000248 RID: 584 RVA: 0x0000A325 File Offset: 0x00008525
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleIssueV2 other)
		{
			return this.Equals(other);
		}

		// Token: 0x06000249 RID: 585 RVA: 0x0000A32E File Offset: 0x0000852E
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleDependencyValidationIssue other)
		{
			return this == other || (base.Equals(other) && EqualityComparer<string>.Default.Equals(this.<DependencyId>k__BackingField, other.<DependencyId>k__BackingField));
		}

		// Token: 0x0600024B RID: 587 RVA: 0x0000A361 File Offset: 0x00008561
		[CompilerGenerated]
		private ModuleDependencyValidationIssue(ModuleDependencyValidationIssue original)
			: base(original)
		{
			this.DependencyId = original.<DependencyId>k__BackingField;
		}

		// Token: 0x0600024C RID: 588 RVA: 0x0000A378 File Offset: 0x00008578
		[CompilerGenerated]
		public void Deconstruct(out ModuleInfoExtended Module, out string DependencyId)
		{
			Module = base.Module;
			DependencyId = this.DependencyId;
		}
	}
}
