using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace MonoMod.Core
{
	// Token: 0x020004E0 RID: 1248
	[CLSCompliant(true)]
	internal readonly struct CreateNativeDetourRequest : IEquatable<CreateNativeDetourRequest>
	{
		// Token: 0x06001BB7 RID: 7095 RVA: 0x00058BAF File Offset: 0x00056DAF
		public CreateNativeDetourRequest(IntPtr Source, IntPtr Target)
		{
			this.Source = Source;
			this.Target = Target;
			this.ApplyByDefault = true;
		}

		// Token: 0x170005FC RID: 1532
		// (get) Token: 0x06001BB8 RID: 7096 RVA: 0x00058BC6 File Offset: 0x00056DC6
		// (set) Token: 0x06001BB9 RID: 7097 RVA: 0x00058BCE File Offset: 0x00056DCE
		public IntPtr Source { get; set; }

		// Token: 0x170005FD RID: 1533
		// (get) Token: 0x06001BBA RID: 7098 RVA: 0x00058BD7 File Offset: 0x00056DD7
		// (set) Token: 0x06001BBB RID: 7099 RVA: 0x00058BDF File Offset: 0x00056DDF
		public IntPtr Target { get; set; }

		// Token: 0x170005FE RID: 1534
		// (get) Token: 0x06001BBC RID: 7100 RVA: 0x00058BE8 File Offset: 0x00056DE8
		// (set) Token: 0x06001BBD RID: 7101 RVA: 0x00058BF0 File Offset: 0x00056DF0
		public bool ApplyByDefault { get; set; }

		// Token: 0x06001BBE RID: 7102 RVA: 0x00058BFC File Offset: 0x00056DFC
		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("CreateNativeDetourRequest");
			stringBuilder.Append(" { ");
			if (this.PrintMembers(stringBuilder))
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}

		// Token: 0x06001BBF RID: 7103 RVA: 0x00058C48 File Offset: 0x00056E48
		[CompilerGenerated]
		private bool PrintMembers(StringBuilder builder)
		{
			builder.Append("Source = ");
			builder.Append(this.Source.ToString());
			builder.Append(", Target = ");
			builder.Append(this.Target.ToString());
			builder.Append(", ApplyByDefault = ");
			builder.Append(this.ApplyByDefault.ToString());
			return true;
		}

		// Token: 0x06001BC0 RID: 7104 RVA: 0x00058CCB File Offset: 0x00056ECB
		[CompilerGenerated]
		public static bool operator !=(CreateNativeDetourRequest left, CreateNativeDetourRequest right)
		{
			return !(left == right);
		}

		// Token: 0x06001BC1 RID: 7105 RVA: 0x00058CD7 File Offset: 0x00056ED7
		[CompilerGenerated]
		public static bool operator ==(CreateNativeDetourRequest left, CreateNativeDetourRequest right)
		{
			return left.Equals(right);
		}

		// Token: 0x06001BC2 RID: 7106 RVA: 0x00058CE1 File Offset: 0x00056EE1
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return (EqualityComparer<IntPtr>.Default.GetHashCode(this.<Source>k__BackingField) * -1521134295 + EqualityComparer<IntPtr>.Default.GetHashCode(this.<Target>k__BackingField)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(this.<ApplyByDefault>k__BackingField);
		}

		// Token: 0x06001BC3 RID: 7107 RVA: 0x00058D21 File Offset: 0x00056F21
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return obj is CreateNativeDetourRequest && this.Equals((CreateNativeDetourRequest)obj);
		}

		// Token: 0x06001BC4 RID: 7108 RVA: 0x00058D3C File Offset: 0x00056F3C
		[CompilerGenerated]
		public bool Equals(CreateNativeDetourRequest other)
		{
			return EqualityComparer<IntPtr>.Default.Equals(this.<Source>k__BackingField, other.<Source>k__BackingField) && EqualityComparer<IntPtr>.Default.Equals(this.<Target>k__BackingField, other.<Target>k__BackingField) && EqualityComparer<bool>.Default.Equals(this.<ApplyByDefault>k__BackingField, other.<ApplyByDefault>k__BackingField);
		}

		// Token: 0x06001BC5 RID: 7109 RVA: 0x00058D91 File Offset: 0x00056F91
		[CompilerGenerated]
		public void Deconstruct(out IntPtr Source, out IntPtr Target)
		{
			Source = this.Source;
			Target = this.Target;
		}
	}
}
