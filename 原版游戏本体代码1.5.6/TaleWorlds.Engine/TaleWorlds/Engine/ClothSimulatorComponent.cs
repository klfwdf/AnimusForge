using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[EngineClass("rglCloth_simulator_component")]
public sealed class ClothSimulatorComponent : GameEntityComponent
{
	internal ClothSimulatorComponent(UIntPtr pointer)
		: base(pointer)
	{
	}

	public void SetMaxDistanceMultiplier(float multiplier)
	{
		EngineApplicationInterface.IClothSimulatorComponent.SetMaxDistanceMultiplier(base.Pointer, multiplier);
	}

	public void SetForcedWind(Vec3 windVector, bool isLocal)
	{
		EngineApplicationInterface.IClothSimulatorComponent.SetForcedWind(base.Pointer, windVector, isLocal);
	}

	public void DisableForcedWind()
	{
		EngineApplicationInterface.IClothSimulatorComponent.DisableForcedWind(base.Pointer);
	}

	public void SetForcedGustStrength(float gustStrength)
	{
		EngineApplicationInterface.IClothSimulatorComponent.SetForcedGustStrength(base.Pointer, gustStrength);
	}

	public void SetResetRequired()
	{
		EngineApplicationInterface.IClothSimulatorComponent.SetResetRequired(base.Pointer);
	}

	public void DisableMorphAnimation()
	{
		EngineApplicationInterface.IClothSimulatorComponent.DisableMorphAnimation(base.Pointer);
	}

	public void SetMorphBuffer(float morphKey)
	{
		EngineApplicationInterface.IClothSimulatorComponent.SetMorphAnimation(base.Pointer, morphKey);
	}

	public int GetNumberOfMorphKeys()
	{
		return EngineApplicationInterface.IClothSimulatorComponent.GetNumberOfMorphKeys(base.Pointer);
	}

	public void SetVectorArgument(float x, float y, float z, float w)
	{
		EngineApplicationInterface.IClothSimulatorComponent.SetVectorArgument(base.Pointer, x, y, z, w);
	}

	public void GetMorphAnimLeftPoints(Vec3[] leftPoints)
	{
		EngineApplicationInterface.IClothSimulatorComponent.GetMorphAnimLeftPoints(base.Pointer, leftPoints);
	}

	public void GetMorphAnimRightPoints(Vec3[] rightPoints)
	{
		EngineApplicationInterface.IClothSimulatorComponent.GetMorphAnimRightPoints(base.Pointer, rightPoints);
	}

	public void GetMorphAnimCenterPoints(Vec3[] centerPoints)
	{
		EngineApplicationInterface.IClothSimulatorComponent.GetMorphAnimCenterPoints(base.Pointer, centerPoints);
	}

	public void SetForcedVelocity(in Vec3 forcedVelocity)
	{
		EngineApplicationInterface.IClothSimulatorComponent.SetForcedVelocity(base.Pointer, in forcedVelocity);
	}
}
