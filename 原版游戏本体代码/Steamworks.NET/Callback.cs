using System;

namespace Steamworks
{
	// Token: 0x020001B1 RID: 433
	public abstract class Callback
	{
		// Token: 0x17000021 RID: 33
		// (get) Token: 0x06000AC7 RID: 2759
		public abstract bool IsGameServer { get; }

		// Token: 0x06000AC8 RID: 2760
		internal abstract Type GetCallbackType();

		// Token: 0x06000AC9 RID: 2761
		internal abstract void OnRunCallback(IntPtr pvParam);

		// Token: 0x06000ACA RID: 2762
		internal abstract void SetUnregistered();
	}
}
