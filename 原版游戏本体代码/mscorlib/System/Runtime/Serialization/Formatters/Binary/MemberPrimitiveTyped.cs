using System;
using System.Diagnostics;
using System.Security;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x0200078B RID: 1931
	internal sealed class MemberPrimitiveTyped : IStreamable
	{
		// Token: 0x060053FD RID: 21501 RVA: 0x00127E2E File Offset: 0x0012602E
		internal MemberPrimitiveTyped()
		{
		}

		// Token: 0x060053FE RID: 21502 RVA: 0x00127E36 File Offset: 0x00126036
		internal void Set(InternalPrimitiveTypeE primitiveTypeEnum, object value)
		{
			this.primitiveTypeEnum = primitiveTypeEnum;
			this.value = value;
		}

		// Token: 0x060053FF RID: 21503 RVA: 0x00127E46 File Offset: 0x00126046
		public void Write(__BinaryWriter sout)
		{
			sout.WriteByte(8);
			sout.WriteByte((byte)this.primitiveTypeEnum);
			sout.WriteValue(this.primitiveTypeEnum, this.value);
		}

		// Token: 0x06005400 RID: 21504 RVA: 0x00127E6E File Offset: 0x0012606E
		[SecurityCritical]
		public void Read(__BinaryParser input)
		{
			this.primitiveTypeEnum = (InternalPrimitiveTypeE)input.ReadByte();
			this.value = input.ReadValue(this.primitiveTypeEnum);
		}

		// Token: 0x06005401 RID: 21505 RVA: 0x00127E8E File Offset: 0x0012608E
		public void Dump()
		{
		}

		// Token: 0x06005402 RID: 21506 RVA: 0x00127E90 File Offset: 0x00126090
		[Conditional("_LOGGING")]
		private void DumpInternal()
		{
			BCLDebug.CheckEnabled("BINARY");
		}

		// Token: 0x040025E6 RID: 9702
		internal InternalPrimitiveTypeE primitiveTypeEnum;

		// Token: 0x040025E7 RID: 9703
		internal object value;
	}
}
