using System;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection;

public abstract class CampaignOptionData : ICampaignOptionData
{
	private int _priorityIndex;

	private string _identifier;

	private bool _isRelatedToDifficultyPreset;

	private CampaignOptionEnableState _enableState;

	private TextObject _name;

	private TextObject _description;

	private Func<CampaignOptionDisableStatus> _getIsDisabledWithReason;

	protected Func<float> _getValue;

	protected Action<float> _setValue;

	protected Func<float, CampaignOptionsDifficultyPresets> _onGetDifficultyPresetFromValue;

	protected Func<CampaignOptionsDifficultyPresets, float> _onGetValueFromDifficultyPreset;

	public CampaignOptionData(string identifier, int priorityIndex, CampaignOptionEnableState enableState, Func<float> getValue, Action<float> setValue, Func<CampaignOptionDisableStatus> getIsDisabledWithReason = null, bool isRelatedToDifficultyPreset = false, Func<float, CampaignOptionsDifficultyPresets> onGetDifficultyPresetFromValue = null, Func<CampaignOptionsDifficultyPresets, float> onGetValueFromDifficultyPreset = null)
	{
		_priorityIndex = priorityIndex;
		_identifier = identifier;
		_isRelatedToDifficultyPreset = isRelatedToDifficultyPreset;
		_getIsDisabledWithReason = getIsDisabledWithReason;
		_onGetDifficultyPresetFromValue = onGetDifficultyPresetFromValue;
		_onGetValueFromDifficultyPreset = onGetValueFromDifficultyPreset;
		_enableState = enableState;
		_name = GetNameOfOption(identifier);
		_description = GetDescriptionOfOption(identifier);
		_getValue = getValue;
		_setValue = setValue;
	}

	public static TextObject GetNameOfOption(string optionIdentifier)
	{
		if (CheckIsPlayStation() && GameTexts.TryGetText("str_campaign_options_type", out var textObject, optionIdentifier + "_ps"))
		{
			return textObject;
		}
		return GameTexts.FindText("str_campaign_options_type", optionIdentifier);
	}

	public static TextObject GetDescriptionOfOption(string optionIdentifier)
	{
		if (CheckIsPlayStation() && GameTexts.TryGetText("str_campaign_options_description", out var textObject, optionIdentifier + "_ps"))
		{
			return textObject;
		}
		return GameTexts.FindText("str_campaign_options_description", optionIdentifier);
	}

	private static bool CheckIsPlayStation()
	{
		return TaleWorlds.InputSystem.Input.ControllerType.IsPlaystation();
	}

	public int GetPriorityIndex()
	{
		return _priorityIndex;
	}

	public abstract CampaignOptionDataType GetDataType();

	public bool IsRelatedToDifficultyPreset()
	{
		return _isRelatedToDifficultyPreset;
	}

	public float GetValueFromDifficultyPreset(CampaignOptionsDifficultyPresets preset)
	{
		if (_onGetValueFromDifficultyPreset != null)
		{
			return _onGetValueFromDifficultyPreset(preset);
		}
		return preset switch
		{
			CampaignOptionsDifficultyPresets.Freebooter => 0f, 
			CampaignOptionsDifficultyPresets.Warrior => 1f, 
			CampaignOptionsDifficultyPresets.Bannerlord => 2f, 
			_ => 0f, 
		};
	}

	public CampaignOptionDisableStatus GetIsDisabledWithReason()
	{
		CampaignOptionDisableStatus? campaignOptionDisableStatus = _getIsDisabledWithReason?.Invoke();
		bool isDisabled = false;
		string text = string.Empty;
		float valueIfDisabled = -1f;
		if (_enableState == CampaignOptionEnableState.Disabled)
		{
			isDisabled = true;
			text = GameTexts.FindText("str_campaign_options_disabled_reason", _identifier).ToString();
		}
		else if (_enableState == CampaignOptionEnableState.DisabledLater)
		{
			text = GameTexts.FindText("str_campaign_options_persistency_warning").ToString();
		}
		if (campaignOptionDisableStatus.HasValue && campaignOptionDisableStatus.Value.IsDisabled)
		{
			isDisabled = true;
			if (!string.IsNullOrEmpty(campaignOptionDisableStatus.Value.DisabledReason))
			{
				if (!string.IsNullOrEmpty(text))
				{
					TextObject textObject = GameTexts.FindText("str_string_newline_string").CopyTextObject();
					textObject.SetTextVariable("STR1", text);
					textObject.SetTextVariable("STR2", campaignOptionDisableStatus.Value.DisabledReason);
					text = textObject.ToString();
				}
				else
				{
					text = campaignOptionDisableStatus.Value.DisabledReason;
				}
			}
			valueIfDisabled = campaignOptionDisableStatus.Value.ValueIfDisabled;
		}
		return new CampaignOptionDisableStatus(isDisabled, text, valueIfDisabled);
	}

	public string GetIdentifier()
	{
		return _identifier;
	}

	public CampaignOptionEnableState GetEnableState()
	{
		return _enableState;
	}

	public string GetName()
	{
		return _name.ToString();
	}

	public string GetDescription()
	{
		return _description.ToString();
	}

	public float GetValue()
	{
		return _getValue?.Invoke() ?? 0f;
	}

	public void SetValue(float value)
	{
		_setValue?.Invoke(value);
	}
}
