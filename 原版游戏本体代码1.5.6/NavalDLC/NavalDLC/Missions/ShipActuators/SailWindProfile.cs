using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NavalDLC.Missions.ShipActuators;

public class SailWindProfile
{
	private const int BinCount = 36;

	private const float BinAngleInDegrees = 10f;

	private static SailWindProfile _instance;

	private (float dragCoef, float liftCoef)[][] _sailWindProfiles;

	public static SailWindProfile Instance => _instance;

	public static bool IsSailWindProfileInitialized => _instance != null;

	public static void InitializeProfile()
	{
		_instance = new SailWindProfile();
	}

	public static void InitializeProfileForEditor()
	{
		if (_instance == null)
		{
			_instance = new SailWindProfile();
		}
	}

	public static void FinalizeProfile()
	{
		_instance.Destroy();
		_instance = null;
	}

	private void FillSailProfiles()
	{
		(float, float)[] array = GenerateSquareSailWindProfile();
		_sailWindProfiles[0] = array;
		(float, float)[] array2 = GenerateLateenSailWindProfile();
		_sailWindProfiles[1] = array2;
	}

	private SailWindProfile()
	{
		_sailWindProfiles = new(float, float)[2][];
		FillSailProfiles();
	}

	private void Destroy()
	{
		for (int i = 0; i < 2; i++)
		{
			_sailWindProfiles[i] = null;
		}
		_sailWindProfiles = null;
	}

	public float ComputeSailThrustValue(SailType sailType, Vec2 sailDir, Vec2 desiredThrustDir, Vec2 windDir)
	{
		return Vec2.DotProduct(GetSailForceCoefficients(sailType, sailDir, windDir), desiredThrustDir);
	}

	public Vec2 GetMaximumSailForceCoefficients(SailType sailType)
	{
		float num = -System.MathF.PI;
		float num2 = -System.MathF.PI;
		Vec2 result = new Vec2(0f, 0f);
		float num3 = 0.17453292f;
		for (int i = 0; i < 36; i++)
		{
			Vec2 sailDir = new Vec2(TaleWorlds.Library.MathF.Cos(num), TaleWorlds.Library.MathF.Sin(num));
			for (int j = 0; j < 36; j++)
			{
				Vec2 windDir = new Vec2(TaleWorlds.Library.MathF.Cos(num2), TaleWorlds.Library.MathF.Sin(num2));
				float angleOfAttack = GetAngleOfAttack(in sailDir, in windDir);
				(float, float) sailCoefs = GetSailCoefs(angleOfAttack, sailType);
				Vec2 vec = windDir.LeftVec();
				Vec2 vec2 = windDir * sailCoefs.Item1 + vec * sailCoefs.Item2;
				if (vec2.LengthSquared >= result.LengthSquared)
				{
					result = vec2;
				}
				num2 += num3;
			}
			num += num3;
		}
		return result;
	}

	public Vec2 GetSailForceCoefficients(SailType sailType, Vec2 sailDir, Vec2 windDir)
	{
		float angleOfAttack = GetAngleOfAttack(in sailDir, in windDir);
		(float, float) sailCoefs = GetSailCoefs(angleOfAttack, sailType);
		Vec2 vec = windDir.LeftVec();
		return windDir * sailCoefs.Item1 + vec * sailCoefs.Item2;
	}

	public (float dragCoef, float liftCoef) GetSailCoefs(float angleOfAttackInRadians, SailType sailType)
	{
		float num = ((angleOfAttackInRadians < 0f) ? (angleOfAttackInRadians + System.MathF.PI * 2f) : angleOfAttackInRadians) * 57.29578f;
		int num2 = (int)(num / 10f) % 36;
		int num3 = (num2 + 1) % 36;
		(float, float)[] array = _sailWindProfiles[(int)sailType];
		float num4 = num % 10f / 10f;
		float item = (1f - num4) * array[num2].Item1 + num4 * array[num3].Item1;
		float item2 = (1f - num4) * array[num2].Item2 + num4 * array[num3].Item2;
		return (dragCoef: item, liftCoef: item2);
	}

	private (float dragCoef, float liftCoef)[] GenerateLateenSailWindProfile()
	{
		return new(float, float)[36]
		{
			(0.02f, 0f),
			(0.06f, 0.08f),
			(0.08f, 0.12f),
			(0.12f, 0.1f),
			(0.13f, 0.08f),
			(0.17f, 0.06f),
			(0.28f, 0.04f),
			(0.41f, 0.03f),
			(0.46f, 0.02f),
			(0.6f, 0f),
			(0.46f, -0.02f),
			(0.41f, -0.03f),
			(0.28f, -0.04f),
			(0.17f, -0.06f),
			(0.13f, -0.08f),
			(0.12f, -0.1f),
			(0.08f, -0.12f),
			(0.06f, -0.08f),
			(0.02f, 0f),
			(0.06f, 0.12f),
			(0.08f, 0.38f),
			(0.14f, 0.36f),
			(0.26f, 0.24f),
			(0.34f, 0.16f),
			(0.56f, 0.12f),
			(0.82f, 0.09f),
			(0.92f, 0.03f),
			(1f, 0f),
			(0.92f, -0.03f),
			(0.82f, -0.09f),
			(0.56f, -0.12f),
			(0.34f, -0.16f),
			(0.26f, -0.24f),
			(0.14f, -0.36f),
			(0.08f, -0.38f),
			(0.06f, -0.12f)
		};
	}

	private (float dragCoef, float liftCoef)[] GenerateSquareSailWindProfile()
	{
		return new(float, float)[36]
		{
			(1f, 0f),
			(0.94f, -0.03f),
			(0.86f, -0.09f),
			(0.72f, -0.12f),
			(0.52f, -0.16f),
			(0.36f, -0.24f),
			(0.32f, -0.36f),
			(0.18f, -0.38f),
			(0.06f, -0.12f),
			(0.04f, -0f),
			(0.06f, 0.03f),
			(0.18f, 0.07f),
			(0.32f, 0.1f),
			(0.36f, 0.13f),
			(0.52f, 0.13f),
			(0.72f, 0.1f),
			(0.86f, 0.07f),
			(0.94f, 0.03f),
			(1f, 0f),
			(0.94f, -0.03f),
			(0.86f, -0.07f),
			(0.72f, -0.1f),
			(0.52f, -0.13f),
			(0.36f, -0.13f),
			(0.32f, -0.1f),
			(0.18f, -0.07f),
			(0.06f, -0.03f),
			(0.04f, 0f),
			(0.06f, 0.12f),
			(0.18f, 0.38f),
			(0.32f, 0.36f),
			(0.36f, 0.24f),
			(0.52f, 0.16f),
			(0.72f, 0.12f),
			(0.86f, 0.09f),
			(0.94f, 0.03f)
		};
	}

	public static float GetAngleOfAttack(in Vec2 sailDir, in Vec2 windDir)
	{
		Vec3 vec = Vec3.CrossProduct(sailDir.ToVec3(), windDir.ToVec3());
		return TaleWorlds.Library.MathF.Atan2(x: Vec2.DotProduct(sailDir, windDir), y: vec.z);
	}

	public static float NormalizeThrustValue(float thrustValue, float minThrustValue, float maxThrustValue)
	{
		if (maxThrustValue == minThrustValue)
		{
			return 0f;
		}
		return (thrustValue - minThrustValue) / (maxThrustValue - minThrustValue);
	}
}
