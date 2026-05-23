using System;

namespace Iced.Intel
{
	// Token: 0x02000656 RID: 1622
	internal static class InstructionMemorySizes
	{
		// Token: 0x170007F1 RID: 2033
		// (get) Token: 0x06002358 RID: 9048 RVA: 0x00072581 File Offset: 0x00070781
		internal unsafe static ReadOnlySpan<byte> SizesNormal
		{
			get
			{
				return new ReadOnlySpan<byte>((void*)(&<b37590d4-39fb-478a-88de-d293f3364852><PrivateImplementationDetails>.482D105BA971F851D057FEFDF545E95495ADF035BA9720D50CC6AC7B9DB6E735), 4936);
			}
		}

		// Token: 0x170007F2 RID: 2034
		// (get) Token: 0x06002359 RID: 9049 RVA: 0x00072592 File Offset: 0x00070792
		internal unsafe static ReadOnlySpan<byte> SizesBcst
		{
			get
			{
				return new ReadOnlySpan<byte>((void*)(&<b37590d4-39fb-478a-88de-d293f3364852><PrivateImplementationDetails>.5A3FD922B8AB7E003D3A82741A1891EFEFA74BA05FC859CCC975EDAF6AD22F6C), 4936);
			}
		}
	}
}
