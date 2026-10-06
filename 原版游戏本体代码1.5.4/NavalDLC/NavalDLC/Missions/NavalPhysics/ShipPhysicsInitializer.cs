using TaleWorlds.Library;

namespace NavalDLC.Missions.NavalPhysics;

public static class ShipPhysicsInitializer
{
	public static Vec3 GetDefaultInertia(float mass, in Vec3 draftVolume)
	{
		float x = 0.08333f * mass * (draftVolume.y * draftVolume.y + draftVolume.z * draftVolume.z);
		float y = 0.08333f * mass * (draftVolume.x * draftVolume.x + draftVolume.z * draftVolume.z);
		float z = 0.08333f * mass * (draftVolume.x * draftVolume.x + draftVolume.y * draftVolume.y);
		return new Vec3(x, y, z);
	}
}
