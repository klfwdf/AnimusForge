using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager.Models.Issues
{
	// Token: 0x02000030 RID: 48
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleVersionTooLowIssue : ModuleVersionMismatchSpecificIssue, IEquatable<ModuleVersionTooLowIssue>
	{
		// Token: 0x0600027A RID: 634 RVA: 0x0000A7A1 File Offset: 0x000089A1
		public ModuleVersionTooLowIssue(ModuleInfoExtended Module, ModuleInfoExtended Dependency, ApplicationVersion Version)
			: base(Module, Dependency, Version)
		{
		}

		// Token: 0x1700007E RID: 126
		// (get) Token: 0x0600027B RID: 635 RVA: 0x0000A7AD File Offset: 0x000089AD
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleVersionTooLowIssue);
			}
		}

		// Token: 0x0600027C RID: 636 RVA: 0x0000A7BC File Offset: 0x000089BC
		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(74, 4);
			defaultInterpolatedStringHandler.AppendLiteral("The module '");
			defaultInterpolatedStringHandler.AppendFormatted(base.Module.Id);
			defaultInterpolatedStringHandler.AppendLiteral("' requires version ");
			defaultInterpolatedStringHandler.AppendFormatted<ApplicationVersion>(base.Version);
			defaultInterpolatedStringHandler.AppendLiteral(" or higher of '");
			defaultInterpolatedStringHandler.AppendFormatted(base.Dependency.Id);
			defaultInterpolatedStringHandler.AppendLiteral("', but version ");
			defaultInterpolatedStringHandler.AppendFormatted<ApplicationVersion>(base.Dependency.Version);
			defaultInterpolatedStringHandler.AppendLiteral(" is installed");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x0600027D RID: 637 RVA: 0x0000A862 File Offset: 0x00008A62
		public override ModuleIssue ToLegacy()
		{
			return new ModuleIssue(base.Module, base.Dependency.Id, ModuleIssueType.VersionMismatchLessThanOrEqual, this.ToString(), new ApplicationVersionRange(base.Version, base.Version));
		}

		// Token: 0x0600027E RID: 638 RVA: 0x0000A892 File Offset: 0x00008A92
		[CompilerGenerated]
		protected override bool PrintMembers(StringBuilder builder)
		{
			return base.PrintMembers(builder);
		}

		// Token: 0x0600027F RID: 639 RVA: 0x0000A89B File Offset: 0x00008A9B
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleVersionTooLowIssue left, ModuleVersionTooLowIssue right)
		{
			return !(left == right);
		}

		// Token: 0x06000280 RID: 640 RVA: 0x0000A8A7 File Offset: 0x00008AA7
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleVersionTooLowIssue left, ModuleVersionTooLowIssue right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x06000281 RID: 641 RVA: 0x0000A8BD File Offset: 0x00008ABD
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x06000282 RID: 642 RVA: 0x0000A8C5 File Offset: 0x00008AC5
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleVersionTooLowIssue);
		}

		// Token: 0x06000283 RID: 643 RVA: 0x0000A8D3 File Offset: 0x00008AD3
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleVersionMismatchSpecificIssue other)
		{
			return this.Equals(other);
		}

		// Token: 0x06000284 RID: 644 RVA: 0x0000A8DC File Offset: 0x00008ADC
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleVersionTooLowIssue other)
		{
			return this == other || base.Equals(other);
		}

		// Token: 0x06000286 RID: 646 RVA: 0x0000A8F4 File Offset: 0x00008AF4
		[CompilerGenerated]
		private ModuleVersionTooLowIssue(ModuleVersionTooLowIssue original)
			: base(original)
		{
		}

		// Token: 0x06000287 RID: 647 RVA: 0x0000A8FF File Offset: 0x00008AFF
		[CompilerGenerated]
		public new void Deconstruct(out ModuleInfoExtended Module, out ModuleInfoExtended Dependency, out ApplicationVersion Version)
		{
			Module = base.Module;
			Dependency = base.Dependency;
			Version = base.Version;
		}
	}
}
