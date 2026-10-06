using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.InitialMenu;

public class InitialMenuOptionVM : ViewModel
{
	public readonly InitialStateOption InitialStateOption;

	private HintViewModel _disabledHint;

	private HintViewModel _enabledHint;

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
	public HintViewModel EnabledHint
	{
		get
		{
			return _enabledHint;
		}
		set
		{
			if (value != _enabledHint)
			{
				_enabledHint = value;
				OnPropertyChangedWithValue(value, "EnabledHint");
			}
		}
	}

	[DataSourceProperty]
	public string NameText => InitialStateOption.Name.ToString();

	[DataSourceProperty]
	public bool IsDisabled => InitialStateOption.IsDisabledAndReason().Item1;

	[DataSourceProperty]
	public bool IsHidden => InitialStateOption.IsHidden?.Invoke() ?? false;

	public InitialMenuOptionVM(InitialStateOption initialStateOption)
	{
		InitialStateOption = initialStateOption;
		DisabledHint = new HintViewModel(initialStateOption.IsDisabledAndReason().Item2);
		EnabledHint = new HintViewModel(initialStateOption.EnabledHint);
	}

	public void ExecuteAction()
	{
		if (GameStateManager.Current.ActiveState is InitialState initialState)
		{
			initialState.OnExecutedInitialStateOption(InitialStateOption);
			InitialStateOption.DoAction();
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		DisabledHint.HintText = InitialStateOption.IsDisabledAndReason().Item2;
	}
}
