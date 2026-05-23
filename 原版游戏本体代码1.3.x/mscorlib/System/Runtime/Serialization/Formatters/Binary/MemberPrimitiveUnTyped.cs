using System;
using System.Diagnostics;
using System.Security;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x0200078F RID: 1935
	internal sealed class MemberPrimitiveUnTyped : IStreamable
	{
		// Token: 0x06005414 RID: 21524 RVA: 0x0012867B File Offset: 0x0012687B
		internal MemberPrimitiveUnTyped()
		{
		}

		// Token: 0x06005415 RID: 21525 RVA: 0x00128683 File Offset: 0x00126883
		internal void Set(InternalPrimitiveTypeE typeInformation, object value)
		{
			this.typeInformation = typeInformation;
			this.value = value;
		}

		// Token: 0x06005416 RID: 21526 RVA: 0x00128693 File Offset: 0x00126893
		internal void Set(InternalPrimitiveTypeE typeInformation)
		{
			this.typeInformation = typeInformation;
		}

		// Token: 0x06005417 RID: 21527 RVA: 0x0012869C File Offset: 0x0012689C
		public void Write(__BinaryWriter sout)
		{
			sout.WriteValue(this.typeInformation, this.value);
		}

		// Token: 0x06005418 RID: 21528 RVA: 0x001286B0 File Offset: 0x001268B0
		[SecurityCritical]
		public void Read(__BinaryParser input)
		{
			this.value = input.ReadValue(this.typeInformation);
		}

		// Token: 0x06005419 RID: 21529 RVA: 0x001286C4 File Offset: 0x001268C4
		public void Dump()
		{
		}

		// Token: 0x0600541A RID: 21530 RVA: 0x001286C8 File Offset: 0x001268C8
		[Conditional("_LOGGING")]
		private void DumpInternal()
		{
			if (BCLDebug.CheckEnabled("BINARY"))
			{
				string text = Converter.ToComType(this.typeInformation);
			}
		}

		// Token: 0x04002600 RID: 9728
		internal InternalPrimitiveTypeE typeInformation;

		// Token: 0x04002601 RID: 9729
		internal object value;
	}
}
