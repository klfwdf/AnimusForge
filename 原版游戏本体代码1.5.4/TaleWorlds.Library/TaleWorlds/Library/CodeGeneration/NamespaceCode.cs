using System.Collections.Generic;

namespace TaleWorlds.Library.CodeGeneration;

public class NamespaceCode
{
	public string Name { get; set; }

	public List<ClassCode> Classes { get; private set; }

	public NamespaceCode()
	{
		Classes = new List<ClassCode>();
	}

	public void GenerateInto(CodeGenerationFile codeGenerationFile)
	{
		codeGenerationFile.AddLine("namespace " + Name);
		codeGenerationFile.AddLine("{");
		foreach (ClassCode @class in Classes)
		{
			@class.GenerateInto(codeGenerationFile);
			codeGenerationFile.AddLine("");
		}
		codeGenerationFile.AddLine("}");
	}

	public void AddClass(ClassCode clasCode)
	{
		Classes.Add(clasCode);
	}
}
