using System;
using System.Collections.Generic;

namespace TaleWorlds.CampaignSystem.ViewModelCollection
{
	// Token: 0x0200000E RID: 14
	public interface ICampaignOptionProvider
	{
		// Token: 0x060000AF RID: 175
		IEnumerable<ICampaignOptionData> GetGameplayCampaignOptions();

		// Token: 0x060000B0 RID: 176
		IEnumerable<ICampaignOptionData> GetCharacterCreationCampaignOptions();
	}
}
