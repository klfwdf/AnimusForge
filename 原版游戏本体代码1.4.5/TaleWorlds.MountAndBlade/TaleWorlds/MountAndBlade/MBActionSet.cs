using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[EngineStruct("int", false, null)]
public struct MBActionSet
{
	[CustomEngineStructMemberData("ignoredMember", true)]
	internal readonly int Index;

	public static readonly MBActionSet InvalidActionSet = new MBActionSet(-1);

	public bool IsValid => Index >= 0;

	internal MBActionSet(int i)
	{
		Index = i;
	}

	public bool Equals(MBActionSet a)
	{
		return Index == a.Index;
	}

	public bool Equals(int index)
	{
		return Index == index;
	}

	public override int GetHashCode()
	{
		return Index;
	}

	public string GetName()
	{
		if (!IsValid)
		{
			return "Invalid";
		}
		return MBAPI.IMBActionSet.GetNameWithIndex(Index);
	}

	public string GetSkeletonName()
	{
		return MBAPI.IMBActionSet.GetSkeletonName(Index);
	}

	public string GetAnimationName(in ActionIndexCache actionCode)
	{
		return MBAPI.IMBActionSet.GetAnimationName(Index, actionCode.Index);
	}

	public bool AreActionsAlternatives(in ActionIndexCache actionCode1, in ActionIndexCache actionCode2)
	{
		return MBAPI.IMBActionSet.AreActionsAlternatives(Index, actionCode1.Index, actionCode2.Index);
	}

	public static int GetNumberOfActionSets()
	{
		return MBAPI.IMBActionSet.GetNumberOfActionSets();
	}

	public static int GetNumberOfMonsterUsageSets()
	{
		return MBAPI.IMBActionSet.GetNumberOfMonsterUsageSets();
	}

	public static MBActionSet GetActionSet(string objectID)
	{
		return GetActionSetWithIndex(MBAPI.IMBActionSet.GetIndexWithID(objectID));
	}

	public static MBActionSet GetActionSetWithIndex(int index)
	{
		return new MBActionSet(index);
	}

	public static sbyte GetBoneIndexWithId(string actionSetId, string boneId)
	{
		return MBAPI.IMBActionSet.GetBoneIndexWithId(actionSetId, boneId);
	}

	public static bool GetBoneHasParentBone(string actionSetId, sbyte boneIndex)
	{
		return MBAPI.IMBActionSet.GetBoneHasParentBone(actionSetId, boneIndex);
	}

	public static Vec3 GetActionDisplacementVector(MBActionSet actionSet, in ActionIndexCache actionIndexCache)
	{
		return MBAPI.IMBAnimation.GetDisplacementVector(actionSet.Index, actionIndexCache.Index);
	}

	public static AnimFlags GetActionAnimationFlags(MBActionSet actionSet, in ActionIndexCache actionIndexCache)
	{
		return MBAPI.IMBAnimation.GetAnimationFlags(actionSet.Index, actionIndexCache.Index);
	}

	public static bool CheckActionAnimationClipExists(MBActionSet actionSet, in ActionIndexCache actionIndexCache)
	{
		return MBAPI.IMBAnimation.CheckAnimationClipExists(actionSet.Index, actionIndexCache.Index);
	}

	public static int GetAnimationIndexOfAction(MBActionSet actionSet, in ActionIndexCache actionIndexCache)
	{
		return MBAPI.IMBAnimation.AnimationIndexOfActionCode(actionSet.Index, actionIndexCache.Index);
	}

	public static string GetActionAnimationName(MBActionSet actionSet, in ActionIndexCache actionIndexCache)
	{
		return MBAPI.IMBAnimation.GetAnimationName(actionSet.Index, actionIndexCache.Index);
	}

	public static float GetActionAnimationDuration(MBActionSet actionSet, in ActionIndexCache actionIndexCache)
	{
		return MBAPI.IMBAnimation.GetActionAnimationDuration(actionSet.Index, actionIndexCache.Index);
	}

	public static ActionIndexCache GetActionAnimationContinueToAction(MBActionSet actionSet, in ActionIndexCache actionIndexCache)
	{
		return new ActionIndexCache(MBAPI.IMBAnimation.GetAnimationContinueToAction(actionSet.Index, actionIndexCache.Index));
	}

	public static float GetTotalAnimationDurationWithContinueToAction(MBActionSet actionSet, ActionIndexCache actionIndexCache)
	{
		float num = 0f;
		while (actionIndexCache != ActionIndexCache.act_none)
		{
			num += GetActionAnimationDuration(actionSet, in actionIndexCache);
			actionIndexCache = GetActionAnimationContinueToAction(actionSet, in actionIndexCache);
		}
		return num;
	}

	public static float GetActionBlendOutStartProgress(MBActionSet actionSet, in ActionIndexCache actionIndexCache)
	{
		return MBAPI.IMBAnimation.GetActionBlendOutStartProgress(actionSet.Index, actionIndexCache.Index);
	}
}
