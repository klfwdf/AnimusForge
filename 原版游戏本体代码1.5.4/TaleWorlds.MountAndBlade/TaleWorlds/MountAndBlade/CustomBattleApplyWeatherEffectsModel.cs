using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace TaleWorlds.MountAndBlade;

public class CustomBattleApplyWeatherEffectsModel : ApplyWeatherEffectsModel
{
	public override void ApplyWeatherEffects()
	{
		Scene scene = Mission.Current.Scene;
		if (scene != null)
		{
			bool num = scene.GetRainDensity() > 0f;
			bool flag = scene.GetSnowDensity() > 0f;
			bool flag2 = num || flag;
			bool flag3 = scene.GetFog() > 0f;
			Mission.Current.SetBowMissileSpeedModifier(flag2 ? 0.9f : 1f);
			Mission.Current.SetCrossbowMissileSpeedModifier(flag2 ? 0.9f : 1f);
			Mission.Current.SetMissileRangeModifier(flag3 ? 0.8f : 1f);
		}
	}
}
