using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View;

public class PopupSceneSkeletonAnimationScript : ScriptComponentBehavior
{
	public string SkeletonName = "";

	public int BoneIndex;

	public Vec3 AttachmentOffset = new Vec3(0f, 0f, 0f, -1f);

	public string InitialAnimationClip = "";

	public string PositiveAnimationClip = "";

	public string NegativeAnimationClip = "";

	public string InitialAnimationContinueClip = "";

	public string PositiveAnimationContinueClip = "";

	public string NegativeAnimationContinueClip = "";

	private int _currentState;

	protected override void OnInit()
	{
		base.OnInit();
		SetScriptComponentToTick(GetTickRequirement());
	}

	public void Initialize()
	{
		if (SkeletonName != "" && (base.GameEntity.Skeleton == null || base.GameEntity.Skeleton.GetName() != SkeletonName))
		{
			base.GameEntity.CreateSimpleSkeleton(SkeletonName);
		}
		else if (SkeletonName == "" && base.GameEntity.Skeleton != null)
		{
			base.GameEntity.RemoveSkeleton();
		}
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick;
	}

	protected override void OnTick(float dt)
	{
		if (base.GameEntity.Skeleton.GetAnimationAtChannel(0) == InitialAnimationClip && base.GameEntity.Skeleton.GetAnimationParameterAtChannel(0) >= 1f)
		{
			base.GameEntity.Skeleton.SetAnimationAtChannel(InitialAnimationContinueClip, 0);
		}
		if (base.GameEntity.Skeleton.GetAnimationAtChannel(0) == PositiveAnimationClip && base.GameEntity.Skeleton.GetAnimationParameterAtChannel(0) >= 1f)
		{
			base.GameEntity.Skeleton.SetAnimationAtChannel(PositiveAnimationContinueClip, 0);
		}
		if (base.GameEntity.Skeleton.GetAnimationAtChannel(0) == NegativeAnimationClip && base.GameEntity.Skeleton.GetAnimationParameterAtChannel(0) >= 1f)
		{
			base.GameEntity.Skeleton.SetAnimationAtChannel(NegativeAnimationContinueClip, 0);
		}
	}

	public void SetState(int state)
	{
		string text = state switch
		{
			0 => InitialAnimationClip, 
			1 => PositiveAnimationClip, 
			_ => NegativeAnimationClip, 
		};
		if (base.GameEntity.IsValid && base.GameEntity.Skeleton != null && !string.IsNullOrEmpty(text))
		{
			base.GameEntity.Skeleton.SetAnimationAtChannel(text, 0);
		}
		_currentState = state;
	}
}
