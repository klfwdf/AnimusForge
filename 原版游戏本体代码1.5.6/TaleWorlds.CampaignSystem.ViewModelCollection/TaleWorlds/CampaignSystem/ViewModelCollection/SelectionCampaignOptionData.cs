using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection;

public class SelectionCampaignOptionData : CampaignOptionData
{
	public List<TextObject> Selections { get; private set; }

	public SelectionCampaignOptionData(string identifier, int priorityIndex, CampaignOptionEnableState enableState, Func<float> getValue, Action<float> setValue, List<TextObject> customSelectionTexts = null, Func<CampaignOptionDisableStatus> getIsDisabledWithReason = null, bool isRelatedToDifficultyPreset = false, Func<float, CampaignOptionsDifficultyPresets> onGetDifficultyPresetFromValue = null, Func<CampaignOptionsDifficultyPresets, float> onGetValueFromDifficultyPreset = null)
		: base(identifier, priorityIndex, enableState, getValue, setValue, getIsDisabledWithReason, isRelatedToDifficultyPreset, onGetDifficultyPresetFromValue, onGetValueFromDifficultyPreset)
	{
		if (customSelectionTexts != null)
		{
			Selections = customSelectionTexts;
		}
		else
		{
			Selections = GetPresetTexts(identifier);
		}
	}

	private List<TextObject> GetPresetTexts(string identifier)
	{
		List<TextObject> list = new List<TextObject>();
		foreach (object value in Enum.GetValues(typeof(CampaignOptions.Difficulty)))
		{
			list.Add(GameTexts.FindText("str_campaign_options_type_" + identifier, value.ToString()));
		}
		return list;
	}

	public override CampaignOptionDataType GetDataType()
	{
		return CampaignOptionDataType.Selection;
	}
}
