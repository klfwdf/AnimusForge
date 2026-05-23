using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x020006B3 RID: 1715
	[Guid("ace1b703-1aac-4956-ab87-90cac8b93ce6")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	internal interface IManifestParseErrorCallback
	{
		// Token: 0x0600501C RID: 20508
		[SecurityCritical]
		void OnError([In] uint StartLine, [In] uint nStartColumn, [In] uint cCharacterCount, [In] int hr, [MarshalAs(UnmanagedType.LPWStr)] [In] string ErrorStatusHostFile, [In] uint ParameterCount, [MarshalAs(UnmanagedType.LPArray)] [In] string[] Parameters);
	}
}
