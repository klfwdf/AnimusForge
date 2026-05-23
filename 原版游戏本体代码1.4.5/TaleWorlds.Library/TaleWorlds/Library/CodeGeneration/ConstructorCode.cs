using System.Collections.Generic;

namespace TaleWorlds.Library.CodeGeneration;

public class ConstructorCode
{
	private List<string> _lines;

	public string Name { get; set; }

	public string MethodSignature { get; set; }

	public string BaseCall { get; set; }

	public bool IsStatic { get; set; }

	public MethodCodeAccessModifier AccessModifier { get; set; }

	public ConstructorCode()
	{
		Name = "UnassignedConstructorName";
		MethodSignature = "()";
		BaseCall = "";
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
		text = text + Name + MethodSignature;
		if (!string.IsNullOrEmpty(BaseCall))
		{
			text = text + " : base" + BaseCall;
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
}
