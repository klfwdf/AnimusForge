using TaleWorlds.GauntletUI.PrefabSystem;

namespace TaleWorlds.GauntletUI.CodeGenerator;

public class ConstantGenerationContext
{
	public ConstantDefinition ConstantDefinition { get; private set; }

	public ConstantGenerationContext(ConstantDefinition constantDefinition)
	{
		ConstantDefinition = constantDefinition;
	}
}
