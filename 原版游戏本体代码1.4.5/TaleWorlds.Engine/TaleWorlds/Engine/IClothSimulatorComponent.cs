using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IClothSimulatorComponent
{
	[EngineMethod("set_maxdistance_multiplier", false, null, false)]
	void SetMaxDistanceMultiplier(UIntPtr cloth_pointer, float multiplier);

	[EngineMethod("set_forced_wind", false, null, false)]
	void SetForcedWind(UIntPtr cloth_pointer, Vec3 windVector, bool isLocal);

	[EngineMethod("disable_forced_wind", false, null, false)]
	void DisableForcedWind(UIntPtr cloth_pointer);

	[EngineMethod("set_forced_gust_strength", false, null, false)]
	void SetForcedGustStrength(UIntPtr cloth_pointer, float gustStrength);

	[EngineMethod("set_reset_required", false, null, false)]
	void SetResetRequired(UIntPtr cloth_pointer);

	[EngineMethod("disable_morph_animation", false, null, false)]
	void DisableMorphAnimation(UIntPtr cloth_pointer);

	[EngineMethod("set_morph_animation", false, null, false)]
	void SetMorphAnimation(UIntPtr cloth_pointer, float morphKey);

	[EngineMethod("get_number_of_morph_keys", false, null, false)]
	int GetNumberOfMorphKeys(UIntPtr cloth_pointer);

	[EngineMethod("set_vector_argument", false, null, false)]
	void SetVectorArgument(UIntPtr cloth_pointer, float x, float y, float z, float w);

	[EngineMethod("get_morph_anim_left_points", false, null, false)]
	void GetMorphAnimLeftPoints(UIntPtr cloth_pointer, Vec3[] leftPoints);

	[EngineMethod("get_morph_anim_right_points", false, null, false)]
	void GetMorphAnimRightPoints(UIntPtr cloth_pointer, Vec3[] rightPoints);

	[EngineMethod("get_morph_anim_center_points", false, null, false)]
	void GetMorphAnimCenterPoints(UIntPtr cloth_pointer, Vec3[] leftPoints);

	[EngineMethod("set_forced_velocity", false, null, true)]
	void SetForcedVelocity(UIntPtr cloth_pointer, in Vec3 velocity);
}
