using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Multiplayer.Admin;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.AdminPanel;

public abstract class MultiplayerAdminPanelOptionBaseVM : ViewModel
{
	protected readonly IAdminPanelOption _option;

	private bool _isRequired;

	private bool _isDisabled;

	private bool _isDirty;

	private bool _canResetToDefault;

	private bool _isFilteredOut;

	private bool _requiresRestart;

	private string _optionTitle;

	private string _optionDescription;

	private HintViewModel _disabledHint;

	private HintViewModel _descriptionHint;

	private HintViewModel _requiresRestartHint;

	private HintViewModel _isDirtyHint;

	private HintViewModel _restoreToDefaultsHint;

	[DataSourceProperty]
	public bool IsRequired
	{
		get
		{
			return _isRequired;
		}
		set
		{
			if (value != _isRequired)
			{
				_isRequired = value;
				OnPropertyChangedWithValue(value, "IsRequired");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDisabled
	{
		get
		{
			return _isDisabled;
		}
		set
		{
			if (value != _isDisabled)
			{
				_isDisabled = value;
				OnPropertyChangedWithValue(value, "IsDisabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDirty
	{
		get
		{
			return _isDirty;
		}
		set
		{
			if (value != _isDirty)
			{
				_isDirty = value;
				OnPropertyChangedWithValue(value, "IsDirty");
			}
		}
	}

	[DataSourceProperty]
	public bool CanResetToDefault
	{
		get
		{
			return _canResetToDefault;
		}
		set
		{
			if (value != _canResetToDefault)
			{
				_canResetToDefault = value;
				OnPropertyChangedWithValue(value, "CanResetToDefault");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFilteredOut
	{
		get
		{
			return _isFilteredOut;
		}
		set
		{
			if (value != _isFilteredOut)
			{
				_isFilteredOut = value;
				OnPropertyChangedWithValue(value, "IsFilteredOut");
			}
		}
	}

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
	public string OptionTitle
	{
		get
		{
			return _optionTitle;
		}
		set
		{
			if (value != _optionTitle)
			{
				_optionTitle = value;
				OnPropertyChangedWithValue(value, "OptionTitle");
			}
		}
	}

	[DataSourceProperty]
	public string OptionDescription
	{
		get
		{
			return _optionDescription;
		}
		set
		{
			if (value != _optionDescription)
			{
				_optionDescription = value;
				OnPropertyChangedWithValue(value, "OptionDescription");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel DisabledHint
	{
		get
		{
			return _disabledHint;
		}
		set
		{
			if (value != _disabledHint)
			{
				_disabledHint = value;
				OnPropertyChangedWithValue(value, "DisabledHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel DescriptionHint
	{
		get
		{
			return _descriptionHint;
		}
		set
		{
			if (value != _descriptionHint)
			{
				_descriptionHint = value;
				OnPropertyChangedWithValue(value, "DescriptionHint");
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
	public HintViewModel IsDirtyHint
	{
		get
		{
			return _isDirtyHint;
		}
		set
		{
			if (value != _isDirtyHint)
			{
				_isDirtyHint = value;
				OnPropertyChangedWithValue(value, "IsDirtyHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel RestoreToDefaultsHint
	{
		get
		{
			return _restoreToDefaultsHint;
		}
		set
		{
			if (value != _restoreToDefaultsHint)
			{
				_restoreToDefaultsHint = value;
				OnPropertyChangedWithValue(value, "RestoreToDefaultsHint");
			}
		}
	}

	public static event Action<MultiplayerAdminPanelOptionBaseVM> OnOptionRefreshed;

	protected MultiplayerAdminPanelOptionBaseVM(IAdminPanelOption option)
	{
		_option = option;
		_option?.SetOnRefreshCallback(OnOptionRefreshedAux);
		RequiresRestart = option?.RequiresMissionRestart ?? false;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		OptionTitle = _option?.Name?.ToString();
		OptionDescription = _option?.Description?.ToString();
		if (!string.IsNullOrEmpty(_option?.Description))
		{
			DescriptionHint = new HintViewModel(new TextObject("{=!}" + _option.Description));
		}
		else
		{
			DescriptionHint = null;
		}
		RequiresRestartHint = new HintViewModel(new TextObject("{=MxRJ4CWL}This option won't take effect until next mission."));
		IsDirtyHint = new HintViewModel(new TextObject("{=ftM2TjQ5}Revert changes"));
		RestoreToDefaultsHint = new HintViewModel(new TextObject("{=36ll5uSI}Restore to defaults"));
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		_option?.SetOnRefreshCallback(null);
	}

	private void OnOptionRefreshedAux()
	{
		MultiplayerAdminPanelOptionBaseVM.OnOptionRefreshed?.Invoke(this);
	}

	public virtual void UpdateValues()
	{
		IAdminPanelOption option = _option;
		IsFilteredOut = option != null && !option.GetIsAvailable();
		IsDirty = _option?.IsDirty ?? false;
		CanResetToDefault = _option?.CanRevertToDefaultValue ?? false;
		string reason = string.Empty;
		IsDisabled = _option?.GetIsDisabled(out reason) ?? false;
		IsRequired = _option?.IsRequired ?? false;
		if (!string.IsNullOrEmpty(reason))
		{
			DisabledHint = new HintViewModel(new TextObject("{=!}" + reason));
		}
		else
		{
			DisabledHint = null;
		}
	}

	public virtual void ExecuteRevertChanges()
	{
		_option?.RevertChanges();
	}

	public virtual void ExecuteRestoreDefaults()
	{
		_option?.RestoreDefaults();
	}
}
