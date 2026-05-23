using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Bannerlord.ModuleManager;

namespace Bannerlord.BUTR.Shared.Helpers
{
	// Token: 0x0200000F RID: 15
	[NullableContext(1)]
	[Nullable(0)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal class ModuleInfoExtendedHelper : ModuleInfoExtended, IEquatable<ModuleInfoExtendedHelper>
	{
		// Token: 0x17000031 RID: 49
		// (get) Token: 0x0600009E RID: 158 RVA: 0x000040B1 File Offset: 0x000022B1
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleInfoExtendedHelper);
			}
		}

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x0600009F RID: 159 RVA: 0x000040BD File Offset: 0x000022BD
		public bool IsExternal { get; }

		// Token: 0x17000033 RID: 51
		// (get) Token: 0x060000A0 RID: 160 RVA: 0x000040C5 File Offset: 0x000022C5
		public string Path { get; }

		// Token: 0x060000A1 RID: 161 RVA: 0x000040CD File Offset: 0x000022CD
		public ModuleInfoExtendedHelper(ModuleInfoExtended module, bool isExternal, string path)
			: base(module)
		{
			this.IsExternal = isExternal;
			this.Path = path;
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x000040E8 File Offset: 0x000022E8
		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("ModuleInfoExtendedHelper");
			stringBuilder.Append(" { ");
			if (this.PrintMembers(stringBuilder))
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x00004134 File Offset: 0x00002334
		[CompilerGenerated]
		protected override bool PrintMembers(StringBuilder builder)
		{
			if (base.PrintMembers(builder))
			{
				builder.Append(", ");
			}
			builder.Append("IsExternal = ");
			builder.Append(this.IsExternal.ToString());
			builder.Append(", Path = ");
			builder.Append(this.Path);
			return true;
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x00004197 File Offset: 0x00002397
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleInfoExtendedHelper left, ModuleInfoExtendedHelper right)
		{
			return !(left == right);
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x000041A3 File Offset: 0x000023A3
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleInfoExtendedHelper left, ModuleInfoExtendedHelper right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x000041B9 File Offset: 0x000023B9
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return (base.GetHashCode() * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(this.<IsExternal>k__BackingField)) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.<Path>k__BackingField);
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x000041EF File Offset: 0x000023EF
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleInfoExtendedHelper);
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x000041FD File Offset: 0x000023FD
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleInfoExtended other)
		{
			return this.Equals(other);
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x00004208 File Offset: 0x00002408
		[NullableContext(2)]
		[CompilerGenerated]
		public virtual bool Equals(ModuleInfoExtendedHelper other)
		{
			return this == other || (base.Equals(other) && EqualityComparer<bool>.Default.Equals(this.<IsExternal>k__BackingField, other.<IsExternal>k__BackingField) && EqualityComparer<string>.Default.Equals(this.<Path>k__BackingField, other.<Path>k__BackingField));
		}

		// Token: 0x060000AB RID: 171 RVA: 0x0000425E File Offset: 0x0000245E
		[CompilerGenerated]
		protected ModuleInfoExtendedHelper(ModuleInfoExtendedHelper original)
			: base(original)
		{
			this.IsExternal = original.<IsExternal>k__BackingField;
			this.Path = original.<Path>k__BackingField;
		}
	}
}
