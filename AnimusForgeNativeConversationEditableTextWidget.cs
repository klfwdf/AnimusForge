using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace AnimusForge;

public sealed class AnimusForgeNativeConversationEditableTextWidget : EditableTextWidget
{
	private bool _autoFocusApplied;

	private bool _focusRequested;

	private int _focusRequestId;

	public bool AutoFocus { get; set; }

	public int FocusRequestId
	{
		get => _focusRequestId;
		set
		{
			if (value != _focusRequestId)
			{
				_focusRequestId = value;
				_focusRequested = true;
			}
		}
	}

	public AnimusForgeNativeConversationEditableTextWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (EventManager == null || !IsEnabled || !IsRecursivelyVisible())
		{
			return;
		}
		if (AutoFocus && !_autoFocusApplied)
		{
			FocusSelf();
			_autoFocusApplied = true;
			_focusRequested = false;
		}
		else if (_focusRequested)
		{
			FocusSelf();
			_focusRequested = false;
			_autoFocusApplied = true;
		}
	}

	private void FocusSelf()
	{
		if (EventManager != null && EventManager.FocusedWidget != this)
		{
			EventManager.FocusedWidget = this;
		}
	}
}
