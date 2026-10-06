using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[EngineClass("rglParticle_system_instanced")]
public sealed class ParticleSystem : GameEntityComponent
{
	internal ParticleSystem(UIntPtr pointer)
		: base(pointer)
	{
	}

	public static ParticleSystem CreateParticleSystemAttachedToBone(string systemName, Skeleton skeleton, sbyte boneIndex, ref MatrixFrame boneLocalFrame)
	{
		return CreateParticleSystemAttachedToBone(ParticleSystemManager.GetRuntimeIdByName(systemName), skeleton, boneIndex, ref boneLocalFrame);
	}

	public static ParticleSystem CreateParticleSystemAttachedToBone(int systemRuntimeId, Skeleton skeleton, sbyte boneIndex, ref MatrixFrame boneLocalFrame)
	{
		return EngineApplicationInterface.IParticleSystem.CreateParticleSystemAttachedToBone(systemRuntimeId, skeleton.Pointer, boneIndex, ref boneLocalFrame);
	}

	public static ParticleSystem CreateParticleSystemAttachedToEntity(string systemName, GameEntity parentEntity, ref MatrixFrame boneLocalFrame)
	{
		return CreateParticleSystemAttachedToEntity(ParticleSystemManager.GetRuntimeIdByName(systemName), parentEntity, ref boneLocalFrame);
	}

	public static ParticleSystem CreateParticleSystemAttachedToEntity(string systemName, WeakGameEntity parentEntity, ref MatrixFrame boneLocalFrame)
	{
		return CreateParticleSystemAttachedToEntity(ParticleSystemManager.GetRuntimeIdByName(systemName), parentEntity, ref boneLocalFrame);
	}

	public static ParticleSystem CreateParticleSystemAttachedToEntity(int systemRuntimeId, GameEntity parentEntity, ref MatrixFrame boneLocalFrame)
	{
		return EngineApplicationInterface.IParticleSystem.CreateParticleSystemAttachedToEntity(systemRuntimeId, parentEntity.Pointer, ref boneLocalFrame);
	}

	public static ParticleSystem CreateParticleSystemAttachedToEntity(int systemRuntimeId, WeakGameEntity parentEntity, ref MatrixFrame boneLocalFrame)
	{
		return EngineApplicationInterface.IParticleSystem.CreateParticleSystemAttachedToEntity(systemRuntimeId, parentEntity.Pointer, ref boneLocalFrame);
	}

	public void AddMesh(Mesh mesh)
	{
		EngineApplicationInterface.IMetaMesh.AddMesh(base.Pointer, mesh.Pointer, 0u);
	}

	public void SetEnable(bool enable)
	{
		EngineApplicationInterface.IParticleSystem.SetEnable(base.Pointer, enable);
	}

	public void SetRuntimeEmissionRateMultiplier(float multiplier)
	{
		EngineApplicationInterface.IParticleSystem.SetRuntimeEmissionRateMultiplier(base.Pointer, multiplier);
	}

	public void Restart()
	{
		EngineApplicationInterface.IParticleSystem.Restart(base.Pointer);
	}

	public void SetLocalFrame(in MatrixFrame newLocalFrame)
	{
		EngineApplicationInterface.IParticleSystem.SetLocalFrame(base.Pointer, in newLocalFrame);
	}

	public void SetPreviousGlobalFrame(in MatrixFrame globalFrame)
	{
		EngineApplicationInterface.IParticleSystem.SetPreviousGlobalFrame(base.Pointer, in globalFrame);
	}

	public MatrixFrame GetLocalFrame()
	{
		MatrixFrame frame = MatrixFrame.Identity;
		EngineApplicationInterface.IParticleSystem.GetLocalFrame(base.Pointer, ref frame);
		return frame;
	}

	public bool HasAliveParticles()
	{
		return EngineApplicationInterface.IParticleSystem.HasAliveParticles(base.Pointer);
	}

	public void SetDontRemoveFromEntity(bool value)
	{
		EngineApplicationInterface.IParticleSystem.SetDontRemoveFromEntity(base.Pointer, value);
	}

	public void SetParticleEffectByName(string effectName)
	{
		EngineApplicationInterface.IParticleSystem.SetParticleEffectByName(base.Pointer, effectName);
	}
}
