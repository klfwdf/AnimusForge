using NavalDLC.Map;
using SandBox;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace NavalDLC.View.Map.Visuals;

public class StormVisual : MapEntityVisual<Storm>
{
	private enum StormVisualState
	{
		VisualNotInitialized,
		Developing,
		Active,
		Finalizing,
		ReadyToBeReleased
	}

	public const int DefaultStormVisualHeight = 0;

	private StormVisualState _visualState;

	private SoundEvent _stormSoundEvent;

	public GameEntity VisualEntity;

	private Scene _mapScene;

	public override CampaignVec2 InteractionPositionForPlayer => new CampaignVec2(base.MapEntity.CurrentPosition, isOnLand: true);

	public override MapEntityVisual AttachedTo => null;

	public bool IsReadyToBeReleased => base.MapEntity.IsReadyToBeFinalized;

	public StormVisual(Storm storm)
		: base(storm)
	{
		_mapScene = ((MapScene)Campaign.Current.MapSceneWrapper).Scene;
		_visualState = StormVisualState.VisualNotInitialized;
		_stormSoundEvent = SoundManager.CreateEvent("event:/map/ambient/node/hurricane", _mapScene);
		_stormSoundEvent.SetPosition(storm.CurrentPosition.ToVec3());
		_stormSoundEvent.SetParameter("StormIntensity", (float)storm.StormType);
	}

	public override bool OnMapClick(bool followModifierUsed)
	{
		return false;
	}

	public override void OnHover()
	{
	}

	public override void OnOpenEncyclopedia()
	{
	}

	public override bool IsVisibleOrFadingOut()
	{
		return base.MapEntity.IsActive;
	}

	public override Vec3 GetVisualPosition()
	{
		return InteractionPositionForPlayer.AsVec3();
	}

	public void Tick()
	{
		StormVisualState stormVisualState = GetStormVisualState(base.MapEntity);
		if (_visualState != stormVisualState)
		{
			UpdateVisualState(stormVisualState);
		}
		if (VisualEntity != null)
		{
			VisualTick();
		}
		base.MapEntity.OnVisualUpdated();
	}

	private void VisualTick()
	{
		Vec3 localPosition = new Vec3(base.MapEntity.CurrentPosition);
		VisualEntity.SetLocalPosition(localPosition);
		_stormSoundEvent.SetPosition(VisualEntity.GlobalPosition);
	}

	private void UpdateVisualState(StormVisualState newState)
	{
		if (VisualEntity != null)
		{
			_mapScene.RemoveEntity(VisualEntity, 0);
			VisualEntity = null;
		}
		_visualState = newState;
		switch (newState)
		{
		case StormVisualState.Developing:
			if (NavalDLCManager.Instance.StormManager.DebugVisualsEnabled)
			{
				VisualEntity = GameEntity.Instantiate(_mapScene, "editor_cube", MatrixFrame.Identity);
			}
			break;
		case StormVisualState.Active:
			_stormSoundEvent.Play();
			switch (base.MapEntity.StormType)
			{
			case Storm.StormTypes.Storm:
				VisualEntity = GameEntity.Instantiate(_mapScene, "psys_mapicon_lightclouds", MatrixFrame.Identity);
				break;
			case Storm.StormTypes.ThunderStorm:
				VisualEntity = GameEntity.Instantiate(_mapScene, "psys_mapicon_darkclouds", MatrixFrame.Identity);
				break;
			case Storm.StormTypes.Hurricane:
				VisualEntity = GameEntity.Instantiate(_mapScene, "psys_mapicon_typhoon", MatrixFrame.Identity);
				break;
			}
			_visualState = StormVisualState.Active;
			break;
		case StormVisualState.Finalizing:
			_stormSoundEvent.Stop();
			if (NavalDLCManager.Instance.StormManager.DebugVisualsEnabled)
			{
				VisualEntity = GameEntity.Instantiate(_mapScene, "editor_cube", MatrixFrame.Identity);
			}
			break;
		}
	}

	private StormVisualState GetStormVisualState(Storm storm)
	{
		if (storm.IsReadyToBeFinalized)
		{
			return StormVisualState.ReadyToBeReleased;
		}
		if (storm.IsActive)
		{
			return StormVisualState.Active;
		}
		if (storm.IsInDevelopingState)
		{
			return StormVisualState.Developing;
		}
		if (storm.IsInFinalizingState)
		{
			return StormVisualState.Finalizing;
		}
		return StormVisualState.VisualNotInitialized;
	}
}
