using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public struct MBAnimation
{
	private readonly int _index;

	public MBAnimation(MBAnimation animation)
	{
		_index = animation._index;
	}

	internal MBAnimation(int i)
	{
		_index = i;
	}

	public bool Equals(MBAnimation a)
	{
		return _index == a._index;
	}

	public override int GetHashCode()
	{
		return _index;
	}

	public static int GetAnimationIndexWithName(string animationName)
	{
		if (string.IsNullOrEmpty(animationName))
		{
			return -1;
		}
		return MBAPI.IMBAnimation.GetIndexWithID(animationName);
	}

	public static Agent.ActionCodeType GetActionType(ActionIndexCache actionIndex)
	{
		if (!(actionIndex == ActionIndexCache.act_none))
		{
			return MBAPI.IMBAnimation.GetActionType(actionIndex.Index);
		}
		return Agent.ActionCodeType.Other;
	}

	public static void PrefetchAnimationClip(MBActionSet actionSet, ActionIndexCache actionIndexCache)
	{
		MBAPI.IMBAnimation.PrefetchAnimationClip(actionSet.Index, actionIndexCache.Index);
	}

	public static float GetAnimationDuration(string animationName)
	{
		int indexWithID = MBAPI.IMBAnimation.GetIndexWithID(animationName);
		return MBAPI.IMBAnimation.GetAnimationDuration(indexWithID);
	}

	public static float GetAnimationDuration(int animationIndex)
	{
		return MBAPI.IMBAnimation.GetAnimationDuration(animationIndex);
	}

	public static float GetAnimationParameter1(string animationName)
	{
		int indexWithID = MBAPI.IMBAnimation.GetIndexWithID(animationName);
		return MBAPI.IMBAnimation.GetAnimationParameter1(indexWithID);
	}

	public static float GetAnimationParameter1(int animationIndex)
	{
		return MBAPI.IMBAnimation.GetAnimationParameter1(animationIndex);
	}

	public static float GetAnimationParameter2(string animationName)
	{
		int indexWithID = MBAPI.IMBAnimation.GetIndexWithID(animationName);
		return MBAPI.IMBAnimation.GetAnimationParameter2(indexWithID);
	}

	public static float GetAnimationParameter2(int animationIndex)
	{
		return MBAPI.IMBAnimation.GetAnimationParameter2(animationIndex);
	}

	public static float GetAnimationParameter3(string animationName)
	{
		int indexWithID = MBAPI.IMBAnimation.GetIndexWithID(animationName);
		return MBAPI.IMBAnimation.GetAnimationParameter3(indexWithID);
	}

	public static float GetAnimationBlendInPeriod(string animationName)
	{
		int indexWithID = MBAPI.IMBAnimation.GetIndexWithID(animationName);
		return MBAPI.IMBAnimation.GetAnimationBlendInPeriod(indexWithID);
	}

	public static float GetAnimationBlendInPeriod(int animationIndex)
	{
		return MBAPI.IMBAnimation.GetAnimationBlendInPeriod(animationIndex);
	}

	public static ActionIndexCache GetAnimationBlendsWithActionIndex(string animationName)
	{
		int indexWithID = MBAPI.IMBAnimation.GetIndexWithID(animationName);
		return new ActionIndexCache(MBAPI.IMBAnimation.GetAnimationBlendsWithActionIndex(indexWithID));
	}

	public static ActionIndexCache GetAnimationBlendsWithActionIndex(int animationIndex)
	{
		return new ActionIndexCache(MBAPI.IMBAnimation.GetAnimationBlendsWithActionIndex(animationIndex));
	}

	public static Vec3 GetAnimationDisplacementAtProgress(string animationName, float progress)
	{
		int indexWithID = MBAPI.IMBAnimation.GetIndexWithID(animationName);
		return MBAPI.IMBAnimation.GetAnimationDisplacementAtProgress(indexWithID, progress);
	}

	public static Vec3 GetAnimationDisplacementAtProgress(int animationIndex, float progress)
	{
		return MBAPI.IMBAnimation.GetAnimationDisplacementAtProgress(animationIndex, progress);
	}

	public static int GetActionCodeWithName(string name)
	{
		if (!string.IsNullOrEmpty(name))
		{
			return MBAPI.IMBAnimation.GetActionCodeWithName(name);
		}
		return ActionIndexCache.act_none.Index;
	}

	public static int GetNumActionCodes()
	{
		return MBAPI.IMBAnimation.GetNumActionCodes();
	}

	public static int GetNumAnimations()
	{
		return MBAPI.IMBAnimation.GetNumAnimations();
	}

	public static bool IsAnyAnimationLoadingFromDisk()
	{
		return MBAPI.IMBAnimation.IsAnyAnimationLoadingFromDisk();
	}
}
