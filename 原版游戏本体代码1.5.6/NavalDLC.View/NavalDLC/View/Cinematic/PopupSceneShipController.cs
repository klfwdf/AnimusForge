using NavalDLC.Missions.NavalPhysics;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace NavalDLC.View.Cinematic;

public class PopupSceneShipController : ScriptComponentBehavior
{
	[EditableScriptComponentVariable(true, "")]
	private Vec3 _continousForce = Vec3.Zero;

	[EditableScriptComponentVariable(true, "")]
	private bool _isAnchored;

	[EditableScriptComponentVariable(true, "")]
	private string _targetShipEntityTag = string.Empty;

	private GameEntity _targetShipEntity;

	private MatrixFrame _initialShipFrame;

	private bool _isApplyingForce;

	public SimpleButton StartApplyingForce;

	public SimpleButton StopApplyingForce;

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.FixedTick;
	}

	public PopupSceneShipController()
	{
		StartApplyingForce = new SimpleButton();
		StopApplyingForce = new SimpleButton();
	}

	protected override void OnInit()
	{
		base.OnInit();
		_isApplyingForce = true;
		_targetShipEntity = base.Scene.FindEntityWithTag(_targetShipEntityTag);
	}

	protected override void OnFixedTick(float fixedDt)
	{
		ApplyForce(fixedDt);
	}

	protected override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		ApplyForce(0.016f);
	}

	protected override void OnParallelFixedTick(float fixedDt)
	{
		base.OnParallelFixedTick(fixedDt);
		ApplyForce(fixedDt);
	}

	private void ApplyForce(float dt)
	{
		if (_targetShipEntity?.Scene != base.Scene)
		{
			_targetShipEntity = base.Scene.FindEntityWithTag(_targetShipEntityTag);
		}
		if (_targetShipEntity?.Scene != base.Scene)
		{
			return;
		}
		NavalPhysics firstScriptOfType = _targetShipEntity.GetFirstScriptOfType<NavalPhysics>();
		if (_isAnchored)
		{
			if (firstScriptOfType != null)
			{
				firstScriptOfType.SetAnchorFrame(in Vec2.Zero, in Vec2.Forward);
				firstScriptOfType.SetAnchor(isAnchored: true);
			}
		}
		else if (_isApplyingForce)
		{
			Vec3 localForce = _continousForce * _targetShipEntity.Mass * dt;
			_targetShipEntity.ApplyLocalForceAtLocalPosToDynamicBody(base.GameEntity.CenterOfMass, localForce, GameEntityPhysicsExtensions.ForceMode.Force);
		}
	}

	protected override void OnEditorVariableChanged(string variableName)
	{
		base.OnEditorVariableChanged(variableName);
		if (variableName == "StartApplyingForce")
		{
			if (!_isApplyingForce)
			{
				_targetShipEntity = base.Scene.FindEntityWithTag(_targetShipEntityTag);
				if (_targetShipEntity != null)
				{
					_isApplyingForce = true;
					_initialShipFrame = _targetShipEntity.GetGlobalFrame();
				}
			}
		}
		else if (variableName == "StopApplyingForce" && _isApplyingForce)
		{
			_targetShipEntity = base.Scene.FindEntityWithTag(_targetShipEntityTag);
			if (_targetShipEntity != null)
			{
				_targetShipEntity.SetGlobalFrame(in _initialShipFrame);
				_targetShipEntity.SetAngularVelocity(Vec3.Zero);
				_targetShipEntity.SetLinearVelocity(Vec3.Zero);
				_isApplyingForce = false;
			}
		}
	}
}
