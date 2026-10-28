using System.Collections.Generic;

namespace TaleWorlds.TwoDimension;

public class StyleFontContainer
{
	public struct FontData(Font font, float fontSize)
	{
		public Font Font = font;

		public float FontSize = fontSize;
	}

	private readonly Dictionary<string, FontData> _styleFonts;

	public StyleFontContainer()
	{
		_styleFonts = new Dictionary<string, FontData>();
	}

	public void Add(string style, Font font, float fontSize)
	{
		FontData value = new FontData(font, fontSize);
		_styleFonts.Add(style, value);
	}

	public FontData GetFontData(string style)
	{
		if (_styleFonts.TryGetValue(style, out var value))
		{
			return value;
		}
		_styleFonts.TryGetValue("Default", out var value2);
		return value2;
	}

	public void ClearFonts()
	{
		_styleFonts.Clear();
	}
}
