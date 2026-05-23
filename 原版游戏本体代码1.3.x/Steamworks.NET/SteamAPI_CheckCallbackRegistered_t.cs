using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000179 RID: 377
	// (Invoke) Token: 0x060008AA RID: 2218
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	public delegate void SteamAPI_CheckCallbackRegistered_t(int iCallbackNum);
}
