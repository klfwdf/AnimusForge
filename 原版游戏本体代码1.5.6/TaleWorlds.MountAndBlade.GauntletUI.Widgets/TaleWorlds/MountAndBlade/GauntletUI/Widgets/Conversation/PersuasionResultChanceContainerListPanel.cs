using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Conversation;

public class PersuasionResultChanceContainerListPanel : BrushListPanel
{
	private Widget _resultVisualWidget;

	private float _delayStartTime = -1f;

	private int _resultIndex;

	public float StayTime { get; set; } = 1f;

	public Widget CritFailWidget { get; set; }

	public Widget FailWidget { get; set; }

	public Widget SuccessWidget { get; set; }

	public Widget CritSuccessWidget { get; set; }

	public bool IsResultReady { get; set; }

	[Editor(false)]
	public int ResultIndex
	{
		get
		{
			return _resultIndex;
		}
		set
		{
			if (value != _resultIndex)
			{
				_resultIndex = value;
				OnPropertyChanged(value, "ResultIndex");
				switch (value)
				{
				case 0:
					_resultVisualWidget = CritFailWidget;
					SetState("CriticalFail");
					break;
				case 1:
					_resultVisualWidget = FailWidget;
					SetState("Fail");
					break;
				case 2:
					_resultVisualWidget = SuccessWidget;
					SetState("Success");
					break;
				case 3:
					_resultVisualWidget = CritSuccessWidget;
					SetState("CriticalSuccess");
					break;
				}
			}
		}
	}

	public PersuasionResultChanceContainerListPanel(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (IsResultReady)
		{
			if (_delayStartTime == -1f && base.AlphaFactor <= 0.001f)
			{
				_delayStartTime = base.EventManager.Time;
			}
			float alphaFactor = Mathf.Lerp(base.AlphaFactor, 0f, 0.35f);
			this.SetGlobalAlphaRecursively(alphaFactor);
			_resultVisualWidget?.SetGlobalAlphaRecursively(1f);
			if (_delayStartTime != -1f && base.EventManager.Time - _delayStartTime > StayTime)
			{
				EventFired("OnReadyToContinue");
			}
		}
	}
}
