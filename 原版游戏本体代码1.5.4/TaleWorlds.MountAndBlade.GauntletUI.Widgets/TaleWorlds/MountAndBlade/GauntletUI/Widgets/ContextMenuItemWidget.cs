using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class ContextMenuItemWidget : Widget
{
	private const float _disabledAlpha = 0.5f;

	private const float _enabledAlpha = 1f;

	private bool _isInitialized;

	private bool _canBeUsed = true;

	public Widget TypeIconWidget { get; set; }

	public ButtonWidget ActionButtonWidget { get; set; }

	public string TypeIconState { get; set; }

	public bool CanBeUsed
	{
		get
		{
			return _canBeUsed;
		}
		set
		{
			if (value != _canBeUsed)
			{
				_canBeUsed = value;
				OnPropertyChanged(value, "CanBeUsed");
				RefreshState();
			}
		}
	}

	public ContextMenuItemWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_isInitialized)
		{
			if (TypeIconWidget != null && !string.IsNullOrEmpty(TypeIconState))
			{
				TypeIconWidget.RegisterBrushStatesOfWidget();
				TypeIconWidget.SetState(TypeIconState);
			}
			_isInitialized = true;
		}
	}

	protected override void RefreshState()
	{
		base.RefreshState();
		if (!CanBeUsed)
		{
			this.SetGlobalAlphaRecursively(0.5f);
		}
		else
		{
			this.SetGlobalAlphaRecursively(1f);
		}
	}
}
