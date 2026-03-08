using System;
using System.Diagnostics.CodeAnalysis;

namespace Iced.Intel
{
	// Token: 0x02000668 RID: 1640
	internal static class ThrowHelper
	{
		// Token: 0x06002397 RID: 9111 RVA: 0x00072D39 File Offset: 0x00070F39
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentException()
		{
			throw new ArgumentException();
		}

		// Token: 0x06002398 RID: 9112 RVA: 0x0001C627 File Offset: 0x0001A827
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowInvalidOperationException()
		{
			throw new InvalidOperationException();
		}

		// Token: 0x06002399 RID: 9113 RVA: 0x00072D40 File Offset: 0x00070F40
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentNullException_codeWriter()
		{
			throw new ArgumentNullException("codeWriter");
		}

		// Token: 0x0600239A RID: 9114 RVA: 0x00072D4C File Offset: 0x00070F4C
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentNullException_data()
		{
			throw new ArgumentNullException("data");
		}

		// Token: 0x0600239B RID: 9115 RVA: 0x00072D58 File Offset: 0x00070F58
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentNullException_writer()
		{
			throw new ArgumentNullException("writer");
		}

		// Token: 0x0600239C RID: 9116 RVA: 0x00072D64 File Offset: 0x00070F64
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentNullException_options()
		{
			throw new ArgumentNullException("options");
		}

		// Token: 0x0600239D RID: 9117 RVA: 0x00072D70 File Offset: 0x00070F70
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentNullException_value()
		{
			throw new ArgumentNullException("value");
		}

		// Token: 0x0600239E RID: 9118 RVA: 0x00072D7C File Offset: 0x00070F7C
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentNullException_list()
		{
			throw new ArgumentNullException("list");
		}

		// Token: 0x0600239F RID: 9119 RVA: 0x00072D88 File Offset: 0x00070F88
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentNullException_collection()
		{
			throw new ArgumentNullException("collection");
		}

		// Token: 0x060023A0 RID: 9120 RVA: 0x00072D94 File Offset: 0x00070F94
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentNullException_array()
		{
			throw new ArgumentNullException("array");
		}

		// Token: 0x060023A1 RID: 9121 RVA: 0x00072DA0 File Offset: 0x00070FA0
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentNullException_sb()
		{
			throw new ArgumentNullException("sb");
		}

		// Token: 0x060023A2 RID: 9122 RVA: 0x00072DAC File Offset: 0x00070FAC
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentNullException_output()
		{
			throw new ArgumentNullException("output");
		}

		// Token: 0x060023A3 RID: 9123 RVA: 0x00072DB8 File Offset: 0x00070FB8
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException_value()
		{
			throw new ArgumentOutOfRangeException("value");
		}

		// Token: 0x060023A4 RID: 9124 RVA: 0x00072DC4 File Offset: 0x00070FC4
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException_index()
		{
			throw new ArgumentOutOfRangeException("index");
		}

		// Token: 0x060023A5 RID: 9125 RVA: 0x00072DD0 File Offset: 0x00070FD0
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException_count()
		{
			throw new ArgumentOutOfRangeException("count");
		}

		// Token: 0x060023A6 RID: 9126 RVA: 0x00072DDC File Offset: 0x00070FDC
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException_length()
		{
			throw new ArgumentOutOfRangeException("length");
		}

		// Token: 0x060023A7 RID: 9127 RVA: 0x00072DE8 File Offset: 0x00070FE8
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException_operand()
		{
			throw new ArgumentOutOfRangeException("operand");
		}

		// Token: 0x060023A8 RID: 9128 RVA: 0x00072DF4 File Offset: 0x00070FF4
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException_instructionOperand()
		{
			throw new ArgumentOutOfRangeException("instructionOperand");
		}

		// Token: 0x060023A9 RID: 9129 RVA: 0x00072E00 File Offset: 0x00071000
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException_capacity()
		{
			throw new ArgumentOutOfRangeException("capacity");
		}

		// Token: 0x060023AA RID: 9130 RVA: 0x00072E0C File Offset: 0x0007100C
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException_memorySize()
		{
			throw new ArgumentOutOfRangeException("memorySize");
		}

		// Token: 0x060023AB RID: 9131 RVA: 0x00072E18 File Offset: 0x00071018
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException_size()
		{
			throw new ArgumentOutOfRangeException("size");
		}

		// Token: 0x060023AC RID: 9132 RVA: 0x00072E24 File Offset: 0x00071024
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException_elementSize()
		{
			throw new ArgumentOutOfRangeException("elementSize");
		}

		// Token: 0x060023AD RID: 9133 RVA: 0x00072E30 File Offset: 0x00071030
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException_register()
		{
			throw new ArgumentOutOfRangeException("register");
		}

		// Token: 0x060023AE RID: 9134 RVA: 0x00072E3C File Offset: 0x0007103C
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException_code()
		{
			throw new ArgumentOutOfRangeException("code");
		}

		// Token: 0x060023AF RID: 9135 RVA: 0x00072E48 File Offset: 0x00071048
		[<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException_data()
		{
			throw new ArgumentOutOfRangeException("data");
		}
	}
}
