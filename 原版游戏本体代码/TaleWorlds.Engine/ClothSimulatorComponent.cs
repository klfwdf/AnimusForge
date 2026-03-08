using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000010 RID: 16
	[EngineClass("rglCloth_simulator_component")]
	public sealed class ClothSimulatorComponent : GameEntityComponent
	{
		// Token: 0x0600006E RID: 110 RVA: 0x000032F5 File Offset: 0x000014F5
		internal ClothSimulatorComponent(UIntPtr pointer)
			: base(pointer)
		{
		}

		// Token: 0x0600006F RID: 111 RVA: 0x000032FE File Offset: 0x000014FE
		public void SetMaxDistanceMultiplier(float multiplier)
		{
			EngineApplicationInterface.IClothSimulatorComponent.SetMaxDistanceMultiplier(base.Pointer, multiplier);
		}

		// Token: 0x06000070 RID: 112 RVA: 0x00003311 File Offset: 0x00001511
		public void SetForcedWind(Vec3 windVector, bool isLocal)
		{
			EngineApplicationInterface.IClothSimulatorComponent.SetForcedWind(base.Pointer, windVector, isLocal);
		}

		// Token: 0x06000071 RID: 113 RVA: 0x00003325 File Offset: 0x00001525
		public void DisableForcedWind()
		{
			EngineApplicationInterface.IClothSimulatorComponent.DisableForcedWind(base.Pointer);
		}

		// Token: 0x06000072 RID: 114 RVA: 0x00003337 File Offset: 0x00001537
		public void SetForcedGustStrength(float gustStrength)
		{
			EngineApplicationInterface.IClothSimulatorComponent.SetForcedGustStrength(base.Pointer, gustStrength);
		}

		// Token: 0x06000073 RID: 115 RVA: 0x0000334A File Offset: 0x0000154A
		public void SetResetRequired()
		{
			EngineApplicationInterface.IClothSimulatorComponent.SetResetRequired(base.Pointer);
		}

		// Token: 0x06000074 RID: 116 RVA: 0x0000335C File Offset: 0x0000155C
		public void DisableMorphAnimation()
		{
			EngineApplicationInterface.IClothSimulatorComponent.DisableMorphAnimation(base.Pointer);
		}

		// Token: 0x06000075 RID: 117 RVA: 0x0000336E File Offset: 0x0000156E
		public void SetMorphBuffer(float morphKey)
		{
			EngineApplicationInterface.IClothSimulatorComponent.SetMorphAnimation(base.Pointer, morphKey);
		}

		// Token: 0x06000076 RID: 118 RVA: 0x00003381 File Offset: 0x00001581
		public int GetNumberOfMorphKeys()
		{
			return EngineApplicationInterface.IClothSimulatorComponent.GetNumberOfMorphKeys(base.Pointer);
		}

		// Token: 0x06000077 RID: 119 RVA: 0x00003393 File Offset: 0x00001593
		public void SetVectorArgument(float x, float y, float z, float w)
		{
			EngineApplicationInterface.IClothSimulatorComponent.SetVectorArgument(base.Pointer, x, y, z, w);
		}

		// Token: 0x06000078 RID: 120 RVA: 0x000033AA File Offset: 0x000015AA
		public void GetMorphAnimLeftPoints(Vec3[] leftPoints)
		{
			EngineApplicationInterface.IClothSimulatorComponent.GetMorphAnimLeftPoints(base.Pointer, leftPoints);
		}

		// Token: 0x06000079 RID: 121 RVA: 0x000033BD File Offset: 0x000015BD
		public void GetMorphAnimRightPoints(Vec3[] rightPoints)
		{
			EngineApplicationInterface.IClothSimulatorComponent.GetMorphAnimRightPoints(base.Pointer, rightPoints);
		}

		// Token: 0x0600007A RID: 122 RVA: 0x000033D0 File Offset: 0x000015D0
		public void GetMorphAnimCenterPoints(Vec3[] centerPoints)
		{
			EngineApplicationInterface.IClothSimulatorComponent.GetMorphAnimCenterPoints(base.Pointer, centerPoints);
		}

		// Token: 0x0600007B RID: 123 RVA: 0x000033E3 File Offset: 0x000015E3
		public void SetForcedVelocity(in Vec3 forcedVelocity)
		{
			EngineApplicationInterface.IClothSimulatorComponent.SetForcedVelocity(base.Pointer, forcedVelocity);
		}
	}
}
