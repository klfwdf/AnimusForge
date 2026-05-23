using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000047 RID: 71
	public interface ReferenceIMBEvent<T1, T2> : IMbEventBase
	{
		// Token: 0x06000878 RID: 2168
		void AddNonSerializedListener(object owner, ReferenceAction<T1, T2> action);
	}
}
