using System;

namespace TaleWorlds.CampaignSystem.ViewModelCollection
{
	// Token: 0x02000015 RID: 21
	public class BooleanCampaignOptionData : CampaignOptionData
	{
		// Token: 0x060000E5 RID: 229 RVA: 0x0000463C File Offset: 0x0000283C
		public BooleanCampaignOptionData(string identifier, int priorityIndex, CampaignOptionEnableState enableState, Func<float> getValue, Action<float> setValue, Func<CampaignOptionDisableStatus> getIsDisabledWithReason = null, bool isRelatedToDifficultyPreset = false, Func<float, CampaignOptionsDifficultyPresets> onGetDifficultyPresetFromValue = null, Func<CampaignOptionsDifficultyPresets, float> onGetValueFromDifficultyPreset = null)
			: base(identifier, priorityIndex, enableState, getValue, setValue, getIsDisabledWithReason, isRelatedToDifficultyPreset, onGetDifficultyPresetFromValue, onGetValueFromDifficultyPreset)
		{
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x0000465E File Offset: 0x0000285E
		public override CampaignOptionDataType GetDataType()
		{
			return CampaignOptionDataType.Boolean;
		}
	}
}
