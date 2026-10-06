using TaleWorlds.Engine;

namespace SandBox.BoardGames.Objects;

public class Tile : ScriptComponentBehavior
{
	public MetaMesh TileMesh;

	protected override void OnInit()
	{
		base.OnInit();
		base.GameEntity.RemoveMultiMesh(base.GameEntity.GetMetaMesh(0));
	}

	public void SetVisibility(bool visible)
	{
		base.GameEntity.SetVisibilityExcludeParents(visible);
	}

	protected override bool MovesEntity()
	{
		return false;
	}
}
