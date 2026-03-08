using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200003C RID: 60
	[ApplicationInterfaceBase]
	internal interface ITime
	{
		// Token: 0x06000636 RID: 1590
		[EngineMethod("get_application_time", false, null, false)]
		float GetApplicationTime();
	}
}
