using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000038 RID: 56
	public interface IMbEvent
	{
		// Token: 0x060003D4 RID: 980
		void AddNonSerializedListener(object owner, Action action);

		// Token: 0x060003D5 RID: 981
		void ClearListeners(object o);
	}
}
