using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class Markable : ScriptComponentBehavior
{
	public string MarkerPrefabName = "highlight_beam";

	private GameEntity _marker;

	private DestructableComponent _destructibleComponent;

	private bool _markerActive;

	private bool _markerVisible;

	private float _markerEventBeginningTime;

	private float _markerActiveDuration;

	private float _markerPassiveDuration;

	private bool MarkerActive
	{
		get
		{
			return _markerActive;
		}
		set
		{
			if (_markerActive != value)
			{
				_markerActive = value;
				SetScriptComponentToTick(GetTickRequirement());
			}
		}
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		_marker = TaleWorlds.Engine.GameEntity.Instantiate(Mission.Current.Scene, "highlight_beam", base.GameEntity.GetGlobalFrame());
		DeactivateMarker();
		_destructibleComponent = base.GameEntity.GetFirstScriptOfType<DestructableComponent>();
		SetScriptComponentToTick(GetTickRequirement());
	}

	public override TickRequirement GetTickRequirement()
	{
		if (MarkerActive)
		{
			return TickRequirement.Tick | base.GetTickRequirement();
		}
		return base.GetTickRequirement();
	}

	protected internal override void OnTick(float dt)
	{
		if (!MarkerActive)
		{
			return;
		}
		if (_destructibleComponent != null && _destructibleComponent.IsDestroyed)
		{
			if (_markerVisible)
			{
				DisableMarkerActivation();
			}
		}
		else if (_markerVisible)
		{
			if (Mission.Current.CurrentTime - _markerEventBeginningTime > _markerActiveDuration)
			{
				DeactivateMarker();
			}
		}
		else if (!_markerVisible && Mission.Current.CurrentTime - _markerEventBeginningTime > _markerPassiveDuration)
		{
			ActivateMarkerFor(_markerActiveDuration, _markerPassiveDuration);
		}
	}

	public void DisableMarkerActivation()
	{
		MarkerActive = false;
		DeactivateMarker();
	}

	public void ActivateMarkerFor(float activeSeconds, float passiveSeconds)
	{
		if (_destructibleComponent == null || !_destructibleComponent.IsDestroyed)
		{
			MarkerActive = true;
			_markerVisible = true;
			_markerEventBeginningTime = Mission.Current.CurrentTime;
			_markerActiveDuration = activeSeconds;
			_markerPassiveDuration = passiveSeconds;
			_marker.SetVisibilityExcludeParents(visible: true);
			_marker.BurstEntityParticle(doChildren: true);
		}
	}

	private void DeactivateMarker()
	{
		_markerVisible = false;
		_marker.SetVisibilityExcludeParents(visible: false);
		_markerEventBeginningTime = Mission.Current.CurrentTime;
	}

	public void ResetPassiveDurationTimer()
	{
		if (!_markerVisible && MarkerActive)
		{
			_markerEventBeginningTime = Mission.Current.CurrentTime;
		}
	}
}
