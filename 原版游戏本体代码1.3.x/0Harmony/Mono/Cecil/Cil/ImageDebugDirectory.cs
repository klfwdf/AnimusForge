using System;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000306 RID: 774
	internal struct ImageDebugDirectory
	{
		// Token: 0x04000A07 RID: 2567
		public const int Size = 28;

		// Token: 0x04000A08 RID: 2568
		public int Characteristics;

		// Token: 0x04000A09 RID: 2569
		public int TimeDateStamp;

		// Token: 0x04000A0A RID: 2570
		public short MajorVersion;

		// Token: 0x04000A0B RID: 2571
		public short MinorVersion;

		// Token: 0x04000A0C RID: 2572
		public ImageDebugType Type;

		// Token: 0x04000A0D RID: 2573
		public int SizeOfData;

		// Token: 0x04000A0E RID: 2574
		public int AddressOfRawData;

		// Token: 0x04000A0F RID: 2575
		public int PointerToRawData;
	}
}
