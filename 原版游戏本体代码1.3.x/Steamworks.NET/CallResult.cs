using System;

namespace Steamworks
{
	// Token: 0x020001B3 RID: 435
	public abstract class CallResult
	{
		// Token: 0x06000AD9 RID: 2777
		internal abstract Type GetCallbackType();

		// Token: 0x06000ADA RID: 2778
		internal abstract void OnRunCallResult(IntPtr pvParam, bool bFailed, ulong hSteamAPICall);

		// Token: 0x06000ADB RID: 2779
		internal abstract void SetUnregistered();
	}
}
