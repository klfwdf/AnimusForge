using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200004D RID: 77
	public interface IMbEvent<out T1, out T2, out T3> : IMbEventBase
	{
		// Token: 0x0600088D RID: 2189
		void AddNonSerializedListener(object owner, Action<T1, T2, T3> action);
	}
}
