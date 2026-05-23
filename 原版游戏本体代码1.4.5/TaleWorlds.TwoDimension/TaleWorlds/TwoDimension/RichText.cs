using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension.BitmapFont;

namespace TaleWorlds.TwoDimension;

public class RichText : IText
{
	public ILanguage CurrentLanguage;

	private TextHorizontalAlignment _horizontalAlignment;

	private TextVerticalAlignment _verticalAlignment;

	private bool _meshNeedsUpdate = true;

	private bool _preferredSizeNeedsUpdate = true;

	private bool _positionNeedsUpdate = true;

	private bool _tokensNeedUpdate = true;

	private bool _isFixedWidth;

	private bool _isFixedHeight;

	private Vector2 _preferredSize;

	private string _text;

	private List<TextToken> _tokens;

	private float _widthSize = -1f;

	protected const float ExtraLetterPaddingHorizontal = 0.5f;

	protected const float ExtraLetterPaddingVertical = 5f;

	private const float RichTextIconHorizontalPadding = 8f;

	private const float RichTextIconVerticalPadding = 0f;

	private List<RichTextPart> _richTextParts;

	private List<RichTextLinkGroup> _linkGroups;

	private Stack<string> _styleStack;

	private TextTokenOutput _focusedToken;

	private RichTextLinkGroup _focusedLinkGroup;

	private bool _gotFocus;

	private int _numOfAddedSeparators;

	protected readonly Func<int, Font> _getUsableFontForCharacter;

	private bool _shouldAddNewLineWhenExceedingContainerWidth = true;

	private bool _canBreakWords = true;

	internal int Width { get; private set; }

	internal int Height { get; private set; }

	internal float WidthSize
	{
		get
		{
			if (!(_widthSize < 1E-05f))
			{
				return _widthSize;
			}
			return Width;
		}
	}

	public string CurrentStyle { get; set; } = "Default";

	public int TextHeight
	{
		get
		{
			if (TextOutput == null)
			{
				return -1;
			}
			return (int)TextOutput.TextHeight;
		}
	}

	public StyleFontContainer StyleFontContainer { get; private set; }

	public TextHorizontalAlignment HorizontalAlignment
	{
		get
		{
			return _horizontalAlignment;
		}
		set
		{
			if (_horizontalAlignment != value)
			{
				_horizontalAlignment = value;
				_positionNeedsUpdate = true;
			}
		}
	}

	public TextVerticalAlignment VerticalAlignment
	{
		get
		{
			return _verticalAlignment;
		}
		set
		{
			if (_verticalAlignment != value)
			{
				_verticalAlignment = value;
				_positionNeedsUpdate = true;
			}
		}
	}

	public string Value
	{
		get
		{
			return _text;
		}
		set
		{
			string text = value;
			if (text == null)
			{
				text = "";
			}
			if (_text != text)
			{
				_text = text;
				try
				{
					_tokens = RichTextParser.Parse(text);
				}
				catch (RichTextException ex)
				{
					_ = ex.Message;
					_tokens = TextToken.CreateTokenArrayFromWord(text);
				}
				SetAllDirty();
			}
		}
	}

	internal TextOutput TextOutput { get; private set; }

	private int _textLength => _text.Length + _numOfAddedSeparators;

	public RichTextLinkGroup FocusedLinkGroup => _focusedLinkGroup;

	public bool SkipLineOnContainerExceeded
	{
		get
		{
			return _shouldAddNewLineWhenExceedingContainerWidth;
		}
		set
		{
			if (value != _shouldAddNewLineWhenExceedingContainerWidth)
			{
				_shouldAddNewLineWhenExceedingContainerWidth = value;
				SetAllDirty();
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
				SetAllDirty();
			}
		}
	}

	public RichText(int width, int height, Font font, Func<int, Font> getUsableFontForCharacter)
	{
		Width = width;
		Height = height;
		_getUsableFontForCharacter = getUsableFontForCharacter;
		_gotFocus = false;
		_text = "";
		_tokens = null;
		TextOutput = new TextOutput(0f);
		_focusedToken = null;
		_focusedLinkGroup = null;
		_richTextParts = new List<RichTextPart>();
		_linkGroups = new List<RichTextLinkGroup>();
		_styleStack = new Stack<string>();
		StyleFontContainer = new StyleFontContainer();
		StyleFontContainer.Add("Default", font, 1f);
	}

	public virtual void Update(float dt, SpriteData spriteData, Vector2 focusPosition, bool focus, bool isFixedWidth, bool isFixedHeight, float renderScale)
	{
		bool flag = false;
		bool num = string.IsNullOrEmpty(_text);
		_isFixedWidth = isFixedWidth;
		_isFixedHeight = isFixedHeight;
		if (num)
		{
			return;
		}
		if (_tokensNeedUpdate)
		{
			CalculateTextOutput(WidthSize, Height, spriteData, renderScale);
			flag = true;
		}
		if (_gotFocus != focus)
		{
			_gotFocus = focus;
			flag = true;
		}
		if (_meshNeedsUpdate)
		{
			FindLinkGroups();
			flag = true;
			_meshNeedsUpdate = false;
		}
		TextTokenOutput tokenUnderPosition = GetTokenUnderPosition(focusPosition);
		if (tokenUnderPosition != _focusedToken)
		{
			_focusedToken = tokenUnderPosition;
			RichTextLinkGroup richTextLinkGroup = FindLinkGroup(_focusedToken?.Token);
			if (_focusedLinkGroup != richTextLinkGroup)
			{
				_focusedLinkGroup = richTextLinkGroup;
				flag = true;
			}
		}
		if (_positionNeedsUpdate)
		{
			PositionTokensInTextOutput(spriteData, renderScale);
			if (!_positionNeedsUpdate)
			{
				flag = true;
			}
		}
		if (flag)
		{
			FillPartsWithTokens(spriteData, renderScale);
			GenerateMeshes(renderScale);
		}
	}

	public void SetAllDirty()
	{
		_meshNeedsUpdate = true;
		_preferredSizeNeedsUpdate = true;
		_positionNeedsUpdate = true;
		_tokensNeedUpdate = true;
	}

	private float GetEmptyCharacterWidth(Font font, float scaleValue)
	{
		return ((float)font.Characters[32].XAdvance + 0.5f) * scaleValue;
	}

	public Vector2 GetPreferredSize(bool fixedWidth, float widthSize, bool fixedHeight, float heightSize, SpriteData spriteData, float renderScale)
	{
		_isFixedHeight = fixedHeight;
		_isFixedWidth = fixedWidth;
		_widthSize = widthSize;
		if (!string.IsNullOrEmpty(_text))
		{
			if (_tokensNeedUpdate)
			{
				CalculateTextOutput(WidthSize, heightSize, spriteData, renderScale);
			}
			if (_preferredSizeNeedsUpdate)
			{
				TextOutput textOutput = TextOutput;
				if (textOutput != null && textOutput.Tokens?.Any() == true)
				{
					Vector2 preferredSize = _preferredSize;
					float maxLineWidth = TextOutput.MaxLineWidth;
					_preferredSize = new Vector2(y: (float)Math.Ceiling(TextOutput.TextHeight), x: (float)Math.Ceiling(maxLineWidth));
					if (preferredSize != _preferredSize)
					{
						_meshNeedsUpdate = true;
						_positionNeedsUpdate = true;
					}
					_preferredSizeNeedsUpdate = false;
				}
			}
		}
		return _preferredSize;
	}

	public void CalculateTextOutput(float width, float height, SpriteData spriteData, float renderScale)
	{
		if (!_tokensNeedUpdate)
		{
			return;
		}
		TextOutput.Clear();
		_numOfAddedSeparators = 0;
		_styleStack.Clear();
		_styleStack.Push(CurrentStyle);
		for (int i = 0; i < _tokens.Count; i++)
		{
			TextToken textToken = _tokens[i];
			string style = _styleStack.Peek();
			StyleFontContainer.FontData fontData = StyleFontContainer.GetFontData(style);
			Font font = fontData.Font;
			float num = fontData.FontSize / (float)font.Size;
			float num2 = ((float)font.Base + 5f) * num;
			if (textToken.Type == TextToken.TokenType.NewLine)
			{
				TextOutput.AddNewLine(currentLineEnded: true, num2);
			}
			else if (textToken.Type == TextToken.TokenType.EmptyCharacter)
			{
				float emptyCharacterWidth = GetEmptyCharacterWidth(font, num);
				bool flag = TextOutput.LastLineWidth + emptyCharacterWidth > width;
				float num3 = TextOutput.TextHeight + num2;
				bool flag2 = !_isFixedHeight || num3 <= height;
				if (_isFixedWidth && flag && flag2 && SkipLineOnContainerExceeded)
				{
					TextOutput.AddNewLine(currentLineEnded: false, num2);
				}
				else
				{
					TextOutput.AddToken(textToken, emptyCharacterWidth, num, style, num2);
				}
			}
			else if (textToken.Type == TextToken.TokenType.ZeroWidthSpace)
			{
				TextOutput.AddToken(textToken, 0f, num, style, num2);
			}
			else if (textToken.Type == TextToken.TokenType.NonBreakingSpace)
			{
				float emptyCharacterWidth2 = GetEmptyCharacterWidth(font, num);
				TextOutput.AddToken(textToken, emptyCharacterWidth2, num, style, num2);
			}
			else if (textToken.Type == TextToken.TokenType.WordJoiner)
			{
				TextOutput.AddToken(textToken, 0f, num, style, num2);
			}
			else if (textToken.Type == TextToken.TokenType.Character)
			{
				char token = textToken.Token;
				float num4 = 0f;
				if (!font.Characters.ContainsKey(token))
				{
					Font font2 = _getUsableFontForCharacter(token) ?? fontData.Font;
					num = fontData.FontSize / (float)font2.Size;
					num4 = font2.GetCharacterWidth(token, 0.5f) * num;
				}
				else
				{
					num = fontData.FontSize / (float)font.Size;
					num4 = font.GetCharacterWidth(token, 0.5f) * num;
				}
				bool flag3 = TextOutput.LastLineWidth + num4 > width;
				if (_isFixedWidth && flag3 && SkipLineOnContainerExceeded)
				{
					List<TextToken> list = TextOutput.Tokens.Select((TextTokenOutput t) => t.Token).ToList();
					int indexOfFirstAppropriateCharacterToMoveToNextLineBackwardsFromIndex = TextHelper.GetIndexOfFirstAppropriateCharacterToMoveToNextLineBackwardsFromIndex(list, list.Count - 1, CurrentLanguage, CanBreakWords);
					int num5 = TextHelper.GetIndexOfFirstAppropriateCharacterToMoveToNextLineForwardsFromIndex(_tokens, i, CurrentLanguage, CanBreakWords);
					float num6 = 0f;
					if (indexOfFirstAppropriateCharacterToMoveToNextLineBackwardsFromIndex != -1)
					{
						if (num5 == -1)
						{
							num5 = _tokens.Count;
						}
						num6 = TextHelper.GetTotalWordWidthBetweenIndices(indexOfFirstAppropriateCharacterToMoveToNextLineBackwardsFromIndex, num5, _tokens, GetFontForTextToken, 0.5f, fontData.FontSize);
					}
					bool flag4 = num4 <= width;
					bool flag5 = flag4 && (num6 == 0f || (indexOfFirstAppropriateCharacterToMoveToNextLineBackwardsFromIndex != -1 && num6 <= width));
					if (CanBreakWords && (!flag5 || indexOfFirstAppropriateCharacterToMoveToNextLineBackwardsFromIndex == -1))
					{
						float num7 = fontData.FontSize / (float)font.Size;
						float num8 = fontData.Font.GetCharacterWidth(CurrentLanguage.GetLineSeperatorChar(), 0.5f) * num7;
						if (!flag4)
						{
							TextOutput.AddToken(textToken, num4, num, style, num2);
							if (i != _tokens.Count - 1)
							{
								if (_tokens[i + 1].Type == TextToken.TokenType.Character && !TextHelper.IsTokenEqualToSeparatorChar(textToken, CurrentLanguage) && !TextHelper.IsTokenEqualToSeparatorChar(_tokens[i + 1], CurrentLanguage))
								{
									_numOfAddedSeparators++;
									TextOutput.AddToken(TextToken.CreateCharacter(CurrentLanguage.GetLineSeperatorChar()), num8, num7, style);
								}
								TextOutput.AddNewLine(currentLineEnded: false, num2);
							}
						}
						else if (TextOutput.Tokens.Any())
						{
							TextTokenOutput? textTokenOutput = TextOutput.Tokens.LastOrDefault();
							bool num9 = textTokenOutput == null || textTokenOutput.Token.Type != TextToken.TokenType.Character || TextHelper.IsTokenEqualToSeparatorChar(TextOutput.Tokens.LastOrDefault()?.Token, CurrentLanguage) || TextHelper.IsTokenEqualToSeparatorChar(textToken, CurrentLanguage);
							bool flag6 = TextOutput.LastLineWidth + num8 > width;
							TextTokenOutput textTokenOutput2 = null;
							if (!num9 && flag6)
							{
								textTokenOutput2 = TextOutput.RemoveTokensFromEnd(1).First();
							}
							TextToken token2 = textTokenOutput2?.Token ?? textToken;
							TextTokenOutput? textTokenOutput3 = TextOutput.Tokens.LastOrDefault();
							if (textTokenOutput3 != null && textTokenOutput3.Token.Type == TextToken.TokenType.Character && !TextHelper.IsTokenEqualToSeparatorChar(TextOutput.Tokens.LastOrDefault()?.Token, CurrentLanguage) && !TextHelper.IsTokenEqualToSeparatorChar(token2, CurrentLanguage))
							{
								_numOfAddedSeparators++;
								TextOutput.AddToken(TextToken.CreateCharacter(CurrentLanguage.GetLineSeperatorChar()), num8, num7, style);
							}
							TextOutput.AddNewLine(currentLineEnded: false, num2);
							if (textTokenOutput2 != null)
							{
								TextOutput.AddToken(textTokenOutput2.Token, textTokenOutput2.Width, textTokenOutput2.Scale, textTokenOutput2.Style);
							}
							TextOutput.AddToken(textToken, num4, num, style, num2);
						}
						continue;
					}
					if (indexOfFirstAppropriateCharacterToMoveToNextLineBackwardsFromIndex != -1)
					{
						List<TextTokenOutput> list2 = TextOutput.RemoveTokensFromEnd(list.Count - indexOfFirstAppropriateCharacterToMoveToNextLineBackwardsFromIndex);
						TextOutput.AddNewLine(currentLineEnded: false, num2);
						for (int num10 = list2.Count - 1; num10 >= 0; num10--)
						{
							TextTokenOutput textTokenOutput4 = list2[num10];
							if (textTokenOutput4.Token.Type != TextToken.TokenType.EmptyCharacter && textTokenOutput4.Token.Type != TextToken.TokenType.ZeroWidthSpace)
							{
								TextOutput.AddToken(textTokenOutput4.Token, textTokenOutput4.Width, textTokenOutput4.Scale, textTokenOutput4.Style, num2);
							}
						}
					}
					TextOutput.AddToken(textToken, num4, num, style, num2);
				}
				else
				{
					TextOutput.AddToken(textToken, num4, num, style, num2);
				}
			}
			else
			{
				if (textToken.Type != TextToken.TokenType.Tag)
				{
					continue;
				}
				RichTextTag tag = textToken.Tag;
				if (tag.Name == "img")
				{
					string attribute = tag.GetAttribute("src");
					Sprite sprite = null;
					if (!string.IsNullOrEmpty(attribute))
					{
						sprite = spriteData.GetSprite(attribute);
					}
					float num11 = 0f;
					if (sprite != null)
					{
						num11 = ((float)fontData.Font.Base + 0f) * num * ((float)sprite.Width / (float)sprite.Height) + 8f * renderScale;
					}
					bool flag7 = TextOutput.LastLineWidth + num11 > width;
					float num12 = TextOutput.TextHeight + num2;
					bool flag8 = !_isFixedHeight || num12 <= height;
					if (_isFixedWidth && flag7 && flag8 && SkipLineOnContainerExceeded)
					{
						TextOutput.AddNewLine(currentLineEnded: false, num2);
					}
					TextOutput.AddToken(textToken, num11, num, style, num2);
				}
				else if (tag.Name == "a")
				{
					FindLinkGroup(textToken);
					string text = "Link";
					string attribute2 = tag.GetAttribute("style");
					if (!string.IsNullOrEmpty(attribute2))
					{
						text = attribute2;
					}
					if (tag.Type == RichTextTagType.Open)
					{
						_styleStack.Push(text);
					}
					else if (tag.Type == RichTextTagType.Close)
					{
						_styleStack.Pop();
					}
					TextOutput.AddToken(textToken, 0f, num, text);
				}
				else if (tag.Name == "span")
				{
					string attribute3 = tag.GetAttribute("style");
					if (tag.Type == RichTextTagType.Open)
					{
						_styleStack.Push(attribute3);
					}
					else if (tag.Type == RichTextTagType.Close)
					{
						_styleStack.Pop();
					}
					TextOutput.AddToken(textToken, 0f, num, attribute3);
				}
			}
		}
		_tokensNeedUpdate = false;
	}

	public void UpdateSize(int width, int height)
	{
		bool num = Width != width;
		bool flag = Height != height;
		if (num || flag)
		{
			Width = Math.Max(0, width);
			Height = Math.Max(0, height);
			SetAllDirty();
		}
	}

	private TextTokenOutput GetTokenUnderPosition(Vector2 position)
	{
		if (position.X >= 0f && position.Y >= 0f && position.X < (float)Width && position.Y < (float)Height && TextOutput != null && TextOutput.Tokens != null)
		{
			foreach (TextTokenOutput token in TextOutput.Tokens)
			{
				if (token == null)
				{
					Debug.FailedAssert("TextOutputToken returned null. This shouldn't happen.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.TwoDimension\\BitmapFont\\RichText.cs", "GetTokenUnderPosition", 608);
				}
				else if (token.Rectangle.IsPointInside(position))
				{
					return token;
				}
			}
		}
		return null;
	}

	private void PositionTokensInTextOutput(SpriteData spriteData, float renderScale)
	{
		if (_preferredSize.X == 0f && _preferredSize.Y == 0f)
		{
			return;
		}
		float num = 0f;
		float num2 = 0f;
		if (_verticalAlignment == TextVerticalAlignment.Center || _verticalAlignment == TextVerticalAlignment.Bottom)
		{
			float num3 = 0f;
			float textHeight = TextOutput.TextHeight;
			if (_verticalAlignment == TextVerticalAlignment.Center)
			{
				num3 = (float)Height - textHeight;
				num3 *= 0.5f;
			}
			else
			{
				num3 = (float)Height - textHeight;
			}
			num2 += num3;
		}
		for (int i = 0; i < TextOutput.LineCount; i++)
		{
			TextLineOutput line = TextOutput.GetLine(i);
			float num4 = 0f;
			if (_horizontalAlignment != TextHorizontalAlignment.Left)
			{
				if (_horizontalAlignment == TextHorizontalAlignment.Center)
				{
					num = ((float)Width - line.Width) * 0.5f;
				}
				else if (_horizontalAlignment == TextHorizontalAlignment.Right)
				{
					num = (float)Width - line.Width;
				}
				else if (_horizontalAlignment == TextHorizontalAlignment.Justify)
				{
					float num5 = (float)Width - line.TextWidth;
					if (!line.LineEnded && line.TokenCount > 0)
					{
						int num6 = line.EmptyCharacterCount;
						for (int j = 1; line.GetToken(line.TokenCount - j).Type == TextToken.TokenType.EmptyCharacter; j++)
						{
							num6--;
						}
						for (int j = 0; line.GetToken(j).Type == TextToken.TokenType.EmptyCharacter; j++)
						{
							num6--;
						}
						num4 = num5 / (float)num6;
					}
				}
			}
			float num7 = 0f;
			for (int k = 0; k < line.TokenCount; k++)
			{
				TextTokenOutput tokenOutput = line.GetTokenOutput(k);
				StyleFontContainer.FontData fontData = StyleFontContainer.GetFontData(tokenOutput.Style);
				Font font = fontData.Font;
				float num8 = fontData.FontSize / (float)font.Size;
				float num9 = ((float)font.Base + 5f) * num8;
				if (num9 > num7)
				{
					num7 = num9;
				}
			}
			for (int l = 0; l < line.TokenCount; l++)
			{
				TextTokenOutput tokenOutput2 = line.GetTokenOutput(l);
				TextToken token = tokenOutput2.Token;
				StyleFontContainer.FontData fontData2 = StyleFontContainer.GetFontData(tokenOutput2.Style);
				Font font2 = fontData2.Font;
				float num10 = fontData2.FontSize / (float)font2.Size;
				float num11 = ((float)font2.Base + 5f) * num10;
				_ = font2.Base;
				tokenOutput2.SetPosition(num, num2 + (num7 - num11));
				if (token.Type == TextToken.TokenType.EmptyCharacter || token.Type == TextToken.TokenType.NonBreakingSpace)
				{
					num4 = GetEmptyCharacterWidth(font2, num10);
					num += num4;
				}
				else if (token.Type == TextToken.TokenType.Character)
				{
					char token2 = token.Token;
					float num12 = 0f;
					if (!font2.Characters.ContainsKey(token2))
					{
						Font font3 = _getUsableFontForCharacter(token2) ?? fontData2.Font;
						num10 = fontData2.FontSize / (float)font3.Size;
						num12 = font3.GetCharacterWidth(token2, 0.5f) * num10;
					}
					else
					{
						num10 = fontData2.FontSize / (float)font2.Size;
						num12 = font2.GetCharacterWidth(token2, 0.5f) * num10;
					}
					num += num12;
				}
				else
				{
					if (token.Type != TextToken.TokenType.Tag)
					{
						continue;
					}
					RichTextTag tag = token.Tag;
					if (tag.Name == "img")
					{
						string attribute = tag.GetAttribute("src");
						Sprite sprite = null;
						if (!string.IsNullOrEmpty(attribute))
						{
							sprite = spriteData.GetSprite(attribute);
						}
						float num13 = 0f;
						if (sprite != null)
						{
							num13 = ((float)font2.Base + 0f) * num10 * ((float)sprite.Width / (float)sprite.Height) + 8f * renderScale;
						}
						num += num13;
					}
				}
			}
			num = 0f;
			num2 += line.Height;
		}
		_positionNeedsUpdate = false;
	}

	private void FindLinkGroups()
	{
		_linkGroups.Clear();
		RichTextLinkGroup richTextLinkGroup = null;
		for (int i = 0; i < _tokens.Count; i++)
		{
			TextToken textToken = _tokens[i];
			if (textToken.Type == TextToken.TokenType.Tag)
			{
				RichTextTag tag = textToken.Tag;
				if (tag.Name == "a")
				{
					if (tag.Type == RichTextTagType.Open)
					{
						richTextLinkGroup = new RichTextLinkGroup(i, tag.GetAttribute("href"));
					}
					else if (tag.Type == RichTextTagType.Close)
					{
						richTextLinkGroup.AddToken(textToken);
						_linkGroups.Add(richTextLinkGroup);
						richTextLinkGroup = null;
					}
				}
			}
			richTextLinkGroup?.AddToken(textToken);
		}
	}

	private RichTextPart GetOrCreateTextPartOfStyle(string style, Font font, float x, float y)
	{
		RichTextPart richTextPart = _richTextParts.LastOrDefault();
		if (richTextPart != null && richTextPart.Type == RichTextPartType.Text && richTextPart.Style == style && richTextPart.DefaultFont == font)
		{
			return richTextPart;
		}
		float scaleValue = StyleFontContainer.GetFontData(style).FontSize / (float)font.Size;
		TextMeshGenerator textMeshGenerator = new TextMeshGenerator();
		textMeshGenerator.Refresh(font, _textLength, scaleValue);
		RichTextPart richTextPart2 = new RichTextPart
		{
			TextMeshGenerator = textMeshGenerator,
			Type = RichTextPartType.Text,
			Style = style,
			WordWidth = 0f,
			PartPosition = new Vector2(x, y),
			DefaultFont = font
		};
		_richTextParts.Add(richTextPart2);
		return richTextPart2;
	}

	private void FillPartsWithTokens(SpriteData spriteData, float renderScale)
	{
		_richTextParts.Clear();
		TextTokenOutput[] array = TextOutput.Tokens.ToArray();
		foreach (TextTokenOutput textTokenOutput in array)
		{
			string text = textTokenOutput.Style;
			TextToken token = textTokenOutput.Token;
			float x = textTokenOutput.X;
			float y = textTokenOutput.Y;
			StyleFontContainer.FontData fontData = StyleFontContainer.GetFontData(text);
			Font font = fontData.Font;
			float num = fontData.FontSize / (float)font.Size;
			if (token.Type == TextToken.TokenType.Character)
			{
				char token2 = token.Token;
				float num2 = x;
				float num3 = y;
				int num4 = token2;
				if (!fontData.Font.Characters.ContainsKey(num4))
				{
					font = _getUsableFontForCharacter(num4);
					if (font == null)
					{
						font = fontData.Font;
						num4 = 0;
					}
					num = fontData.FontSize / (float)font.Size;
				}
				else
				{
					font = fontData.Font;
					num = fontData.FontSize / (float)fontData.Font.Size;
				}
				RichTextLinkGroup richTextLinkGroup = FindLinkGroup(textTokenOutput.Token);
				if (richTextLinkGroup != null && _focusedLinkGroup == richTextLinkGroup)
				{
					text = ((!_gotFocus) ? (text + ".MouseOver") : (text + ".MouseDown"));
				}
				RichTextPart orCreateTextPartOfStyle = GetOrCreateTextPartOfStyle(text, font, x, y);
				TextMeshGenerator textMeshGenerator = orCreateTextPartOfStyle.TextMeshGenerator;
				BitmapFontCharacter fontCharacter = font.Characters[num4];
				float x2 = num2 + (float)fontCharacter.XOffset * num;
				float y2 = num3 + (float)fontCharacter.YOffset * num;
				textMeshGenerator.AddCharacterToMesh(x2, y2, fontCharacter);
				orCreateTextPartOfStyle.WordWidth += ((float)fontCharacter.XAdvance + 0.5f) * num;
				num2 += ((float)fontCharacter.XAdvance + 0.5f) * num;
			}
			else if (token.Type == TextToken.TokenType.EmptyCharacter || token.Type == TextToken.TokenType.NonBreakingSpace)
			{
				GetOrCreateTextPartOfStyle(text, font, x, y).WordWidth += GetEmptyCharacterWidth(font, num);
			}
			else
			{
				if (token.Type != TextToken.TokenType.Tag)
				{
					continue;
				}
				RichTextTag tag = token.Tag;
				if (tag.Name == "img")
				{
					string attribute = tag.GetAttribute("extend");
					float num5 = 0f;
					if (!string.IsNullOrEmpty(attribute) && float.TryParse(attribute, out var result))
					{
						num5 = result;
					}
					string attribute2 = tag.GetAttribute("src");
					Sprite sprite = null;
					if (!string.IsNullOrEmpty(attribute2))
					{
						sprite = spriteData.GetSprite(attribute2);
					}
					if (sprite != null)
					{
						float num6 = (float)font.Base * num * 0.2f;
						num6 -= num5 * renderScale;
						float num7 = (float)sprite.Width / (float)sprite.Height;
						float num8 = (float)font.Base * num * 0.1f;
						num8 -= num5 * renderScale * num7;
						num8 += 4f * renderScale;
						float x3 = x + num8;
						float y3 = y + num6;
						RichTextPart richTextPart = new RichTextPart();
						richTextPart.Sprite = sprite;
						richTextPart.SpritePosition = new Vector2(x3, y3);
						richTextPart.Type = RichTextPartType.Sprite;
						richTextPart.Extend = num5;
						_richTextParts.Add(richTextPart);
					}
				}
			}
		}
	}

	private void GenerateMeshes(float renderScale)
	{
		for (int i = 0; i < _richTextParts.Count; i++)
		{
			RichTextPart richTextPart = _richTextParts[i];
			Sprite sprite = richTextPart.Sprite;
			TextMeshGenerator textMeshGenerator = richTextPart.TextMeshGenerator;
			if (sprite != null)
			{
				StyleFontContainer.FontData fontData = StyleFontContainer.GetFontData(richTextPart.Style);
				float num = fontData.FontSize / (float)fontData.Font.Size;
				float num2 = (float)fontData.Font.Base * num * 0.8f + richTextPart.Extend * 2f * renderScale;
				float x = num2 * ((float)sprite.Width / (float)sprite.Height);
				Rectangle2D rectangle = Rectangle2D.Create();
				rectangle.LocalScale = new Vector2(x, num2);
				ImageDrawObject imageDrawObject = ImageDrawObject.Create(in rectangle, sprite.GetMinUvs(), sprite.GetMaxUvs());
				imageDrawObject.Scale = num;
				richTextPart.ImageDrawObject = imageDrawObject;
			}
			if (textMeshGenerator != null)
			{
				TextDrawObject textDrawObject = textMeshGenerator.GenerateMesh();
				richTextPart.TextDrawObject = textDrawObject;
			}
		}
	}

	private Font GetFontForTextToken(TextToken token)
	{
		Font font = StyleFontContainer.GetFontData("Default").Font;
		if (font.Characters.ContainsKey(token.Token))
		{
			return font;
		}
		return _getUsableFontForCharacter(token.Token);
	}

	public List<RichTextPart> GetParts()
	{
		return _richTextParts;
	}

	private RichTextLinkGroup FindLinkGroup(TextToken textToken)
	{
		foreach (RichTextLinkGroup linkGroup in _linkGroups)
		{
			if (linkGroup.Contains(textToken))
			{
				return linkGroup;
			}
		}
		return null;
	}
}
