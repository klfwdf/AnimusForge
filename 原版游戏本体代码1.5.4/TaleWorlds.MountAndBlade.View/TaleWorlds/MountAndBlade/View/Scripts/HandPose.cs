using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.Scripts;

public class HandPose : ScriptComponentBehavior
{
	private MBGameManager _editorGameManager;

	private bool _initiliazed;

	private bool _isFinished;

	protected override void OnEditorInit()
	{
		base.OnEditorInit();
		if (Game.Current == null)
		{
			_editorGameManager = new EditorGameManager();
		}
	}

	protected override void OnEditorTick(float dt)
	{
		if (!_isFinished && _editorGameManager != null)
		{
			_isFinished = !_editorGameManager.DoLoadingForGameManager();
		}
		if (Game.Current != null && !_initiliazed)
		{
			AnimationSystemData animationSystemData = Game.Current.DefaultMonster.FillAnimationSystemData(MBActionSet.GetActionSet(Game.Current.DefaultMonster.ActionSetCode), 1f, hasClippingPlane: false);
			base.GameEntity.CreateSkeletonWithActionSet(ref animationSystemData);
			base.GameEntity.CopyComponentsToSkeleton();
			base.GameEntity.Skeleton.SetAgentActionChannel(0, in ActionIndexCache.act_tableau_hand_armor_pose);
			base.GameEntity.Skeleton.TickAnimationsAndForceUpdate(0.01f, base.GameEntity.GetGlobalFrame(), tickAnimsForChildren: true);
			base.GameEntity.Skeleton.Freeze(p: false);
			base.GameEntity.Skeleton.TickAnimationsAndForceUpdate(0.001f, base.GameEntity.GetGlobalFrame(), tickAnimsForChildren: false);
			base.GameEntity.Skeleton.SetAnimationParameterAtChannel(0, MBMath.ClampFloat(0f, 0f, 1f));
			base.GameEntity.Skeleton.SetUptoDate(value: false);
			base.GameEntity.Skeleton.Freeze(p: true);
			_initiliazed = true;
		}
		if (_initiliazed)
		{
			base.GameEntity.Skeleton.Freeze(p: false);
			base.GameEntity.Skeleton.TickAnimationsAndForceUpdate(0.001f, base.GameEntity.GetGlobalFrame(), tickAnimsForChildren: false);
			base.GameEntity.Skeleton.SetAnimationParameterAtChannel(0, MBMath.ClampFloat(0f, 0f, 1f));
			base.GameEntity.Skeleton.SetUptoDate(value: false);
			base.GameEntity.Skeleton.Freeze(p: true);
		}
	}
}
