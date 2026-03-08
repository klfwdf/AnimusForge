using System;

namespace TaleWorlds.CampaignSystem.ViewModelCollection
{
	// Token: 0x02000016 RID: 22
	public class NumericCampaignOptionData : CampaignOptionData
	{
		// Token: 0x17000036 RID: 54
		// (get) Token: 0x060000E7 RID: 231 RVA: 0x00004661 File Offset: 0x00002861
		// (set) Token: 0x060000E8 RID: 232 RVA: 0x00004669 File Offset: 0x00002869
		public float MinValue { get; private set; }

		// Token: 0x17000037 RID: 55
		// (get) Token: 0x060000E9 RID: 233 RVA: 0x00004672 File Offset: 0x00002872
		// (set) Token: 0x060000EA RID: 234 RVA: 0x0000467A File Offset: 0x0000287A
		public float MaxValue { get; private set; }

		// Token: 0x17000038 RID: 56
		// (get) Token: 0x060000EB RID: 235 RVA: 0x00004683 File Offset: 0x00002883
		// (set) Token: 0x060000EC RID: 236 RVA: 0x0000468B File Offset: 0x0000288B
		public bool IsDiscrete { get; private set; }

		// Token: 0x060000ED RID: 237 RVA: 0x00004694 File Offset: 0x00002894
		public NumericCampaignOptionData(string identifier, int priorityIndex, CampaignOptionEnableState enableState, Func<float> getValue, Action<float> setValue, float minValue, float maxValue, bool isDiscrete, Func<CampaignOptionDisableStatus> getIsDisabledWithReason = null, bool isRelatedToDifficultyPreset = false, Func<float, CampaignOptionsDifficultyPresets> onGetDifficultyPresetFromValue = null, Func<CampaignOptionsDifficultyPresets, float> onGetValueFromDifficultyPreset = null)
			: base(identifier, priorityIndex, enableState, getValue, setValue, getIsDisabledWithReason, isRelatedToDifficultyPreset, onGetDifficultyPresetFromValue, onGetValueFromDifficultyPreset)
		{
			this.MinValue = minValue;
			this.MaxValue = maxValue;
			this.IsDiscrete = isDiscrete;
		}

		// Token: 0x060000EE RID: 238 RVA: 0x000046CE File Offset: 0x000028CE
		public override CampaignOptionDataType GetDataType()
		{
			return CampaignOptionDataType.Numeric;
		}
	}
}
