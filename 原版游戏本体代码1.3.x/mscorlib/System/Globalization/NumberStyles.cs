using System;
using System.Runtime.InteropServices;

namespace System.Globalization
{
	// Token: 0x020003DA RID: 986
	[Flags]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public enum NumberStyles
	{
		// Token: 0x04001573 RID: 5491
		[__DynamicallyInvokable]
		None = 0,
		// Token: 0x04001574 RID: 5492
		[__DynamicallyInvokable]
		AllowLeadingWhite = 1,
		// Token: 0x04001575 RID: 5493
		[__DynamicallyInvokable]
		AllowTrailingWhite = 2,
		// Token: 0x04001576 RID: 5494
		[__DynamicallyInvokable]
		AllowLeadingSign = 4,
		// Token: 0x04001577 RID: 5495
		[__DynamicallyInvokable]
		AllowTrailingSign = 8,
		// Token: 0x04001578 RID: 5496
		[__DynamicallyInvokable]
		AllowParentheses = 16,
		// Token: 0x04001579 RID: 5497
		[__DynamicallyInvokable]
		AllowDecimalPoint = 32,
		// Token: 0x0400157A RID: 5498
		[__DynamicallyInvokable]
		AllowThousands = 64,
		// Token: 0x0400157B RID: 5499
		[__DynamicallyInvokable]
		AllowExponent = 128,
		// Token: 0x0400157C RID: 5500
		[__DynamicallyInvokable]
		AllowCurrencySymbol = 256,
		// Token: 0x0400157D RID: 5501
		[__DynamicallyInvokable]
		AllowHexSpecifier = 512,
		// Token: 0x0400157E RID: 5502
		[__DynamicallyInvokable]
		Integer = 7,
		// Token: 0x0400157F RID: 5503
		[__DynamicallyInvokable]
		HexNumber = 515,
		// Token: 0x04001580 RID: 5504
		[__DynamicallyInvokable]
		Number = 111,
		// Token: 0x04001581 RID: 5505
		[__DynamicallyInvokable]
		Float = 167,
		// Token: 0x04001582 RID: 5506
		[__DynamicallyInvokable]
		Currency = 383,
		// Token: 0x04001583 RID: 5507
		[__DynamicallyInvokable]
		Any = 511
	}
}
