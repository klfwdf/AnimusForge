using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class BattleSideSpawnPathSelector
{
	private struct ReinforcementPathCandidate
	{
		public readonly float Cost;

		public readonly Path Path;

		public readonly float PivotOffset;

		public readonly float ReinforcementOffset;

		public readonly bool IsInverted;

		public ReinforcementPathCandidate(float cost, Path path, float pivotOffset, float reinforcementOffset, bool isInverted)
		{
			Cost = cost;
			Path = path;
			PivotOffset = pivotOffset;
			ReinforcementOffset = reinforcementOffset;
			IsInverted = isInverted;
		}
	}

	public const int MaxNeighborCount = 2;

	private const int MaxPointsOnPath = 200;

	private readonly Mission _mission;

	private readonly SpawnPathData _initialSpawnPath;

	private readonly SpawnPathData.SnapMethod _pathSnapMethod;

	private readonly MBList<(SpawnPathData pathData, float startOffset)> _reinforcementSpawnPaths;

	private readonly MatrixFrame[] _tempPathPoints;

	private float[] _tempCandidatePointOffsetsOnSegment = new float[5];

	public SpawnPathData InitialSpawnPath => _initialSpawnPath;

	public MBReadOnlyList<(SpawnPathData pathData, float startOffset)> ReinforcementPaths => _reinforcementSpawnPaths;

	public BattleSideSpawnPathSelector(Mission mission, Path initialPath, float initialPivotOffset, bool initialPathIsInverted)
	{
		_mission = mission;
		_pathSnapMethod = (mission.IsNavalBattle ? SpawnPathData.SnapMethod.SnapToWaterLevel : (mission.IsFieldBattle ? SpawnPathData.SnapMethod.SnapToTerrain : SpawnPathData.SnapMethod.DontSnap));
		_initialSpawnPath = SpawnPathData.Create(_mission.Scene, initialPath, initialPivotOffset, initialPathIsInverted, _pathSnapMethod);
		_reinforcementSpawnPaths = new MBList<(SpawnPathData, float)>();
		_tempPathPoints = new MatrixFrame[200];
		FindReinforcementPaths();
	}

	public bool HasReinforcementPath(Path path)
	{
		if (path != null)
		{
			return _reinforcementSpawnPaths.Exists(((SpawnPathData pathData, float startOffset) pdt) => pdt.pathData.Path.Pointer == path.Pointer);
		}
		return false;
	}

	private void FindReinforcementPaths()
	{
		_reinforcementSpawnPaths.Clear();
		MBList<ReinforcementPathCandidate> mBList = new MBList<ReinforcementPathCandidate>();
		float pathLength = _initialSpawnPath.PathLength;
		float pivotOffset = pathLength * 0.5f;
		float reinforcementOffset = (0f - pathLength) * 0.5f;
		ReinforcementPathCandidate item = new ReinforcementPathCandidate(0f, _initialSpawnPath.Path, pivotOffset, reinforcementOffset, _initialSpawnPath.IsInverted);
		mBList.Add(item);
		MBList<Path> allSpawnPaths = MBSceneUtilities.GetAllSpawnPaths(_mission.Scene);
		if (allSpawnPaths.Count > 1)
		{
			_initialSpawnPath.Path.GetPoints(_tempPathPoints);
			GetPathBaseFrameData(_tempPathPoints, _initialSpawnPath.Path.NumberOfPoints, _initialSpawnPath.IsInverted, out var mainPathBasePosition, out var _);
			Vec2 mainPathCenterPosition = _initialSpawnPath.GetCenterFrame().origin.AsVec2;
			MBList<ReinforcementPathCandidate> mBList2 = new MBList<ReinforcementPathCandidate>();
			MBList<ReinforcementPathCandidate> mBList3 = new MBList<ReinforcementPathCandidate>();
			foreach (Path item4 in allSpawnPaths)
			{
				if (item4.Pointer == item.Path.Pointer)
				{
					continue;
				}
				item4.GetPoints(_tempPathPoints);
				float cost = float.MaxValue;
				float arcLength = 0f;
				float reinforcementOffset2 = 0f;
				float num = item4.GetTotalLength() * 0.5f;
				if (IsValidReinforcementCandidate(in mainPathBasePosition, in mainPathCenterPosition, item4, _tempPathPoints, num, isCandidatePathInverted: false, out cost, out arcLength, out reinforcementOffset2))
				{
					if (arcLength > 1E-05f)
					{
						mBList2.Add(new ReinforcementPathCandidate(cost, item4, num, reinforcementOffset2, isInverted: false));
					}
					else if (arcLength < -1E-05f)
					{
						mBList3.Add(new ReinforcementPathCandidate(cost, item4, num, reinforcementOffset2, isInverted: false));
					}
				}
				if (IsValidReinforcementCandidate(in mainPathBasePosition, in mainPathCenterPosition, item4, _tempPathPoints, num, isCandidatePathInverted: true, out cost, out arcLength, out reinforcementOffset2))
				{
					if (arcLength > 0.001f)
					{
						mBList2.Add(new ReinforcementPathCandidate(cost, item4, num, reinforcementOffset2, isInverted: true));
					}
					else if (arcLength < -0.001f)
					{
						mBList3.Add(new ReinforcementPathCandidate(cost, item4, num, reinforcementOffset2, isInverted: true));
					}
				}
			}
			if (mBList2.Count > 0 || mBList3.Count > 0)
			{
				mBList2.Sort(delegate(ReinforcementPathCandidate left, ReinforcementPathCandidate right)
				{
					float cost2 = right.Cost;
					return cost2.CompareTo(left.Cost);
				});
				mBList3.Sort(delegate(ReinforcementPathCandidate left, ReinforcementPathCandidate right)
				{
					float cost2 = right.Cost;
					return cost2.CompareTo(left.Cost);
				});
				int num2 = 2;
				MBList<UIntPtr> mBList4 = new MBList<UIntPtr>();
				MBList<ReinforcementPathCandidate>[] array = new MBList<ReinforcementPathCandidate>[2] { mBList2, mBList3 };
				int num3 = 0;
				while (num2 > 0 && (mBList2.Count > 0 || mBList3.Count > 0))
				{
					MBList<ReinforcementPathCandidate> mBList5 = array[num3];
					if (mBList5.Count > 0)
					{
						int index = mBList5.Count - 1;
						ReinforcementPathCandidate item2 = mBList5[index];
						mBList5.RemoveAt(index);
						if (!mBList4.Contains(item2.Path.Pointer))
						{
							mBList.Add(item2);
							mBList4.Add(item2.Path.Pointer);
							num2--;
						}
					}
					num3 = (num3 + 1) % array.Length;
				}
			}
		}
		foreach (ReinforcementPathCandidate item5 in mBList)
		{
			SpawnPathData item3 = SpawnPathData.Create(_initialSpawnPath.Scene, item5.Path, item5.PivotOffset, item5.IsInverted, _pathSnapMethod);
			_reinforcementSpawnPaths.Add((item3, item5.ReinforcementOffset));
		}
	}

	private void GetPathBaseFrameData(MatrixFrame[] pathPoints, int pathPointCount, bool isInverted, out Vec2 mainPathBasePosition, out Vec2 mainPathForward)
	{
		MatrixFrame matrixFrame;
		MatrixFrame matrixFrame2;
		if (isInverted)
		{
			matrixFrame = pathPoints[pathPointCount - 1];
			matrixFrame2 = pathPoints[pathPointCount - 2];
		}
		else
		{
			matrixFrame = pathPoints[0];
			matrixFrame2 = pathPoints[1];
		}
		mainPathForward = (matrixFrame2.origin - matrixFrame.origin).AsVec2.Normalized();
		mainPathBasePosition = matrixFrame.origin.AsVec2;
	}

	private bool IsValidReinforcementCandidate(in Vec2 mainPathBasePosition, in Vec2 mainPathCenterPosition, Path candidatePath, MatrixFrame[] candidatePathPoints, float candidatePathPivotOffset, bool isCandidatePathInverted, out float cost, out float arcLength, out float reinforcementOffset)
	{
		cost = float.MaxValue;
		arcLength = 0f;
		reinforcementOffset = 0f;
		int numberOfPoints = candidatePath.NumberOfPoints;
		Vec2 circleCenter = mainPathCenterPosition;
		Vec2 vec = mainPathBasePosition - circleCenter;
		float length = vec.Length;
		float mainBaseAngle = TaleWorlds.Library.MathF.Atan2(vec.y, vec.x);
		float totalLength = candidatePath.GetTotalLength();
		float b = (isCandidatePathInverted ? (totalLength * 0.5f) : 0f);
		float b2 = (isCandidatePathInverted ? totalLength : (totalLength * 0.5f));
		float num = 0f;
		float bestDistanceToCircle = float.MaxValue;
		float bestArcLength = float.MaxValue;
		float bestOffset = 0f;
		bool bestPointIsInsideCircle = false;
		bool foundCandidate = false;
		for (int i = 0; i < numberOfPoints - 1; i++)
		{
			Vec2 segmentStart = candidatePathPoints[i].origin.AsVec2;
			Vec2 segmentEnd = candidatePathPoints[i + 1].origin.AsVec2;
			float length2 = (segmentEnd - segmentStart).Length;
			if (length2 > 0.001f)
			{
				float num2 = num;
				float num3 = num + length2;
				float num4 = TaleWorlds.Library.MathF.Max(num2, b);
				float num5 = TaleWorlds.Library.MathF.Min(num3, b2);
				if (num4 < num5)
				{
					float localStartT = (num4 - num2) / length2;
					float localEndT = (num5 - num2) / length2;
					EvaluateSegmentAgainstReferenceCircle(in segmentStart, in segmentEnd, length2, num2, localStartT, localEndT, in circleCenter, length, mainBaseAngle, ref bestDistanceToCircle, ref bestArcLength, ref bestOffset, ref bestPointIsInsideCircle, ref foundCandidate);
				}
				num = num3;
			}
		}
		float num6 = TaleWorlds.Library.MathF.Abs(bestArcLength);
		if (foundCandidate && bestDistanceToCircle <= 50f && num6 >= 40f && num6 <= 200f)
		{
			arcLength = bestArcLength;
			cost = bestDistanceToCircle + num6 * 0.25f;
			if (bestPointIsInsideCircle)
			{
				cost += 5f;
			}
			reinforcementOffset = (isCandidatePathInverted ? (candidatePathPivotOffset - bestOffset) : (bestOffset - candidatePathPivotOffset));
			return true;
		}
		return false;
	}

	private void EvaluateSegmentAgainstReferenceCircle(in Vec2 segmentStart, in Vec2 segmentEnd, float segmentLength, float segmentStartOffset, float localStartT, float localEndT, in Vec2 circleCenter, float circleRadius, float mainBaseAngle, ref float bestDistanceToCircle, ref float bestArcLength, ref float bestOffset, ref bool bestPointIsInsideCircle, ref bool foundCandidate)
	{
		Vec2 vec = segmentEnd - segmentStart;
		float num = Vec2.DotProduct(vec, vec);
		if (!(num > 0.001f))
		{
			return;
		}
		int num2 = 0;
		_tempCandidatePointOffsetsOnSegment[num2++] = localStartT;
		_tempCandidatePointOffsetsOnSegment[num2++] = localEndT;
		Vec2 vec2 = circleCenter - segmentStart;
		float num3 = Vec2.DotProduct(vec2, vec) / num;
		if (num3 >= localStartT && num3 <= localEndT)
		{
			_tempCandidatePointOffsetsOnSegment[num2++] = num3;
		}
		Vec2 vec3 = -vec2;
		float num4 = num;
		float num5 = 2f * Vec2.DotProduct(vec3, vec);
		float num6 = Vec2.DotProduct(vec3, vec3) - circleRadius * circleRadius;
		float num7 = num5 * num5 - 4f * num4 * num6;
		if (num7 >= 0f)
		{
			float num8 = TaleWorlds.Library.MathF.Sqrt(num7);
			float num9 = 1f / (2f * num4);
			float num10 = (0f - num5 - num8) * num9;
			float num11 = (0f - num5 + num8) * num9;
			if (num10 >= localStartT && num10 <= localEndT)
			{
				_tempCandidatePointOffsetsOnSegment[num2++] = num10;
			}
			if (num11 >= localStartT && num11 <= localEndT)
			{
				_tempCandidatePointOffsetsOnSegment[num2++] = num11;
			}
		}
		for (int i = 0; i < num2; i++)
		{
			float num12 = _tempCandidatePointOffsetsOnSegment[i];
			Vec2 vec4 = segmentStart + vec * num12 - circleCenter;
			float length = vec4.Length;
			if (!(length > 0.001f))
			{
				continue;
			}
			float num13 = TaleWorlds.Library.MathF.Abs(length - circleRadius);
			float toAngle = TaleWorlds.Library.MathF.Atan2(vec4.y, vec4.x);
			float num14 = MBMath.GetSmallestDifferenceBetweenTwoAngles(mainBaseAngle, toAngle) * circleRadius;
			float num15 = TaleWorlds.Library.MathF.Abs(num14);
			bool flag = length < circleRadius;
			bool flag2 = false;
			if (!foundCandidate)
			{
				flag2 = true;
			}
			else if (num13 < bestDistanceToCircle - 0.001f)
			{
				flag2 = true;
			}
			else if (TaleWorlds.Library.MathF.Abs(num13 - bestDistanceToCircle) <= 0.001f)
			{
				float num16 = TaleWorlds.Library.MathF.Abs(bestArcLength);
				if (num15 < num16 - 0.001f)
				{
					flag2 = true;
				}
				else if (TaleWorlds.Library.MathF.Abs(num15 - num16) <= 0.001f && bestPointIsInsideCircle && !flag)
				{
					flag2 = true;
				}
			}
			if (flag2)
			{
				foundCandidate = true;
				bestDistanceToCircle = num13;
				bestArcLength = num14;
				bestOffset = segmentStartOffset + num12 * segmentLength;
				bestPointIsInsideCircle = flag;
			}
		}
	}
}
