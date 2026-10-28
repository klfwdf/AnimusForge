using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.View;

public class PirateSpawnPoint : ScriptComponentBehavior
{
	public string ClanStringId;

	public bool ToggleDebugRadius;

	public float Radius = 10f;

	public Vec2 GetPosition()
	{
		return base.GameEntity.GlobalPosition.AsVec2;
	}

	protected override void OnInit()
	{
	}

	protected override void OnEditorInit()
	{
	}

	protected override void OnSceneSave(string saveFolder)
	{
	}

	protected override void OnEditorTick(float dt)
	{
		if (ToggleDebugRadius || MBEditor.IsEntitySelected(base.GameEntity))
		{
			DebugExtensions.RenderDebugCircleOnTerrain(base.Scene, base.GameEntity.GetGlobalFrame(), Radius, Colors.Red.ToUnsignedInteger());
		}
	}
}
