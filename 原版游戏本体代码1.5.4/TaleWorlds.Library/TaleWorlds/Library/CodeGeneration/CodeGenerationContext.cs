using System.Collections.Generic;

namespace TaleWorlds.Library.CodeGeneration;

public class CodeGenerationContext
{
	public List<NamespaceCode> Namespaces { get; private set; }

	public CodeGenerationContext()
	{
		Namespaces = new List<NamespaceCode>();
	}

	public NamespaceCode FindOrCreateNamespace(string name)
	{
		foreach (NamespaceCode @namespace in Namespaces)
		{
			if (@namespace.Name == name)
			{
				return @namespace;
			}
		}
		NamespaceCode namespaceCode = new NamespaceCode();
		namespaceCode.Name = name;
		Namespaces.Add(namespaceCode);
		return namespaceCode;
	}

	public void GenerateInto(CodeGenerationFile codeGenerationFile)
	{
		foreach (NamespaceCode @namespace in Namespaces)
		{
			@namespace.GenerateInto(codeGenerationFile);
			codeGenerationFile.AddLine("");
		}
	}
}
