using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x020006B8 RID: 1720
	[Guid("07662534-750b-4ed5-9cfb-1c5bc5acfd07")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	internal interface IStateManager
	{
		// Token: 0x0600503C RID: 20540
		[SecurityCritical]
		void PrepareApplicationState([In] UIntPtr Inputs, ref UIntPtr Outputs);

		// Token: 0x0600503D RID: 20541
		[SecurityCritical]
		void SetApplicationRunningState([In] uint Flags, [In] IActContext Context, [In] uint RunningState, out uint Disposition);

		// Token: 0x0600503E RID: 20542
		[SecurityCritical]
		void GetApplicationStateFilesystemLocation([In] uint Flags, [In] IDefinitionAppId Appidentity, [In] IDefinitionIdentity ComponentIdentity, [In] UIntPtr Coordinates, [MarshalAs(UnmanagedType.LPWStr)] out string Path);

		// Token: 0x0600503F RID: 20543
		[SecurityCritical]
		void Scavenge([In] uint Flags, out uint Disposition);
	}
}
