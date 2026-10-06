using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.GauntletUI.Map;

public class GauntletMapEventVisual : IMapEventVisual
{
	private static int _battleSoundEventIndex = SoundManager.GetEventGlobalIndex("event:/map/ambient/node/battle");

	private static int _navalBattleSoundEventIndex = SoundManager.GetEventGlobalIndex("event:/map/ambient/node/naval_battle_loop");

	private static int _raidSoundEventIndex = SoundManager.GetEventGlobalIndex("event:/map/ambient/node/battle_raid");

	private static int _siegeSoundEventIndex = SoundManager.GetEventGlobalIndex("event:/map/ambient/node/battle_siege");

	private static int _hideoutBattleSoundEventIndex = SoundManager.GetEventGlobalIndex("event:/map/ambient/node/battle_hideout");

	private SoundEvent _mapEventSoundEvent;

	private readonly Action<GauntletMapEventVisual> _onDeactivate;

	private readonly Action<GauntletMapEventVisual> _onInitialized;

	private readonly Action<GauntletMapEventVisual> _onVisibilityChanged;

	private Scene _mapScene;

	public MapEvent MapEvent { get; private set; }

	public Vec2 WorldPosition { get; private set; }

	public bool IsVisible { get; private set; }

	private Scene MapScene
	{
		get
		{
			if (_mapScene == null && Campaign.Current?.MapSceneWrapper != null)
			{
				_mapScene = ((MapScene)Campaign.Current.MapSceneWrapper).Scene;
			}
			return _mapScene;
		}
	}

	public GauntletMapEventVisual(MapEvent mapEvent, Action<GauntletMapEventVisual> onInitialized, Action<GauntletMapEventVisual> onVisibilityChanged, Action<GauntletMapEventVisual> onDeactivate)
	{
		_onDeactivate = onDeactivate;
		_onInitialized = onInitialized;
		_onVisibilityChanged = onVisibilityChanged;
		MapEvent = mapEvent;
	}

	public void Initialize(CampaignVec2 position, bool isVisible)
	{
		WorldPosition = position.ToVec2();
		IsVisible = isVisible;
		_onInitialized?.Invoke(this);
		int num = -1;
		int num2 = 4;
		if (MapEvent.IsNavalMapEvent || MapEvent.IsBlockade || MapEvent.IsBlockadeSallyOut)
		{
			num = _navalBattleSoundEventIndex;
		}
		else if (MapEvent.IsFieldBattle || MapEvent.IsSallyOut)
		{
			num = _battleSoundEventIndex;
			num2 = GetBattleSizeValue();
		}
		else if (MapEvent.IsSiegeAssault || MapEvent.IsSiegeOutside || MapEvent.IsSiegeAmbush)
		{
			num = _siegeSoundEventIndex;
		}
		else if (MapEvent.IsRaid)
		{
			num = _raidSoundEventIndex;
		}
		else if (MapEvent.IsHideoutBattle)
		{
			num = _hideoutBattleSoundEventIndex;
		}
		if (num != -1)
		{
			float height = 0f;
			CampaignVec2 point = MapEvent.MapEventSettlement?.Position ?? MapEvent.Position;
			Campaign.Current.MapSceneWrapper.GetHeightAtPoint(in point, ref height);
			_mapEventSoundEvent = SoundEvent.CreateEvent(num, MapScene);
			_mapEventSoundEvent.SetParameter("battle_size", num2);
			_mapEventSoundEvent.PlayInPosition(new Vec3(position.X, position.Y, height + 2f));
			if (!isVisible)
			{
				_mapEventSoundEvent.Pause();
			}
		}
	}

	private int GetBattleSizeValue()
	{
		if (MapEvent.IsSiegeAssault)
		{
			return 4;
		}
		int numberOfInvolvedMen = MapEvent.GetNumberOfInvolvedMen();
		if (numberOfInvolvedMen < 30)
		{
			return 0;
		}
		if (numberOfInvolvedMen < 80)
		{
			return 1;
		}
		if (numberOfInvolvedMen >= 120)
		{
			return 3;
		}
		return 2;
	}

	public void OnMapEventEnd()
	{
		_onDeactivate?.Invoke(this);
		if (_mapEventSoundEvent != null)
		{
			_mapEventSoundEvent.Stop();
			_mapEventSoundEvent = null;
		}
	}

	public void SetVisibility(bool isVisible)
	{
		IsVisible = isVisible;
		_onVisibilityChanged?.Invoke(this);
		SoundEvent mapEventSoundEvent = _mapEventSoundEvent;
		if (mapEventSoundEvent != null && mapEventSoundEvent.IsValid)
		{
			if (isVisible && _mapEventSoundEvent.IsPaused())
			{
				_mapEventSoundEvent.Resume();
			}
			else if (!isVisible && !_mapEventSoundEvent.IsPaused())
			{
				_mapEventSoundEvent.Pause();
			}
		}
	}
}
