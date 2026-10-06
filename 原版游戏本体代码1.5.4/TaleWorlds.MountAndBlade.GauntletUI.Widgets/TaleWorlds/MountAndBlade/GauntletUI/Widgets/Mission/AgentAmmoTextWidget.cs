using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission;

public class AgentAmmoTextWidget : TextWidget
{
	private bool _isAlertEnabled;

	public bool IsAlertEnabled
	{
		get
		{
			return _isAlertEnabled;
		}
		set
		{
			if (_isAlertEnabled != value)
			{
				_isAlertEnabled = value;
				OnPropertyChanged(value, "IsAlertEnabled");
			}
		}
	}

	public AgentAmmoTextWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (IsAlertEnabled)
		{
			SetState("Alert");
		}
		else
		{
			SetState("Default");
		}
	}
}
