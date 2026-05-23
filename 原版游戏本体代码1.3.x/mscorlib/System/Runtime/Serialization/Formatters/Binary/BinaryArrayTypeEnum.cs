using System;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x0200076A RID: 1898
	[Serializable]
	internal enum BinaryArrayTypeEnum
	{
		// Token: 0x04002507 RID: 9479
		Single,
		// Token: 0x04002508 RID: 9480
		Jagged,
		// Token: 0x04002509 RID: 9481
		Rectangular,
		// Token: 0x0400250A RID: 9482
		SingleOffset,
		// Token: 0x0400250B RID: 9483
		JaggedOffset,
		// Token: 0x0400250C RID: 9484
		RectangularOffset
	}
}
