using System;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class MBAgentRendererSceneController
{
	private UIntPtr _pointer;

	internal MBAgentRendererSceneController(UIntPtr pointer)
	{
		_pointer = pointer;
	}

	public void SetEnforcedVisibilityForAllAgents(Scene scene)
	{
		MBAPI.IMBAgentVisuals.SetEnforcedVisibilityForAllAgents(scene.Pointer, _pointer);
	}

	public static MBAgentRendererSceneController CreateNewAgentRendererSceneController(Scene scene)
	{
		return new MBAgentRendererSceneController(MBAPI.IMBAgentVisuals.CreateAgentRendererSceneController(scene.Pointer));
	}

	public void SetDoTimerBasedForcedSkeletonUpdates(bool value)
	{
		MBAPI.IMBAgentVisuals.SetDoTimerBasedForcedSkeletonUpdates(_pointer, value);
	}

	public static void DestructAgentRendererSceneController(Scene scene, MBAgentRendererSceneController rendererSceneController, bool deleteThisFrame)
	{
		MBAPI.IMBAgentVisuals.DestructAgentRendererSceneController(scene.Pointer, rendererSceneController._pointer, deleteThisFrame);
		rendererSceneController._pointer = UIntPtr.Zero;
	}

	public static void ValidateAgentVisualsReseted(Scene scene, MBAgentRendererSceneController rendererSceneController)
	{
		MBAPI.IMBAgentVisuals.ValidateAgentVisualsReseted(scene.Pointer, rendererSceneController._pointer);
	}
}
