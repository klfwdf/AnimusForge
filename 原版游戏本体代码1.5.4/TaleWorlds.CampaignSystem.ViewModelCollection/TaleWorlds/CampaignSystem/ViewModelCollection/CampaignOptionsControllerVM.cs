using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection;

public class CampaignOptionsControllerVM : ViewModel
{
	private class CampaignOptionComparer : IComparer<CampaignOptionItemVM>
	{
		public int Compare(CampaignOptionItemVM x, CampaignOptionItemVM y)
		{
			int priorityIndex = x.OptionData.GetPriorityIndex();
			int priorityIndex2 = y.OptionData.GetPriorityIndex();
			return priorityIndex.CompareTo(priorityIndex2);
		}
	}

	private const string _difficultyPresetsId = "DifficultyPresets";

	internal const int AutosaveDisableValue = -1;

	private SelectionCampaignOptionData _difficultyPreset;

	private Dictionary<string, CampaignOptionItemVM> _optionItems;

	private bool _isUpdatingPresetData;

	private List<CampaignOptionItemVM> _difficultyPresetRelatedOptions;

	private MBBindingList<CampaignOptionItemVM> _options;

	[DataSourceProperty]
	public MBBindingList<CampaignOptionItemVM> Options
	{
		get
		{
			return _options;
		}
		set
		{
			if (value != _options)
			{
				_options = value;
				OnPropertyChangedWithValue(value, "Options");
			}
		}
	}

	public CampaignOptionsControllerVM(MBBindingList<CampaignOptionItemVM> options)
	{
		_optionItems = new Dictionary<string, CampaignOptionItemVM>();
		Options = options;
		_difficultyPreset = Options.FirstOrDefault((CampaignOptionItemVM x) => x.OptionData.GetIdentifier() == "DifficultyPresets")?.OptionData as SelectionCampaignOptionData;
		Options.Sort(new CampaignOptionComparer());
		for (int num = 0; num < Options.Count; num++)
		{
			_optionItems.Add(Options[num].OptionData.GetIdentifier(), Options[num]);
		}
		Options.ApplyActionOnAllItems(delegate(CampaignOptionItemVM x)
		{
			x.RefreshDisabledStatus();
		});
		Options.ApplyActionOnAllItems(delegate(CampaignOptionItemVM x)
		{
			x.SetOnValueChangedCallback(OnOptionChanged);
		});
		_difficultyPresetRelatedOptions = Options.Where((CampaignOptionItemVM x) => x.OptionData.IsRelatedToDifficultyPreset()).ToList();
		UpdatePresetData(_difficultyPresetRelatedOptions.FirstOrDefault());
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignOptionsManager.ClearCachedOptions();
	}

	private void OnOptionChanged(CampaignOptionItemVM optionVM)
	{
		UpdatePresetData(optionVM);
		Options.ApplyActionOnAllItems(delegate(CampaignOptionItemVM x)
		{
			x.RefreshDisabledStatus();
		});
	}

	private void UpdatePresetData(CampaignOptionItemVM changedOption)
	{
		if (_isUpdatingPresetData || changedOption == null || !_optionItems.TryGetValue(_difficultyPreset.GetIdentifier(), out var value))
		{
			return;
		}
		_isUpdatingPresetData = true;
		if (changedOption.OptionData == _difficultyPreset)
		{
			foreach (CampaignOptionItemVM difficultyPresetRelatedOption in _difficultyPresetRelatedOptions)
			{
				string identifier = difficultyPresetRelatedOption.OptionData.GetIdentifier();
				CampaignOptionsDifficultyPresets preset = (CampaignOptionsDifficultyPresets)_difficultyPreset.GetValue();
				float valueFromDifficultyPreset = difficultyPresetRelatedOption.OptionData.GetValueFromDifficultyPreset(preset);
				if (_optionItems.TryGetValue(identifier, out var value2) && !value2.IsDisabled)
				{
					value2.SetValue(valueFromDifficultyPreset);
				}
			}
		}
		else if (_difficultyPresetRelatedOptions.Any((CampaignOptionItemVM x) => x.OptionData.GetIdentifier() == changedOption.OptionData.GetIdentifier()))
		{
			CampaignOptionItemVM campaignOptionItemVM = _difficultyPresetRelatedOptions[0];
			CampaignOptionsDifficultyPresets campaignOptionsDifficultyPresets = FindOptionPresetForValue(campaignOptionItemVM.OptionData);
			bool flag = true;
			for (int num = 0; num < _difficultyPresetRelatedOptions.Count; num++)
			{
				if (FindOptionPresetForValue(_difficultyPresetRelatedOptions[num].OptionData) != campaignOptionsDifficultyPresets)
				{
					flag = false;
					break;
				}
			}
			value.SetValue(flag ? ((float)campaignOptionsDifficultyPresets) : 3f);
		}
		_isUpdatingPresetData = false;
	}

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
}
