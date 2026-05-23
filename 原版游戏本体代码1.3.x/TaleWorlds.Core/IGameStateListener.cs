using System;

namespace TaleWorlds.Core
{
	// Token: 0x02000086 RID: 134
	public interface IGameStateListener
	{
		// Token: 0x06000895 RID: 2197
		void OnActivate();

		// Token: 0x06000896 RID: 2198
		void OnDeactivate();

		// Token: 0x06000897 RID: 2199
		void OnInitialize();

		// Token: 0x06000898 RID: 2200
		void OnFinalize();
	}
}
