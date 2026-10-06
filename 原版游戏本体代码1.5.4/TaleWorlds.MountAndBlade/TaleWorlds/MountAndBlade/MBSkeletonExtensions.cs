using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public static class MBSkeletonExtensions
{
	public static Skeleton CreateWithActionSet(ref AnimationSystemData animationSystemData)
	{
		return MBAPI.IMBSkeletonExtensions.CreateWithActionSet(ref animationSystemData);
	}

	public static float GetSkeletonFaceAnimationTime(Skeleton skeleton)
	{
		return MBAPI.IMBSkeletonExtensions.GetSkeletonFaceAnimationTime(skeleton.Pointer);
	}

	public static void SetSkeletonFaceAnimationTime(Skeleton skeleton, float time)
	{
		MBAPI.IMBSkeletonExtensions.SetSkeletonFaceAnimationTime(skeleton.Pointer, time);
	}

	public static string GetSkeletonFaceAnimationName(Skeleton skeleton)
	{
		return MBAPI.IMBSkeletonExtensions.GetSkeletonFaceAnimationName(skeleton.Pointer);
	}

	public static MatrixFrame GetBoneEntitialFrameAtAnimationProgress(this Skeleton skeleton, sbyte boneIndex, int animationIndex, float progress)
	{
		MatrixFrame outFrame = default(MatrixFrame);
		MBAPI.IMBSkeletonExtensions.GetBoneEntitialFrameAtAnimationProgress(skeleton.Pointer, boneIndex, animationIndex, progress, ref outFrame);
		return outFrame;
	}

	public static MatrixFrame GetBoneEntitialFrame(this Skeleton skeleton, sbyte boneNumber, bool forceToUpdate = false)
	{
		MatrixFrame outFrame = default(MatrixFrame);
		MBAPI.IMBSkeletonExtensions.GetBoneEntitialFrame(skeleton.Pointer, boneNumber, useBoneMapping: false, forceToUpdate, ref outFrame);
		return outFrame;
	}

	public static void SetFacialAnimation(this Skeleton skeleton, Agent.FacialAnimChannel channel, string faceAnimation, bool playSound, bool loop)
	{
		MBAPI.IMBSkeletonExtensions.SetFacialAnimationOfChannel(skeleton.Pointer, (int)channel, faceAnimation, playSound, loop);
	}

	public static void SetAgentActionChannel(this Skeleton skeleton, int actionChannelNo, in ActionIndexCache actionIndex, float channelParameter = 0f, float blendPeriodOverride = -0.2f, bool forceFaceMorphRestart = true, float blendWithNextActionFactor = 0f)
	{
		MBAPI.IMBSkeletonExtensions.SetAgentActionChannel(skeleton.Pointer, actionChannelNo, actionIndex.Index, channelParameter, blendPeriodOverride, forceFaceMorphRestart, blendWithNextActionFactor);
	}

	public static bool DoesActionContinueWithCurrentActionAtChannel(this Skeleton skeleton, int actionChannelNo, in ActionIndexCache actionIndex)
	{
		return MBAPI.IMBSkeletonExtensions.DoesActionContinueWithCurrentActionAtChannel(skeleton.Pointer, actionChannelNo, actionIndex.Index);
	}

	public static void TickActionChannels(this Skeleton skeleton)
	{
		MBAPI.IMBSkeletonExtensions.TickActionChannels(skeleton.Pointer);
	}

	public static void SetAnimationAtChannel(this Skeleton skeleton, string animationName, int channelNo, float animationSpeedMultiplier = 1f, float blendInPeriod = -1f, float startProgress = 0f)
	{
		int indexWithID = MBAPI.IMBAnimation.GetIndexWithID(animationName);
		skeleton.SetAnimationAtChannel(indexWithID, channelNo, animationSpeedMultiplier, blendInPeriod, startProgress);
	}

	public static void SetAnimationAtChannel(this Skeleton skeleton, int animationIndex, int channelNo, float animationSpeedMultiplier = 1f, float blendInPeriod = -1f, float startProgress = 0f)
	{
		MBAPI.IMBSkeletonExtensions.SetAnimationAtChannel(skeleton.Pointer, animationIndex, channelNo, animationSpeedMultiplier, blendInPeriod, startProgress);
	}

	public static ActionIndexCache GetActionAtChannel(this Skeleton skeleton, int channelNo)
	{
		return new ActionIndexCache(MBAPI.IMBSkeletonExtensions.GetActionAtChannel(skeleton.Pointer, channelNo));
	}
}
