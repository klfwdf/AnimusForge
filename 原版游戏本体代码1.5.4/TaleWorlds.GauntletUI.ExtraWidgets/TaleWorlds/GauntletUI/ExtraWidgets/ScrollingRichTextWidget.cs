using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.ExtraWidgets;

public class ScrollingRichTextWidget : RichTextWidget
{
	private bool _shouldScroll;

	private float _scrollTimeNeeded;

	private float _scrollTimeElapsed;

	private float _totalScrollAmount;

	private float _currentScrollAmount;

	private Vec2 _currentSize;

	private bool _isHovering;

	private float _wordWidth;

	private Widget _scrollOnHoverWidget;

	private bool _isAutoScrolling = true;

	private float _scrollPerSecond = 30f;

	private float _scrollRatioPerSecond;

	private float _inbetweenScrollDuration = 1f;

	private TextHorizontalAlignment _defaultTextHorizontalAlignment;

	public string ActualText { get; private set; } = string.Empty;

	[Editor(false)]
	public Widget ScrollOnHoverWidget
	{
		get
		{
			return _scrollOnHoverWidget;
		}
		set
		{
			if (value != _scrollOnHoverWidget)
			{
				_scrollOnHoverWidget = value;
				OnPropertyChanged(value, "ScrollOnHoverWidget");
			}
		}
	}

	[Editor(false)]
	public bool IsAutoScrolling
	{
		get
		{
			return _isAutoScrolling;
		}
		set
		{
			if (value != _isAutoScrolling)
			{
				_isAutoScrolling = value;
				OnPropertyChanged(value, "IsAutoScrolling");
			}
		}
	}

	[Editor(false)]
	public float ScrollPerSecond
	{
		get
		{
			return _scrollPerSecond;
		}
		set
		{
			if (value != _scrollPerSecond)
			{
				_scrollPerSecond = value;
				OnPropertyChanged(value, "ScrollPerSecond");
			}
		}
	}

	[Editor(false)]
	public float ScrollRatioPerSecond
	{
		get
		{
			return _scrollRatioPerSecond;
		}
		set
		{
			if (value != _scrollRatioPerSecond)
			{
				_scrollRatioPerSecond = value;
				OnPropertyChanged(value, "ScrollRatioPerSecond");
			}
		}
	}

	[Editor(false)]
	public float InbetweenScrollDuration
	{
		get
		{
			return _inbetweenScrollDuration;
		}
		set
		{
			if (value != _inbetweenScrollDuration)
			{
				_inbetweenScrollDuration = value;
				OnPropertyChanged(value, "InbetweenScrollDuration");
			}
		}
	}

	[Editor(false)]
	public TextHorizontalAlignment DefaultTextHorizontalAlignment
	{
		get
		{
			return _defaultTextHorizontalAlignment;
		}
		set
		{
			if (value != _defaultTextHorizontalAlignment)
			{
				_defaultTextHorizontalAlignment = value;
				switch (value)
				{
				case TextHorizontalAlignment.Left:
					OnPropertyChanged("Left", "DefaultTextHorizontalAlignment");
					break;
				case TextHorizontalAlignment.Right:
					OnPropertyChanged("Right", "DefaultTextHorizontalAlignment");
					break;
				case TextHorizontalAlignment.Center:
					OnPropertyChanged("Center", "DefaultTextHorizontalAlignment");
					break;
				case TextHorizontalAlignment.Justify:
					OnPropertyChanged("Justify", "DefaultTextHorizontalAlignment");
					break;
				}
			}
		}
	}

	public ScrollingRichTextWidget(UIContext context)
		: base(context)
	{
		ScrollOnHoverWidget = this;
		DefaultTextHorizontalAlignment = base.Brush.TextHorizontalAlignment;
		base.ClipContents = true;
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (base.Size != _currentSize)
		{
			_currentSize = base.Size;
			UpdateScrollable();
		}
		if (_shouldScroll)
		{
			_scrollTimeElapsed += dt;
			if (_scrollTimeElapsed < InbetweenScrollDuration)
			{
				_currentScrollAmount = 0f;
			}
			else if (_scrollTimeElapsed >= InbetweenScrollDuration && _currentScrollAmount < _totalScrollAmount)
			{
				_currentScrollAmount += dt * ScrollPerSecond;
				_currentScrollAmount += dt * ScrollRatioPerSecond * _totalScrollAmount;
			}
			else if (_currentScrollAmount >= _totalScrollAmount)
			{
				if (_scrollTimeNeeded.ApproximatelyEqualsTo(0f))
				{
					_scrollTimeNeeded = _scrollTimeElapsed;
				}
				if (_scrollTimeElapsed < _scrollTimeNeeded + InbetweenScrollDuration)
				{
					_currentScrollAmount = _totalScrollAmount;
				}
				else
				{
					_scrollTimeNeeded = 0f;
					_scrollTimeElapsed = 0f;
				}
			}
		}
		if (base.EventManager.HoveredWidget == ScrollOnHoverWidget && !_isHovering)
		{
			_isHovering = true;
			if (!IsAutoScrolling)
			{
				base.Text = ActualText;
				UpdateWordWidth();
				_shouldScroll = _wordWidth > GetMaximumAllowedWidth();
			}
		}
		else if (base.EventManager.HoveredWidget != ScrollOnHoverWidget && _isHovering)
		{
			if (!IsAutoScrolling)
			{
				ResetScroll();
			}
			_isHovering = false;
			UpdateScrollable();
		}
		_renderOffset.x = 0f - _currentScrollAmount;
	}

	public override void OnBrushChanged()
	{
		base.OnBrushChanged();
		DefaultTextHorizontalAlignment = base.Brush.TextHorizontalAlignment;
		UpdateScrollable();
	}

	protected override void SetText(string value)
	{
		base.SetText(value);
		_richText.SkipLineOnContainerExceeded = false;
		ActualText = _richText.Value;
		_currentSize = Vec2.Zero;
		ResetScroll();
	}

	private void UpdateScrollable()
	{
		UpdateWordWidth();
		if (_wordWidth > GetMaximumAllowedWidth())
		{
			_shouldScroll = IsAutoScrolling;
			_totalScrollAmount = _wordWidth - GetMaximumAllowedWidth();
			base.Brush.TextHorizontalAlignment = TextHorizontalAlignment.Left;
			if (IsAutoScrolling || _isHovering)
			{
				return;
			}
			bool flag = false;
			for (int num = ActualText.Length; num > 3; num--)
			{
				if (ActualText[num - 1] == '>')
				{
					flag = true;
				}
				else if (ActualText[num - 1] == '<')
				{
					flag = false;
				}
				if (!flag && _richText.GetPreferredSize(base.WidthSizePolicy == SizePolicy.Fixed, base.SuggestedWidth, base.HeightSizePolicy == SizePolicy.Fixed, base.SuggestedHeight, base.Context.SpriteData, base._scaleToUse).X <= GetMaximumAllowedWidth())
				{
					_richText.Value = ActualText.Substring(0, num - 3) + "...";
					break;
				}
			}
		}
		else
		{
			ResetScroll();
		}
	}

	private float GetMaximumAllowedWidth()
	{
		if (base.WidthSizePolicy == SizePolicy.CoverChildren)
		{
			if (base.ScaledMaxWidth == 0f)
			{
				return 2.1474836E+09f;
			}
			return base.ScaledMaxWidth;
		}
		return base.Size.X;
	}

	private void UpdateWordWidth()
	{
		_wordWidth = _richText.GetPreferredSize(base.WidthSizePolicy == SizePolicy.Fixed, base.SuggestedWidth, base.HeightSizePolicy == SizePolicy.Fixed, base.SuggestedHeight, base.Context.SpriteData, base._scaleToUse).X;
	}

	private void ResetScroll()
	{
		_shouldScroll = false;
		_scrollTimeElapsed = 0f;
		_currentScrollAmount = 0f;
		base.Brush.TextHorizontalAlignment = DefaultTextHorizontalAlignment;
	}
}
