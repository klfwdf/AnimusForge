using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class Mover : ScriptComponentBehavior
{
	[EditorVisibleScriptComponentVariable(true)]
	private string _pathname = "";

	[EditorVisibleScriptComponentVariable(true)]
	private float _speed;

	[EditorVisibleScriptComponentVariable(true)]
	private bool _moveGhost;

	private GameEntity _moverGhost;

	private PathTracker _tracker;

	protected internal override void OnEditorTick(float dt)
	{
		if (base.GameEntity.EntityFlags.HasAnyFlag(EntityFlags.IsHelper))
		{
			return;
		}
		if (_moverGhost == null && _pathname != "")
		{
			CreateOrUpdateMoverGhost();
		}
		if (_tracker == null || !_tracker.IsValid)
		{
			return;
		}
		if (_moveGhost)
		{
			_tracker.Advance(_speed * dt);
			if (_tracker.TotalDistanceTraveled >= _tracker.GetPathLength())
			{
				_tracker.Reset();
			}
		}
		else
		{
			_tracker.Advance(0f);
		}
		MatrixFrame frame = _tracker.CurrentFrame;
		_moverGhost.SetFrame(ref frame);
	}

	protected internal override void OnEditorVariableChanged(string variableName)
	{
		if (variableName == "_pathname")
		{
			CreateOrUpdateMoverGhost();
		}
		else if (variableName == "_moveGhost")
		{
			if (!_moveGhost)
			{
				_moverGhost.SetVisibilityExcludeParents(visible: false);
			}
			else
			{
				_moverGhost.SetVisibilityExcludeParents(visible: true);
			}
		}
	}

	private void CreateOrUpdateMoverGhost()
	{
		Path pathWithName = base.GameEntity.Scene.GetPathWithName(_pathname);
		if (pathWithName != null)
		{
			_tracker = new PathTracker(pathWithName, Vec3.One);
			_tracker.Reset();
			base.GameEntity.SetLocalPosition(_tracker.CurrentFrame.origin);
			if (_moverGhost == null)
			{
				_moverGhost = TaleWorlds.Engine.GameEntity.CopyFrom(base.GameEntity.Scene, base.GameEntity);
				_moverGhost.EntityFlags |= EntityFlags.IsHelper | EntityFlags.DontSaveToScene | EntityFlags.DoNotTick;
				_moverGhost.SetAlpha(0.2f);
			}
			else
			{
				_moverGhost.SetLocalPosition(_tracker.CurrentFrame.origin);
			}
		}
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		Path pathWithName = base.GameEntity.Scene.GetPathWithName(_pathname);
		if (pathWithName != null)
		{
			_tracker = new PathTracker(pathWithName, Vec3.One);
			_tracker.Reset();
			base.GameEntity.SetLocalPosition(_tracker.CurrentFrame.origin);
		}
		SetScriptComponentToTick(GetTickRequirement());
	}

	public override TickRequirement GetTickRequirement()
	{
		if (base.GameEntity.IsVisibleIncludeParents())
		{
			return TickRequirement.Tick | base.GetTickRequirement();
		}
		return base.GetTickRequirement();
	}

	protected internal override void OnTick(float dt)
	{
		if (Mission.Current.Mode == MissionMode.Battle && _tracker != null && _tracker.IsValid && _tracker.TotalDistanceTraveled < _tracker.GetPathLength())
		{
			_tracker.Advance(_speed * dt);
			MatrixFrame frame = _tracker.CurrentFrame;
			base.GameEntity.SetFrame(ref frame);
		}
	}
}
