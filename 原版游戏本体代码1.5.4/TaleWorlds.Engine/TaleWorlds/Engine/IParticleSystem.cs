using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IParticleSystem
{
	[EngineMethod("set_enable", false, null, true)]
	void SetEnable(UIntPtr psysPointer, bool enable);

	[EngineMethod("set_runtime_emission_rate_multiplier", false, null, false)]
	void SetRuntimeEmissionRateMultiplier(UIntPtr pointer, float multiplier);

	[EngineMethod("restart", false, null, false)]
	void Restart(UIntPtr psysPointer);

	[EngineMethod("set_local_frame", false, null, true)]
	void SetLocalFrame(UIntPtr pointer, in MatrixFrame newFrame);

	[EngineMethod("set_previous_global_frame", false, null, true)]
	void SetPreviousGlobalFrame(UIntPtr pointer, in MatrixFrame newFrame);

	[EngineMethod("get_local_frame", false, null, false)]
	void GetLocalFrame(UIntPtr pointer, ref MatrixFrame frame);

	[EngineMethod("has_alive_particles", false, null, true)]
	bool HasAliveParticles(UIntPtr pointer);

	[EngineMethod("set_dont_remove_from_entity", false, null, false)]
	void SetDontRemoveFromEntity(UIntPtr pointer, bool value);

	[EngineMethod("get_runtime_id_by_name", false, null, false)]
	int GetRuntimeIdByName(string particleSystemName);

	[EngineMethod("create_particle_system_attached_to_bone", false, null, false)]
	ParticleSystem CreateParticleSystemAttachedToBone(int runtimeId, UIntPtr skeletonPtr, sbyte boneIndex, ref MatrixFrame boneLocalFrame);

	[EngineMethod("create_particle_system_attached_to_entity", false, null, false)]
	ParticleSystem CreateParticleSystemAttachedToEntity(int runtimeId, UIntPtr entityPtr, ref MatrixFrame boneLocalFrame);

	[EngineMethod("set_particle_effect_by_name", false, null, false)]
	void SetParticleEffectByName(UIntPtr pointer, string effectName);
}
