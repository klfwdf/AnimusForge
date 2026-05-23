using System;
using System.Reflection;
using TaleWorlds.SaveSystem.Resolvers;

namespace TaleWorlds.SaveSystem.Definition;

internal class EnumDefinition : TypeDefinitionBase
{
	public readonly IEnumResolver Resolver;

	public readonly bool HasFlags;

	public EnumDefinition(Type type, SaveId saveId, IEnumResolver resolver)
		: base(type, saveId)
	{
		Resolver = resolver;
		HasFlags = type.GetCustomAttribute<FlagsAttribute>() != null;
	}

	public EnumDefinition(Type type, int saveId, IEnumResolver resolver)
		: this(type, new TypeSaveId(saveId), resolver)
	{
	}
}
