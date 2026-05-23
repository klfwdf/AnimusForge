using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000053 RID: 83
	public interface IMbEvent<out T1, out T2, out T3, out T4, out T5, out T6> : IMbEventBase
	{
		// Token: 0x060008A2 RID: 2210
		void AddNonSerializedListener(object owner, Action<T1, T2, T3, T4, T5, T6> action);
	}
}
