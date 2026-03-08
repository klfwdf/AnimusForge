using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace MonoMod.Core
{
	// Token: 0x020004DF RID: 1247
	[NullableContext(1)]
	[Nullable(0)]
	[CLSCompliant(true)]
	internal readonly struct CreateDetourRequest : IEquatable<CreateDetourRequest>
	{
		// Token: 0x06001BA6 RID: 7078 RVA: 0x00058962 File Offset: 0x00056B62
		public CreateDetourRequest(MethodBase Source, MethodBase Target)
		{
			this.CreateSourceCloneIfNotILClone = false;
			this.Source = Source;
			this.Target = Target;
			this.ApplyByDefault = true;
		}

		// Token: 0x170005F8 RID: 1528
		// (get) Token: 0x06001BA7 RID: 7079 RVA: 0x00058980 File Offset: 0x00056B80
		// (set) Token: 0x06001BA8 RID: 7080 RVA: 0x00058988 File Offset: 0x00056B88
		public MethodBase Source { get; set; }

		// Token: 0x170005F9 RID: 1529
		// (get) Token: 0x06001BA9 RID: 7081 RVA: 0x00058991 File Offset: 0x00056B91
		// (set) Token: 0x06001BAA RID: 7082 RVA: 0x00058999 File Offset: 0x00056B99
		public MethodBase Target { get; set; }

		// Token: 0x170005FA RID: 1530
		// (get) Token: 0x06001BAB RID: 7083 RVA: 0x000589A2 File Offset: 0x00056BA2
		// (set) Token: 0x06001BAC RID: 7084 RVA: 0x000589AA File Offset: 0x00056BAA
		public bool ApplyByDefault { get; set; }

		// Token: 0x170005FB RID: 1531
		// (get) Token: 0x06001BAD RID: 7085 RVA: 0x000589B3 File Offset: 0x00056BB3
		// (set) Token: 0x06001BAE RID: 7086 RVA: 0x000589BB File Offset: 0x00056BBB
		public bool CreateSourceCloneIfNotILClone { get; set; }

		// Token: 0x06001BAF RID: 7087 RVA: 0x000589C4 File Offset: 0x00056BC4
		[NullableContext(0)]
		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("CreateDetourRequest");
			stringBuilder.Append(" { ");
			if (this.PrintMembers(stringBuilder))
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}

		// Token: 0x06001BB0 RID: 7088 RVA: 0x00058A10 File Offset: 0x00056C10
		[NullableContext(0)]
		[CompilerGenerated]
		private bool PrintMembers(StringBuilder builder)
		{
			builder.Append("Source = ");
			builder.Append(this.Source);
			builder.Append(", Target = ");
			builder.Append(this.Target);
			builder.Append(", ApplyByDefault = ");
			builder.Append(this.ApplyByDefault.ToString());
			builder.Append(", CreateSourceCloneIfNotILClone = ");
			builder.Append(this.CreateSourceCloneIfNotILClone.ToString());
			return true;
		}

		// Token: 0x06001BB1 RID: 7089 RVA: 0x00058A9E File Offset: 0x00056C9E
		[CompilerGenerated]
		public static bool operator !=(CreateDetourRequest left, CreateDetourRequest right)
		{
			return !(left == right);
		}

		// Token: 0x06001BB2 RID: 7090 RVA: 0x00058AAA File Offset: 0x00056CAA
		[CompilerGenerated]
		public static bool operator ==(CreateDetourRequest left, CreateDetourRequest right)
		{
			return left.Equals(right);
		}

		// Token: 0x06001BB3 RID: 7091 RVA: 0x00058AB4 File Offset: 0x00056CB4
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return ((EqualityComparer<MethodBase>.Default.GetHashCode(this.<Source>k__BackingField) * -1521134295 + EqualityComparer<MethodBase>.Default.GetHashCode(this.<Target>k__BackingField)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(this.<ApplyByDefault>k__BackingField)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(this.<CreateSourceCloneIfNotILClone>k__BackingField);
		}

		// Token: 0x06001BB4 RID: 7092 RVA: 0x00058B16 File Offset: 0x00056D16
		[NullableContext(0)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return obj is CreateDetourRequest && this.Equals((CreateDetourRequest)obj);
		}

		// Token: 0x06001BB5 RID: 7093 RVA: 0x00058B30 File Offset: 0x00056D30
		[CompilerGenerated]
		public bool Equals(CreateDetourRequest other)
		{
			return EqualityComparer<MethodBase>.Default.Equals(this.<Source>k__BackingField, other.<Source>k__BackingField) && EqualityComparer<MethodBase>.Default.Equals(this.<Target>k__BackingField, other.<Target>k__BackingField) && EqualityComparer<bool>.Default.Equals(this.<ApplyByDefault>k__BackingField, other.<ApplyByDefault>k__BackingField) && EqualityComparer<bool>.Default.Equals(this.<CreateSourceCloneIfNotILClone>k__BackingField, other.<CreateSourceCloneIfNotILClone>k__BackingField);
		}

		// Token: 0x06001BB6 RID: 7094 RVA: 0x00058B9D File Offset: 0x00056D9D
		[CompilerGenerated]
		public void Deconstruct(out MethodBase Source, out MethodBase Target)
		{
			Source = this.Source;
			Target = this.Target;
		}
	}
}
