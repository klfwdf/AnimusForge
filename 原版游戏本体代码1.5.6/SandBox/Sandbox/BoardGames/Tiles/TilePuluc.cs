using SandBox.BoardGames.Objects;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.BoardGames.Tiles;

public class TilePuluc : Tile1D
{
	public Vec3 PosLeft { get; private set; }

	public Vec3 PosLeftMid { get; private set; }

	public Vec3 PosRight { get; private set; }

	public Vec3 PosRightMid { get; private set; }

	public TilePuluc(GameEntity entity, BoardGameDecal decal, int x)
		: base(entity, decal, x)
	{
		UpdateTilePosition();
	}

	public void UpdateTilePosition()
	{
		MatrixFrame globalFrame = base.Entity.GetGlobalFrame();
		MetaMesh tileMesh = base.Entity.GetFirstScriptOfType<Tile>().TileMesh;
		Vec3 vec = tileMesh.GetBoundingBox().max - tileMesh.GetBoundingBox().min;
		ref Mat3 rotation = ref globalFrame.rotation;
		MatrixFrame frame = tileMesh.Frame;
		Mat3 mat = rotation.TransformToParent(in frame.rotation);
		Vec3 vec2 = mat.TransformToParent(new Vec3(0f, vec.y / 6f));
		Vec3 vec3 = mat.TransformToParent(new Vec3(0f, vec.y / 3f));
		Vec3 globalPosition = base.Entity.GlobalPosition;
		PosLeft = globalPosition + vec3;
		PosLeftMid = globalPosition + vec2;
		PosRight = globalPosition - vec3;
		PosRightMid = globalPosition - vec2;
	}
}
