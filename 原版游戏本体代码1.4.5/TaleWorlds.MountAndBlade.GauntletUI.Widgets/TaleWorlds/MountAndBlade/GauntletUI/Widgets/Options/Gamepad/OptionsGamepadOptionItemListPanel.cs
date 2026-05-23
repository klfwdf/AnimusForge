using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Options.Gamepad;

public class OptionsGamepadOptionItemListPanel : ListPanel
{
	public delegate void OnActionTextChangeEvent();

	private string _actionText;

	private int _keyId;

	public OptionsGamepadKeyLocationWidget TargetKey { get; private set; }

	public string ActionText
	{
		get
		{
			return _actionText;
		}
		set
		{
			if (_actionText != value)
			{
				_actionText = value;
				this.OnActionTextChanged?.Invoke();
			}
		}
	}

	public int KeyId
	{
		get
		{
			return _keyId;
		}
		set
		{
			if (value != _keyId)
			{
				_keyId = value;
			}
		}
	}

	public event OnActionTextChangeEvent OnActionTextChanged;

	public OptionsGamepadOptionItemListPanel(UIContext context)
		: base(context)
	{
	}

	public void SetKeyProperties(OptionsGamepadKeyLocationWidget currentTarget, Widget parentAreaWidget)
	{
		TargetKey = currentTarget;
		TargetKey.SetKeyProperties(ActionText, parentAreaWidget);
	}
}
