namespace TaleWorlds.Engine;

public sealed class ParticleSystemManager
{
	public static int GetRuntimeIdByName(string particleSystemName)
	{
		return EngineApplicationInterface.IParticleSystem.GetRuntimeIdByName(particleSystemName);
	}
}
