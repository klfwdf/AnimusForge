namespace TaleWorlds.GauntletUI.CodeGenerator;

public class ParameterGenerationContext
{
	public string Name { get; private set; }

	public string Value { get; private set; }

	public ParameterGenerationContext(string name, string value)
	{
		Name = name;
		Value = value;
	}
}
