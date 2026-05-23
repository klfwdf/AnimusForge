using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.ExtraWidgets;

public class ScrollingTextWidget : TextWidget
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

	private float _scrollPerTick = 30f;

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
	public float ScrollPerTick
	{
		get
		{
			return _scrollPerTick;
		}
		set
		{
			if (value != _scrollPerTick)
			{
				_scrollPerTick = value;
				OnPropertyChanged(value, "ScrollPerTick");
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

	public ScrollingTextWidget(UIContext context)
		: base(context)
	{
		ScrollOnHoverWidget = this;
		DefaultTextHorizontalAlignment = base.Brush.TextHorizontalAlignment;
		base.ClipHorizontalContent = true;
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
				_currentScrollAmount += dt * ScrollPerTick;
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
			if (!IsAutoScrolling)
			{
				_text.Value = ActualText;
				UpdateWordWidth();
				_shouldScroll = _wordWidth > GetMaximumAllowedWidth();
			}
			_isHovering = true;
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
		DefaultTextHorizontalAlignment = base.Brush.TextHorizontalAlignment;
		UpdateScrollable();
	}

	protected override void SetText(string value)
	{
		base.SetText(value);
		_text.SkipLineOnContainerExceeded = false;
		_text.ResizeTextOnOverflow = false;
		ActualText = _text.Value;
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
			if (IsAutoScrolling)
			{
				return;
			}
			for (int num = ActualText.Length; num > 3; num--)
			{
				if (GetWordWidth(ActualText.Substring(0, num - 3) + "...", 0.25f) * base._scaleToUse <= GetMaximumAllowedWidth())
				{
					_text.Value = ActualText.Substring(0, num - 3) + "...";
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
		float padding = 0.5f;
		if (base.WidthSizePolicy == SizePolicy.CoverChildren)
		{
			padding = 0f;
		}
		_wordWidth = GetWordWidth(_text.Value, padding) * base._scaleToUse;
	}

	private float GetWordWidth(string word, float padding)
	{
		float num = padding * 2f;
		for (int i = 0; i < word.Length; i++)
		{
			num += GetCharacterWidth(word[i]);
		}
		return num;
	}

	private float GetCharacterWidth(char character)
	{
		Font mappedFontForLocalization = base.Context.FontFactory.GetMappedFontForLocalization(base.Brush?.Font?.Name);
		float num;
		if (!mappedFontForLocalization.Characters.ContainsKey(character))
		{
			Font font = base.Context.FontFactory.GetUsableFontForCharacter(character) ?? mappedFontForLocalization;
			num = (float)base.Brush.FontSize / (float)font.Size;
			return font.GetCharacterWidth(character, 0.5f) * num;
		}
		num = (float)base.Brush.FontSize / (float)mappedFontForLocalization.Size;
		return mappedFontForLocalization.GetCharacterWidth(character, 0.5f) * num;
	}

	private void ResetScroll()
	{
		_shouldScroll = false;
		_scrollTimeElapsed = 0f;
		_currentScrollAmount = 0f;
		_renderOffset.x = 0f;
		base.Brush.TextHorizontalAlignment = DefaultTextHorizontalAlignment;
	}
}
