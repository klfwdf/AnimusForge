using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension.BitmapFont;

namespace TaleWorlds.TwoDimension;

public class Text : IText
{
	private TextHorizontalAlignment _horizontalAlignment;

	private TextVerticalAlignment _verticalAlignment;

	private bool _meshNeedsUpdate;

	private bool _preferredSizeNeedsUpdate;

	private bool _fixedHeight;

	private bool _fixedWidth;

	private float _desiredHeight;

	private float _desiredWidth;

	private Vector2 _preferredSize;

	private string _text;

	private List<TextToken> _tokens;

	private List<TextPart> _textParts;

	private int _fontSize;

	private Font _font;

	private float _scaleValue;

	private int _numOfAddedSeparators;

	private readonly Func<int, Font> _getUsableFontForCharacter;

	private bool _skipLineOnContainerExceeded = true;

	private bool _resizeTextOnOverflow = true;

	private bool _canBreakWords = true;

	public ILanguage CurrentLanguage { get; set; }

	public float ScaleToFitTextInLayout { get; private set; } = 1f;

	public int LineCount { get; private set; }

	internal TextOutput TextOutput { get; private set; }

	public int Width { get; private set; }

	public int Height { get; private set; }

	public Font Font
	{
		get
		{
			return _font;
		}
		set
		{
			if (_font != value)
			{
				_font = value;
				SetAllDirty();
			}
		}
	}

	private float ExtraPaddingHorizontal => 0.5f;

	private float ExtraPaddingVertical => 5f;

	private int _textLength => _text.Length + _numOfAddedSeparators;

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
				SetAllDirty();
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
				SetAllDirty();
			}
		}
	}

	public float FontSize
	{
		get
		{
			return _fontSize;
		}
		set
		{
			if (_fontSize != (int)Mathf.Round(value))
			{
				_fontSize = (int)Mathf.Round(value);
				SetAllDirty();
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
				_tokens = TextParser.Parse(text, CurrentLanguage);
				SetAllDirty();
			}
		}
	}

	private float EmptyCharacterWidth => ((float)Font.Characters[32].XAdvance + ExtraPaddingHorizontal) * _scaleValue;

	private float LineHeight => ((float)Font.Base + ExtraPaddingVertical) * _scaleValue;

	public bool SkipLineOnContainerExceeded
	{
		get
		{
			return _skipLineOnContainerExceeded;
		}
		set
		{
			if (value != _skipLineOnContainerExceeded)
			{
				_skipLineOnContainerExceeded = value;
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

	public bool ResizeTextOnOverflow
	{
		get
		{
			return _resizeTextOnOverflow;
		}
		set
		{
			if (value != _resizeTextOnOverflow)
			{
				_resizeTextOnOverflow = value;
				SetAllDirty();
			}
		}
	}

	public Text(int width, int height, Font bitmapFont, Func<int, Font> getUsableFontForCharacter)
	{
		Font = bitmapFont;
		Width = width;
		Height = height;
		_getUsableFontForCharacter = getUsableFontForCharacter;
		_textParts = new List<TextPart>();
		SetAllDirty();
		_text = "";
		_fontSize = 32;
		_tokens = null;
	}

	public Vector2 GetPreferredSize(bool fixedWidth, float widthSize, bool fixedHeight, float heightSize, SpriteData spriteData, float renderScale)
	{
		_fixedWidth = fixedWidth;
		_fixedHeight = fixedHeight;
		_desiredHeight = heightSize;
		_desiredWidth = widthSize;
		if (_preferredSizeNeedsUpdate)
		{
			_preferredSize = new Vector2(0f, 0f);
			if (_fontSize != 0 && !string.IsNullOrEmpty(_text))
			{
				_scaleValue = (float)_fontSize / (float)Font.Size;
				float num = 0f;
				LineCount = 1;
				for (int i = 0; i < _tokens.Count; i++)
				{
					TextToken textToken = _tokens[i];
					if (textToken.Type == TextToken.TokenType.Tag)
					{
						continue;
					}
					if (textToken.Type == TextToken.TokenType.NewLine)
					{
						if (num > _preferredSize.X)
						{
							_preferredSize.X = num;
						}
						LineCount++;
						num = 0f;
					}
					else if (textToken.Type == TextToken.TokenType.EmptyCharacter || textToken.Type == TextToken.TokenType.NonBreakingSpace)
					{
						num += EmptyCharacterWidth;
					}
					else
					{
						if (textToken.Type != TextToken.TokenType.Character)
						{
							continue;
						}
						char token = textToken.Token;
						float num2 = Font.GetCharacterWidth(token, ExtraPaddingHorizontal) * _scaleValue;
						if (!Font.Characters.ContainsKey(token))
						{
							Font font = _getUsableFontForCharacter(token) ?? Font;
							float num3 = (float)_fontSize / (float)font.Size;
							num2 = font.GetCharacterWidth(token, ExtraPaddingHorizontal) * num3;
						}
						if (fixedWidth && _skipLineOnContainerExceeded)
						{
							if (num + num2 > _desiredWidth && num > 0f)
							{
								int indexOfFirstAppropriateCharacterToMoveToNextLineBackwardsFromIndex = TextHelper.GetIndexOfFirstAppropriateCharacterToMoveToNextLineBackwardsFromIndex(_tokens, i, CurrentLanguage, CanBreakWords);
								if (indexOfFirstAppropriateCharacterToMoveToNextLineBackwardsFromIndex != -1)
								{
									float totalWordWidthBetweenIndices = TextHelper.GetTotalWordWidthBetweenIndices(indexOfFirstAppropriateCharacterToMoveToNextLineBackwardsFromIndex, i, _tokens, GetFontForTextToken, ExtraPaddingHorizontal, _fontSize);
									num -= totalWordWidthBetweenIndices;
									if (num > _preferredSize.X)
									{
										_preferredSize.X = num;
									}
									LineCount++;
									num = totalWordWidthBetweenIndices + num2;
								}
								else if (CanBreakWords)
								{
									int startIndex = Math.Max(0, _tokens.Count - 2);
									int endIndex = Math.Max(0, _tokens.Count - 1);
									float totalWordWidthBetweenIndices2 = TextHelper.GetTotalWordWidthBetweenIndices(startIndex, endIndex, _tokens, GetFontForTextToken, ExtraPaddingHorizontal, _fontSize);
									num -= totalWordWidthBetweenIndices2;
									if (num > _preferredSize.X)
									{
										_preferredSize.X = num;
									}
									LineCount++;
									num = totalWordWidthBetweenIndices2 + num2;
								}
								else
								{
									num += num2;
								}
							}
							else
							{
								num += num2;
							}
						}
						else
						{
							num += num2;
						}
					}
				}
				if (num > _preferredSize.X)
				{
					_preferredSize.X = num;
				}
				_preferredSize.Y = (float)LineCount * LineHeight;
			}
			_preferredSize = new Vector2((float)Math.Ceiling(_preferredSize.X), (float)Math.Ceiling(_preferredSize.Y));
			_preferredSizeNeedsUpdate = false;
		}
		return _preferredSize;
	}

	public void UpdateSize(int width, int height)
	{
		if (Width != width || Height != height)
		{
			Width = width;
			Height = height;
			SetAllDirty();
			ScaleToFitTextInLayout = 1f;
		}
	}

	public void SetAllDirty()
	{
		_meshNeedsUpdate = true;
		_preferredSizeNeedsUpdate = true;
	}

	private Font GetFontForTextToken(TextToken token)
	{
		if (Font.Characters.ContainsKey(token.Token))
		{
			return Font;
		}
		return _getUsableFontForCharacter(token.Token);
	}

	private void UpdateMesh()
	{
		RecalculateTextMesh(_fontSize);
		if (ResizeTextOnOverflow)
		{
			float num = _fontSize;
			int num2 = 0;
			while (ScaleToFitTextInLayout < 0.9f && num2 < 3)
			{
				num2++;
				num *= TaleWorlds.Library.MathF.Sqrt(ScaleToFitTextInLayout);
				RecalculateTextMesh(num);
			}
			if (ScaleToFitTextInLayout != 1f)
			{
				num *= ScaleToFitTextInLayout;
				RecalculateTextMesh(num);
			}
		}
	}

	private void RecalculateTextMesh(float desiredFontSize)
	{
		_textParts.Clear();
		TextOutput?.Clear();
		_numOfAddedSeparators = 0;
		if (desiredFontSize != 0f && !string.IsNullOrEmpty(_text))
		{
			_scaleValue = desiredFontSize / (float)Font.Size;
			TextOutput = new TextOutput(LineHeight);
			for (int i = 0; i < _tokens.Count; i++)
			{
				TextToken textToken = _tokens[i];
				if (textToken.Type == TextToken.TokenType.NewLine)
				{
					TextOutput.AddNewLine(currentLineEnded: true);
				}
				else if (textToken.Type == TextToken.TokenType.EmptyCharacter || textToken.Type == TextToken.TokenType.NonBreakingSpace)
				{
					TextOutput.AddToken(textToken, EmptyCharacterWidth, _scaleValue);
				}
				else
				{
					if (textToken.Type == TextToken.TokenType.ZeroWidthSpace)
					{
						continue;
					}
					if (textToken.Type == TextToken.TokenType.WordJoiner)
					{
						TextOutput.AddToken(textToken, 0f, _scaleValue);
					}
					else
					{
						if (textToken.Type != TextToken.TokenType.Character)
						{
							continue;
						}
						char token = textToken.Token;
						float num = Font.GetCharacterWidth(token, ExtraPaddingHorizontal) * _scaleValue;
						if (!Font.Characters.ContainsKey(token))
						{
							Font font = _getUsableFontForCharacter(token) ?? Font;
							float num2 = desiredFontSize / (float)font.Size;
							num = font.GetCharacterWidth(token, ExtraPaddingHorizontal) * num2;
						}
						bool flag = TextOutput.LastLineWidth + num > (float)Width;
						if (_fixedWidth && flag && SkipLineOnContainerExceeded)
						{
							int indexOfFirstAppropriateCharacterToMoveToNextLineBackwardsFromIndex = TextHelper.GetIndexOfFirstAppropriateCharacterToMoveToNextLineBackwardsFromIndex(_tokens, i, CurrentLanguage, CanBreakWords);
							int num3 = TextHelper.GetIndexOfFirstAppropriateCharacterToMoveToNextLineForwardsFromIndex(_tokens, i, CurrentLanguage, CanBreakWords);
							float num4 = 0f;
							if (indexOfFirstAppropriateCharacterToMoveToNextLineBackwardsFromIndex != -1)
							{
								if (num3 == -1)
								{
									num3 = _tokens.Count;
								}
								num4 = TextHelper.GetTotalWordWidthBetweenIndices(indexOfFirstAppropriateCharacterToMoveToNextLineBackwardsFromIndex, num3, _tokens, GetFontForTextToken, ExtraPaddingHorizontal, desiredFontSize);
							}
							bool flag2 = num <= (float)Width;
							bool flag3 = flag2 && (num4 == 0f || (indexOfFirstAppropriateCharacterToMoveToNextLineBackwardsFromIndex != -1 && num4 <= (float)Width));
							if (CanBreakWords && (!flag3 || indexOfFirstAppropriateCharacterToMoveToNextLineBackwardsFromIndex == -1))
							{
								float num5 = Font.GetCharacterWidth(CurrentLanguage.GetLineSeperatorChar(), ExtraPaddingHorizontal) * _scaleValue;
								if (!flag2)
								{
									TextOutput.AddToken(textToken, num, _scaleValue);
									if (i != _tokens.Count - 1)
									{
										if (_tokens[i + 1].Type == TextToken.TokenType.Character && !TextHelper.IsTokenEqualToSeparatorChar(textToken, CurrentLanguage) && !TextHelper.IsTokenEqualToSeparatorChar(_tokens[i + 1], CurrentLanguage))
										{
											_numOfAddedSeparators++;
											TextOutput.AddToken(TextToken.CreateCharacter(CurrentLanguage.GetLineSeperatorChar()), num5, _scaleValue);
										}
										TextOutput.AddNewLine(currentLineEnded: false);
									}
								}
								else if (TextOutput.Tokens.Any())
								{
									TextTokenOutput? textTokenOutput = TextOutput.Tokens.LastOrDefault();
									bool num6 = textTokenOutput == null || textTokenOutput.Token.Type != TextToken.TokenType.Character || TextHelper.IsTokenEqualToSeparatorChar(TextOutput.Tokens.LastOrDefault()?.Token, CurrentLanguage) || TextHelper.IsTokenEqualToSeparatorChar(textToken, CurrentLanguage);
									bool flag4 = TextOutput.LastLineWidth + num5 > (float)Width;
									TextTokenOutput textTokenOutput2 = null;
									if (!num6 && flag4)
									{
										textTokenOutput2 = TextOutput.RemoveTokensFromEnd(1).First();
									}
									TextToken token2 = textTokenOutput2?.Token ?? textToken;
									TextTokenOutput? textTokenOutput3 = TextOutput.Tokens.LastOrDefault();
									if (textTokenOutput3 != null && textTokenOutput3.Token.Type == TextToken.TokenType.Character && !TextHelper.IsTokenEqualToSeparatorChar(TextOutput.Tokens.LastOrDefault()?.Token, CurrentLanguage) && !TextHelper.IsTokenEqualToSeparatorChar(token2, CurrentLanguage))
									{
										_numOfAddedSeparators++;
										TextOutput.AddToken(TextToken.CreateCharacter(CurrentLanguage.GetLineSeperatorChar()), num5, _scaleValue);
									}
									TextOutput.AddNewLine(currentLineEnded: false);
									if (textTokenOutput2 != null)
									{
										TextOutput.AddToken(textTokenOutput2.Token, textTokenOutput2.Width, textTokenOutput2.Scale);
									}
									TextOutput.AddToken(textToken, num, _scaleValue);
								}
								continue;
							}
							if (indexOfFirstAppropriateCharacterToMoveToNextLineBackwardsFromIndex != -1)
							{
								List<TextTokenOutput> list = TextOutput.RemoveTokensFromEnd(i - indexOfFirstAppropriateCharacterToMoveToNextLineBackwardsFromIndex);
								TextOutput.AddNewLine(currentLineEnded: false);
								for (int num7 = list.Count - 1; num7 >= 0; num7--)
								{
									TextTokenOutput textTokenOutput4 = list[num7];
									if (textTokenOutput4.Token.Type != TextToken.TokenType.EmptyCharacter && textTokenOutput4.Token.Type != TextToken.TokenType.ZeroWidthSpace)
									{
										TextOutput.AddToken(textTokenOutput4.Token, textTokenOutput4.Width, _scaleValue);
									}
								}
							}
							TextOutput.AddToken(textToken, num, _scaleValue);
						}
						else
						{
							TextOutput.AddToken(textToken, num, _scaleValue);
						}
					}
				}
			}
			float num8 = 0f;
			float num9 = 0f;
			for (int j = 0; j < TextOutput.LineCount; j++)
			{
				num8 = 0f;
				TextLineOutput line = TextOutput.GetLine(j);
				float num10 = EmptyCharacterWidth;
				switch (_horizontalAlignment)
				{
				case TextHorizontalAlignment.Center:
				{
					float num13 = 0f;
					if (!line.LineEnded)
					{
						for (int l = 1; l < line.TokenCount && line.GetToken(line.TokenCount - l).Type == TextToken.TokenType.EmptyCharacter; l++)
						{
							num13 += EmptyCharacterWidth;
						}
						for (int l = 0; l < line.TokenCount && line.GetToken(l).Type == TextToken.TokenType.EmptyCharacter; l++)
						{
							num13 += EmptyCharacterWidth;
						}
					}
					num8 = ((float)Width - (line.Width - num13)) * 0.5f;
					break;
				}
				case TextHorizontalAlignment.Right:
					num8 = (float)Width - line.Width;
					break;
				case TextHorizontalAlignment.Justify:
				{
					float num11 = (float)Width - line.TextWidth;
					if (!line.LineEnded)
					{
						int num12 = line.EmptyCharacterCount;
						for (int k = 1; line.GetToken(line.TokenCount - k).Type == TextToken.TokenType.EmptyCharacter; k++)
						{
							num12--;
						}
						for (int k = 0; line.GetToken(k).Type == TextToken.TokenType.EmptyCharacter; k++)
						{
							num12--;
						}
						num10 = num11 / (float)num12;
					}
					break;
				}
				}
				for (int m = 0; m < line.TokenCount; m++)
				{
					Font font2 = Font;
					TextToken token3 = line.GetToken(m);
					switch (token3.Type)
					{
					case TextToken.TokenType.EmptyCharacter:
					case TextToken.TokenType.NonBreakingSpace:
						GetOrCreateTextPart(font2, num8, num9, desiredFontSize).WordWidth += num10;
						num8 += num10;
						break;
					case TextToken.TokenType.Character:
					{
						int num14 = token3.Token;
						float num15 = _scaleValue;
						if (!Font.Characters.ContainsKey(num14))
						{
							font2 = _getUsableFontForCharacter(num14);
							if (font2 == null)
							{
								font2 = Font;
								num14 = 0;
							}
							else
							{
								num15 = desiredFontSize / (float)font2.Size;
							}
						}
						TextPart orCreateTextPart = GetOrCreateTextPart(font2, num8, num9, desiredFontSize);
						BitmapFontCharacter fontCharacter = font2.Characters[num14];
						float x = num8 + (float)fontCharacter.XOffset * _scaleValue;
						float y = num9 + (float)fontCharacter.YOffset * _scaleValue;
						orCreateTextPart.TextMeshGenerator.AddCharacterToMesh(x, y, fontCharacter);
						orCreateTextPart.WordWidth += ((float)fontCharacter.XAdvance + ExtraPaddingHorizontal) * num15;
						num8 += ((float)fontCharacter.XAdvance + ExtraPaddingHorizontal) * num15;
						break;
					}
					}
				}
				num9 += LineHeight;
			}
			if (_verticalAlignment == TextVerticalAlignment.Center || _verticalAlignment == TextVerticalAlignment.Bottom)
			{
				float extraY;
				if (_verticalAlignment == TextVerticalAlignment.Center)
				{
					extraY = (float)Height - num9;
					extraY *= 0.5f;
				}
				else
				{
					extraY = (float)Height - num9;
				}
				_textParts.ForEach(delegate(TextPart textPart)
				{
					textPart.TextMeshGenerator.AddValueToY(extraY);
				});
			}
			GenerateMeshes();
			ScaleToFitTextInLayout = 1f;
			if (_fixedHeight && num9 > _desiredHeight && _desiredHeight > 1f)
			{
				ScaleToFitTextInLayout = _desiredHeight / num9;
			}
			if (_fixedWidth && num8 > _desiredWidth && _desiredWidth > 1f)
			{
				ScaleToFitTextInLayout = Math.Min(ScaleToFitTextInLayout, _desiredWidth / num8);
			}
		}
		else
		{
			ScaleToFitTextInLayout = 1f;
		}
	}

	private void GenerateMeshes()
	{
		for (int i = 0; i < _textParts.Count; i++)
		{
			TextPart textPart = _textParts[i];
			textPart.DrawObject2D = textPart.TextMeshGenerator.GenerateMesh();
		}
	}

	private TextPart GetOrCreateTextPart(Font font, float x, float y, float fontSize)
	{
		TextPart textPart = _textParts.LastOrDefault();
		if (textPart != null && textPart.DefaultFont == font)
		{
			return textPart;
		}
		float scaleValue = fontSize / (float)font.Size;
		TextMeshGenerator textMeshGenerator = new TextMeshGenerator();
		textMeshGenerator.Refresh(font, _textLength, scaleValue);
		TextPart textPart2 = new TextPart
		{
			TextMeshGenerator = textMeshGenerator,
			WordWidth = 0f,
			PartPosition = new Vector2(x, y),
			DefaultFont = font
		};
		_textParts.Add(textPart2);
		return textPart2;
	}

	public List<TextPart> GetParts()
	{
		if (_meshNeedsUpdate)
		{
			UpdateMesh();
			_meshNeedsUpdate = false;
		}
		return _textParts;
	}
}
