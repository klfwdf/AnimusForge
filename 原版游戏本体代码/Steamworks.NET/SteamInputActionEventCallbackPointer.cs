using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000189 RID: 393
	// (Invoke) Token: 0x06000953 RID: 2387
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void SteamInputActionEventCallbackPointer(IntPtr SteamInputActionEvent);
}
