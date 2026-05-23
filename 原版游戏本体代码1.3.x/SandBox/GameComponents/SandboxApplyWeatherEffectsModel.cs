using System;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace SandBox.GameComponents
{
	// Token: 0x020000BE RID: 190
	public class SandboxApplyWeatherEffectsModel : ApplyWeatherEffectsModel
	{
		// Token: 0x060007ED RID: 2029 RVA: 0x00038954 File Offset: 0x00036B54
		public override void ApplyWeatherEffects()
		{
			Scene scene = Mission.Current.Scene;
			if (scene != null)
			{
				bool flag = scene.GetRainDensity() > 0f;
				bool flag2 = scene.GetSnowDensity() > 0f;
				bool flag3 = flag || flag2;
				bool flag4 = scene.GetFog() > 0f;
				Mission.Current.SetBowMissileSpeedModifier(flag3 ? 0.9f : 1f);
				Mission.Current.SetCrossbowMissileSpeedModifier(flag3 ? 0.9f : 1f);
				Mission.Current.SetMissileRangeModifier(flag4 ? 0.8f : 1f);
			}
		}
	}
}
