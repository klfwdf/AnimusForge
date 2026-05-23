using System;

namespace TaleWorlds.CampaignSystem.ViewModelCollection
{
	// Token: 0x02000014 RID: 20
	public interface ICampaignOptionData
	{
		// Token: 0x060000DA RID: 218
		CampaignOptionDataType GetDataType();

		// Token: 0x060000DB RID: 219
		int GetPriorityIndex();

		// Token: 0x060000DC RID: 220
		bool IsRelatedToDifficultyPreset();

		// Token: 0x060000DD RID: 221
		float GetValueFromDifficultyPreset(CampaignOptionsDifficultyPresets preset);

		// Token: 0x060000DE RID: 222
		string GetIdentifier();

		// Token: 0x060000DF RID: 223
		CampaignOptionEnableState GetEnableState();

		// Token: 0x060000E0 RID: 224
		string GetName();

		// Token: 0x060000E1 RID: 225
		string GetDescription();

		// Token: 0x060000E2 RID: 226
		float GetValue();

		// Token: 0x060000E3 RID: 227
		void SetValue(float value);

		// Token: 0x060000E4 RID: 228
		CampaignOptionDisableStatus GetIsDisabledWithReason();
	}
}
