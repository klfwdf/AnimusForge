using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection
{
	// Token: 0x0200000B RID: 11
	public class CampaignOptionsControllerVM : ViewModel
	{
		// Token: 0x060000A1 RID: 161 RVA: 0x00003A1C File Offset: 0x00001C1C
		public CampaignOptionsControllerVM(MBBindingList<CampaignOptionItemVM> options)
		{
			this._optionItems = new Dictionary<string, CampaignOptionItemVM>();
			this.Options = options;
			CampaignOptionItemVM campaignOptionItemVM = this.Options.FirstOrDefault((CampaignOptionItemVM x) => x.OptionData.GetIdentifier() == "DifficultyPresets");
			this._difficultyPreset = ((campaignOptionItemVM != null) ? campaignOptionItemVM.OptionData : null) as SelectionCampaignOptionData;
			this.Options.Sort(new CampaignOptionsControllerVM.CampaignOptionComparer());
			for (int i = 0; i < this.Options.Count; i++)
			{
				this._optionItems.Add(this.Options[i].OptionData.GetIdentifier(), this.Options[i]);
			}
			this.Options.ApplyActionOnAllItems(delegate(CampaignOptionItemVM x)
			{
				x.RefreshDisabledStatus();
			});
			this.Options.ApplyActionOnAllItems(delegate(CampaignOptionItemVM x)
			{
				x.SetOnValueChangedCallback(new Action<CampaignOptionItemVM>(this.OnOptionChanged));
			});
			this._difficultyPresetRelatedOptions = (from x in this.Options
				where x.OptionData.IsRelatedToDifficultyPreset()
				select x).ToList<CampaignOptionItemVM>();
			this.UpdatePresetData(this._difficultyPresetRelatedOptions.FirstOrDefault<CampaignOptionItemVM>());
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x00003B5C File Offset: 0x00001D5C
		public override void OnFinalize()
		{
			base.OnFinalize();
			CampaignOptionsManager.ClearCachedOptions();
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x00003B69 File Offset: 0x00001D69
		private void OnOptionChanged(CampaignOptionItemVM optionVM)
		{
			this.UpdatePresetData(optionVM);
			this.Options.ApplyActionOnAllItems(delegate(CampaignOptionItemVM x)
			{
				x.RefreshDisabledStatus();
			});
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x00003B9C File Offset: 0x00001D9C
		private void UpdatePresetData(CampaignOptionItemVM changedOption)
		{
			if (this._isUpdatingPresetData || changedOption == null)
			{
				return;
			}
			CampaignOptionItemVM campaignOptionItemVM;
			if (!this._optionItems.TryGetValue(this._difficultyPreset.GetIdentifier(), out campaignOptionItemVM))
			{
				return;
			}
			this._isUpdatingPresetData = true;
			if (changedOption.OptionData == this._difficultyPreset)
			{
				using (List<CampaignOptionItemVM>.Enumerator enumerator = this._difficultyPresetRelatedOptions.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						CampaignOptionItemVM campaignOptionItemVM2 = enumerator.Current;
						string identifier = campaignOptionItemVM2.OptionData.GetIdentifier();
						CampaignOptionsDifficultyPresets preset = (CampaignOptionsDifficultyPresets)this._difficultyPreset.GetValue();
						float valueFromDifficultyPreset = campaignOptionItemVM2.OptionData.GetValueFromDifficultyPreset(preset);
						CampaignOptionItemVM campaignOptionItemVM3;
						if (this._optionItems.TryGetValue(identifier, out campaignOptionItemVM3) && !campaignOptionItemVM3.IsDisabled)
						{
							campaignOptionItemVM3.SetValue(valueFromDifficultyPreset);
						}
					}
					goto IL_156;
				}
			}
			if (this._difficultyPresetRelatedOptions.Any((CampaignOptionItemVM x) => x.OptionData.GetIdentifier() == changedOption.OptionData.GetIdentifier()))
			{
				CampaignOptionItemVM campaignOptionItemVM4 = this._difficultyPresetRelatedOptions[0];
				CampaignOptionsDifficultyPresets campaignOptionsDifficultyPresets = this.FindOptionPresetForValue(campaignOptionItemVM4.OptionData);
				bool flag = true;
				for (int i = 0; i < this._difficultyPresetRelatedOptions.Count; i++)
				{
					if (this.FindOptionPresetForValue(this._difficultyPresetRelatedOptions[i].OptionData) != campaignOptionsDifficultyPresets)
					{
						flag = false;
						break;
					}
				}
				campaignOptionItemVM.SetValue(flag ? ((float)campaignOptionsDifficultyPresets) : 3f);
			}
			IL_156:
			this._isUpdatingPresetData = false;
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x00003D18 File Offset: 0x00001F18
		private CampaignOptionsDifficultyPresets FindOptionPresetForValue(ICampaignOptionData option)
		{
			float value = option.GetValue();
			if (option.GetValueFromDifficultyPreset(CampaignOptionsDifficultyPresets.Freebooter) == value)
			{
				return CampaignOptionsDifficultyPresets.Freebooter;
			}
			if (option.GetValueFromDifficultyPreset(CampaignOptionsDifficultyPresets.Warrior) == value)
			{
				return CampaignOptionsDifficultyPresets.Warrior;
			}
			if (option.GetValueFromDifficultyPreset(CampaignOptionsDifficultyPresets.Bannerlord) == value)
			{
				return CampaignOptionsDifficultyPresets.Bannerlord;
			}
			return CampaignOptionsDifficultyPresets.Custom;
		}

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x060000A6 RID: 166 RVA: 0x00003D51 File Offset: 0x00001F51
		// (set) Token: 0x060000A7 RID: 167 RVA: 0x00003D59 File Offset: 0x00001F59
		[DataSourceProperty]
		public MBBindingList<CampaignOptionItemVM> Options
		{
			get
			{
				return this._options;
			}
			set
			{
				if (value != this._options)
				{
					this._options = value;
					base.OnPropertyChangedWithValue<MBBindingList<CampaignOptionItemVM>>(value, "Options");
				}
			}
		}

		// Token: 0x04000053 RID: 83
		private const string _difficultyPresetsId = "DifficultyPresets";

		// Token: 0x04000054 RID: 84
		internal const int AutosaveDisableValue = -1;

		// Token: 0x04000055 RID: 85
		private SelectionCampaignOptionData _difficultyPreset;

		// Token: 0x04000056 RID: 86
		private Dictionary<string, CampaignOptionItemVM> _optionItems;

		// Token: 0x04000057 RID: 87
		private bool _isUpdatingPresetData;

		// Token: 0x04000058 RID: 88
		private List<CampaignOptionItemVM> _difficultyPresetRelatedOptions;

		// Token: 0x04000059 RID: 89
		private MBBindingList<CampaignOptionItemVM> _options;

		// Token: 0x02000162 RID: 354
		private class CampaignOptionComparer : IComparer<CampaignOptionItemVM>
		{
			// Token: 0x060021CA RID: 8650 RVA: 0x0007A1FC File Offset: 0x000783FC
			public int Compare(CampaignOptionItemVM x, CampaignOptionItemVM y)
			{
				int priorityIndex = x.OptionData.GetPriorityIndex();
				int priorityIndex2 = y.OptionData.GetPriorityIndex();
				return priorityIndex.CompareTo(priorityIndex2);
			}
		}
	}
}
