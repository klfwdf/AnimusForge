using System;
using System.Diagnostics;
using System.Security;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x02000790 RID: 1936
	internal sealed class MemberReference : IStreamable
	{
		// Token: 0x0600541B RID: 21531 RVA: 0x001286ED File Offset: 0x001268ED
		internal MemberReference()
		{
		}

		// Token: 0x0600541C RID: 21532 RVA: 0x001286F5 File Offset: 0x001268F5
		internal void Set(int idRef)
		{
			this.idRef = idRef;
		}

		// Token: 0x0600541D RID: 21533 RVA: 0x001286FE File Offset: 0x001268FE
		public void Write(__BinaryWriter sout)
		{
			sout.WriteByte(9);
			sout.WriteInt32(this.idRef);
		}

		// Token: 0x0600541E RID: 21534 RVA: 0x00128714 File Offset: 0x00126914
		[SecurityCritical]
		public void Read(__BinaryParser input)
		{
			this.idRef = input.ReadInt32();
		}

		// Token: 0x0600541F RID: 21535 RVA: 0x00128722 File Offset: 0x00126922
		public void Dump()
		{
		}

		// Token: 0x06005420 RID: 21536 RVA: 0x00128724 File Offset: 0x00126924
		[Conditional("_LOGGING")]
		private void DumpInternal()
		{
			BCLDebug.CheckEnabled("BINARY");
		}

		// Token: 0x04002602 RID: 9730
		internal int idRef;
	}
}
