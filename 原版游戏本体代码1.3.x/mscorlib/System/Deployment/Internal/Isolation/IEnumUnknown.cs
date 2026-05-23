using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x02000672 RID: 1650
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("00000100-0000-0000-C000-000000000046")]
	[ComImport]
	internal interface IEnumUnknown
	{
		// Token: 0x06004F26 RID: 20262
		[PreserveSig]
		int Next(uint celt, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.IUnknown)] [Out] object[] rgelt, ref uint celtFetched);

		// Token: 0x06004F27 RID: 20263
		[PreserveSig]
		int Skip(uint celt);

		// Token: 0x06004F28 RID: 20264
		[PreserveSig]
		int Reset();

		// Token: 0x06004F29 RID: 20265
		[PreserveSig]
		int Clone(out IEnumUnknown enumUnknown);
	}
}
