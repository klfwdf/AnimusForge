using System;

namespace TaleWorlds.CampaignSystem.ViewModelCollection;

public class BooleanCampaignOptionData : CampaignOptionData
{
	public BooleanCampaignOptionData(string identifier, int priorityIndex, CampaignOptionEnableState enableState, Func<float> getValue, Action<float> setValue, Func<CampaignOptionDisableStatus> getIsDisabledWithReason = null, bool isRelatedToDifficultyPreset = false, Func<float, CampaignOptionsDifficultyPresets> onGetDifficultyPresetFromValue = null, Func<CampaignOptionsDifficultyPresets, float> onGetValueFromDifficultyPreset = null)
		: base(identifier, priorityIndex, enableState, getValue, setValue, getIsDisabledWithReason, isRelatedToDifficultyPreset, onGetDifficultyPresetFromValue, onGetValueFromDifficultyPreset)
	{
	}

	public override CampaignOptionDataType GetDataType()
	{
		return CampaignOptionDataType.Boolean;
	}
}
