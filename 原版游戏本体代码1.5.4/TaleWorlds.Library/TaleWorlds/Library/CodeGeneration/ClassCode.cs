using System.Collections.Generic;

namespace TaleWorlds.Library.CodeGeneration;

public class ClassCode
{
	public string Name { get; set; }

	public bool IsGeneric { get; set; }

	public int GenericTypeCount { get; set; }

	public bool IsPartial { get; set; }

	public ClassCodeAccessModifier AccessModifier { get; set; }

	public bool IsClass { get; set; }

	public List<string> InheritedInterfaces { get; private set; }

	public List<ClassCode> NestedClasses { get; private set; }

	public List<MethodCode> Methods { get; private set; }

	public List<ConstructorCode> Constructors { get; private set; }

	public List<VariableCode> Variables { get; private set; }

	public CommentSection CommentSection { get; set; }

	public ClassCode()
	{
		IsClass = true;
		IsGeneric = false;
		GenericTypeCount = 0;
		InheritedInterfaces = new List<string>();
		NestedClasses = new List<ClassCode>();
		Methods = new List<MethodCode>();
		Constructors = new List<ConstructorCode>();
		Variables = new List<VariableCode>();
		AccessModifier = ClassCodeAccessModifier.DoNotMention;
		Name = "UnnamedClass";
		CommentSection = null;
	}

	public void GenerateInto(CodeGenerationFile codeGenerationFile)
	{
		if (CommentSection != null)
		{
			CommentSection.GenerateInto(codeGenerationFile);
		}
		string text = "";
		if (AccessModifier == ClassCodeAccessModifier.Public)
		{
			text += "public ";
		}
		else if (AccessModifier == ClassCodeAccessModifier.Internal)
		{
			text += "internal ";
		}
		if (IsPartial)
		{
			text += "partial ";
		}
		string text2 = "class";
		if (!IsClass)
		{
			text2 = "struct";
		}
		text = text + text2 + " " + Name;
		if (InheritedInterfaces.Count > 0)
		{
			text += " : ";
			for (int i = 0; i < InheritedInterfaces.Count; i++)
			{
				string text3 = InheritedInterfaces[i];
				text = text + " " + text3;
				if (i + 1 != InheritedInterfaces.Count)
				{
					text += ", ";
				}
			}
		}
		if (IsGeneric)
		{
			text += "<";
			for (int j = 0; j < GenericTypeCount; j++)
			{
				text = ((GenericTypeCount != 1) ? (text + "T" + j) : (text + "T"));
				if (j + 1 != GenericTypeCount)
				{
					text += ", ";
				}
			}
			text += ">";
		}
		codeGenerationFile.AddLine(text);
		codeGenerationFile.AddLine("{");
		foreach (ClassCode nestedClass in NestedClasses)
		{
			nestedClass.GenerateInto(codeGenerationFile);
		}
		foreach (VariableCode variable in Variables)
		{
			string line = variable.GenerateLine();
			codeGenerationFile.AddLine(line);
		}
		if (Variables.Count > 0)
		{
			codeGenerationFile.AddLine("");
		}
		foreach (ConstructorCode constructor in Constructors)
		{
			constructor.GenerateInto(codeGenerationFile);
			codeGenerationFile.AddLine("");
		}
		foreach (MethodCode method in Methods)
		{
			method.GenerateInto(codeGenerationFile);
			codeGenerationFile.AddLine("");
		}
		codeGenerationFile.AddLine("}");
	}

	public void AddVariable(VariableCode variableCode)
	{
		Variables.Add(variableCode);
	}

	public void AddNestedClass(ClassCode clasCode)
	{
		NestedClasses.Add(clasCode);
	}

	public void AddMethod(MethodCode methodCode)
	{
		Methods.Add(methodCode);
	}

	public void AddConsturctor(ConstructorCode constructorCode)
	{
		constructorCode.Name = Name;
		Constructors.Add(constructorCode);
	}

	public void AddInterface(string interfaceName)
	{
		InheritedInterfaces.Add(interfaceName);
	}
}
