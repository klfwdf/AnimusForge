using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class SpawnPathData
{
	public enum SnapMethod
	{
		DontSnap,
		SnapToTerrain,
		SnapToWaterLevel
	}

	public enum SearchDirection
	{
		Forward,
		Backward
	}

	public const float MinimumSpawnPathOffset = 1f;

	public readonly Scene Scene;

	public readonly Path Path;

	public readonly bool IsInverted;

	public readonly float PivotOffset;

	public readonly float PathLength;

	public readonly SnapMethod SnapType;

	private readonly MBList<(float startOffset, float endOffset)> _freeSegments = new MBList<(float, float)>();

	public bool IsValid
	{
		get
		{
			if (Scene != null && Path != null)
			{
				return Path.NumberOfPoints > 1;
			}
			return false;
		}
	}

	public int FreeSegmentCount => _freeSegments.Count;

	public SpawnPathData Invert()
	{
		float a = PathLength - PivotOffset;
		return new SpawnPathData(Scene, Path, MathF.Max(a, 0f), !IsInverted, SnapType);
	}

	public void ClampPathOffset(ref float relativePathOffset)
	{
		float num = MathF.Clamp(PivotOffset + relativePathOffset, 0f, PathLength) - PivotOffset;
		relativePathOffset = num;
	}

	public float ConvertPointToRelativePathOffset(int pointIndex)
	{
		float num = 0f;
		for (int i = 0; i < pointIndex; i++)
		{
			num += Path.GetArcLength(i);
		}
		num = MathF.Clamp(num, 0f, PathLength);
		num = (IsInverted ? (PathLength - num) : num);
		return num - PivotOffset;
	}

	public float ConvertRelativePathOffsetToPathDistance(float relativePathOffset)
	{
		float num = MathF.Clamp(PivotOffset + relativePathOffset, 0f, PathLength);
		return IsInverted ? (PathLength - num) : num;
	}

	public int GetNodeIndexAtPathDistance(float pathDistance)
	{
		int result = Path.NumberOfPoints - 2;
		float num = 0f;
		for (int i = 0; i < Path.NumberOfPoints - 1; i++)
		{
			float arcLength = Path.GetArcLength(i);
			if (pathDistance >= num && pathDistance < num + arcLength)
			{
				result = i;
				break;
			}
			num += arcLength;
		}
		return result;
	}

	public float GetBaseOffset()
	{
		if (IsInverted)
		{
			return PivotOffset - PathLength + 1f;
		}
		return 0f - PivotOffset + 1f;
	}

	public bool IsPathOffsetValid(float relativePathOffset)
	{
		float pathDistance = ConvertRelativePathOffsetToPathDistance(relativePathOffset);
		int nodeIndexAtPathDistance = GetNodeIndexAtPathDistance(pathDistance);
		int nodeIndex = nodeIndexAtPathDistance + 1;
		if (!Path.HasValidAlphaAtPathPoint(nodeIndexAtPathDistance))
		{
			return Path.HasValidAlphaAtPathPoint(nodeIndex);
		}
		return true;
	}

	public float GetOffsetOverflow(float relativePathOffset)
	{
		float num = PivotOffset + relativePathOffset;
		if (num < 0f)
		{
			return num;
		}
		if (num > PathLength)
		{
			return num - PathLength;
		}
		return 0f;
	}

	public MatrixFrame GetSpawnFrame(float relativePathOffset, bool searchNearestValidFrame = false, SearchDirection searchDirection = SearchDirection.Backward)
	{
		MatrixFrame identity = MatrixFrame.Identity;
		float distance = ConvertRelativePathOffsetToPathDistance(relativePathOffset);
		if (searchNearestValidFrame)
		{
			bool flag = searchDirection == SearchDirection.Forward;
			flag = (IsInverted ? (!flag) : flag);
			identity = Path.GetNearestFrameWithValidAlphaForDistance(distance, flag);
		}
		else
		{
			identity = Path.GetFrameForDistance(distance);
		}
		identity.rotation.f = (IsInverted ? (-identity.rotation.f) : identity.rotation.f);
		identity.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		if (SnapType != SnapMethod.DontSnap)
		{
			if (SnapType == SnapMethod.SnapToTerrain)
			{
				identity.origin.z = Scene.GetTerrainHeight(identity.origin.AsVec2);
			}
			else if (SnapType == SnapMethod.SnapToWaterLevel)
			{
				identity.origin.z = Scene.GetWaterLevel();
			}
		}
		return identity;
	}

	public void GetSpawnPathFrameFacingTarget(float basePathOffset, float targetPathOffset, bool useTangentDirection, out Vec2 spawnPathPosition, out Vec2 spawnPathDirection, bool decideDirectionDynamically = false, float dynamicDistancePercentage = 0.2f)
	{
		ClampPathOffset(ref basePathOffset);
		ClampPathOffset(ref targetPathOffset);
		MatrixFrame onPathFrame = GetSpawnFrame(basePathOffset);
		float epsilon = 0.01f;
		spawnPathPosition = onPathFrame.origin.AsVec2;
		if (MBMath.ApproximatelyEquals(basePathOffset, targetPathOffset, epsilon))
		{
			if (MBMath.ApproximatelyEquals(basePathOffset, 0f, epsilon))
			{
				spawnPathDirection = GetSpawnFrame(0f).rotation.f.AsVec2.Normalized();
			}
			else if (useTangentDirection)
			{
				spawnPathDirection = GetSpawnPathTangentDirection(basePathOffset, in onPathFrame, 0f);
			}
			else
			{
				spawnPathDirection = (GetSpawnFrame(0f).origin.AsVec2 - spawnPathPosition).Normalized();
			}
			return;
		}
		MatrixFrame spawnFrame = GetSpawnFrame(targetPathOffset);
		if (decideDirectionDynamically)
		{
			WorldPosition point = new WorldPosition(Scene, onPathFrame.origin);
			WorldPosition point2 = new WorldPosition(Scene, spawnFrame.origin);
			if (Scene.GetPathDistanceBetweenPositions(ref point, ref point2, 0.1f, out var pathDistance))
			{
				float length = (spawnFrame.origin - onPathFrame.origin).Length;
				useTangentDirection = pathDistance >= length * (1f + dynamicDistancePercentage);
			}
		}
		if (useTangentDirection)
		{
			spawnPathDirection = GetSpawnPathTangentDirection(basePathOffset, in onPathFrame, targetPathOffset);
		}
		else
		{
			spawnPathDirection = (spawnFrame.origin.AsVec2 - spawnPathPosition).Normalized();
		}
	}

	private Vec2 GetSpawnPathTangentDirection(float onPathOffset, in MatrixFrame onPathFrame, float referenceOffset)
	{
		Vec2 asVec = onPathFrame.origin.AsVec2;
		if (MBMath.ApproximatelyEquals(onPathOffset, referenceOffset))
		{
			return onPathFrame.rotation.f.AsVec2.Normalized();
		}
		int num = ((!(onPathOffset > referenceOffset)) ? 1 : (-1));
		float relativePathOffset = onPathOffset + (float)num * 1f;
		ClampPathOffset(ref relativePathOffset);
		Vec2 vec = GetSpawnFrame(relativePathOffset).origin.AsVec2 - asVec;
		if (vec.LengthSquared < 1E-06f)
		{
			return num * onPathFrame.rotation.f.AsVec2.Normalized();
		}
		return vec.Normalized();
	}

	private SpawnPathData(Scene scene, Path path, float pivotOffset, bool isInverted = false, SnapMethod snapType = SnapMethod.DontSnap)
	{
		Scene = scene;
		Path = path;
		PathLength = Path.GetTotalLength();
		PivotOffset = MathF.Clamp(pivotOffset, 1f, PathLength - 1f);
		IsInverted = isInverted;
		SnapType = snapType;
		BuildFreeSegments();
	}

	private void BuildFreeSegments()
	{
		if (!IsValid)
		{
			return;
		}
		int numberOfPoints = Path.NumberOfPoints;
		float[] array = new float[numberOfPoints];
		for (int i = 0; i < numberOfPoints; i++)
		{
			array[i] = ConvertPointToRelativePathOffset(i);
		}
		bool flag = false;
		float num = 0f;
		float num2 = 0f;
		int num3 = 0;
		int num4 = numberOfPoints - 1;
		int num5 = 1;
		if (IsInverted)
		{
			num3 = numberOfPoints - 1;
			num4 = 0;
			num5 = -1;
		}
		for (int j = num3; j != num4; j += num5)
		{
			int num6 = j + num5;
			bool num7 = Path.HasValidAlphaAtPathPoint(j) || Path.HasValidAlphaAtPathPoint(num6);
			float num8 = array[j];
			float num9 = array[num6];
			if (!num7)
			{
				if (flag)
				{
					float relativePathOffset = num + 0.001f;
					float relativePathOffset2 = num2 - 0.001f;
					if (relativePathOffset2 > relativePathOffset)
					{
						ClampPathOffset(ref relativePathOffset);
						ClampPathOffset(ref relativePathOffset2);
						_freeSegments.Add((relativePathOffset, relativePathOffset2));
					}
					flag = false;
				}
			}
			else if (!flag)
			{
				num = num8;
				num2 = num9;
				flag = true;
			}
			else
			{
				num2 = num9;
			}
		}
		if (!flag)
		{
			return;
		}
		float relativePathOffset3 = num + 0.001f;
		float relativePathOffset4 = num2 - 0.001f;
		if (relativePathOffset4 > relativePathOffset3)
		{
			ClampPathOffset(ref relativePathOffset3);
			ClampPathOffset(ref relativePathOffset4);
			if (relativePathOffset4 > relativePathOffset3)
			{
				_freeSegments.Add((relativePathOffset3, relativePathOffset4));
			}
		}
	}

	public static SpawnPathData Create(Scene scene, Path path, float pivotOffset, bool isInverted = false, SnapMethod snapType = SnapMethod.DontSnap)
	{
		return new SpawnPathData(scene, path, pivotOffset, isInverted, snapType);
	}

	internal (float startOffset, float endOffset) GetFreeSegment(int segmentIndex)
	{
		return _freeSegments[segmentIndex];
	}
}
