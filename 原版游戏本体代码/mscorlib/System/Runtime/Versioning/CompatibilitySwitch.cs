using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.Versioning
{
	// Token: 0x02000727 RID: 1831
	public static class CompatibilitySwitch
	{
		// Token: 0x0600516A RID: 20842 RVA: 0x0011F18A File Offset: 0x0011D38A
		[SecurityCritical]
		public static bool IsEnabled(string compatibilitySwitchName)
		{
			return CompatibilitySwitch.IsEnabledInternalCall(compatibilitySwitchName, true);
		}

		// Token: 0x0600516B RID: 20843 RVA: 0x0011F193 File Offset: 0x0011D393
		[SecurityCritical]
		public static string GetValue(string compatibilitySwitchName)
		{
			return CompatibilitySwitch.GetValueInternalCall(compatibilitySwitchName, true);
		}

		// Token: 0x0600516C RID: 20844 RVA: 0x0011F19C File Offset: 0x0011D39C
		[SecurityCritical]
		internal static bool IsEnabledInternal(string compatibilitySwitchName)
		{
			return CompatibilitySwitch.IsEnabledInternalCall(compatibilitySwitchName, false);
		}

		// Token: 0x0600516D RID: 20845 RVA: 0x0011F1A5 File Offset: 0x0011D3A5
		[SecurityCritical]
		internal static string GetValueInternal(string compatibilitySwitchName)
		{
			return CompatibilitySwitch.GetValueInternalCall(compatibilitySwitchName, false);
		}

		// Token: 0x0600516E RID: 20846
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern string GetAppContextOverridesInternalCall();

		// Token: 0x0600516F RID: 20847
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool IsEnabledInternalCall(string compatibilitySwitchName, bool onlyDB);

		// Token: 0x06005170 RID: 20848
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern string GetValueInternalCall(string compatibilitySwitchName, bool onlyDB);
	}
}
