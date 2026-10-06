using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI;

public class VisualState
{
	private float _transitionDuration;

	private float _positionXOffset;

	private float _positionYOffset;

	private float _suggestedWidth;

	private float _suggestedHeight;

	private float _marginTop;

	private float _marginBottom;

	private float _marginLeft;

	private float _marginRight;

	public string State { get; private set; }

	public float TransitionDuration
	{
		get
		{
			return _transitionDuration;
		}
		set
		{
			_transitionDuration = value;
			GotTransitionDuration = true;
		}
	}

	public float PositionXOffset
	{
		get
		{
			return _positionXOffset;
		}
		set
		{
			_positionXOffset = value;
			GotPositionXOffset = true;
		}
	}

	public float PositionYOffset
	{
		get
		{
			return _positionYOffset;
		}
		set
		{
			_positionYOffset = value;
			GotPositionYOffset = true;
		}
	}

	public float SuggestedWidth
	{
		get
		{
			return _suggestedWidth;
		}
		set
		{
			_suggestedWidth = value;
			GotSuggestedWidth = true;
		}
	}

	public float SuggestedHeight
	{
		get
		{
			return _suggestedHeight;
		}
		set
		{
			_suggestedHeight = value;
			GotSuggestedHeight = true;
		}
	}

	public float MarginTop
	{
		get
		{
			return _marginTop;
		}
		set
		{
			_marginTop = value;
			GotMarginTop = true;
		}
	}

	public float MarginBottom
	{
		get
		{
			return _marginBottom;
		}
		set
		{
			_marginBottom = value;
			GotMarginBottom = true;
		}
	}

	public float MarginLeft
	{
		get
		{
			return _marginLeft;
		}
		set
		{
			_marginLeft = value;
			GotMarginLeft = true;
		}
	}

	public float MarginRight
	{
		get
		{
			return _marginRight;
		}
		set
		{
			_marginRight = value;
			GotMarginRight = true;
		}
	}

	public bool GotTransitionDuration { get; private set; }

	public bool GotPositionXOffset { get; private set; }

	public bool GotPositionYOffset { get; private set; }

	public bool GotSuggestedWidth { get; private set; }

	public bool GotSuggestedHeight { get; private set; }

	public bool GotMarginTop { get; private set; }

	public bool GotMarginBottom { get; private set; }

	public bool GotMarginLeft { get; private set; }

	public bool GotMarginRight { get; private set; }

	public VisualState(string state)
	{
		State = state;
	}

	public void FillFromWidget(Widget widget)
	{
		PositionXOffset = widget.PositionXOffset;
		PositionYOffset = widget.PositionYOffset;
		SuggestedWidth = widget.SuggestedWidth;
		SuggestedHeight = widget.SuggestedHeight;
		MarginTop = widget.MarginTop;
		MarginBottom = widget.MarginBottom;
		MarginLeft = widget.MarginLeft;
		MarginRight = widget.MarginRight;
	}
}
