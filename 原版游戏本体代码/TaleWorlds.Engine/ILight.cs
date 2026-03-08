using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200002C RID: 44
	[ApplicationInterfaceBase]
	internal interface ILight
	{
		// Token: 0x060004E9 RID: 1257
		[EngineMethod("create_point_light", false, null, false)]
		Light CreatePointLight(float lightRadius);

		// Token: 0x060004EA RID: 1258
		[EngineMethod("set_radius", false, null, false)]
		void SetRadius(UIntPtr lightpointer, float radius);

		// Token: 0x060004EB RID: 1259
		[EngineMethod("set_light_flicker", false, null, false)]
		void SetLightFlicker(UIntPtr lightpointer, float magnitude, float interval);

		// Token: 0x060004EC RID: 1260
		[EngineMethod("enable_shadow", false, null, false)]
		void EnableShadow(UIntPtr lightpointer, bool shadowEnabled);

		// Token: 0x060004ED RID: 1261
		[EngineMethod("is_shadow_enabled", false, null, false)]
		bool IsShadowEnabled(UIntPtr lightpointer);

		// Token: 0x060004EE RID: 1262
		[EngineMethod("set_volumetric_properties", false, null, false)]
		void SetVolumetricProperties(UIntPtr lightpointer, bool volumelightenabled, float volumeparameter);

		// Token: 0x060004EF RID: 1263
		[EngineMethod("set_visibility", false, null, false)]
		void SetVisibility(UIntPtr lightpointer, bool value);

		// Token: 0x060004F0 RID: 1264
		[EngineMethod("get_radius", false, null, false)]
		float GetRadius(UIntPtr lightpointer);

		// Token: 0x060004F1 RID: 1265
		[EngineMethod("set_shadows", false, null, false)]
		void SetShadows(UIntPtr lightPointer, int shadowType);

		// Token: 0x060004F2 RID: 1266
		[EngineMethod("set_light_color", false, null, false)]
		void SetLightColor(UIntPtr lightpointer, Vec3 color);

		// Token: 0x060004F3 RID: 1267
		[EngineMethod("get_light_color", false, null, false)]
		Vec3 GetLightColor(UIntPtr lightpointer);

		// Token: 0x060004F4 RID: 1268
		[EngineMethod("set_intensity", false, null, false)]
		void SetIntensity(UIntPtr lightPointer, float value);

		// Token: 0x060004F5 RID: 1269
		[EngineMethod("get_intensity", false, null, false)]
		float GetIntensity(UIntPtr lightPointer);

		// Token: 0x060004F6 RID: 1270
		[EngineMethod("release", false, null, false)]
		void Release(UIntPtr lightpointer);

		// Token: 0x060004F7 RID: 1271
		[EngineMethod("set_frame", false, null, false)]
		void SetFrame(UIntPtr lightPointer, ref MatrixFrame frame);

		// Token: 0x060004F8 RID: 1272
		[EngineMethod("get_frame", false, null, false)]
		void GetFrame(UIntPtr lightPointer, out MatrixFrame result);
	}
}
