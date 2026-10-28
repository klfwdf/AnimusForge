using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class SettlementStatTextWidget : TextWidget
{
	public enum State
	{
		Idle,
		Start,
		Playing,
		End
	}

	private State _state;

	private bool _isWarning;

	[Editor(false)]
	public bool IsWarning
	{
		get
		{
			return _isWarning;
		}
		set
		{
			if (value != _isWarning)
			{
				_isWarning = value;
				OnPropertyChanged(value, "IsWarning");
				SetState(_isWarning ? "Warning" : "Default");
			}
		}
	}

	public SettlementStatTextWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		switch (_state)
		{
		case State.Idle:
			if (IsWarning)
			{
				SetState("Warning");
			}
			else
			{
				SetState("Default");
			}
			_state = State.Start;
			break;
		case State.Start:
			_state = ((base.BrushRenderer.Brush == null) ? State.Start : State.Playing);
			break;
		case State.Playing:
			base.BrushRenderer.RestartAnimation();
			_state = State.End;
			break;
		case State.End:
			break;
		}
	}
}
