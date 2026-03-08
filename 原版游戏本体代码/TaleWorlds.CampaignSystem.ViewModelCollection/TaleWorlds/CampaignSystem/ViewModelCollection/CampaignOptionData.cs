using System;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection
{
	// Token: 0x02000011 RID: 17
	public abstract class CampaignOptionData : ICampaignOptionData
	{
		// Token: 0x060000C4 RID: 196 RVA: 0x00004324 File Offset: 0x00002524
		public CampaignOptionData(string identifier, int priorityIndex, CampaignOptionEnableState enableState, Func<float> getValue, Action<float> setValue, Func<CampaignOptionDisableStatus> getIsDisabledWithReason = null, bool isRelatedToDifficultyPreset = false, Func<float, CampaignOptionsDifficultyPresets> onGetDifficultyPresetFromValue = null, Func<CampaignOptionsDifficultyPresets, float> onGetValueFromDifficultyPreset = null)
		{
			this._priorityIndex = priorityIndex;
			this._identifier = identifier;
			this._isRelatedToDifficultyPreset = isRelatedToDifficultyPreset;
			this._getIsDisabledWithReason = getIsDisabledWithReason;
			this._onGetDifficultyPresetFromValue = onGetDifficultyPresetFromValue;
			this._onGetValueFromDifficultyPreset = onGetValueFromDifficultyPreset;
			this._enableState = enableState;
			this._name = CampaignOptionData.GetNameOfOption(identifier);
			this._description = CampaignOptionData.GetDescriptionOfOption(identifier);
			this._getValue = getValue;
			this._setValue = setValue;
		}

		// Token: 0x060000C5 RID: 197 RVA: 0x00004394 File Offset: 0x00002594
		public static TextObject GetNameOfOption(string optionIdentifier)
		{
			TextObject result;
			if (CampaignOptionData.CheckIsPlayStation() && GameTexts.TryGetText("str_campaign_options_type", out result, optionIdentifier + "_ps"))
			{
				return result;
			}
			return GameTexts.FindText("str_campaign_options_type", optionIdentifier);
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x000043D0 File Offset: 0x000025D0
		public static TextObject GetDescriptionOfOption(string optionIdentifier)
		{
			TextObject result;
			if (CampaignOptionData.CheckIsPlayStation() && GameTexts.TryGetText("str_campaign_options_description", out result, optionIdentifier + "_ps"))
			{
				return result;
			}
			return GameTexts.FindText("str_campaign_options_description", optionIdentifier);
		}

		// Token: 0x060000C7 RID: 199 RVA: 0x0000440A File Offset: 0x0000260A
		private static bool CheckIsPlayStation()
		{
			return Input.ControllerType.IsPlaystation();
		}

		// Token: 0x060000C8 RID: 200 RVA: 0x00004416 File Offset: 0x00002616
		public int GetPriorityIndex()
		{
			return this._priorityIndex;
		}

		// Token: 0x060000C9 RID: 201
		public abstract CampaignOptionDataType GetDataType();

		// Token: 0x060000CA RID: 202 RVA: 0x0000441E File Offset: 0x0000261E
		public bool IsRelatedToDifficultyPreset()
		{
			return this._isRelatedToDifficultyPreset;
		}

		// Token: 0x060000CB RID: 203 RVA: 0x00004428 File Offset: 0x00002628
		public float GetValueFromDifficultyPreset(CampaignOptionsDifficultyPresets preset)
		{
			if (this._onGetValueFromDifficultyPreset != null)
			{
				return this._onGetValueFromDifficultyPreset(preset);
			}
			switch (preset)
			{
			case CampaignOptionsDifficultyPresets.Freebooter:
				return 0f;
			case CampaignOptionsDifficultyPresets.Warrior:
				return 1f;
			case CampaignOptionsDifficultyPresets.Bannerlord:
				return 2f;
			default:
				return 0f;
			}
		}

		// Token: 0x060000CC RID: 204 RVA: 0x00004478 File Offset: 0x00002678
		public CampaignOptionDisableStatus GetIsDisabledWithReason()
		{
			Func<CampaignOptionDisableStatus> getIsDisabledWithReason = this._getIsDisabledWithReason;
			CampaignOptionDisableStatus? campaignOptionDisableStatus = ((getIsDisabledWithReason != null) ? new CampaignOptionDisableStatus?(getIsDisabledWithReason()) : null);
			bool isDisabled = false;
			string text = string.Empty;
			float valueIfDisabled = -1f;
			if (this._enableState == CampaignOptionEnableState.Disabled)
			{
				isDisabled = true;
				text = GameTexts.FindText("str_campaign_options_disabled_reason", this._identifier).ToString();
			}
			else if (this._enableState == CampaignOptionEnableState.DisabledLater)
			{
				text = GameTexts.FindText("str_campaign_options_persistency_warning", null).ToString();
			}
			if (campaignOptionDisableStatus != null && campaignOptionDisableStatus.Value.IsDisabled)
			{
				isDisabled = true;
				if (!string.IsNullOrEmpty(campaignOptionDisableStatus.Value.DisabledReason))
				{
					if (!string.IsNullOrEmpty(text))
					{
						TextObject textObject = GameTexts.FindText("str_string_newline_string", null).CopyTextObject();
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

		// Token: 0x060000CD RID: 205 RVA: 0x0000459E File Offset: 0x0000279E
		public string GetIdentifier()
		{
			return this._identifier;
		}

		// Token: 0x060000CE RID: 206 RVA: 0x000045A6 File Offset: 0x000027A6
		public CampaignOptionEnableState GetEnableState()
		{
			return this._enableState;
		}

		// Token: 0x060000CF RID: 207 RVA: 0x000045AE File Offset: 0x000027AE
		public string GetName()
		{
			return this._name.ToString();
		}

		// Token: 0x060000D0 RID: 208 RVA: 0x000045BB File Offset: 0x000027BB
		public string GetDescription()
		{
			return this._description.ToString();
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x000045C8 File Offset: 0x000027C8
		public float GetValue()
		{
			Func<float> getValue = this._getValue;
			if (getValue == null)
			{
				return 0f;
			}
			return getValue();
		}

		// Token: 0x060000D2 RID: 210 RVA: 0x000045DF File Offset: 0x000027DF
		public void SetValue(float value)
		{
			Action<float> setValue = this._setValue;
			if (setValue == null)
			{
				return;
			}
			setValue(value);
		}

		// Token: 0x04000062 RID: 98
		private int _priorityIndex;

		// Token: 0x04000063 RID: 99
		private string _identifier;

		// Token: 0x04000064 RID: 100
		private bool _isRelatedToDifficultyPreset;

		// Token: 0x04000065 RID: 101
		private CampaignOptionEnableState _enableState;

		// Token: 0x04000066 RID: 102
		private TextObject _name;

		// Token: 0x04000067 RID: 103
		private TextObject _description;

		// Token: 0x04000068 RID: 104
		private Func<CampaignOptionDisableStatus> _getIsDisabledWithReason;

		// Token: 0x04000069 RID: 105
		protected Func<float> _getValue;

		// Token: 0x0400006A RID: 106
		protected Action<float> _setValue;

		// Token: 0x0400006B RID: 107
		protected Func<float, CampaignOptionsDifficultyPresets> _onGetDifficultyPresetFromValue;

		// Token: 0x0400006C RID: 108
		protected Func<CampaignOptionsDifficultyPresets, float> _onGetValueFromDifficultyPreset;
	}
}
