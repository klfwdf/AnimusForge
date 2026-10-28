using System;

namespace TaleWorlds.SaveSystem.Definition;

internal class BasicTypeDefinition : TypeDefinitionBase
{
	public IBasicTypeSerializer Serializer { get; private set; }

	public BasicTypeDefinition(Type type, int saveId, IBasicTypeSerializer serializer)
		: base(type, new TypeSaveId(saveId))
	{
		Serializer = serializer;
	}
}
