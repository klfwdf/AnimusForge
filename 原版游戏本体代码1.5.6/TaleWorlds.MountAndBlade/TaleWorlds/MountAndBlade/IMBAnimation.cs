using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[ScriptingInterfaceBase]
internal interface IMBAnimation
{
	[EngineMethod("get_id_with_index", false, null, false)]
	string GetIDWithIndex(int index);

	[EngineMethod("get_index_with_id", false, null, false)]
	int GetIndexWithID(string id);

	[EngineMethod("get_displacement_vector", false, null, false)]
	Vec3 GetDisplacementVector(int actionSetNo, int actionIndex);

	[EngineMethod("check_animation_clip_exists", false, null, false)]
	bool CheckAnimationClipExists(int actionSetNo, int actionIndex);

	[EngineMethod("prefetch_animation_clip", false, null, false)]
	void PrefetchAnimationClip(int actionSetNo, int actionIndex);

	[EngineMethod("get_animation_index_of_action_code", false, null, true)]
	int AnimationIndexOfActionCode(int actionSetNo, int actionIndex);

	[EngineMethod("get_animation_flags", false, null, false)]
	AnimFlags GetAnimationFlags(int actionSetNo, int actionIndex);

	[EngineMethod("get_action_type", false, null, true)]
	Agent.ActionCodeType GetActionType(int actionIndex);

	[EngineMethod("get_animation_duration", false, null, false)]
	float GetAnimationDuration(int animationIndex);

	[EngineMethod("get_animation_parameter1", false, null, false)]
	float GetAnimationParameter1(int animationIndex);

	[EngineMethod("get_animation_parameter2", false, null, false)]
	float GetAnimationParameter2(int animationIndex);

	[EngineMethod("get_animation_parameter3", false, null, false)]
	float GetAnimationParameter3(int animationIndex);

	[EngineMethod("get_action_animation_duration", false, null, false)]
	float GetActionAnimationDuration(int actionSetNo, int actionIndex);

	[EngineMethod("get_animation_name", false, null, false)]
	string GetAnimationName(int actionSetNo, int actionIndex);

	[EngineMethod("get_animation_continue_to_action", false, null, false)]
	int GetAnimationContinueToAction(int actionSetNo, int actionIndex);

	[EngineMethod("get_animation_blend_in_period", false, null, false)]
	float GetAnimationBlendInPeriod(int animationIndex);

	[EngineMethod("get_action_blend_out_start_progress", false, null, false)]
	float GetActionBlendOutStartProgress(int actionSetNo, int actionIndex);

	[EngineMethod("get_animation_blends_with_action_index", false, null, false)]
	int GetAnimationBlendsWithActionIndex(int animationIndex);

	[EngineMethod("get_animation_displacement_at_progress", false, null, true)]
	Vec3 GetAnimationDisplacementAtProgress(int animationIndex, float progress);

	[EngineMethod("get_action_code_with_name", false, null, false)]
	int GetActionCodeWithName(string name);

	[EngineMethod("get_action_name_with_code", false, null, false)]
	string GetActionNameWithCode(int index);

	[EngineMethod("get_num_action_codes", false, null, false)]
	int GetNumActionCodes();

	[EngineMethod("get_num_animations", false, null, false)]
	int GetNumAnimations();

	[EngineMethod("is_any_animation_loading_from_disk", false, null, false)]
	bool IsAnyAnimationLoadingFromDisk();
}
