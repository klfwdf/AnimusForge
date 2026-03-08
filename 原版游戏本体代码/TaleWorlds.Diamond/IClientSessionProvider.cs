using System;

namespace TaleWorlds.Diamond
{
	// Token: 0x0200000F RID: 15
	public interface IClientSessionProvider<T> where T : Client<T>
	{
		// Token: 0x06000051 RID: 81
		IClientSession CreateSession(T session);
	}
}
