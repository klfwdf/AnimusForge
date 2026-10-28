using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Multiplayer.Admin;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.AdminPanel;

public class MultiplayerAdminPanelOptionGroupVM : ViewModel
{
	private readonly IAdminPanelOptionGroup _optionGroup;

	private readonly Func<IAdminPanelOption, MultiplayerAdminPanelOptionBaseVM> _onCreateOptionVM;

	private readonly Func<IAdminPanelAction, MultiplayerAdminPanelOptionBaseVM> _onCreateActionVM;

	private bool _requiresRestart;

	private string _groupName;

	private HintViewModel _requiresRestartHint;

	private MBBindingList<MultiplayerAdminPanelOptionBaseVM> _options;

	[DataSourceProperty]
	public bool RequiresRestart
	{
		get
		{
			return _requiresRestart;
		}
		set
		{
			if (value != _requiresRestart)
			{
				_requiresRestart = value;
				OnPropertyChangedWithValue(value, "RequiresRestart");
			}
		}
	}

	[DataSourceProperty]
	public string GroupName
	{
		get
		{
			return _groupName;
		}
		set
		{
			if (value != _groupName)
			{
				_groupName = value;
				OnPropertyChangedWithValue(value, "GroupName");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel RequiresRestartHint
	{
		get
		{
			return _requiresRestartHint;
		}
		set
		{
			if (value != _requiresRestartHint)
			{
				_requiresRestartHint = value;
				OnPropertyChangedWithValue(value, "RequiresRestartHint");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MultiplayerAdminPanelOptionBaseVM> Options
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

	public MultiplayerAdminPanelOptionGroupVM(IAdminPanelOptionGroup optionGroup, Func<IAdminPanelOption, MultiplayerAdminPanelOptionBaseVM> onCreateOptionVm, Func<IAdminPanelAction, MultiplayerAdminPanelOptionBaseVM> onCreateActionVm)
	{
		_optionGroup = optionGroup;
		_onCreateOptionVM = onCreateOptionVm;
		_onCreateActionVM = onCreateActionVm;
		Options = new MBBindingList<MultiplayerAdminPanelOptionBaseVM>();
		for (int i = 0; i < _optionGroup.Options.Count; i++)
		{
			MultiplayerAdminPanelOptionBaseVM multiplayerAdminPanelOptionBaseVM = _onCreateOptionVM?.Invoke(optionGroup.Options[i]);
			if (multiplayerAdminPanelOptionBaseVM != null)
			{
				Options.Add(multiplayerAdminPanelOptionBaseVM);
			}
			else
			{
				Debug.FailedAssert("Failed to create view model for option type: " + optionGroup.Options[i].GetType().Name, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\AdminPanel\\MultiplayerAdminPanelOptionGroupVM.cs", ".ctor", 34);
			}
		}
		for (int j = 0; j < _optionGroup.Actions.Count; j++)
		{
			MultiplayerAdminPanelOptionBaseVM multiplayerAdminPanelOptionBaseVM2 = _onCreateActionVM?.Invoke(optionGroup.Actions[j]);
			if (multiplayerAdminPanelOptionBaseVM2 != null)
			{
				Options.Add(multiplayerAdminPanelOptionBaseVM2);
			}
			else
			{
				Debug.FailedAssert("Failed to create view model for option type: " + optionGroup.Options[j].GetType().Name, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\AdminPanel\\MultiplayerAdminPanelOptionGroupVM.cs", ".ctor", 48);
			}
		}
		RequiresRestart = _optionGroup.RequiresRestart;
		RequiresRestartHint = new HintViewModel();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		RequiresRestartHint.HintText = (RequiresRestart ? new TextObject("{=sTVcpXkf}All options under this category requires restart.") : TextObject.GetEmpty());
		GroupName = _optionGroup.Name.ToString();
		Options.ApplyActionOnAllItems(delegate(MultiplayerAdminPanelOptionBaseVM o)
		{
			o.RefreshValues();
		});
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		Options.ApplyActionOnAllItems(delegate(MultiplayerAdminPanelOptionBaseVM o)
		{
			o.OnFinalize();
		});
	}
}
