using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public static class MBGlobals
{
	public const float Gravity = 9.806f;

	public static readonly Vec3 GravitationalAcceleration = new Vec3(0f, 0f, -9.806f);

	private static bool _initialized;

	private static Dictionary<string, MBActionSet> _actionSets;

	public static void InitializeReferences()
	{
		if (!_initialized)
		{
			_actionSets = new Dictionary<string, MBActionSet>();
			_initialized = true;
		}
	}

	public static MBActionSet GetActionSetWithSuffix(Monster monster, bool isFemale, string suffix)
	{
		return GetActionSet(ActionSetCode.GenerateActionSetNameWithSuffix(monster, isFemale, suffix));
	}

	public static MBActionSet GetActionSet(string actionSetCode)
	{
		if (!_actionSets.TryGetValue(actionSetCode, out var value))
		{
			value = MBActionSet.GetActionSet(actionSetCode);
			if (!value.IsValid)
			{
				Debug.FailedAssert("No action set found with action set code: " + actionSetCode, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Base\\MBGlobals.cs", "GetActionSet", 40);
				throw new Exception("Invalid action set code");
			}
			_actionSets[actionSetCode] = value;
		}
		return value;
	}

	public static string GetMemberName<T>(Expression<Func<T>> memberExpression)
	{
		return ((MemberExpression)memberExpression.Body).Member.Name;
	}

	public static string GetMethodName<T>(Expression<Func<T>> memberExpression)
	{
		return ((MethodCallExpression)memberExpression.Body).Method.Name;
	}
}
