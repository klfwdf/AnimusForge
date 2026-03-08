using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms
{
	// Token: 0x020004F4 RID: 1268
	internal readonly struct Abi : IEquatable<Abi>
	{
		// Token: 0x06001C40 RID: 7232 RVA: 0x0005AA8E File Offset: 0x00058C8E
		public Abi(ReadOnlyMemory<SpecialArgumentKind> ArgumentOrder, [Nullable(1)] Classifier Classifier, bool ReturnsReturnBuffer)
		{
			this.ArgumentOrder = ArgumentOrder;
			this.Classifier = Classifier;
			this.ReturnsReturnBuffer = ReturnsReturnBuffer;
		}

		// Token: 0x17000616 RID: 1558
		// (get) Token: 0x06001C41 RID: 7233 RVA: 0x0005AAA5 File Offset: 0x00058CA5
		// (set) Token: 0x06001C42 RID: 7234 RVA: 0x0005AAAD File Offset: 0x00058CAD
		public ReadOnlyMemory<SpecialArgumentKind> ArgumentOrder { get; set; }

		// Token: 0x17000617 RID: 1559
		// (get) Token: 0x06001C43 RID: 7235 RVA: 0x0005AAB6 File Offset: 0x00058CB6
		// (set) Token: 0x06001C44 RID: 7236 RVA: 0x0005AABE File Offset: 0x00058CBE
		[Nullable(1)]
		public Classifier Classifier
		{
			[NullableContext(1)]
			get;
			[NullableContext(1)]
			set;
		}

		// Token: 0x17000618 RID: 1560
		// (get) Token: 0x06001C45 RID: 7237 RVA: 0x0005AAC7 File Offset: 0x00058CC7
		// (set) Token: 0x06001C46 RID: 7238 RVA: 0x0005AACF File Offset: 0x00058CCF
		public bool ReturnsReturnBuffer { get; set; }

		// Token: 0x06001C47 RID: 7239 RVA: 0x0005AAD8 File Offset: 0x00058CD8
		[NullableContext(1)]
		public TypeClassification Classify(Type type, bool isReturn)
		{
			Helpers.ThrowIfArgumentNull<Type>(type, "type");
			if (type == typeof(void))
			{
				return TypeClassification.InRegister;
			}
			if (!type.IsValueType)
			{
				return TypeClassification.InRegister;
			}
			if (type.IsPointer)
			{
				return TypeClassification.InRegister;
			}
			if (type.IsByRef)
			{
				return TypeClassification.InRegister;
			}
			return this.Classifier(type, isReturn);
		}

		// Token: 0x06001C48 RID: 7240 RVA: 0x0005AB30 File Offset: 0x00058D30
		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("Abi");
			stringBuilder.Append(" { ");
			if (this.PrintMembers(stringBuilder))
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}

		// Token: 0x06001C49 RID: 7241 RVA: 0x0005AB7C File Offset: 0x00058D7C
		[CompilerGenerated]
		private bool PrintMembers(StringBuilder builder)
		{
			builder.Append("ArgumentOrder = ");
			builder.Append(this.ArgumentOrder.ToString());
			builder.Append(", Classifier = ");
			builder.Append(this.Classifier);
			builder.Append(", ReturnsReturnBuffer = ");
			builder.Append(this.ReturnsReturnBuffer.ToString());
			return true;
		}

		// Token: 0x06001C4A RID: 7242 RVA: 0x0005ABF1 File Offset: 0x00058DF1
		[CompilerGenerated]
		public static bool operator !=(Abi left, Abi right)
		{
			return !(left == right);
		}

		// Token: 0x06001C4B RID: 7243 RVA: 0x0005ABFD File Offset: 0x00058DFD
		[CompilerGenerated]
		public static bool operator ==(Abi left, Abi right)
		{
			return left.Equals(right);
		}

		// Token: 0x06001C4C RID: 7244 RVA: 0x0005AC07 File Offset: 0x00058E07
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return (EqualityComparer<ReadOnlyMemory<SpecialArgumentKind>>.Default.GetHashCode(this.<ArgumentOrder>k__BackingField) * -1521134295 + EqualityComparer<Classifier>.Default.GetHashCode(this.<Classifier>k__BackingField)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(this.<ReturnsReturnBuffer>k__BackingField);
		}

		// Token: 0x06001C4D RID: 7245 RVA: 0x0005AC47 File Offset: 0x00058E47
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return obj is Abi && this.Equals((Abi)obj);
		}

		// Token: 0x06001C4E RID: 7246 RVA: 0x0005AC60 File Offset: 0x00058E60
		[CompilerGenerated]
		public bool Equals(Abi other)
		{
			return EqualityComparer<ReadOnlyMemory<SpecialArgumentKind>>.Default.Equals(this.<ArgumentOrder>k__BackingField, other.<ArgumentOrder>k__BackingField) && EqualityComparer<Classifier>.Default.Equals(this.<Classifier>k__BackingField, other.<Classifier>k__BackingField) && EqualityComparer<bool>.Default.Equals(this.<ReturnsReturnBuffer>k__BackingField, other.<ReturnsReturnBuffer>k__BackingField);
		}

		// Token: 0x06001C4F RID: 7247 RVA: 0x0005ACB5 File Offset: 0x00058EB5
		[CompilerGenerated]
		public void Deconstruct(out ReadOnlyMemory<SpecialArgumentKind> ArgumentOrder, [Nullable(1)] out Classifier Classifier, out bool ReturnsReturnBuffer)
		{
			ArgumentOrder = this.ArgumentOrder;
			Classifier = this.Classifier;
			ReturnsReturnBuffer = this.ReturnsReturnBuffer;
		}
	}
}
