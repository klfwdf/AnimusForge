using System;
using System.Security.Permissions;

namespace System.Diagnostics
{
	// Token: 0x020003F2 RID: 1010
	// (Invoke) Token: 0x06003333 RID: 13107
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	[Serializable]
	internal delegate void LogMessageEventHandler(LoggingLevels level, LogSwitch category, string message, StackTrace location);
}
