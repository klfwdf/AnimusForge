using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager
{
	// Token: 0x02000064 RID: 100
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModuleSorterOptions : IEquatable<ModuleSorterOptions>
	{
		// Token: 0x17000110 RID: 272
		// (get) Token: 0x060003AA RID: 938 RVA: 0x0000D5F2 File Offset: 0x0000B7F2
		[CompilerGenerated]
		private Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleSorterOptions);
			}
		}

		// Token: 0x17000111 RID: 273
		// (get) Token: 0x060003AB RID: 939 RVA: 0x0000D5FE File Offset: 0x0000B7FE
		// (set) Token: 0x060003AC RID: 940 RVA: 0x0000D606 File Offset: 0x0000B806
		public bool SkipOptionals { get; set; }

		// Token: 0x17000112 RID: 274
		// (get) Token: 0x060003AD RID: 941 RVA: 0x0000D60F File Offset: 0x0000B80F
		// (set) Token: 0x060003AE RID: 942 RVA: 0x0000D617 File Offset: 0x0000B817
		public bool SkipExternalDependencies { get; set; }

		// Token: 0x060003AF RID: 943 RVA: 0x0000D620 File Offset: 0x0000B820
		public ModuleSorterOptions()
		{
		}

		// Token: 0x060003B0 RID: 944 RVA: 0x0000D628 File Offset: 0x0000B828
		public ModuleSorterOptions(bool skipOptionals, bool skipExternalDependencies)
		{
			this.SkipOptionals = skipOptionals;
			this.SkipExternalDependencies = skipExternalDependencies;
		}

		// Token: 0x060003B1 RID: 945 RVA: 0x0000D640 File Offset: 0x0000B840
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

		// Token: 0x060003B2 RID: 946 RVA: 0x0000D68C File Offset: 0x0000B88C
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

		// Token: 0x060003B3 RID: 947 RVA: 0x0000D6ED File Offset: 0x0000B8ED
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleSorterOptions left, ModuleSorterOptions right)
		{
			return !(left == right);
		}

		// Token: 0x060003B4 RID: 948 RVA: 0x0000D6F9 File Offset: 0x0000B8F9
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleSorterOptions left, ModuleSorterOptions right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x060003B5 RID: 949 RVA: 0x0000D70D File Offset: 0x0000B90D
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return (EqualityComparer<Type>.Default.GetHashCode(this.EqualityContract) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(this.<SkipOptionals>k__BackingField)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(this.<SkipExternalDependencies>k__BackingField);
		}

		// Token: 0x060003B6 RID: 950 RVA: 0x0000D74D File Offset: 0x0000B94D
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleSorterOptions);
		}

		// Token: 0x060003B7 RID: 951 RVA: 0x0000D75C File Offset: 0x0000B95C
		[NullableContext(2)]
		[CompilerGenerated]
		public bool Equals(ModuleSorterOptions other)
		{
			return this == other || (other != null && this.EqualityContract == other.EqualityContract && EqualityComparer<bool>.Default.Equals(this.<SkipOptionals>k__BackingField, other.<SkipOptionals>k__BackingField) && EqualityComparer<bool>.Default.Equals(this.<SkipExternalDependencies>k__BackingField, other.<SkipExternalDependencies>k__BackingField));
		}

		// Token: 0x060003B9 RID: 953 RVA: 0x0000D7BD File Offset: 0x0000B9BD
		[CompilerGenerated]
		private ModuleSorterOptions(ModuleSorterOptions original)
		{
			this.SkipOptionals = original.<SkipOptionals>k__BackingField;
			this.SkipExternalDependencies = original.<SkipExternalDependencies>k__BackingField;
		}
	}
}
