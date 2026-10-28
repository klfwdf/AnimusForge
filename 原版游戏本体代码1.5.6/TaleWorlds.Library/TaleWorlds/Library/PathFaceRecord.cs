namespace TaleWorlds.Library;

public struct PathFaceRecord(int index, int groupIndex, int islandIndex)
{
	public int FaceIndex = index;

	public int FaceGroupIndex = groupIndex;

	public int FaceIslandIndex = islandIndex;

	public static readonly PathFaceRecord NullFaceRecord = new PathFaceRecord(-1, -1, -1);

	public bool IsValid()
	{
		return FaceIndex != -1;
	}
}
