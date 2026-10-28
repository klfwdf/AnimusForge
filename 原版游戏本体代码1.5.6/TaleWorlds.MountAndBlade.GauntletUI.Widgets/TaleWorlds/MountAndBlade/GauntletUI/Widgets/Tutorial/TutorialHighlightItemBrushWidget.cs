using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Tutorial;

public class TutorialHighlightItemBrushWidget : BrushWidget
{
	public enum AnimState
	{
		Idle,
		Start,
		FirstFrame,
		Playing
	}

	public class HighlightElementToggledEvent : EventBase
	{
		public bool IsEnabled { get; private set; }

		public TutorialHighlightItemBrushWidget HighlightFrameWidget { get; private set; }

		public HighlightElementToggledEvent(bool isEnabled, TutorialHighlightItemBrushWidget highlightFrameWidget)
		{
			IsEnabled = isEnabled;
			HighlightFrameWidget = highlightFrameWidget;
		}
	}

	private AnimState _animState;

	private bool _isDisabled;

	private bool _isHighlightEnabled;

	public Widget CustomSizeSyncTarget { get; set; }

	public bool DoNotOverrideWidth { get; set; }

	public bool DoNotOverrideHeight { get; set; }

	[Editor(false)]
	public bool IsHighlightEnabled
	{
		get
		{
			return _isHighlightEnabled;
		}
		set
		{
			if (_isHighlightEnabled != value)
			{
				_isHighlightEnabled = value;
				OnPropertyChanged(value, "IsHighlightEnabled");
				if (IsHighlightEnabled)
				{
					_animState = AnimState.Start;
				}
				base.IsVisible = value;
				TaleWorlds.GauntletUI.EventManager.UIEventManager.TriggerEvent(new HighlightElementToggledEvent(value, value ? this : null));
			}
		}
	}

	public TutorialHighlightItemBrushWidget(UIContext context)
		: base(context)
	{
		base.UseGlobalTimeForAnimation = true;
		base.DoNotAcceptEvents = true;
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_animState == AnimState.Start)
		{
			_animState = AnimState.FirstFrame;
		}
		else if (_animState == AnimState.FirstFrame)
		{
			if (base.BrushRenderer.Brush == null)
			{
				_animState = AnimState.Start;
			}
			else
			{
				_animState = AnimState.Playing;
				base.BrushRenderer.RestartAnimation();
			}
		}
		if (IsHighlightEnabled && _isDisabled)
		{
			_isDisabled = false;
			SetState("Default");
		}
		else if (!IsHighlightEnabled && !_isDisabled)
		{
			SetState("Disabled");
			_isDisabled = true;
		}
		UpdateTargetSize();
	}

	private void UpdateTargetSize()
	{
		Widget widget = CustomSizeSyncTarget ?? base.ParentWidget;
		if (widget == null)
		{
			return;
		}
		bool flag;
		if (widget.HeightSizePolicy == SizePolicy.CoverChildren || widget.WidthSizePolicy == SizePolicy.CoverChildren)
		{
			if (!DoNotOverrideWidth)
			{
				base.WidthSizePolicy = SizePolicy.Fixed;
			}
			if (!DoNotOverrideHeight)
			{
				base.HeightSizePolicy = SizePolicy.Fixed;
			}
			flag = true;
		}
		else
		{
			base.WidthSizePolicy = SizePolicy.StretchToParent;
			base.HeightSizePolicy = SizePolicy.StretchToParent;
			flag = false;
		}
		if (flag && widget.Size.X > 1f && widget.Size.Y > 1f)
		{
			if (!DoNotOverrideWidth)
			{
				base.ScaledSuggestedWidth = widget.Size.X - 1f;
			}
			if (!DoNotOverrideHeight)
			{
				base.ScaledSuggestedHeight = widget.Size.Y - 1f;
			}
		}
	}
}
