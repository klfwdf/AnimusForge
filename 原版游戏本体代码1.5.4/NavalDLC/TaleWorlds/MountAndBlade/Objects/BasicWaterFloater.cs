using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Objects;

internal class BasicWaterFloater : ScriptComponentBehavior
{
	protected override void OnInit()
	{
	}

	protected override void OnTick(float dt)
	{
		Float();
	}

	protected override void OnEditorInit()
	{
	}

	protected override void OnEditorTick(float dt)
	{
		Float();
	}

	private void Float()
	{
		MatrixFrame frame = base.GameEntity.GetGlobalFrame();
		frame.origin.z = base.Scene.GetWaterLevelAtPosition(frame.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
		base.GameEntity.SetGlobalFrame(in frame);
	}
}
