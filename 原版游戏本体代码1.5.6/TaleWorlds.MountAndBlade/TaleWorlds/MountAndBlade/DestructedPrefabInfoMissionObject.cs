using System;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[Obsolete]
public class DestructedPrefabInfoMissionObject : MissionObject
{
	public string DestructedPrefabName;

	public Vec3 Translate = new Vec3(0f, 0f, 0f, -1f);

	public Vec3 Rotation = new Vec3(0f, 0f, 0f, -1f);

	public Vec3 Scale = new Vec3(1f, 1f, 1f);
}
