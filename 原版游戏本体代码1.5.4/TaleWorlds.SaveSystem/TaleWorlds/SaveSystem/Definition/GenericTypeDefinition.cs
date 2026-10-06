using System;

namespace TaleWorlds.SaveSystem.Definition;

internal class GenericTypeDefinition : TypeDefinition
{
	public GenericTypeDefinition(Type type, GenericSaveId saveId)
		: base(type, saveId, null)
	{
	}
}
