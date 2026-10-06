namespace TaleWorlds.Library;

public struct PathFaceRecord
{
	public int FaceIndex;

	public int FaceGroupIndex;

	public int FaceIslandIndex;

	public static readonly PathFaceRecord NullFaceRecord = new PathFaceRecord(-1, -1, -1);

	public PathFaceRecord(int index, int groupIndex, int islandIndex)
	{
		FaceIndex = index;
		FaceGroupIndex = groupIndex;
		FaceIslandIndex = islandIndex;
	}

	public bool IsValid()
	{
		return FaceIndex != -1;
	}
}
