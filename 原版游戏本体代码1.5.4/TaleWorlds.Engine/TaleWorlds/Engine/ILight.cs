using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface ILight
{
	[EngineMethod("create_point_light", false, null, false)]
	Light CreatePointLight(float lightRadius);

	[EngineMethod("set_radius", false, null, false)]
	void SetRadius(UIntPtr lightpointer, float radius);

	[EngineMethod("set_light_flicker", false, null, false)]
	void SetLightFlicker(UIntPtr lightpointer, float magnitude, float interval);

	[EngineMethod("enable_shadow", false, null, false)]
	void EnableShadow(UIntPtr lightpointer, bool shadowEnabled);

	[EngineMethod("is_shadow_enabled", false, null, false)]
	bool IsShadowEnabled(UIntPtr lightpointer);

	[EngineMethod("set_volumetric_properties", false, null, false)]
	void SetVolumetricProperties(UIntPtr lightpointer, bool volumelightenabled, float volumeparameter);

	[EngineMethod("set_visibility", false, null, false)]
	void SetVisibility(UIntPtr lightpointer, bool value);

	[EngineMethod("get_radius", false, null, false)]
	float GetRadius(UIntPtr lightpointer);

	[EngineMethod("set_shadows", false, null, false)]
	void SetShadows(UIntPtr lightPointer, int shadowType);

	[EngineMethod("set_light_color", false, null, false)]
	void SetLightColor(UIntPtr lightpointer, Vec3 color);

	[EngineMethod("get_light_color", false, null, false)]
	Vec3 GetLightColor(UIntPtr lightpointer);

	[EngineMethod("set_intensity", false, null, false)]
	void SetIntensity(UIntPtr lightPointer, float value);

	[EngineMethod("get_intensity", false, null, false)]
	float GetIntensity(UIntPtr lightPointer);

	[EngineMethod("release", false, null, false)]
	void Release(UIntPtr lightpointer);

	[EngineMethod("set_frame", false, null, false)]
	void SetFrame(UIntPtr lightPointer, ref MatrixFrame frame);

	[EngineMethod("get_frame", false, null, false)]
	void GetFrame(UIntPtr lightPointer, out MatrixFrame result);
}
