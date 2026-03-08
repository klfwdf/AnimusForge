using System;
using System.Runtime.CompilerServices;

namespace System.StubHelpers
{
	// Token: 0x020005A5 RID: 1445
	internal static class WinRTTypeNameConverter
	{
		// Token: 0x06004324 RID: 17188
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern string ConvertToWinRTTypeName(Type managedType, out bool isPrimitive);

		// Token: 0x06004325 RID: 17189
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern Type GetTypeFromWinRTTypeName(string typeName, out bool isPrimitive);
	}
}
