using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200001D RID: 29
	[ApplicationInterfaceBase]
	internal interface IParticleSystem
	{
		// Token: 0x06000165 RID: 357
		[EngineMethod("set_enable", false, null, true)]
		void SetEnable(UIntPtr psysPointer, bool enable);

		// Token: 0x06000166 RID: 358
		[EngineMethod("set_runtime_emission_rate_multiplier", false, null, false)]
		void SetRuntimeEmissionRateMultiplier(UIntPtr pointer, float multiplier);

		// Token: 0x06000167 RID: 359
		[EngineMethod("restart", false, null, false)]
		void Restart(UIntPtr psysPointer);

		// Token: 0x06000168 RID: 360
		[EngineMethod("set_local_frame", false, null, true)]
		void SetLocalFrame(UIntPtr pointer, in MatrixFrame newFrame);

		// Token: 0x06000169 RID: 361
		[EngineMethod("set_previous_global_frame", false, null, true)]
		void SetPreviousGlobalFrame(UIntPtr pointer, in MatrixFrame newFrame);

		// Token: 0x0600016A RID: 362
		[EngineMethod("get_local_frame", false, null, false)]
		void GetLocalFrame(UIntPtr pointer, ref MatrixFrame frame);

		// Token: 0x0600016B RID: 363
		[EngineMethod("has_alive_particles", false, null, true)]
		bool HasAliveParticles(UIntPtr pointer);

		// Token: 0x0600016C RID: 364
		[EngineMethod("set_dont_remove_from_entity", false, null, false)]
		void SetDontRemoveFromEntity(UIntPtr pointer, bool value);

		// Token: 0x0600016D RID: 365
		[EngineMethod("get_runtime_id_by_name", false, null, false)]
		int GetRuntimeIdByName(string particleSystemName);

		// Token: 0x0600016E RID: 366
		[EngineMethod("create_particle_system_attached_to_bone", false, null, false)]
		ParticleSystem CreateParticleSystemAttachedToBone(int runtimeId, UIntPtr skeletonPtr, sbyte boneIndex, ref MatrixFrame boneLocalFrame);

		// Token: 0x0600016F RID: 367
		[EngineMethod("create_particle_system_attached_to_entity", false, null, false)]
		ParticleSystem CreateParticleSystemAttachedToEntity(int runtimeId, UIntPtr entityPtr, ref MatrixFrame boneLocalFrame);

		// Token: 0x06000170 RID: 368
		[EngineMethod("set_particle_effect_by_name", false, null, false)]
		void SetParticleEffectByName(UIntPtr pointer, string effectName);
	}
}
