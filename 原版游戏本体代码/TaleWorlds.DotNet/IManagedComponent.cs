using System;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000015 RID: 21
	public interface IManagedComponent
	{
		// Token: 0x06000068 RID: 104
		void OnCustomCallbackMethodPassed(string name, Delegate method);

		// Token: 0x06000069 RID: 105
		void OnStart();

		// Token: 0x0600006A RID: 106
		void OnApplicationTick(float dt);
	}
}
