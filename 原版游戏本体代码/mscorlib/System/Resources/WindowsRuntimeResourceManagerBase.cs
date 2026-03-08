using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Resources
{
	// Token: 0x02000395 RID: 917
	[FriendAccessAllowed]
	[SecurityCritical]
	internal class WindowsRuntimeResourceManagerBase
	{
		// Token: 0x06002D15 RID: 11541 RVA: 0x000AA3D5 File Offset: 0x000A85D5
		[SecurityCritical]
		public virtual bool Initialize(string libpath, string reswFilename, out PRIExceptionInfo exceptionInfo)
		{
			exceptionInfo = null;
			return false;
		}

		// Token: 0x06002D16 RID: 11542 RVA: 0x000AA3DB File Offset: 0x000A85DB
		[SecurityCritical]
		public virtual string GetString(string stringName, string startingCulture, string neutralResourcesCulture)
		{
			return null;
		}

		// Token: 0x170005E8 RID: 1512
		// (get) Token: 0x06002D17 RID: 11543 RVA: 0x000AA3DE File Offset: 0x000A85DE
		public virtual CultureInfo GlobalResourceContextBestFitCultureInfo
		{
			[SecurityCritical]
			get
			{
				return null;
			}
		}

		// Token: 0x06002D18 RID: 11544 RVA: 0x000AA3E1 File Offset: 0x000A85E1
		[SecurityCritical]
		public virtual bool SetGlobalResourceContextDefaultCulture(CultureInfo ci)
		{
			return false;
		}
	}
}
