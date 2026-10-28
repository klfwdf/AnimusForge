using System;
using TaleWorlds.SaveSystem.Resolvers;

namespace TaleWorlds.SaveSystem.Definition;

internal class StructDefinition : TypeDefinition
{
	public StructDefinition(Type type, int saveId)
		: this(type, saveId, null)
	{
	}

	public StructDefinition(Type type, int saveId, IObjectResolver objectResolver)
		: base(type, saveId, objectResolver)
	{
	}
}
