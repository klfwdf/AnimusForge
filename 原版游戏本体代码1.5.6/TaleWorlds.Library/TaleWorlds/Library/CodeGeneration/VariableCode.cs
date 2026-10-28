namespace TaleWorlds.Library.CodeGeneration;

public class VariableCode
{
	public string Name { get; set; }

	public string Type { get; set; }

	public bool IsStatic { get; set; }

	public VariableCodeAccessModifier AccessModifier { get; set; }

	public VariableCode()
	{
		Type = "System.Object";
		Name = "Unnamed variable";
		IsStatic = false;
		AccessModifier = VariableCodeAccessModifier.Private;
	}

	public string GenerateLine()
	{
		string text = "";
		if (AccessModifier == VariableCodeAccessModifier.Public)
		{
			text += "public ";
		}
		else if (AccessModifier == VariableCodeAccessModifier.Protected)
		{
			text += "protected ";
		}
		else if (AccessModifier == VariableCodeAccessModifier.Private)
		{
			text += "private ";
		}
		else if (AccessModifier == VariableCodeAccessModifier.Internal)
		{
			text += "internal ";
		}
		if (IsStatic)
		{
			text += "static ";
		}
		return text + Type + " " + Name + ";";
	}
}
