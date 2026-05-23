using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;

namespace SandBox.View.Map.Managers;

internal class MapAudioManager : CampaignEntityVisualComponent
{
	private const string SeasonParameterId = "Season";

	private const string CameraHeightParameterId = "CampaignCameraHeight";

	private const string TimeOfDayParameterId = "Daytime";

	private const string WeatherEventIntensityParameterId = "Rainfall";

	private CampaignTime.Seasons _lastCachedSeason;

	private float _lastCameraZ;

	private int _lastHourUpdate;

	private MapScene _mapScene;

	public override int Priority => 70;

	public MapAudioManager()
	{
		_mapScene = Campaign.Current.MapSceneWrapper as MapScene;
	}

	public override void OnVisualTick(MapScreen screen, float realDt, float dt)
	{
		if (CampaignTime.Now.GetSeasonOfYear != _lastCachedSeason)
		{
			SoundManager.SetGlobalParameter("Season", (float)CampaignTime.Now.GetSeasonOfYear);
			_lastCachedSeason = CampaignTime.Now.GetSeasonOfYear;
		}
		if (Math.Abs(_lastCameraZ - _mapScene.Scene.LastFinalRenderCameraPosition.Z) > 0.1f)
		{
			SoundManager.SetGlobalParameter("CampaignCameraHeight", _mapScene.Scene.LastFinalRenderCameraPosition.Z);
			_lastCameraZ = _mapScene.Scene.LastFinalRenderCameraPosition.Z;
		}
		if ((int)CampaignTime.Now.CurrentHourInDay == _lastHourUpdate)
		{
			SoundManager.SetGlobalParameter("Daytime", CampaignTime.Now.CurrentHourInDay);
			_lastHourUpdate = (int)CampaignTime.Now.CurrentHourInDay;
		}
	}
}
