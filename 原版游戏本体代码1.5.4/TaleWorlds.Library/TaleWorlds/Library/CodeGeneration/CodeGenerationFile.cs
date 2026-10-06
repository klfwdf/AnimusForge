using System.Collections.Generic;
using System.Text;

namespace TaleWorlds.Library.CodeGeneration;

public class CodeGenerationFile
{
	private List<string> _lines;

	public CodeGenerationFile(List<string> usingDefinitions = null)
	{
		_lines = new List<string>();
		if (usingDefinitions == null || usingDefinitions.Count <= 0)
		{
			return;
		}
		foreach (string usingDefinition in usingDefinitions)
		{
			AddLine("using " + usingDefinition + ";");
		}
	}

	public void AddLine(string line)
	{
		_lines.Add(line);
	}

	public string GenerateText()
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		foreach (string line in _lines)
		{
			if (line == "}" || line == "};")
			{
				num--;
			}
			string text = "";
			for (int i = 0; i < num; i++)
			{
				text += "\t";
			}
			text = text + line + "\n";
			if (line == "{")
			{
				num++;
			}
			stringBuilder.Append(text);
		}
		return stringBuilder.ToString();
	}
}
