using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000045 RID: 69
	public interface ReferenceIMBEvent<T1> : IMbEventBase
	{
		// Token: 0x06000871 RID: 2161
		void AddNonSerializedListener(object owner, ReferenceAction<T1> action);
	}
}
