using System;

namespace TaleWorlds.CampaignSystem.ViewModelCollection;

public class NumericCampaignOptionData : CampaignOptionData
{
	public float MinValue { get; private set; }

	public float MaxValue { get; private set; }

	public bool IsDiscrete { get; private set; }

	public NumericCampaignOptionData(string identifier, int priorityIndex, CampaignOptionEnableState enableState, Func<float> getValue, Action<float> setValue, float minValue, float maxValue, bool isDiscrete, Func<CampaignOptionDisableStatus> getIsDisabledWithReason = null, bool isRelatedToDifficultyPreset = false, Func<float, CampaignOptionsDifficultyPresets> onGetDifficultyPresetFromValue = null, Func<CampaignOptionsDifficultyPresets, float> onGetValueFromDifficultyPreset = null)
		: base(identifier, priorityIndex, enableState, getValue, setValue, getIsDisabledWithReason, isRelatedToDifficultyPreset, onGetDifficultyPresetFromValue, onGetValueFromDifficultyPreset)
	{
		MinValue = minValue;
		MaxValue = maxValue;
		IsDiscrete = isDiscrete;
	}

	public override CampaignOptionDataType GetDataType()
	{
		return CampaignOptionDataType.Numeric;
	}
}
