using System.Collections.Generic;

namespace TaleWorlds.TwoDimension;

public class RichTextLinkGroup
{
	private List<TextToken> _tokens;

	public string Href { get; private set; }

	internal int StartIndex { get; private set; }

	internal int EndIndex => StartIndex + _tokens.Count;

	internal RichTextLinkGroup(int startIndex, string href)
	{
		Href = href;
		StartIndex = startIndex;
		_tokens = new List<TextToken>();
	}

	internal void AddToken(TextToken textToken)
	{
		_tokens.Add(textToken);
	}

	internal bool Contains(TextToken textToken)
	{
		return _tokens.Contains(textToken);
	}
}
