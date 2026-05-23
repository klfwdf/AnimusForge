using System;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x02000769 RID: 1897
	[Serializable]
	internal enum BinaryTypeEnum
	{
		// Token: 0x040024FE RID: 9470
		Primitive,
		// Token: 0x040024FF RID: 9471
		String,
		// Token: 0x04002500 RID: 9472
		Object,
		// Token: 0x04002501 RID: 9473
		ObjectUrt,
		// Token: 0x04002502 RID: 9474
		ObjectUser,
		// Token: 0x04002503 RID: 9475
		ObjectArray,
		// Token: 0x04002504 RID: 9476
		StringArray,
		// Token: 0x04002505 RID: 9477
		PrimitiveArray
	}
}
