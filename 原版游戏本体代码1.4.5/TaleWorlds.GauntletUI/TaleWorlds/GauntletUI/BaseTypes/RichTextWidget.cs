using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using TaleWorlds.GauntletUI.Layout;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.BaseTypes;

public class RichTextWidget : BrushWidget
{
	private enum MouseState
	{
		None,
		Down,
		Up,
		AlternateDown,
		AlternateUp
	}

	protected readonly RichText _richText;

	private bool _autoHideIfEmpty;

	private Brush _lastFontBrush;

	private string _lastLanguageCode;

	private float _lastContextScale;

	private FontFactory _fontFactory;

	private MouseState _mouseState;

	private Dictionary<Texture, SimpleMaterial> _textureMaterialDict;

	private Vector2 _mouseDownPosition;

	private int _textHeight;

	protected Vec2 _renderOffset;

	private string _linkHoverCursorState;

	private bool _canBreakWords = true;

	public bool AutoHideIfEmpty
	{
		get
		{
			return _autoHideIfEmpty;
		}
		set
		{
			if (value != _autoHideIfEmpty)
			{
				_autoHideIfEmpty = value;
				if (_autoHideIfEmpty)
				{
					base.IsVisible = !string.IsNullOrEmpty(Text);
				}
			}
		}
	}

	private Vector2 LocalMousePosition => AreaRect.TransformScreenPositionToLocal(base.EventManager.MousePosition);

	[Editor(false)]
	public string LinkHoverCursorState
	{
		get
		{
			return _linkHoverCursorState;
		}
		set
		{
			if (_linkHoverCursorState != value)
			{
				_linkHoverCursorState = value;
			}
		}
	}

	[Editor(false)]
	public string Text
	{
		get
		{
			return _richText.Value;
		}
		set
		{
			if (_richText.Value != value)
			{
				_richText.CurrentLanguage = base.Context.FontFactory.CurrentLanguage;
				_richText.Value = value;
				OnPropertyChanged(value, "Text");
				SetMeasureAndLayoutDirty();
				SetText(_richText.Value);
			}
		}
	}

	public bool CanBreakWords
	{
		get
		{
			return _canBreakWords;
		}
		set
		{
			if (value != _canBreakWords)
			{
				_canBreakWords = value;
				_richText.CanBreakWords = value;
				OnPropertyChanged(value, "CanBreakWords");
			}
		}
	}

	public RichTextWidget(UIContext context)
		: base(context)
	{
		_fontFactory = context.FontFactory;
		_textHeight = -1;
		Font defaultFont = base.Context.FontFactory.DefaultFont;
		_richText = new RichText((int)base.Size.X, (int)base.Size.Y, defaultFont, _fontFactory.GetUsableFontForCharacter);
		_textureMaterialDict = new Dictionary<Texture, SimpleMaterial>();
		_lastFontBrush = null;
		base.LayoutImp = new TextLayout(_richText);
		CanBreakWords = true;
		AddState("Pressed");
		AddState("Hovered");
		AddState("Disabled");
	}

	public override void OnBrushChanged()
	{
		base.OnBrushChanged();
		UpdateFontData();
	}

	protected virtual void SetText(string value)
	{
		if (AutoHideIfEmpty)
		{
			base.IsVisible = !string.IsNullOrEmpty(Text);
		}
	}

	private void SetRichTextParameters()
	{
		bool flag = false;
		_richText.CurrentLanguage = base.Context.FontFactory.CurrentLanguage;
		UpdateFontData();
		if (_richText.HorizontalAlignment != base.ReadOnlyBrush.TextHorizontalAlignment)
		{
			_richText.HorizontalAlignment = base.ReadOnlyBrush.TextHorizontalAlignment;
			flag = true;
		}
		if (_richText.VerticalAlignment != base.ReadOnlyBrush.TextVerticalAlignment)
		{
			_richText.VerticalAlignment = base.ReadOnlyBrush.TextVerticalAlignment;
			flag = true;
		}
		if (_richText.TextHeight != _textHeight)
		{
			_textHeight = _richText.TextHeight;
			flag = true;
		}
		if (_richText.CurrentStyle != base.CurrentState && !string.IsNullOrEmpty(base.CurrentState))
		{
			_richText.CurrentStyle = base.CurrentState;
			flag = true;
		}
		if (flag)
		{
			SetMeasureAndLayoutDirty();
		}
	}

	protected override void RefreshState()
	{
		base.RefreshState();
		UpdateText();
	}

	private void UpdateText()
	{
		if (base.IsDisabled)
		{
			SetState("Disabled");
		}
		else if (base.IsPressed)
		{
			SetState("Pressed");
		}
		else if (base.IsHovered)
		{
			SetState("Hovered");
		}
		else
		{
			SetState("Default");
		}
	}

	private void UpdateFontData()
	{
		if (_lastFontBrush == base.ReadOnlyBrush && _lastContextScale == base._scaleToUse && _lastLanguageCode == base.Context.FontFactory.CurrentLanguage.LanguageID)
		{
			return;
		}
		_richText.StyleFontContainer.ClearFonts();
		foreach (Style style in base.ReadOnlyBrush.Styles)
		{
			Font font = null;
			font = ((style.Font == null) ? ((base.ReadOnlyBrush.Font == null) ? base.Context.FontFactory.DefaultFont : base.ReadOnlyBrush.Font) : style.Font);
			Font mappedFontForLocalization = base.Context.FontFactory.GetMappedFontForLocalization(font.Name);
			_richText.StyleFontContainer.Add(style.Name, mappedFontForLocalization, (float)style.FontSize * base._scaleToUse);
		}
		_lastFontBrush = base.ReadOnlyBrush;
		_lastLanguageCode = base.Context.FontFactory.CurrentLanguage.LanguageID;
		_lastContextScale = base._scaleToUse;
		_richText.CurrentLanguage = base.Context.FontFactory.CurrentLanguage;
	}

	private Font GetFont(Style style = null)
	{
		if (style?.Font != null)
		{
			return base.Context.FontFactory.GetMappedFontForLocalization(style.Font.Name);
		}
		if (base.ReadOnlyBrush.Font != null)
		{
			return base.Context.FontFactory.GetMappedFontForLocalization(base.ReadOnlyBrush.Font.Name);
		}
		return base.Context.FontFactory.DefaultFont;
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		SetRichTextParameters();
		if (!(base.Size.X > 0f) || !(base.Size.Y > 0f))
		{
			return;
		}
		Vector2 focusPosition = LocalMousePosition;
		bool flag = _mouseState == MouseState.Down || _mouseState == MouseState.AlternateDown;
		bool num = _mouseState == MouseState.Up || _mouseState == MouseState.AlternateUp;
		if (flag)
		{
			focusPosition = _mouseDownPosition;
		}
		RichTextLinkGroup focusedLinkGroup = _richText.FocusedLinkGroup;
		_richText.UpdateSize((int)base.Size.X, (int)base.Size.Y);
		if (focusedLinkGroup != null && LinkHoverCursorState != null)
		{
			base.Context.ActiveCursorOfContext = (UIContext.MouseCursors)Enum.Parse(typeof(UIContext.MouseCursors), LinkHoverCursorState);
		}
		bool isFixedWidth = base.WidthSizePolicy != SizePolicy.CoverChildren || base.MaxWidth != 0f;
		bool isFixedHeight = base.HeightSizePolicy != SizePolicy.CoverChildren || base.MaxHeight != 0f;
		_richText.Update(dt, base.Context.SpriteData, focusPosition, flag, isFixedWidth, isFixedHeight, base._scaleToUse);
		if (num)
		{
			RichTextLinkGroup focusedLinkGroup2 = _richText.FocusedLinkGroup;
			if (focusedLinkGroup != null && focusedLinkGroup == focusedLinkGroup2)
			{
				string text = focusedLinkGroup.Href;
				string[] array = text.Split(new char[1] { ':' });
				if (array.Length == 2)
				{
					text = array[1];
				}
				if (_mouseState == MouseState.Up)
				{
					EventFired("LinkClick", text);
				}
				else if (_mouseState == MouseState.AlternateUp)
				{
					EventFired("LinkAlternateClick", text);
				}
			}
			_mouseState = MouseState.None;
		}
		_renderOffset = Vec2.Zero;
	}

	protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
	{
		base.OnRender(twoDimensionContext, drawContext);
		if (string.IsNullOrEmpty(_richText.Value))
		{
			return;
		}
		List<RichTextPart> parts = _richText.GetParts();
		for (int i = 0; i < parts.Count; i++)
		{
			RichTextPart richTextPart = parts[i];
			if (richTextPart.Type == RichTextPartType.Text)
			{
				RenderText(richTextPart, drawContext);
			}
			else if (richTextPart.Type == RichTextPartType.Sprite)
			{
				RenderImage(richTextPart, drawContext);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void RenderText(RichTextPart richTextPart, TwoDimensionDrawContext drawContext)
	{
		if (richTextPart.TextDrawObject.IsValid)
		{
			TextDrawObject drawObject = richTextPart.TextDrawObject;
			Rectangle2D rectangle = drawObject.Rectangle;
			rectangle.LocalPosition = new Vector2(base.LocalPosition.X + _renderOffset.X, base.LocalPosition.Y + _renderOffset.Y);
			rectangle.LocalScale = new Vector2(drawObject.Text_MeshWidth, drawObject.Text_MeshHeight);
			Style styleOrDefault = base.ReadOnlyBrush.GetStyleOrDefault(richTextPart.Style);
			Font defaultFont = richTextPart.DefaultFont;
			float scaleFactor = (float)styleOrDefault.FontSize * base._scaleToUse;
			TextMaterial textMaterial = styleOrDefault.CreateTextMaterial(drawContext);
			textMaterial.ColorFactor *= base.ReadOnlyBrush.GlobalColorFactor;
			textMaterial.AlphaFactor *= base.ReadOnlyBrush.GlobalAlphaFactor * base.Context.ContextAlpha;
			textMaterial.Color *= base.ReadOnlyBrush.GlobalColor;
			textMaterial.Texture = defaultFont.FontSprite.Texture;
			textMaterial.ScaleFactor = scaleFactor;
			textMaterial.SmoothingConstant = defaultFont.SmoothingConstant;
			textMaterial.Smooth = defaultFont.Smooth;
			rectangle.SetVisualOffset(styleOrDefault.XOffset, styleOrDefault.YOffset);
			rectangle.CalculateMatrixFrame(in base.ParentWidget.AreaRect);
			drawObject.Rectangle = rectangle;
			richTextPart.TextDrawObject = drawObject;
			if (textMaterial.GlowRadius > 0f || textMaterial.Blur > 0f || textMaterial.OutlineAmount > 0f)
			{
				TextMaterial textMaterial2 = styleOrDefault.CreateTextMaterial(drawContext);
				textMaterial2.CopyFrom(textMaterial);
				drawContext.Draw(textMaterial2, in drawObject);
			}
			textMaterial.GlowRadius = 0f;
			textMaterial.Blur = 0f;
			textMaterial.OutlineAmount = 0f;
			drawContext.Draw(textMaterial, in drawObject);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void RenderImage(RichTextPart richTextPart, TwoDimensionDrawContext drawContext)
	{
		Sprite sprite = richTextPart.Sprite;
		if (sprite?.Texture != null && richTextPart.ImageDrawObject.IsValid)
		{
			ImageDrawObject drawObject = richTextPart.ImageDrawObject;
			Rectangle2D rectangle = drawObject.Rectangle;
			rectangle.LocalPosition = new Vector2(base.LocalPosition.X + richTextPart.SpritePosition.X, base.LocalPosition.Y + richTextPart.SpritePosition.Y);
			if (!_textureMaterialDict.ContainsKey(sprite.Texture))
			{
				_textureMaterialDict[sprite.Texture] = new SimpleMaterial(sprite.Texture);
			}
			SimpleMaterial simpleMaterial = _textureMaterialDict[sprite.Texture];
			if (simpleMaterial.ColorFactor != base.ReadOnlyBrush.GlobalColorFactor)
			{
				simpleMaterial.ColorFactor = base.ReadOnlyBrush.GlobalColorFactor;
			}
			if (simpleMaterial.AlphaFactor != base.ReadOnlyBrush.GlobalAlphaFactor * base.Context.ContextAlpha)
			{
				simpleMaterial.AlphaFactor = base.ReadOnlyBrush.GlobalAlphaFactor * base.Context.ContextAlpha;
			}
			if (simpleMaterial.Color != base.ReadOnlyBrush.GlobalColor)
			{
				simpleMaterial.Color = base.ReadOnlyBrush.GlobalColor;
			}
			rectangle.CalculateMatrixFrame(in base.ParentWidget.AreaRect);
			drawObject.Rectangle = rectangle;
			richTextPart.ImageDrawObject = drawObject;
			drawContext.Draw(simpleMaterial, in drawObject);
		}
	}

	protected internal override void OnMousePressed()
	{
		if (_mouseState == MouseState.None)
		{
			_mouseDownPosition = LocalMousePosition;
			_mouseState = MouseState.Down;
		}
	}

	protected internal override void OnMouseReleased(bool isFromInput)
	{
		if (_mouseState == MouseState.Down)
		{
			_mouseState = (isFromInput ? MouseState.Up : MouseState.None);
		}
	}

	protected internal override void OnMouseAlternatePressed()
	{
		if (_mouseState == MouseState.None)
		{
			_mouseDownPosition = LocalMousePosition;
			_mouseState = MouseState.AlternateDown;
		}
	}

	protected internal override void OnMouseAlternateReleased(bool isFromInput)
	{
		if (_mouseState == MouseState.AlternateDown)
		{
			_mouseState = (isFromInput ? MouseState.AlternateUp : MouseState.None);
		}
	}
}
