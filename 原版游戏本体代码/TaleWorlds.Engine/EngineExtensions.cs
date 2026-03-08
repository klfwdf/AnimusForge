using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x020000A1 RID: 161
	public static class EngineExtensions
	{
		// Token: 0x06000F07 RID: 3847 RVA: 0x00011951 File Offset: 0x0000FB51
		public static WorldPosition ToWorldPosition(this Vec3 vec3, Scene scene)
		{
			return new WorldPosition(scene, UIntPtr.Zero, vec3, false);
		}
	}
}
