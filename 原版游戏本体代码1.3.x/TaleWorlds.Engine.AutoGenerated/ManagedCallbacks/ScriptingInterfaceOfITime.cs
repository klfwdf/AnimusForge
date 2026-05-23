using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.Engine;

namespace ManagedCallbacks
{
	// Token: 0x0200002E RID: 46
	internal class ScriptingInterfaceOfITime : ITime
	{
		// Token: 0x06000626 RID: 1574 RVA: 0x00019E03 File Offset: 0x00018003
		public float GetApplicationTime()
		{
			return ScriptingInterfaceOfITime.call_GetApplicationTimeDelegate();
		}

		// Token: 0x04000575 RID: 1397
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x04000576 RID: 1398
		public static ScriptingInterfaceOfITime.GetApplicationTimeDelegate call_GetApplicationTimeDelegate;

		// Token: 0x020005D6 RID: 1494
		// (Invoke) Token: 0x06001D6B RID: 7531
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetApplicationTimeDelegate();
	}
}
