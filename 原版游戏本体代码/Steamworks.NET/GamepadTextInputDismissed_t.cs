using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000F5 RID: 245
	[CallbackIdentity(714)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct GamepadTextInputDismissed_t
	{
		// Token: 0x040002F8 RID: 760
		public const int k_iCallback = 714;

		// Token: 0x040002F9 RID: 761
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bSubmitted;

		// Token: 0x040002FA RID: 762
		public uint m_unSubmittedText;
	}
}
