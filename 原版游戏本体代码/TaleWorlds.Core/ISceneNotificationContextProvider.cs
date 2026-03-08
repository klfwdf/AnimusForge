using System;

namespace TaleWorlds.Core
{
	// Token: 0x020000CB RID: 203
	public interface ISceneNotificationContextProvider
	{
		// Token: 0x06000AF4 RID: 2804
		bool IsContextAllowed(SceneNotificationData.RelevantContextType relevantType);
	}
}
