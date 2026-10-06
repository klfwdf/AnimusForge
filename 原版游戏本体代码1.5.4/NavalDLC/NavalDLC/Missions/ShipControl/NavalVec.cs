using System;
using TaleWorlds.Library;

namespace NavalDLC.Missions.ShipControl;

public struct NavalVec
{
	private Vec2 _deltaPosition;

	private float _deltaOrientation;

	private float _deltaSpeed;

	public Vec2 DeltaPosition => _deltaPosition;

	public float DeltaOrientation => _deltaOrientation;

	public float DeltaSpeed => _deltaSpeed;

	public static NavalVec Zero => new NavalVec(in Vec2.Zero, 0f);

	public NavalVec(in Vec2 deltaPosition, float deltaRotation, float deltaSpeed = 0f)
	{
		_deltaPosition = deltaPosition;
		_deltaOrientation = deltaRotation;
		_deltaSpeed = deltaSpeed;
	}

	public NavalVec(in Vec2 deltaPosition)
	{
		_deltaPosition = deltaPosition;
		_deltaOrientation = 0f;
		_deltaSpeed = 0f;
	}

	public void ClampAngle()
	{
		_deltaOrientation = TaleWorlds.Library.MathF.Clamp(_deltaOrientation, -System.MathF.PI, System.MathF.PI);
	}

	public static NavalVec operator +(in NavalVec vec1, in NavalVec vec2)
	{
		return new NavalVec(vec1.DeltaPosition + vec2.DeltaPosition, vec1.DeltaOrientation + vec2.DeltaOrientation, vec1.DeltaSpeed + vec2.DeltaSpeed);
	}

	public static NavalVec operator -(in NavalVec vec1, in NavalVec vec2)
	{
		return new NavalVec(vec1.DeltaPosition - vec2.DeltaPosition, vec1.DeltaOrientation - vec2.DeltaOrientation, vec1.DeltaSpeed - vec2.DeltaSpeed);
	}

	public static NavalVec operator *(in NavalVec vector, float scalar)
	{
		return new NavalVec(vector.DeltaPosition * scalar, vector.DeltaOrientation * scalar, vector.DeltaSpeed * scalar);
	}

	public static NavalVec operator *(float scalar, in NavalVec vector)
	{
		return new NavalVec(scalar * vector.DeltaPosition, scalar * vector.DeltaOrientation, scalar * vector.DeltaSpeed);
	}

	public static NavalVec operator *(in Vec3 vector, in NavalVec nVector)
	{
		return new NavalVec(vector.x * nVector.DeltaPosition, vector.y * nVector.DeltaOrientation, vector.z * nVector.DeltaSpeed);
	}

	public static NavalVec operator *(in NavalVec nVector, in Vec3 vector)
	{
		return new NavalVec(nVector.DeltaPosition * vector.x, nVector.DeltaOrientation * vector.y, nVector.DeltaSpeed * vector.z);
	}

	public static NavalVec operator /(in NavalVec vector, float scalar)
	{
		return new NavalVec(vector.DeltaPosition / scalar, vector.DeltaOrientation / scalar, vector.DeltaSpeed / scalar);
	}
}
