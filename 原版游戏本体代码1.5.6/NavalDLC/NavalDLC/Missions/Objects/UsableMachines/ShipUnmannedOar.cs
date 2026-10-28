using NavalDLC.Missions.NavalPhysics;
using NavalDLC.Missions.ShipActuators;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Objects.UsableMachines;

public class ShipUnmannedOar : ScriptComponentBehavior, IShipOarScriptComponent
{
	private GameEntity _oarEntity;

	private MatrixFrame _oarExtractedEntitialFrame;

	private MatrixFrame _oarRetractedEntitialFrame;

	private MissionOar _oar;

	private float _lastIdleTime;

	private DestructableComponent _destructableComponent;

	private BoundingBox _unmannedOarBaseBoundingBox;

	protected override void OnInit()
	{
		base.OnInit();
		ShipOarDeck.LoadOarScriptEntity(base.GameEntity, out var oarEntity, ref _oarExtractedEntitialFrame, ref _oarRetractedEntitialFrame, out var _);
		_oarEntity = (oarEntity.IsValid ? TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(oarEntity) : null);
		SetScriptComponentToTick(GetTickRequirement());
		_destructableComponent = base.GameEntity.GetFirstScriptOfType<DestructableComponent>();
		base.GameEntity.SetHasCustomBoundingBoxValidationSystem(hasCustomBoundingBox: true);
		_unmannedOarBaseBoundingBox = base.GameEntity.ComputeBoundingBoxFromLongestHalfDimension(2f);
	}

	public void InitializeOar(MissionOar oar)
	{
		_oar = oar;
	}

	public override TickRequirement GetTickRequirement()
	{
		return base.GetTickRequirement() | TickRequirement.TickParallel;
	}

	public void ArrangeOarBoundingBox()
	{
		base.GameEntity.SetManualLocalBoundingBox(in _unmannedOarBaseBoundingBox);
		base.GameEntity.Parent.SetBoundingboxDirty();
	}

	protected override void OnBoundingBoxValidate()
	{
		BoundingBox boundingBox = base.GameEntity.ComputeBoundingBoxIncludeChildren();
		boundingBox.RelaxWithBoundingBox(_unmannedOarBaseBoundingBox);
		boundingBox.RecomputeRadius();
		base.GameEntity.RelaxLocalBoundingBox(in boundingBox);
	}

	public bool CheckOarMachineFlags(bool editMode)
	{
		foreach (WeakGameEntity child in base.GameEntity.GetChildren())
		{
			if (!child.EntityFlags.HasAnyFlag(EntityFlags.DontSaveToScene) && !child.EntityFlags.HasAnyFlag(EntityFlags.DoesNotAffectParentsLocalBb))
			{
				string msg = $"Root Entity: {base.GameEntity.Root.Name} {base.GameEntity.Name}'s child {child.Name} must have Does not Affect Parent's Local Bounding Box flag.";
				if (editMode)
				{
					MBEditor.AddEntityWarning(child, msg);
				}
				return false;
			}
		}
		return true;
	}

	public void SetSlowDownPhaseForDuration(float slowDownMultiplier, float slowDownDuration)
	{
		_oar.SetSlowDownPhaseForDuration(slowDownMultiplier, slowDownDuration);
	}

	protected override void OnTickParallel(float dt)
	{
		bool newIsUsed = !_oar.OwnerShip.BeingAbandoned && _oar.OwnerShip.Physics.NavalSinkingState == NavalDLC.Missions.NavalPhysics.NavalPhysics.SinkingState.Floating && (_destructableComponent == null || !_destructableComponent.IsDestroyed);
		_oar.SetUsed(newIsUsed, -1);
		MatrixFrame frame = _oar.ComputeOarEntityFrame(dt, base.GameEntity.GetLocalFrame(), _oarEntity.GetLocalFrame(), in _oarExtractedEntitialFrame, in _oarRetractedEntitialFrame, _lastIdleTime, forUnmanned: true);
		_oarEntity.SetLocalFrame(ref frame, isTeleportation: false);
		if (!_oar.IsExtracted)
		{
			_lastIdleTime = Mission.Current.CurrentTime;
		}
	}
}
