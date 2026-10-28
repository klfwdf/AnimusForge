using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.ExtraWidgets;

public class DialogButtonsParentWidget : Widget
{
	private ButtonWidget _cancelButton;

	private ButtonWidget _confirmButton;

	private ButtonWidget _resetButton;

	public string CancelClickSound { get; set; }

	public string ConfirmClickSound { get; set; }

	public string ResetClickSound { get; set; }

	[Editor(false)]
	public ButtonWidget CancelButton
	{
		get
		{
			return _cancelButton;
		}
		set
		{
			if (value != _cancelButton)
			{
				_cancelButton?.ClickEventHandlers.Remove(OnClickCancel);
				_cancelButton = value;
				_cancelButton?.ClickEventHandlers.Add(OnClickCancel);
			}
		}
	}

	[Editor(false)]
	public ButtonWidget ConfirmButton
	{
		get
		{
			return _confirmButton;
		}
		set
		{
			if (value != _confirmButton)
			{
				_confirmButton?.ClickEventHandlers.Remove(OnClickConfirm);
				_confirmButton = value;
				_confirmButton?.ClickEventHandlers.Add(OnClickConfirm);
			}
		}
	}

	[Editor(false)]
	public ButtonWidget ResetButton
	{
		get
		{
			return _resetButton;
		}
		set
		{
			if (value != _resetButton)
			{
				_resetButton?.ClickEventHandlers.Remove(OnClickReset);
				_resetButton = value;
				_resetButton?.ClickEventHandlers.Add(OnClickReset);
			}
		}
	}

	public DialogButtonsParentWidget(UIContext context)
		: base(context)
	{
	}

	private void OnClickCancel(Widget widget)
	{
		if (!string.IsNullOrEmpty(CancelClickSound))
		{
			base.Context.TwoDimensionContext.PlaySound(CancelClickSound);
		}
	}

	private void OnClickConfirm(Widget widget)
	{
		if (!string.IsNullOrEmpty(ConfirmClickSound))
		{
			base.Context.TwoDimensionContext.PlaySound(ConfirmClickSound);
		}
	}

	private void OnClickReset(Widget widget)
	{
		if (!string.IsNullOrEmpty(ResetClickSound))
		{
			base.Context.TwoDimensionContext.PlaySound(ResetClickSound);
		}
	}
}
