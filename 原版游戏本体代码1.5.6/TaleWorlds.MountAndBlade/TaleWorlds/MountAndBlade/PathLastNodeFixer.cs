using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class PathLastNodeFixer : UsableMissionObjectComponent
{
	public IPathHolder PathHolder;

	private Scene _scene;

	protected internal override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		Path pathWithName = _scene.GetPathWithName(PathHolder.PathEntity);
		Update(pathWithName);
	}

	protected internal override void OnAdded(Scene scene)
	{
		base.OnAdded(scene);
		_scene = scene;
		Update();
	}

	public void Update()
	{
		Path pathWithName = _scene.GetPathWithName(PathHolder.PathEntity);
		Update(pathWithName);
	}

	private void Update(Path path)
	{
		_ = path != null;
	}
}
