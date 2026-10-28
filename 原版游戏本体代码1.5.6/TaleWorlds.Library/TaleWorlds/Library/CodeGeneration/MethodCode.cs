using System.Collections.Generic;

namespace TaleWorlds.Library.CodeGeneration;

public class MethodCode
{
	private List<string> _lines;

	public string Comment { get; set; }

	public string Name { get; set; }

	public string MethodSignature { get; set; }

	public string ReturnParameter { get; set; }

	public bool IsStatic { get; set; }

	public MethodCodeAccessModifier AccessModifier { get; set; }

	public MethodCodePolymorphismInfo PolymorphismInfo { get; set; }

	public MethodCode()
	{
		Name = "UnnamedMethod";
		MethodSignature = "()";
		PolymorphismInfo = MethodCodePolymorphismInfo.None;
		ReturnParameter = "void";
		_lines = new List<string>();
	}

	public void GenerateInto(CodeGenerationFile codeGenerationFile)
	{
		string text = "";
		if (AccessModifier == MethodCodeAccessModifier.Public)
		{
			text += "public ";
		}
		else if (AccessModifier == MethodCodeAccessModifier.Protected)
		{
			text += "protected ";
		}
		else if (AccessModifier == MethodCodeAccessModifier.Private)
		{
			text += "private ";
		}
		else if (AccessModifier == MethodCodeAccessModifier.Internal)
		{
			text += "internal ";
		}
		if (IsStatic)
		{
			text += "static ";
		}
		if (PolymorphismInfo == MethodCodePolymorphismInfo.Virtual)
		{
			text += "virtual ";
		}
		else if (PolymorphismInfo == MethodCodePolymorphismInfo.Override)
		{
			text += "override ";
		}
		text = text + ReturnParameter + " " + Name + MethodSignature;
		if (!string.IsNullOrEmpty(Comment))
		{
			codeGenerationFile.AddLine(Comment);
		}
		codeGenerationFile.AddLine(text);
		codeGenerationFile.AddLine("{");
		foreach (string line in _lines)
		{
			codeGenerationFile.AddLine(line);
		}
		codeGenerationFile.AddLine("}");
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

	public void AddCodeBlock(CodeBlock codeBlock)
	{
		AddLines(codeBlock.Lines);
	}
}
