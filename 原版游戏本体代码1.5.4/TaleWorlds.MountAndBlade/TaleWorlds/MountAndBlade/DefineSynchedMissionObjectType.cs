using System;

namespace TaleWorlds.MountAndBlade;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
internal sealed class DefineSynchedMissionObjectType : Attribute
{
	public readonly Type Type;

	public DefineSynchedMissionObjectType(Type type)
	{
		Type = type;
	}
}
