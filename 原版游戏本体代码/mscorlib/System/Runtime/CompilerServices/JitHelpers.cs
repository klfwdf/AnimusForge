using System;
using System.Security;
using System.Threading;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008D7 RID: 2263
	[FriendAccessAllowed]
	internal static class JitHelpers
	{
		// Token: 0x06005DC8 RID: 24008 RVA: 0x001499CC File Offset: 0x00147BCC
		[SecurityCritical]
		internal static StringHandleOnStack GetStringHandleOnStack(ref string s)
		{
			return new StringHandleOnStack(JitHelpers.UnsafeCastToStackPointer<string>(ref s));
		}

		// Token: 0x06005DC9 RID: 24009 RVA: 0x001499D9 File Offset: 0x00147BD9
		[SecurityCritical]
		internal static ObjectHandleOnStack GetObjectHandleOnStack<T>(ref T o) where T : class
		{
			return new ObjectHandleOnStack(JitHelpers.UnsafeCastToStackPointer<T>(ref o));
		}

		// Token: 0x06005DCA RID: 24010 RVA: 0x001499E6 File Offset: 0x00147BE6
		[SecurityCritical]
		internal static StackCrawlMarkHandle GetStackCrawlMarkHandle(ref StackCrawlMark stackMark)
		{
			return new StackCrawlMarkHandle(JitHelpers.UnsafeCastToStackPointer<StackCrawlMark>(ref stackMark));
		}

		// Token: 0x06005DCB RID: 24011 RVA: 0x001499F3 File Offset: 0x00147BF3
		[SecurityCritical]
		[FriendAccessAllowed]
		internal static T UnsafeCast<T>(object o) where T : class
		{
			throw new InvalidOperationException();
		}

		// Token: 0x06005DCC RID: 24012 RVA: 0x001499FA File Offset: 0x00147BFA
		internal static int UnsafeEnumCast<T>(T val) where T : struct
		{
			throw new InvalidOperationException();
		}

		// Token: 0x06005DCD RID: 24013 RVA: 0x00149A01 File Offset: 0x00147C01
		internal static long UnsafeEnumCastLong<T>(T val) where T : struct
		{
			throw new InvalidOperationException();
		}

		// Token: 0x06005DCE RID: 24014 RVA: 0x00149A08 File Offset: 0x00147C08
		[SecurityCritical]
		internal static IntPtr UnsafeCastToStackPointer<T>(ref T val)
		{
			throw new InvalidOperationException();
		}

		// Token: 0x06005DCF RID: 24015
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void UnsafeSetArrayElement(object[] target, int index, object element);

		// Token: 0x06005DD0 RID: 24016 RVA: 0x00149A0F File Offset: 0x00147C0F
		[SecurityCritical]
		internal static PinningHelper GetPinningHelper(object o)
		{
			return JitHelpers.UnsafeCast<PinningHelper>(o);
		}

		// Token: 0x04002A39 RID: 10809
		internal const string QCall = "QCall";
	}
}
