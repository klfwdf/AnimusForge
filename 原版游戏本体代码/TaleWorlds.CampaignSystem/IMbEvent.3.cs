using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200004B RID: 75
	public interface IMbEvent<out T1, out T2> : IMbEventBase
	{
		// Token: 0x06000886 RID: 2182
		void AddNonSerializedListener(object owner, Action<T1, T2> action);
	}
}
