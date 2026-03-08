using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager
{
	// Token: 0x02000022 RID: 34
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleSorterOptions : IEquatable<ModuleSorterOptions>
	{
		// Token: 0x17000065 RID: 101
		// (get) Token: 0x060001A2 RID: 418 RVA: 0x0000888E File Offset: 0x00006A8E
		[CompilerGenerated]
		private Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleSorterOptions);
			}
		}

		// Token: 0x17000066 RID: 102
		// (get) Token: 0x060001A3 RID: 419 RVA: 0x0000889A File Offset: 0x00006A9A
		// (set) Token: 0x060001A4 RID: 420 RVA: 0x000088A2 File Offset: 0x00006AA2
		public bool SkipOptionals { get; set; }

		// Token: 0x17000067 RID: 103
		// (get) Token: 0x060001A5 RID: 421 RVA: 0x000088AB File Offset: 0x00006AAB
		// (set) Token: 0x060001A6 RID: 422 RVA: 0x000088B3 File Offset: 0x00006AB3
		public bool SkipExternalDependencies { get; set; }

		// Token: 0x060001A7 RID: 423 RVA: 0x000088BC File Offset: 0x00006ABC
		public ModuleSorterOptions()
		{
		}

		// Token: 0x060001A8 RID: 424 RVA: 0x000088C6 File Offset: 0x00006AC6
		public ModuleSorterOptions(bool skipOptionals, bool skipExternalDependencies)
		{
			this.SkipOptionals = skipOptionals;
			this.SkipExternalDependencies = skipExternalDependencies;
		}

		// Token: 0x060001A9 RID: 425 RVA: 0x000088E0 File Offset: 0x00006AE0
		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("ModuleSorterOptions");
			stringBuilder.Append(" { ");
			if (this.PrintMembers(stringBuilder))
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}

		// Token: 0x060001AA RID: 426 RVA: 0x0000892C File Offset: 0x00006B2C
		[CompilerGenerated]
		private bool PrintMembers(StringBuilder builder)
		{
			RuntimeHelpers.EnsureSufficientExecutionStack();
			builder.Append("SkipOptionals = ");
			builder.Append(this.SkipOptionals.ToString());
			builder.Append(", SkipExternalDependencies = ");
			builder.Append(this.SkipExternalDependencies.ToString());
			return true;
		}

		// Token: 0x060001AB RID: 427 RVA: 0x0000898D File Offset: 0x00006B8D
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleSorterOptions left, ModuleSorterOptions right)
		{
			return !(left == right);
		}

		// Token: 0x060001AC RID: 428 RVA: 0x00008999 File Offset: 0x00006B99
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleSorterOptions left, ModuleSorterOptions right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x060001AD RID: 429 RVA: 0x000089AF File Offset: 0x00006BAF
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return (EqualityComparer<Type>.Default.GetHashCode(this.EqualityContract) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(this.<SkipOptionals>k__BackingField)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(this.<SkipExternalDependencies>k__BackingField);
		}

		// Token: 0x060001AE RID: 430 RVA: 0x000089EF File Offset: 0x00006BEF
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleSorterOptions);
		}

		// Token: 0x060001AF RID: 431 RVA: 0x00008A00 File Offset: 0x00006C00
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleSorterOptions other)
		{
			return this == other || (other != null && this.EqualityContract == other.EqualityContract && EqualityComparer<bool>.Default.Equals(this.<SkipOptionals>k__BackingField, other.<SkipOptionals>k__BackingField) && EqualityComparer<bool>.Default.Equals(this.<SkipExternalDependencies>k__BackingField, other.<SkipExternalDependencies>k__BackingField));
		}

		// Token: 0x060001B1 RID: 433 RVA: 0x00008A63 File Offset: 0x00006C63
		[CompilerGenerated]
		private ModuleSorterOptions(ModuleSorterOptions original)
		{
			this.SkipOptionals = original.<SkipOptionals>k__BackingField;
			this.SkipExternalDependencies = original.<SkipExternalDependencies>k__BackingField;
		}
	}
}
