using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.Admin.Internal;

internal class AdminPanelMultiSelectionOption : AdminPanelOption<IAdminPanelMultiSelectionItem>, IAdminPanelMultiSelectionOption, IAdminPanelOption<IAdminPanelMultiSelectionItem>, IAdminPanelOption
{
	protected IAdminPanelMultiSelectionItem _selectedOption;

	protected MBList<IAdminPanelMultiSelectionItem> _availableOptions;

	public AdminPanelMultiSelectionOption(string uniqueId)
		: base(uniqueId)
	{
		_availableOptions = new MBList<IAdminPanelMultiSelectionItem>();
	}

	protected override bool AreEqualValues(IAdminPanelMultiSelectionItem first, IAdminPanelMultiSelectionItem second)
	{
		return first == second;
	}

	protected override IAdminPanelMultiSelectionItem GetOptionValue(MultiplayerOptions.OptionType optionType, MultiplayerOptions.MultiplayerOptionsAccessMode accessMode = MultiplayerOptions.MultiplayerOptionsAccessMode.DefaultMapOptions)
	{
		string strValue = optionType.GetStrValue(accessMode);
		for (int i = 0; i < _availableOptions.Count; i++)
		{
			if (_availableOptions[i].Value == strValue)
			{
				return _availableOptions[i];
			}
		}
		return null;
	}

	protected override void OnValueChanged(IAdminPanelMultiSelectionItem previousValue, IAdminPanelMultiSelectionItem newValue)
	{
		_selectedOption = newValue;
		base.OnValueChanged(previousValue, newValue);
	}

	protected override bool OnGetCanRevertToDefaultValue()
	{
		return _availableOptions.Contains(base.DefaultValue);
	}

	public virtual AdminPanelMultiSelectionOption BuildAvailableOptions(MBReadOnlyList<IAdminPanelMultiSelectionItem> options)
	{
		_availableOptions.Clear();
		if (options != null && options.Count > 0)
		{
			for (int i = 0; i < options.Count; i++)
			{
				_availableOptions.Add(options[i]);
			}
		}
		OnRefresh();
		return this;
	}

	public virtual AdminPanelMultiSelectionOption BuildAvailableOptions(MultiplayerOptions.OptionType optionType, bool buildDefaultValue = true)
	{
		_availableOptions.Clear();
		string strValue = optionType.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.DefaultMapOptions);
		List<string> multiplayerOptionsList = MultiplayerOptions.Instance.GetMultiplayerOptionsList(optionType);
		if (multiplayerOptionsList != null && multiplayerOptionsList.Count > 0)
		{
			for (int i = 0; i < multiplayerOptionsList.Count; i++)
			{
				AdminPanelMultiSelectionItem adminPanelMultiSelectionItem = new AdminPanelMultiSelectionItem(multiplayerOptionsList[i], null);
				_availableOptions.Add(adminPanelMultiSelectionItem);
				if (buildDefaultValue && adminPanelMultiSelectionItem.Value == strValue)
				{
					BuildDefaultValue(adminPanelMultiSelectionItem);
					BuildInitialValue(adminPanelMultiSelectionItem);
				}
			}
		}
		OnRefresh();
		return this;
	}

	public MBReadOnlyList<IAdminPanelMultiSelectionItem> GetAvailableOptions()
	{
		return _availableOptions;
	}
}
