using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Missions.Objectives;

internal class GenericMissionObjectiveTarget<T> : MissionObjectiveTarget<T>
{
	internal Func<T, bool> IsActiveCallback;

	internal Func<T, TextObject> GetNameCallback;

	internal Func<T, Vec3> GetGlobalPositionCallback;

	internal TextObject Name;

	internal Vec3 StaticPosition;

	public GenericMissionObjectiveTarget(T target)
		: base(target)
	{
	}

	public override bool IsActive()
	{
		if (IsActiveCallback != null)
		{
			return IsActiveCallback(base.Target);
		}
		return true;
	}

	public override TextObject GetName()
	{
		if (GetNameCallback != null)
		{
			return GetNameCallback(base.Target);
		}
		if (Name != null)
		{
			return Name;
		}
		return TextObject.GetEmpty();
	}

	public override Vec3 GetGlobalPosition()
	{
		if (GetGlobalPositionCallback != null)
		{
			return GetGlobalPositionCallback(base.Target);
		}
		if (StaticPosition.IsValid)
		{
			return StaticPosition;
		}
		Debug.FailedAssert("Static target position or position getter callback was not set for generic mission objective target.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Objectives\\MissionObjectiveTarget.cs", "GetGlobalPosition", 74);
		return Vec3.Zero;
	}
}
