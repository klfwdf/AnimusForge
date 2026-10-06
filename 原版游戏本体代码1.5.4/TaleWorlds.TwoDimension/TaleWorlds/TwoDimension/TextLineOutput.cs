using System.Collections.Generic;

namespace TaleWorlds.TwoDimension;

internal class TextLineOutput
{
	private List<TextTokenOutput> _tokens;

	public float Width { get; private set; }

	public float TextWidth { get; private set; }

	public bool LineEnded { get; internal set; }

	public int EmptyCharacterCount { get; private set; }

	public int TokenCount => _tokens.Count;

	public float Height { get; private set; }

	public float MaxScale { get; private set; }

	public TextLineOutput(float lineHeight)
	{
		_tokens = new List<TextTokenOutput>();
		Height = lineHeight;
	}

	public void AddToken(TextToken textToken, float tokenWidth, float tokenHeight, string style, float scaleValue)
	{
		if (textToken.Type == TextToken.TokenType.EmptyCharacter)
		{
			EmptyCharacterCount++;
		}
		else
		{
			TextWidth += tokenWidth;
		}
		TextTokenOutput item = ((!(tokenHeight > 0f)) ? new TextTokenOutput(textToken, tokenWidth, Height, style, scaleValue) : new TextTokenOutput(textToken, tokenWidth, tokenHeight, style, scaleValue));
		_tokens.Add(item);
		Width += tokenWidth;
		if (tokenHeight > Height)
		{
			Height = tokenHeight;
		}
		if (scaleValue > MaxScale)
		{
			MaxScale = scaleValue;
		}
	}

	public TextToken GetToken(int i)
	{
		return _tokens[i].Token;
	}

	public TextTokenOutput GetTokenOutput(int i)
	{
		return _tokens[i];
	}

	public TextTokenOutput RemoveTokenFromEnd()
	{
		TextTokenOutput textTokenOutput = _tokens[_tokens.Count - 1];
		_tokens.Remove(textTokenOutput);
		Width -= textTokenOutput.Width;
		return textTokenOutput;
	}
}
