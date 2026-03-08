using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection
{
	// Token: 0x02000017 RID: 23
	public class SelectionCampaignOptionData : CampaignOptionData
	{
		// Token: 0x17000039 RID: 57
		// (get) Token: 0x060000EF RID: 239 RVA: 0x000046D1 File Offset: 0x000028D1
		// (set) Token: 0x060000F0 RID: 240 RVA: 0x000046D9 File Offset: 0x000028D9
		public List<TextObject> Selections { get; private set; }

		// Token: 0x060000F1 RID: 241 RVA: 0x000046E4 File Offset: 0x000028E4
		public SelectionCampaignOptionData(string identifier, int priorityIndex, CampaignOptionEnableState enableState, Func<float> getValue, Action<float> setValue, List<TextObject> customSelectionTexts = null, Func<CampaignOptionDisableStatus> getIsDisabledWithReason = null, bool isRelatedToDifficultyPreset = false, Func<float, CampaignOptionsDifficultyPresets> onGetDifficultyPresetFromValue = null, Func<CampaignOptionsDifficultyPresets, float> onGetValueFromDifficultyPreset = null)
			: base(identifier, priorityIndex, enableState, getValue, setValue, getIsDisabledWithReason, isRelatedToDifficultyPreset, onGetDifficultyPresetFromValue, onGetValueFromDifficultyPreset)
		{
			if (customSelectionTexts != null)
			{
				this.Selections = customSelectionTexts;
				return;
			}
			this.Selections = this.GetPresetTexts(identifier);
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x00004720 File Offset: 0x00002920
		private List<TextObject> GetPresetTexts(string identifier)
		{
			List<TextObject> list = new List<TextObject>();
			foreach (object obj in Enum.GetValues(typeof(CampaignOptions.Difficulty)))
			{
				list.Add(GameTexts.FindText("str_campaign_options_type_" + identifier, obj.ToString()));
			}
			return list;
		}

		// Token: 0x060000F3 RID: 243 RVA: 0x0000479C File Offset: 0x0000299C
		public override CampaignOptionDataType GetDataType()
		{
			return CampaignOptionDataType.Selection;
		}
	}
}
