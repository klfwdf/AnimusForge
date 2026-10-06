using System.Collections.Generic;

namespace TaleWorlds.Library.CodeGeneration;

public class CodeBlock
{
	private List<string> _lines;

	public List<string> Lines => _lines;

	public CodeBlock()
	{
		_lines = new List<string>();
	}

	public void AddLine(string line)
	{
		_lines.Add(line);
	}

	public void AddLines(IEnumerable<string> lines)
	{
		foreach (string line in lines)
		{
			_lines.Add(line);
		}
	}
}
