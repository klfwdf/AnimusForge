using System.Collections.Generic;

namespace TaleWorlds.TwoDimension;

internal class TextOutput
{
	private List<TextLineOutput> _tokensWithLines;

	private readonly float _lineHeight;

	public float TextHeight
	{
		get
		{
			float num = 0f;
			for (int i = 0; i < LineCount; i++)
			{
				TextLineOutput line = GetLine(i);
				num += line.Height;
			}
			return num;
		}
	}

	public float TotalLineScale
	{
		get
		{
			float num = 0f;
			for (int i = 0; i < LineCount; i++)
			{
				TextLineOutput line = GetLine(i);
				num += line.MaxScale;
			}
			return num;
		}
	}

	public float LastLineWidth => _tokensWithLines[_tokensWithLines.Count - 1].Width;

	public float MaxLineHeight { get; private set; }

	public float MaxLineWidth { get; private set; }

	public float MaxLineScale { get; private set; }

	public int LineCount => _tokensWithLines.Count;

	public IEnumerable<TextTokenOutput> Tokens
	{
		get
		{
			for (int i = 0; i < _tokensWithLines.Count; i++)
			{
				TextLineOutput tokensWithLine = _tokensWithLines[i];
				for (int j = 0; j < tokensWithLine.TokenCount; j++)
				{
					yield return tokensWithLine.GetTokenOutput(j);
				}
			}
		}
	}

	public IEnumerable<TextTokenOutput> TokensWithNewLines
	{
		get
		{
			for (int i = 0; i < _tokensWithLines.Count; i++)
			{
				TextLineOutput tokensWithLine = _tokensWithLines[i];
				for (int j = 0; j < tokensWithLine.TokenCount; j++)
				{
					yield return tokensWithLine.GetTokenOutput(j);
				}
				if (i < _tokensWithLines.Count - 1)
				{
					yield return new TextTokenOutput(TextToken.CreateNewLine(), 0f, 0f, string.Empty, 0f);
				}
			}
		}
	}

	public TextOutput(float lineHeight)
	{
		_tokensWithLines = new List<TextLineOutput>();
		_lineHeight = lineHeight;
		TextLineOutput textLineOutput = new TextLineOutput(_lineHeight);
		_tokensWithLines.Add(textLineOutput);
		textLineOutput.LineEnded = true;
	}

	public TextLineOutput AddNewLine(bool currentLineEnded, float newLineBaseHeight = 0f)
	{
		TextLineOutput textLineOutput = _tokensWithLines[_tokensWithLines.Count - 1];
		textLineOutput.LineEnded = currentLineEnded;
		TextLineOutput textLineOutput2 = new TextLineOutput(newLineBaseHeight);
		_tokensWithLines.Add(textLineOutput2);
		textLineOutput2.LineEnded = true;
		if (textLineOutput.Width > MaxLineWidth)
		{
			MaxLineWidth = textLineOutput.Width;
		}
		if (textLineOutput.MaxScale > MaxLineScale)
		{
			MaxLineScale = textLineOutput.MaxScale;
		}
		return textLineOutput2;
	}

	public void AddToken(TextToken textToken, float tokenWidth, float scaleValue, string style = "Default", float tokenHeight = -1f)
	{
		TextLineOutput textLineOutput = _tokensWithLines[_tokensWithLines.Count - 1];
		textLineOutput.AddToken(textToken, tokenWidth, tokenHeight, style, scaleValue);
		if (tokenHeight > MaxLineHeight)
		{
			MaxLineHeight = tokenHeight;
		}
		if (textLineOutput.Width > MaxLineWidth)
		{
			MaxLineWidth = textLineOutput.Width;
		}
		if (textLineOutput.MaxScale > MaxLineScale)
		{
			MaxLineScale = textLineOutput.MaxScale;
		}
	}

	public List<TextTokenOutput> RemoveTokensFromEnd(int numberOfTokensToRemove)
	{
		List<TextTokenOutput> list = new List<TextTokenOutput>();
		for (int i = 0; i < numberOfTokensToRemove; i++)
		{
			if (_tokensWithLines[_tokensWithLines.Count - 1].TokenCount > 0)
			{
				TextLineOutput textLineOutput = _tokensWithLines[_tokensWithLines.Count - 1];
				list.Add(textLineOutput.RemoveTokenFromEnd());
			}
			else
			{
				_tokensWithLines.RemoveAt(_tokensWithLines.Count - 1);
				TextLineOutput textLineOutput2 = _tokensWithLines[_tokensWithLines.Count - 1];
				list.Add(textLineOutput2.RemoveTokenFromEnd());
			}
		}
		return list;
	}

	public TextLineOutput GetLine(int i)
	{
		return _tokensWithLines[i];
	}

	public void Clear()
	{
		MaxLineHeight = 0f;
		MaxLineWidth = 0f;
		MaxLineScale = 0f;
		_tokensWithLines.Clear();
		TextLineOutput textLineOutput = new TextLineOutput(_lineHeight);
		_tokensWithLines.Add(textLineOutput);
		textLineOutput.LineEnded = true;
	}
}
