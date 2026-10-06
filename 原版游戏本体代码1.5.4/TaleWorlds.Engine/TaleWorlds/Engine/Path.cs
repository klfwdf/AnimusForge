using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[EngineClass("rglPath")]
public sealed class Path : NativeObject
{
	public int NumberOfPoints => EngineApplicationInterface.IPath.GetNumberOfPoints(base.Pointer);

	public float TotalDistance => EngineApplicationInterface.IPath.GetTotalLength(base.Pointer);

	internal Path(UIntPtr pointer)
	{
		Construct(pointer);
	}

	public MatrixFrame GetHermiteFrameForDt(float phase, int first_point)
	{
		MatrixFrame frame = MatrixFrame.Identity;
		EngineApplicationInterface.IPath.GetHermiteFrameForDt(base.Pointer, ref frame, phase, first_point);
		return frame;
	}

	public MatrixFrame GetFrameForDistance(float distance)
	{
		MatrixFrame frame = MatrixFrame.Identity;
		EngineApplicationInterface.IPath.GetHermiteFrameForDistance(base.Pointer, ref frame, distance);
		return frame;
	}

	public MatrixFrame GetNearestFrameWithValidAlphaForDistance(float distance, bool searchForward = true, float alphaThreshold = 0.5f)
	{
		MatrixFrame frame = MatrixFrame.Identity;
		EngineApplicationInterface.IPath.GetNearestHermiteFrameWithValidAlphaForDistance(base.Pointer, ref frame, distance, searchForward, alphaThreshold);
		return frame;
	}

	public void GetFrameAndColorForDistance(float distance, out MatrixFrame frame, out Vec3 color)
	{
		frame = MatrixFrame.Identity;
		EngineApplicationInterface.IPath.GetHermiteFrameAndColorForDistance(base.Pointer, out frame, out color, distance);
	}

	public float GetArcLength(int first_point)
	{
		return EngineApplicationInterface.IPath.GetArcLength(base.Pointer, first_point);
	}

	public void GetPoints(MatrixFrame[] points)
	{
		EngineApplicationInterface.IPath.GetPoints(base.Pointer, points);
	}

	public float GetTotalLength()
	{
		return EngineApplicationInterface.IPath.GetTotalLength(base.Pointer);
	}

	public int GetVersion()
	{
		return EngineApplicationInterface.IPath.GetVersion(base.Pointer);
	}

	public void SetFrameOfPoint(int pointIndex, ref MatrixFrame frame)
	{
		EngineApplicationInterface.IPath.SetFrameOfPoint(base.Pointer, pointIndex, ref frame);
	}

	public void SetTangentPositionOfPoint(int pointIndex, int tangentIndex, ref Vec3 position)
	{
		EngineApplicationInterface.IPath.SetTangentPositionOfPoint(base.Pointer, pointIndex, tangentIndex, ref position);
	}

	public int AddPathPoint(int newNodeIndex)
	{
		return EngineApplicationInterface.IPath.AddPathPoint(base.Pointer, newNodeIndex);
	}

	public void DeletePathPoint(int nodeIndex)
	{
		EngineApplicationInterface.IPath.DeletePathPoint(base.Pointer, nodeIndex);
	}

	public bool HasValidAlphaAtPathPoint(int nodeIndex, float alphaThreshold = 0.5f)
	{
		return EngineApplicationInterface.IPath.HasValidAlphaAtPathPoint(base.Pointer, nodeIndex, alphaThreshold);
	}

	public string GetName()
	{
		return EngineApplicationInterface.IPath.GetName(base.Pointer);
	}
}
