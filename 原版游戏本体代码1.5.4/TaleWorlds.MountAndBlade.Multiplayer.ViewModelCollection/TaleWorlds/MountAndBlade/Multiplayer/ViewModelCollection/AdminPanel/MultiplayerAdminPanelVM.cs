using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Multiplayer.Admin;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.AdminPanel;

public class MultiplayerAdminPanelVM : ViewModel
{
	private readonly Action<bool> _onEscapeMenuToggled;

	private readonly MBReadOnlyList<IAdminPanelOptionProvider> _optionProviders;

	private readonly Func<IAdminPanelOption, MultiplayerAdminPanelOptionBaseVM> _onCreateOptionViewModel;

	private readonly Func<IAdminPanelAction, MultiplayerAdminPanelOptionBaseVM> _onCreateActionViewModel;

	private bool _areOptionValuesDirty;

	private bool _isApplyDisabled;

	private string _titleText;

	private string _cancelText;

	private string _applyText;

	private string _startMissionText;

	private HintViewModel _applyDisabledHint;

	private MBBindingList<MultiplayerAdminPanelOptionGroupVM> _optionGroups;

	[DataSourceProperty]
	public bool IsApplyDisabled
	{
		get
		{
			return _isApplyDisabled;
		}
		set
		{
			if (value != _isApplyDisabled)
			{
				_isApplyDisabled = value;
				OnPropertyChangedWithValue(value, "IsApplyDisabled");
			}
		}
	}

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public string CancelText
	{
		get
		{
			return _cancelText;
		}
		set
		{
			if (value != _cancelText)
			{
				_cancelText = value;
				OnPropertyChangedWithValue(value, "CancelText");
			}
		}
	}

	[DataSourceProperty]
	public string ApplyText
	{
		get
		{
			return _applyText;
		}
		set
		{
			if (value != _applyText)
			{
				_applyText = value;
				OnPropertyChangedWithValue(value, "ApplyText");
			}
		}
	}

	[DataSourceProperty]
	public string StartMissionText
	{
		get
		{
			return _startMissionText;
		}
		set
		{
			if (value != _startMissionText)
			{
				_startMissionText = value;
				OnPropertyChangedWithValue(value, "StartMissionText");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ApplyDisabledHint
	{
		get
		{
			return _applyDisabledHint;
		}
		set
		{
			if (value != _applyDisabledHint)
			{
				_applyDisabledHint = value;
				OnPropertyChangedWithValue(value, "ApplyDisabledHint");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MultiplayerAdminPanelOptionGroupVM> OptionGroups
	{
		get
		{
			return _optionGroups;
		}
		set
		{
			if (value != _optionGroups)
			{
				_optionGroups = value;
				OnPropertyChangedWithValue(value, "OptionGroups");
			}
		}
	}

	public MultiplayerAdminPanelVM(Action<bool> onEscapeMenuToggled, MBReadOnlyList<IAdminPanelOptionProvider> optionProviders, Func<IAdminPanelOption, MultiplayerAdminPanelOptionBaseVM> onGetOptionViewModel, Func<IAdminPanelAction, MultiplayerAdminPanelOptionBaseVM> onGetActionViewModel)
	{
		_onEscapeMenuToggled = onEscapeMenuToggled;
		_optionProviders = optionProviders;
		_onCreateOptionViewModel = onGetOptionViewModel;
		_onCreateActionViewModel = onGetActionViewModel;
		OptionGroups = new MBBindingList<MultiplayerAdminPanelOptionGroupVM>();
		InitializeOptions();
		InitializeCallbacks();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = new TextObject("{=xILeUbY3}Admin Panel").ToString();
		CancelText = new TextObject("{=3CpNUnVl}Cancel").ToString();
		ApplyText = new TextObject("{=WZQnNSwV}Apply Changes").ToString();
		StartMissionText = new TextObject("{=kwo09aDm}Apply and Start Mission").ToString();
		ApplyDisabledHint = new HintViewModel(new TextObject("{=TrY4VS1R}Please select valid values for options."));
		OptionGroups.ApplyActionOnAllItems(delegate(MultiplayerAdminPanelOptionGroupVM o)
		{
			o.RefreshValues();
		});
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		if (_optionProviders != null)
		{
			for (int i = 0; i < _optionProviders.Count; i++)
			{
				_optionProviders[i].OnFinalize();
			}
		}
		FinalizeCallbacks();
		OptionGroups.ApplyActionOnAllItems(delegate(MultiplayerAdminPanelOptionGroupVM o)
		{
			o.OnFinalize();
		});
	}

	public void OnTick(float dt)
	{
		if (_optionProviders != null)
		{
			for (int i = 0; i < _optionProviders.Count; i++)
			{
				_optionProviders[i].OnTick(dt);
			}
		}
		if (_areOptionValuesDirty)
		{
			UpdateOptionValues();
			_areOptionValuesDirty = false;
		}
	}

	private void InitializeCallbacks()
	{
		MultiplayerAdminPanelOptionBaseVM.OnOptionRefreshed += OnOptionChanged;
	}

	private void FinalizeCallbacks()
	{
		MultiplayerAdminPanelOptionBaseVM.OnOptionRefreshed -= OnOptionChanged;
	}

	private void InitializeOptions()
	{
		OptionGroups.Clear();
		if (_optionProviders != null)
		{
			foreach (IAdminPanelOptionProvider optionProvider in _optionProviders)
			{
				foreach (IAdminPanelOptionGroup optionGroup in optionProvider.GetOptionGroups())
				{
					MultiplayerAdminPanelOptionGroupVM item = new MultiplayerAdminPanelOptionGroupVM(optionGroup, _onCreateOptionViewModel, _onCreateActionViewModel);
					OptionGroups.Add(item);
				}
			}
		}
		UpdateOptionValues();
	}

	private void OnOptionChanged(MultiplayerAdminPanelOptionBaseVM option)
	{
		_areOptionValuesDirty = true;
	}

	private void UpdateOptionValues()
	{
		bool isApplyDisabled = false;
		foreach (MultiplayerAdminPanelOptionGroupVM optionGroup in OptionGroups)
		{
			foreach (MultiplayerAdminPanelOptionBaseVM option in optionGroup.Options)
			{
				option.UpdateValues();
				if (option.IsRequired && option.IsDisabled)
				{
					isApplyDisabled = true;
				}
			}
		}
		IsApplyDisabled = isApplyDisabled;
	}

	public void ExecuteApplyChanges()
	{
		if (_optionProviders == null)
		{
			return;
		}
		foreach (IAdminPanelOptionProvider optionProvider in _optionProviders)
		{
			optionProvider.ApplyOptions();
		}
		_areOptionValuesDirty = true;
	}

	public void ExecuteCancel()
	{
		_onEscapeMenuToggled(obj: false);
	}
}
