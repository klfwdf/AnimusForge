using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000043 RID: 67
	public interface IMbEvent<out T> : IMbEventBase
	{
		// Token: 0x0600086A RID: 2154
		void AddNonSerializedListener(object owner, Action<T> action);
	}
}
