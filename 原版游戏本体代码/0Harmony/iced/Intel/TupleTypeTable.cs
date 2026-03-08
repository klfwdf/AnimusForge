using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel
{
	// Token: 0x0200066A RID: 1642
	internal static class TupleTypeTable
	{
		// Token: 0x170007FC RID: 2044
		// (get) Token: 0x060023B0 RID: 9136 RVA: 0x00072E54 File Offset: 0x00071054
		private unsafe static ReadOnlySpan<byte> tupleTypeData
		{
			get
			{
				return new ReadOnlySpan<byte>((void*)(&<b37590d4-39fb-478a-88de-d293f3364852><PrivateImplementationDetails>.D79DD1C8B320F03F943FB02BA6A6D20562AB453315E27BF5C551D37DD74F1F13), 38);
			}
		}

		// Token: 0x060023B1 RID: 9137 RVA: 0x00072E64 File Offset: 0x00071064
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static uint GetDisp8N(TupleType tupleType, bool bcst)
		{
			int index = (int)(((int)tupleType << 1) | ((bcst > false) ? TupleType.N2 : TupleType.N1));
			return (uint)(*TupleTypeTable.tupleTypeData[index]);
		}
	}
}
