using TaleWorlds.Library;

namespace TaleWorlds.Engine;

public struct WorldFrame(Mat3 rotation, WorldPosition origin)
{
	public Mat3 Rotation = rotation;

	public WorldPosition Origin = origin;

	public static readonly WorldFrame Invalid = new WorldFrame(Mat3.Identity, WorldPosition.Invalid);

	public bool IsValid => Origin.IsValid;

	public MatrixFrame ToGroundMatrixFrame()
	{
		return new MatrixFrame(in Rotation, Origin.GetGroundVec3());
	}

	public MatrixFrame ToGroundMatrixFrameMT()
	{
		return new MatrixFrame(in Rotation, Origin.GetGroundVec3MT());
	}

	public MatrixFrame ToNavMeshMatrixFrame()
	{
		return new MatrixFrame(in Rotation, Origin.GetNavMeshVec3());
	}
}
