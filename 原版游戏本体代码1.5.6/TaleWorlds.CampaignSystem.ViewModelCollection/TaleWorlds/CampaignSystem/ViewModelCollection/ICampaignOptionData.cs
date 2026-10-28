namespace TaleWorlds.CampaignSystem.ViewModelCollection;

public interface ICampaignOptionData
{
	CampaignOptionDataType GetDataType();

	int GetPriorityIndex();

	bool IsRelatedToDifficultyPreset();

	float GetValueFromDifficultyPreset(CampaignOptionsDifficultyPresets preset);

	string GetIdentifier();

	CampaignOptionEnableState GetEnableState();

	string GetName();

	string GetDescription();

	float GetValue();

	void SetValue(float value);

	CampaignOptionDisableStatus GetIsDisabledWithReason();
}
