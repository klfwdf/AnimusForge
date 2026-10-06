using System;

namespace TaleWorlds.SaveSystem.Definition;

internal class InterfaceDefinition : TypeDefinitionBase
{
	public InterfaceDefinition(Type type, SaveId saveId)
		: base(type, saveId)
	{
	}

	public InterfaceDefinition(Type type, int saveId)
		: base(type, new TypeSaveId(saveId))
	{
	}
}
