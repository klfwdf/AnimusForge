using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

public static class EngineExtensions
{
	public static WorldPosition ToWorldPosition(this Vec3 vec3, Scene scene)
	{
		return new WorldPosition(scene, UIntPtr.Zero, vec3, hasValidZ: false);
	}
}
