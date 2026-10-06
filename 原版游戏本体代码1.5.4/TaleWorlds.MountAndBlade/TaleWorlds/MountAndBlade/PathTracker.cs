using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class PathTracker
{
	private readonly Path _path;

	private readonly Vec3 _initialScale;

	private int _version = -1;

	public float TotalDistanceTraveled { get; set; }

	public bool HasChanged
	{
		get
		{
			if (_path != null)
			{
				return _version < _path.GetVersion();
			}
			return false;
		}
	}

	public bool IsValid => _path != null;

	public bool HasReachedEnd => TotalDistanceTraveled >= _path.TotalDistance;

	public float PathTraveledPercentage => TotalDistanceTraveled / _path.TotalDistance;

	public MatrixFrame CurrentFrame
	{
		get
		{
			MatrixFrame frameForDistance = _path.GetFrameForDistance(TotalDistanceTraveled);
			frameForDistance.rotation.RotateAboutUp(System.MathF.PI);
			frameForDistance.rotation.ApplyScaleLocal(in _initialScale);
			return frameForDistance;
		}
	}

	public PathTracker(Path path, Vec3 initialScaleOfEntity)
	{
		_path = path;
		_initialScale = initialScaleOfEntity;
		if (path != null)
		{
			UpdateVersion();
		}
		Reset();
	}

	public void UpdateVersion()
	{
		_version = _path.GetVersion();
	}

	public bool PathExists()
	{
		return _path != null;
	}

	public void Advance(float deltaDistance)
	{
		TotalDistanceTraveled += deltaDistance;
		TotalDistanceTraveled = TaleWorlds.Library.MathF.Min(TotalDistanceTraveled, _path.TotalDistance);
	}

	public float GetPathLength()
	{
		return _path.TotalDistance;
	}

	public void CurrentFrameAndColor(out MatrixFrame frame, out Vec3 color)
	{
		_path.GetFrameAndColorForDistance(TotalDistanceTraveled, out frame, out color);
		frame.rotation.RotateAboutUp(System.MathF.PI);
		frame.rotation.ApplyScaleLocal(in _initialScale);
	}

	public void Reset()
	{
		TotalDistanceTraveled = 0f;
	}
}
