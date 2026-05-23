using System;

namespace TaleWorlds.Engine
{
	// Token: 0x02000051 RID: 81
	public interface ILoadingWindowManager
	{
		// Token: 0x06000869 RID: 2153
		void EnableLoadingWindow();

		// Token: 0x0600086A RID: 2154
		void DisableLoadingWindow();

		// Token: 0x0600086B RID: 2155
		void SetCurrentModeIsMultiplayer(bool isMultiplayer);

		// Token: 0x0600086C RID: 2156
		void Initialize();

		// Token: 0x0600086D RID: 2157
		void Destroy();
	}
}
