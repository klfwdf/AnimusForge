using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Bannerlord.ModuleManager;

namespace Bannerlord.BUTR.Shared.Helpers
{
	// Token: 0x02000051 RID: 81
	[NullableContext(1)]
	[Nullable(0)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal class ModuleInfoExtendedHelper : ModuleInfoExtended, IEquatable<ModuleInfoExtendedHelper>
	{
		// Token: 0x170000DC RID: 220
		// (get) Token: 0x060002A7 RID: 679 RVA: 0x000095C2 File Offset: 0x000077C2
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleInfoExtendedHelper);
			}
		}

		// Token: 0x170000DD RID: 221
		// (get) Token: 0x060002A8 RID: 680 RVA: 0x000095CE File Offset: 0x000077CE
		public bool IsExternal { get; }

		// Token: 0x170000DE RID: 222
		// (get) Token: 0x060002A9 RID: 681 RVA: 0x000095D6 File Offset: 0x000077D6
		public string Path { get; }

		// Token: 0x060002AA RID: 682 RVA: 0x000095DE File Offset: 0x000077DE
		public ModuleInfoExtendedHelper(ModuleInfoExtended module, bool isExternal, string path)
			: base(module)
		{
			this.IsExternal = isExternal;
			this.Path = path;
		}

		// Token: 0x060002AB RID: 683 RVA: 0x000095F8 File Offset: 0x000077F8
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

		// Token: 0x060002AC RID: 684 RVA: 0x00009644 File Offset: 0x00007844
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

		// Token: 0x060002AD RID: 685 RVA: 0x000096A7 File Offset: 0x000078A7
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleInfoExtendedHelper left, ModuleInfoExtendedHelper right)
		{
			return !(left == right);
		}

		// Token: 0x060002AE RID: 686 RVA: 0x000096B3 File Offset: 0x000078B3
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleInfoExtendedHelper left, ModuleInfoExtendedHelper right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x060002AF RID: 687 RVA: 0x000096C7 File Offset: 0x000078C7
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return (base.GetHashCode() * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(this.<IsExternal>k__BackingField)) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.<Path>k__BackingField);
		}

		// Token: 0x060002B0 RID: 688 RVA: 0x000096FD File Offset: 0x000078FD
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleInfoExtendedHelper);
		}

		// Token: 0x060002B1 RID: 689 RVA: 0x0000970B File Offset: 0x0000790B
		[NullableContext(2)]
		[CompilerGenerated]
		public sealed override bool Equals(ModuleInfoExtended other)
		{
			return this.Equals(other);
		}

		// Token: 0x060002B2 RID: 690 RVA: 0x00009714 File Offset: 0x00007914
		[NullableContext(2)]
		[CompilerGenerated]
		public virtual bool Equals(ModuleInfoExtendedHelper other)
		{
			return this == other || (base.Equals(other) && EqualityComparer<bool>.Default.Equals(this.<IsExternal>k__BackingField, other.<IsExternal>k__BackingField) && EqualityComparer<string>.Default.Equals(this.<Path>k__BackingField, other.<Path>k__BackingField));
		}

		// Token: 0x060002B4 RID: 692 RVA: 0x00009768 File Offset: 0x00007968
		[CompilerGenerated]
		protected ModuleInfoExtendedHelper(ModuleInfoExtendedHelper original)
			: base(original)
		{
			this.IsExternal = original.<IsExternal>k__BackingField;
			this.Path = original.<Path>k__BackingField;
		}
	}
}
