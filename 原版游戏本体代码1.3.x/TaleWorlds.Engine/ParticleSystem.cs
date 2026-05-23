using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000072 RID: 114
	[EngineClass("rglParticle_system_instanced")]
	public sealed class ParticleSystem : GameEntityComponent
	{
		// Token: 0x06000A64 RID: 2660 RVA: 0x0000A8DB File Offset: 0x00008ADB
		internal ParticleSystem(UIntPtr pointer)
			: base(pointer)
		{
		}

		// Token: 0x06000A65 RID: 2661 RVA: 0x0000A8E4 File Offset: 0x00008AE4
		public static ParticleSystem CreateParticleSystemAttachedToBone(string systemName, Skeleton skeleton, sbyte boneIndex, ref MatrixFrame boneLocalFrame)
		{
			return ParticleSystem.CreateParticleSystemAttachedToBone(ParticleSystemManager.GetRuntimeIdByName(systemName), skeleton, boneIndex, ref boneLocalFrame);
		}

		// Token: 0x06000A66 RID: 2662 RVA: 0x0000A8F4 File Offset: 0x00008AF4
		public static ParticleSystem CreateParticleSystemAttachedToBone(int systemRuntimeId, Skeleton skeleton, sbyte boneIndex, ref MatrixFrame boneLocalFrame)
		{
			return EngineApplicationInterface.IParticleSystem.CreateParticleSystemAttachedToBone(systemRuntimeId, skeleton.Pointer, boneIndex, ref boneLocalFrame);
		}

		// Token: 0x06000A67 RID: 2663 RVA: 0x0000A909 File Offset: 0x00008B09
		public static ParticleSystem CreateParticleSystemAttachedToEntity(string systemName, GameEntity parentEntity, ref MatrixFrame boneLocalFrame)
		{
			return ParticleSystem.CreateParticleSystemAttachedToEntity(ParticleSystemManager.GetRuntimeIdByName(systemName), parentEntity, ref boneLocalFrame);
		}

		// Token: 0x06000A68 RID: 2664 RVA: 0x0000A918 File Offset: 0x00008B18
		public static ParticleSystem CreateParticleSystemAttachedToEntity(string systemName, WeakGameEntity parentEntity, ref MatrixFrame boneLocalFrame)
		{
			return ParticleSystem.CreateParticleSystemAttachedToEntity(ParticleSystemManager.GetRuntimeIdByName(systemName), parentEntity, ref boneLocalFrame);
		}

		// Token: 0x06000A69 RID: 2665 RVA: 0x0000A927 File Offset: 0x00008B27
		public static ParticleSystem CreateParticleSystemAttachedToEntity(int systemRuntimeId, GameEntity parentEntity, ref MatrixFrame boneLocalFrame)
		{
			return EngineApplicationInterface.IParticleSystem.CreateParticleSystemAttachedToEntity(systemRuntimeId, parentEntity.Pointer, ref boneLocalFrame);
		}

		// Token: 0x06000A6A RID: 2666 RVA: 0x0000A93B File Offset: 0x00008B3B
		public static ParticleSystem CreateParticleSystemAttachedToEntity(int systemRuntimeId, WeakGameEntity parentEntity, ref MatrixFrame boneLocalFrame)
		{
			return EngineApplicationInterface.IParticleSystem.CreateParticleSystemAttachedToEntity(systemRuntimeId, parentEntity.Pointer, ref boneLocalFrame);
		}

		// Token: 0x06000A6B RID: 2667 RVA: 0x0000A950 File Offset: 0x00008B50
		public void AddMesh(Mesh mesh)
		{
			EngineApplicationInterface.IMetaMesh.AddMesh(base.Pointer, mesh.Pointer, 0U);
		}

		// Token: 0x06000A6C RID: 2668 RVA: 0x0000A969 File Offset: 0x00008B69
		public void SetEnable(bool enable)
		{
			EngineApplicationInterface.IParticleSystem.SetEnable(base.Pointer, enable);
		}

		// Token: 0x06000A6D RID: 2669 RVA: 0x0000A97C File Offset: 0x00008B7C
		public void SetRuntimeEmissionRateMultiplier(float multiplier)
		{
			EngineApplicationInterface.IParticleSystem.SetRuntimeEmissionRateMultiplier(base.Pointer, multiplier);
		}

		// Token: 0x06000A6E RID: 2670 RVA: 0x0000A98F File Offset: 0x00008B8F
		public void Restart()
		{
			EngineApplicationInterface.IParticleSystem.Restart(base.Pointer);
		}

		// Token: 0x06000A6F RID: 2671 RVA: 0x0000A9A1 File Offset: 0x00008BA1
		public void SetLocalFrame(in MatrixFrame newLocalFrame)
		{
			EngineApplicationInterface.IParticleSystem.SetLocalFrame(base.Pointer, newLocalFrame);
		}

		// Token: 0x06000A70 RID: 2672 RVA: 0x0000A9B4 File Offset: 0x00008BB4
		public void SetPreviousGlobalFrame(in MatrixFrame globalFrame)
		{
			EngineApplicationInterface.IParticleSystem.SetPreviousGlobalFrame(base.Pointer, globalFrame);
		}

		// Token: 0x06000A71 RID: 2673 RVA: 0x0000A9C8 File Offset: 0x00008BC8
		public MatrixFrame GetLocalFrame()
		{
			MatrixFrame identity = MatrixFrame.Identity;
			EngineApplicationInterface.IParticleSystem.GetLocalFrame(base.Pointer, ref identity);
			return identity;
		}

		// Token: 0x06000A72 RID: 2674 RVA: 0x0000A9EE File Offset: 0x00008BEE
		public bool HasAliveParticles()
		{
			return EngineApplicationInterface.IParticleSystem.HasAliveParticles(base.Pointer);
		}

		// Token: 0x06000A73 RID: 2675 RVA: 0x0000AA00 File Offset: 0x00008C00
		public void SetDontRemoveFromEntity(bool value)
		{
			EngineApplicationInterface.IParticleSystem.SetDontRemoveFromEntity(base.Pointer, value);
		}

		// Token: 0x06000A74 RID: 2676 RVA: 0x0000AA13 File Offset: 0x00008C13
		public void SetParticleEffectByName(string effectName)
		{
			EngineApplicationInterface.IParticleSystem.SetParticleEffectByName(base.Pointer, effectName);
		}
	}
}
