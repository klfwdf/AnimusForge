using System;

namespace TaleWorlds.Engine
{
	// Token: 0x02000073 RID: 115
	public sealed class ParticleSystemManager
	{
		// Token: 0x06000A75 RID: 2677 RVA: 0x0000AA26 File Offset: 0x00008C26
		public static int GetRuntimeIdByName(string particleSystemName)
		{
			return EngineApplicationInterface.IParticleSystem.GetRuntimeIdByName(particleSystemName);
		}
	}
}
