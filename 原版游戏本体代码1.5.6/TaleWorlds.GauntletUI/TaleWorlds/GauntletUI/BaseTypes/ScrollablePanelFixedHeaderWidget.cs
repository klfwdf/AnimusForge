namespace TaleWorlds.GauntletUI.BaseTypes;

public class ScrollablePanelFixedHeaderWidget : Widget
{
	private bool _isDirty;

	private float _headerHeight;

	private float _additionalTopOffset;

	private float _additionalBottomOffset;

	private bool _isRelevant = true;

	public Widget FixedHeader { get; set; }

	public float TopOffset { get; set; }

	public float BottomOffset { get; set; } = float.MinValue;

	public float HeaderHeight
	{
		get
		{
			return _headerHeight;
		}
		set
		{
			if (value != _headerHeight)
			{
				_headerHeight = value;
				base.SuggestedHeight = _headerHeight;
				_isDirty = true;
			}
		}
	}

	public float AdditionalTopOffset
	{
		get
		{
			return _additionalTopOffset;
		}
		set
		{
			if (value != _additionalTopOffset)
			{
				_additionalTopOffset = value;
				_isDirty = true;
			}
		}
	}

	public float AdditionalBottomOffset
	{
		get
		{
			return _additionalBottomOffset;
		}
		set
		{
			if (value != _additionalBottomOffset)
			{
				_additionalBottomOffset = value;
				_isDirty = true;
			}
		}
	}

	[Editor(false)]
	public bool IsRelevant
	{
		get
		{
			return _isRelevant;
		}
		set
		{
			if (value != _isRelevant)
			{
				_isRelevant = value;
				base.IsVisible = value;
				_isDirty = true;
				OnPropertyChanged(value, "IsRelevant");
			}
		}
	}

	public ScrollablePanelFixedHeaderWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_isDirty)
		{
			EventFired("FixedHeaderPropertyChanged");
			_isDirty = false;
		}
	}
}
