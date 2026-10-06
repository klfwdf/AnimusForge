using NavalDLC.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NavalDLC.ComponentInterfaces;

public abstract class MapStormModel : MBGameModel<MapStormModel>
{
	public abstract float MinimumWeatherStrengthInsideStorm { get; }

	public abstract int MaximumNumberOfStorms { get; }

	public abstract float GetPositionDamageForStorm(Storm storm, Vec2 shipPosition, Ship ship);

	public abstract float GetHourlyIntensityChangeForStorm(Storm storm);

	public abstract void GetStormLifeSpan(out CampaignTime minimumDuration, out CampaignTime maximumDuration);

	public abstract CampaignTime GetDevelopingStateDurationOfStorm(Storm storm);

	public abstract CampaignTime GetFinalizingStateDurationOfStorm(Storm storm);

	public abstract float GetHourlyStormSpawnChanceForPosition(Vec2 position);

	public abstract Storm.StormTypes GetSpawnedStormTypeForPosition(Vec2 position);

	public abstract bool CanPartyGetDamagedByStorm(MobileParty mobileParty);

	public abstract float GetEffectRadiusOfStorm(Storm storm);

	public abstract float GetEyeRadiusOfStorm(Storm storm);

	public abstract float GetSpeedOfStorm(Storm storm);

	public abstract float GetMaximumWeatherStrengthAtEye(Storm storm);

	public abstract float GetStormSpawnDistanceSquaredThresholdWithOtherStorms();

	public abstract float GetNormalizedWindStrengthOfStormForPosition(Vec2 position);
}
