using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000049 RID: 73
	public interface ReferenceIMBEvent<T1, T2, T3> : IMbEventBase
	{
		// Token: 0x0600087F RID: 2175
		void AddNonSerializedListener(object owner, ReferenceAction<T1, T2, T3> action);
	}
}
