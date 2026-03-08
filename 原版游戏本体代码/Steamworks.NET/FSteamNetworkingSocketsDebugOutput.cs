using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Steamworks
{
	// Token: 0x02000194 RID: 404
	// (Invoke) Token: 0x060009AF RID: 2479
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void FSteamNetworkingSocketsDebugOutput(ESteamNetworkingSocketsDebugOutputType nType, StringBuilder pszMsg);
}
