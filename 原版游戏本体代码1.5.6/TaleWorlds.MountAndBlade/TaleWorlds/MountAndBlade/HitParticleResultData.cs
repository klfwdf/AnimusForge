using TaleWorlds.DotNet;

namespace TaleWorlds.MountAndBlade;

[EngineStruct("Hit_particle_result_data", false, null)]
public struct HitParticleResultData
{
	public int StartHitParticleIndex;

	public int ContinueHitParticleIndex;

	public int EndHitParticleIndex;

	public void Reset()
	{
		StartHitParticleIndex = -1;
		ContinueHitParticleIndex = -1;
		EndHitParticleIndex = -1;
	}
}
