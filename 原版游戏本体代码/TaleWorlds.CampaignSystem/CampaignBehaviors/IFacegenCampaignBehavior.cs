using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003FD RID: 1021
	public interface IFacegenCampaignBehavior : ICampaignBehavior
	{
		// Token: 0x06003FB0 RID: 16304
		IFaceGeneratorCustomFilter GetFaceGenFilter();
	}
}
