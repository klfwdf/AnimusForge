using System;

namespace TaleWorlds.MountAndBlade;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class DefineSynchedMissionObjectTypeForMod : Attribute
{
	public readonly Type Type;

	public DefineSynchedMissionObjectTypeForMod(Type type)
	{
		Type = type;
	}
}
