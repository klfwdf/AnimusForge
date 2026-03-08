using System;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008DF RID: 2271
	[AttributeUsage(AttributeTargets.All)]
	[ComVisible(false)]
	internal sealed class DecoratedNameAttribute : Attribute
	{
		// Token: 0x06005DD7 RID: 24023 RVA: 0x00149A47 File Offset: 0x00147C47
		public DecoratedNameAttribute(string decoratedName)
		{
		}
	}
}
