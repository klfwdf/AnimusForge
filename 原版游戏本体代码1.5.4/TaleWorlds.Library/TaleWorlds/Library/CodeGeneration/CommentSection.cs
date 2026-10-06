using System.Collections.Generic;

namespace TaleWorlds.Library.CodeGeneration;

public class CommentSection
{
	private List<string> _lines;

	public CommentSection()
	{
		_lines = new List<string>();
	}

	public void AddCommentLine(string line)
	{
		_lines.Add(line);
	}

	public void GenerateInto(CodeGenerationFile codeGenerationFile)
	{
		foreach (string line in _lines)
		{
			codeGenerationFile.AddLine("//" + line);
		}
	}
}
