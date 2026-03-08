using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System
{
	// Token: 0x0200010C RID: 268
	internal static class Mda
	{
		// Token: 0x0600104C RID: 4172
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void ReportStreamWriterBufferedDataLost(string text);

		// Token: 0x0600104D RID: 4173
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool IsStreamWriterBufferedDataLostEnabled();

		// Token: 0x0600104E RID: 4174
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool IsStreamWriterBufferedDataLostCaptureAllocatedCallStack();

		// Token: 0x0600104F RID: 4175
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void MemberInfoCacheCreation();

		// Token: 0x06001050 RID: 4176
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void DateTimeInvalidLocalFormat();

		// Token: 0x06001051 RID: 4177
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool IsInvalidGCHandleCookieProbeEnabled();

		// Token: 0x06001052 RID: 4178
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void FireInvalidGCHandleCookieProbe(IntPtr cookie);

		// Token: 0x06001053 RID: 4179
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void ReportErrorSafeHandleRelease(Exception ex);

		// Token: 0x02000AF1 RID: 2801
		internal static class StreamWriterBufferedDataLost
		{
			// Token: 0x170011EF RID: 4591
			// (get) Token: 0x06006A11 RID: 27153 RVA: 0x0016D4B6 File Offset: 0x0016B6B6
			internal static bool Enabled
			{
				[SecuritySafeCritical]
				get
				{
					if (Mda.StreamWriterBufferedDataLost._enabledState == 0)
					{
						if (Mda.IsStreamWriterBufferedDataLostEnabled())
						{
							Mda.StreamWriterBufferedDataLost._enabledState = 1;
						}
						else
						{
							Mda.StreamWriterBufferedDataLost._enabledState = 2;
						}
					}
					return Mda.StreamWriterBufferedDataLost._enabledState == 1;
				}
			}

			// Token: 0x170011F0 RID: 4592
			// (get) Token: 0x06006A12 RID: 27154 RVA: 0x0016D4E4 File Offset: 0x0016B6E4
			internal static bool CaptureAllocatedCallStack
			{
				[SecuritySafeCritical]
				get
				{
					if (Mda.StreamWriterBufferedDataLost._captureAllocatedCallStackState == 0)
					{
						if (Mda.IsStreamWriterBufferedDataLostCaptureAllocatedCallStack())
						{
							Mda.StreamWriterBufferedDataLost._captureAllocatedCallStackState = 1;
						}
						else
						{
							Mda.StreamWriterBufferedDataLost._captureAllocatedCallStackState = 2;
						}
					}
					return Mda.StreamWriterBufferedDataLost._captureAllocatedCallStackState == 1;
				}
			}

			// Token: 0x06006A13 RID: 27155 RVA: 0x0016D512 File Offset: 0x0016B712
			[SecuritySafeCritical]
			internal static void ReportError(string text)
			{
				Mda.ReportStreamWriterBufferedDataLost(text);
			}

			// Token: 0x040031AC RID: 12716
			private static volatile int _enabledState;

			// Token: 0x040031AD RID: 12717
			private static volatile int _captureAllocatedCallStackState;
		}
	}
}
