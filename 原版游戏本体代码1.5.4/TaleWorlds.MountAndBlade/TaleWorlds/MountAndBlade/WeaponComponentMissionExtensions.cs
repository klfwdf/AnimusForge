using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public static class WeaponComponentMissionExtensions
{
	public static int GetItemUsageIndex(this WeaponComponentData weaponComponentData)
	{
		return MBItem.GetItemUsageIndex(weaponComponentData.ItemUsage);
	}

	public static Vec3 GetWeaponCenterOfMass(this PhysicsShape body)
	{
		return CalculateCenterOfMass(body);
	}

	[MBCallback(null, false)]
	internal static Vec3 CalculateCenterOfMass(PhysicsShape body)
	{
		if (body == null)
		{
			Debug.FailedAssert("Item has no body! Check this!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\ItemCollectionElementMissionExtensions.cs", "CalculateCenterOfMass", 46);
			return Vec3.Zero;
		}
		Vec3 result = Vec3.Zero;
		float num = 0f;
		int num2 = body.CapsuleCount();
		for (int i = 0; i < num2; i++)
		{
			CapsuleData data = default(CapsuleData);
			body.GetCapsule(ref data, i);
			Vec3 vec = (data.P1 + data.P2) * 0.5f;
			float num3 = data.P1.Distance(data.P2);
			float num4 = data.Radius * data.Radius * System.MathF.PI * (1.3333334f * data.Radius + num3);
			num += num4;
			result += vec * num4;
		}
		int num5 = body.SphereCount();
		for (int j = 0; j < num5; j++)
		{
			SphereData data2 = default(SphereData);
			body.GetSphere(ref data2, j);
			float num6 = 4.1887903f * data2.Radius * data2.Radius * data2.Radius;
			num += num6;
			result += data2.Origin * num6;
		}
		if (num > 0f)
		{
			result /= num;
			if (TaleWorlds.Library.MathF.Abs(result.x) < 0.01f)
			{
				result.x = 0f;
			}
			if (TaleWorlds.Library.MathF.Abs(result.y) < 0.01f)
			{
				result.y = 0f;
			}
			if (TaleWorlds.Library.MathF.Abs(result.z) < 0.01f)
			{
				result.z = 0f;
			}
		}
		else
		{
			result = body.GetBoundingBoxCenter();
		}
		return result;
	}
}
