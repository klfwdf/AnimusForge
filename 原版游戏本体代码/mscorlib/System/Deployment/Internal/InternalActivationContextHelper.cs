using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal
{
	// Token: 0x0200066D RID: 1645
	[ComVisible(false)]
	public static class InternalActivationContextHelper
	{
		// Token: 0x06004F16 RID: 20246 RVA: 0x0011C3C4 File Offset: 0x0011A5C4
		[SecuritySafeCritical]
		public static object GetActivationContextData(ActivationContext appInfo)
		{
			return appInfo.ActivationContextData;
		}

		// Token: 0x06004F17 RID: 20247 RVA: 0x0011C3CC File Offset: 0x0011A5CC
		[SecuritySafeCritical]
		public static object GetApplicationComponentManifest(ActivationContext appInfo)
		{
			return appInfo.ApplicationComponentManifest;
		}

		// Token: 0x06004F18 RID: 20248 RVA: 0x0011C3D4 File Offset: 0x0011A5D4
		[SecuritySafeCritical]
		public static object GetDeploymentComponentManifest(ActivationContext appInfo)
		{
			return appInfo.DeploymentComponentManifest;
		}

		// Token: 0x06004F19 RID: 20249 RVA: 0x0011C3DC File Offset: 0x0011A5DC
		public static void PrepareForExecution(ActivationContext appInfo)
		{
			appInfo.PrepareForExecution();
		}

		// Token: 0x06004F1A RID: 20250 RVA: 0x0011C3E4 File Offset: 0x0011A5E4
		public static bool IsFirstRun(ActivationContext appInfo)
		{
			return appInfo.LastApplicationStateResult == ActivationContext.ApplicationStateDisposition.RunningFirstTime;
		}

		// Token: 0x06004F1B RID: 20251 RVA: 0x0011C3F3 File Offset: 0x0011A5F3
		public static byte[] GetApplicationManifestBytes(ActivationContext appInfo)
		{
			if (appInfo == null)
			{
				throw new ArgumentNullException("appInfo");
			}
			return appInfo.GetApplicationManifestBytes();
		}

		// Token: 0x06004F1C RID: 20252 RVA: 0x0011C409 File Offset: 0x0011A609
		public static byte[] GetDeploymentManifestBytes(ActivationContext appInfo)
		{
			if (appInfo == null)
			{
				throw new ArgumentNullException("appInfo");
			}
			return appInfo.GetDeploymentManifestBytes();
		}
	}
}
