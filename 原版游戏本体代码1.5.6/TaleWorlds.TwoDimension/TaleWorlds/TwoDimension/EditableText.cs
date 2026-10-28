using System;
using System.Numerics;
using System.Text.RegularExpressions;

namespace TaleWorlds.TwoDimension;

public class EditableText : RichText
{
	private bool _cursorVisible;

	private int _visibleStart;

	private int _selectionAnchor;

	private string _realVisibleText;

	private Regex _nextWordRegex;

	private float _scrollTextWhileDraggingCooldown;

	public int CursorPosition { get; private set; }

	public bool HighlightStart { get; set; }

	public bool HighlightEnd { get; set; }

	public int SelectedTextBegin { get; private set; }

	public int SelectedTextEnd { get; private set; }

	public float BlinkTimer { get; set; }

	public string VisibleText { get; set; }

	public EditableText(int width, int height, Font font, Func<int, Font> getUsableFontForCharacter)
		: base(width, height, font, getUsableFontForCharacter)
	{
		_cursorVisible = false;
		CursorPosition = 0;
		_visibleStart = 0;
		VisibleText = "";
		BlinkTimer = 0f;
		HighlightStart = false;
		HighlightEnd = true;
		_selectionAnchor = 0;
		string pattern = "\\w+";
		_nextWordRegex = new Regex(pattern);
	}

	public void SetCursorPosition(int position, bool visible)
	{
		if (CursorPosition != position || _cursorVisible != visible)
		{
			CursorPosition = position;
			if (_visibleStart > CursorPosition)
			{
				_visibleStart = CursorPosition;
			}
			_cursorVisible = visible;
			SetAllDirty();
		}
	}

	public void BlinkCursor()
	{
		_cursorVisible = !_cursorVisible;
	}

	public bool IsCursorVisible()
	{
		return _cursorVisible;
	}

	public void ResetSelected()
	{
		_selectionAnchor = 0;
		SelectedTextBegin = 0;
		SelectedTextEnd = 0;
		HighlightStart = false;
		HighlightEnd = true;
	}

	public void BeginSelection()
	{
		_selectionAnchor = CursorPosition;
	}

	public bool IsAnySelected()
	{
		return SelectedTextEnd != SelectedTextBegin;
	}

	public Vector2 GetCursorPosition()
	{
		StyleFontContainer.FontData fontData = base.StyleFontContainer.GetFontData("Default");
		Font font = fontData.Font;
		float num = fontData.FontSize / (float)font.Size;
		float num2 = (float)font.LineHeight * num;
		float wordWidth = GetWordWidth(_realVisibleText);
		float wordWidth2 = GetWordWidth(_realVisibleText.Substring(0, Math.Min(_realVisibleText.Length, CursorPosition - _visibleStart)));
		float num3 = 0f;
		if (base.HorizontalAlignment == TextHorizontalAlignment.Center)
		{
			num3 = ((float)base.Width - wordWidth) * 0.5f;
		}
		else if (base.HorizontalAlignment == TextHorizontalAlignment.Right)
		{
			num3 = (float)base.Width - wordWidth;
		}
		float y = 0f;
		if (base.VerticalAlignment == TextVerticalAlignment.Center)
		{
			y = ((float)base.Height - num2 + 2.5f) * 0.5f;
		}
		else if (base.VerticalAlignment == TextVerticalAlignment.Bottom)
		{
			y = (float)base.Height - num2 + 2.5f;
		}
		return new Vector2(num3 + wordWidth2, y);
	}

	private float GetWordWidth(string word)
	{
		float num = 0f;
		for (int i = 0; i < word.Length; i++)
		{
			num += GetCharacterWidth(word[i]);
		}
		return num;
	}

	private float GetCharacterWidth(char character)
	{
		StyleFontContainer.FontData fontData = base.StyleFontContainer.GetFontData("Default");
		Font font = fontData.Font;
		float num = 0f;
		float num2 = 1f;
		if (!font.Characters.ContainsKey(character))
		{
			Font font2 = _getUsableFontForCharacter(character) ?? fontData.Font;
			num2 = fontData.FontSize / (float)font2.Size;
			return font2.GetCharacterWidth(character, 0.5f) * num2;
		}
		num2 = fontData.FontSize / (float)font.Size;
		return font.GetCharacterWidth(character, 0.5f) * num2;
	}

	private void UpdateSelectedText(float dt, Vector2 mousePosition)
	{
		string text = VisibleText;
		_visibleStart = Math.Min(_visibleStart, CursorPosition);
		StyleFontContainer.FontData fontData = base.StyleFontContainer.GetFontData("Default");
		float scale = fontData.FontSize / (float)fontData.Font.Size;
		int num = 10;
		int i;
		for (i = 0; i < _visibleStart; i++)
		{
			if (!(text != ""))
			{
				break;
			}
			if (!(GetWordWidth(text) > (float)(base.Width - num - num)))
			{
				break;
			}
			text = text.Substring(1);
		}
		_visibleStart = i;
		while (text.Length > CursorPosition - _visibleStart && text != "" && GetWordWidth(text) > (float)(base.Width - num - num))
		{
			text = text.Substring(0, text.Length - 1);
		}
		while (text != "" && GetWordWidth(text) > (float)(base.Width - num - num))
		{
			text = text.Substring(1);
			i++;
			_visibleStart = Math.Min(_visibleStart + 1, CursorPosition);
		}
		Vector2 mousePosition2 = mousePosition;
		if (base.TextOutput != null && base.HorizontalAlignment != TextHorizontalAlignment.Left)
		{
			if (base.HorizontalAlignment == TextHorizontalAlignment.Center)
			{
				mousePosition2.X -= ((float)base.Width - base.TextOutput.GetLine(0).Width) * 0.5f;
			}
			else if (base.HorizontalAlignment == TextHorizontalAlignment.Right)
			{
				mousePosition2.X -= (float)base.Width - base.TextOutput.GetLine(0).Width;
			}
		}
		if (HighlightStart)
		{
			int position = FindCharacterPosition(dt, VisibleText, text, scale, mousePosition2, i);
			HighlightStart = false;
			SetCursor(position);
			BeginSelection();
		}
		if (!HighlightEnd)
		{
			int position2 = FindCharacterPosition(dt, VisibleText, text, scale, mousePosition2, i);
			SetCursor(position2, visible: true, withSelection: true);
		}
		int num2 = Math.Min(Math.Max(SelectedTextBegin - i, 0), text.Length);
		int num3 = Math.Min(Math.Max(SelectedTextEnd - i, 0), text.Length);
		if (num2 > num3)
		{
			int num4 = num2;
			num2 = num3;
			num3 = num4;
		}
		string value = text.Substring(0, num2) + "<span style=\"Highlight\">" + text.Substring(num2, num3 - num2) + "</span>" + text.Substring(num3, text.Length - num3);
		_realVisibleText = text.Substring(0, num2) + text.Substring(num2, num3 - num2) + text.Substring(num3, text.Length - num3);
		base.Value = value;
	}

	public override void Update(float dt, SpriteData spriteData, Vector2 focusPosition, bool focus, bool isFixedWidth, bool isFixedHeight, float renderScale)
	{
		base.Update(dt, spriteData, focusPosition, focus, isFixedWidth, isFixedHeight, renderScale);
		UpdateSelectedText(dt, focusPosition);
	}

	public void SelectAll()
	{
		SelectedTextBegin = 0;
		_selectionAnchor = 0;
		SetCursor(VisibleText.Length, visible: true, withSelection: true);
	}

	public int FindNextWordPosition(int direction)
	{
		MatchCollection matchCollection = _nextWordRegex.Matches(VisibleText);
		int result = 0;
		int result2 = VisibleText.Length;
		foreach (Match item in matchCollection)
		{
			int index = item.Index;
			if (index < CursorPosition)
			{
				result = index;
			}
			else if (index > CursorPosition)
			{
				result2 = index;
				break;
			}
		}
		if (direction <= 0)
		{
			return result;
		}
		return result2;
	}

	public void SetCursor(int position, bool visible = true, bool withSelection = false)
	{
		BlinkTimer = 0f;
		int num = Mathf.Clamp(position, 0, VisibleText.Length);
		SetCursorPosition(num, visible);
		if (withSelection)
		{
			SelectedTextBegin = Math.Min(num, _selectionAnchor);
			SelectedTextEnd = Math.Max(num, _selectionAnchor);
		}
		else
		{
			SelectedTextBegin = 0;
			SelectedTextEnd = 0;
			_selectionAnchor = 0;
		}
	}

	private int FindCharacterPosition(float dt, string fullText, string visibleText, float scale, Vector2 mousePosition, int omitCount)
	{
		if (mousePosition.X > (float)base.Width + 15f * scale)
		{
			int num = (int)((mousePosition.X - (float)base.Width) / (15f * scale));
			if (_scrollTextWhileDraggingCooldown > 0f)
			{
				_scrollTextWhileDraggingCooldown -= dt;
				return Math.Min(omitCount + visibleText.Length, fullText.Length);
			}
			_scrollTextWhileDraggingCooldown = 0.033f;
			return Math.Min(omitCount + visibleText.Length + num, fullText.Length);
		}
		if (mousePosition.X < -15f * scale)
		{
			int num2 = (int)((0f - mousePosition.X) / (15f * scale));
			if (_scrollTextWhileDraggingCooldown > 0f)
			{
				_scrollTextWhileDraggingCooldown -= dt;
				return Math.Max(omitCount, 0);
			}
			_scrollTextWhileDraggingCooldown = 0.033f;
			return Math.Max(omitCount - num2, 0);
		}
		_scrollTextWhileDraggingCooldown = 0f;
		int i = 0;
		float num3 = 0f;
		for (; i < visibleText.Length; i++)
		{
			float num4 = num3;
			num3 += GetCharacterWidth(visibleText[i]);
			if (num3 > mousePosition.X)
			{
				float num5 = mousePosition.X - num4;
				if (!(num3 - mousePosition.X > num5))
				{
					return omitCount + i + 1;
				}
				return omitCount + i;
			}
		}
		return omitCount + i;
	}
}
