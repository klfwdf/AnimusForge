using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200004F RID: 79
	public interface IMbEvent<out T1, out T2, out T3, out T4> : IMbEventBase
	{
		// Token: 0x06000894 RID: 2196
		void AddNonSerializedListener(object owner, Action<T1, T2, T3, T4> action);
	}
}
