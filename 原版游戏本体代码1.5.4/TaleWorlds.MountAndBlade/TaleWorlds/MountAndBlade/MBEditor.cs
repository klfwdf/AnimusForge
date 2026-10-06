using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade;

public class MBEditor
{
	public static Scene _editorScene;

	private static MBAgentRendererSceneController _agentRendererSceneController;

	public static bool _isEditorMissionOn;

	public static bool IsEditModeOn => MBAPI.IMBEditor.IsEditMode();

	public static bool EditModeEnabled => MBAPI.IMBEditor.IsEditModeEnabled();

	[MBCallback(null, false)]
	internal static void SetEditorScene(Scene scene)
	{
		if (_editorScene != null)
		{
			if (_agentRendererSceneController != null)
			{
				MBAgentRendererSceneController.DestructAgentRendererSceneController(_editorScene, _agentRendererSceneController, deleteThisFrame: false);
			}
			_editorScene.ClearAll();
		}
		_editorScene = scene;
		_agentRendererSceneController = MBAgentRendererSceneController.CreateNewAgentRendererSceneController(_editorScene);
	}

	[MBCallback(null, false)]
	internal static void CloseEditorScene()
	{
		if (_agentRendererSceneController != null)
		{
			MBAgentRendererSceneController.DestructAgentRendererSceneController(_editorScene, _agentRendererSceneController, deleteThisFrame: false);
		}
		_agentRendererSceneController = null;
		_editorScene = null;
	}

	[MBCallback(null, false)]
	internal static void DestroyEditor(Scene scene)
	{
		MBAgentRendererSceneController.DestructAgentRendererSceneController(_editorScene, _agentRendererSceneController, deleteThisFrame: false);
		_editorScene.ClearAll();
		_editorScene = null;
		_agentRendererSceneController = null;
	}

	public static void UpdateSceneTree(bool doNextFrame)
	{
		MBAPI.IMBEditor.UpdateSceneTree(doNextFrame);
	}

	public static bool IsEntitySelected(GameEntity entity)
	{
		return MBAPI.IMBEditor.IsEntitySelected(entity.Pointer);
	}

	public static bool IsEntitySelected(WeakGameEntity entity)
	{
		return MBAPI.IMBEditor.IsEntitySelected(entity.Pointer);
	}

	public static void RenderEditorMesh(MetaMesh mesh, MatrixFrame frame)
	{
		MBAPI.IMBEditor.RenderEditorMesh(mesh.Pointer, ref frame);
	}

	public static void ApplyDeltaToEditorCamera(Vec3 delta)
	{
		MBAPI.IMBEditor.ApplyDeltaToEditorCamera(in delta);
	}

	public static void EnterEditMode(SceneView sceneView, MatrixFrame initialCameraFrame, float initialCameraElevation, float initialCameraBearing)
	{
		MBAPI.IMBEditor.EnterEditMode(sceneView.Pointer, ref initialCameraFrame, initialCameraElevation, initialCameraBearing);
	}

	public static void TickEditMode(float dt)
	{
		MBAPI.IMBEditor.TickEditMode(dt);
	}

	public static void LeaveEditMode()
	{
		MBAPI.IMBEditor.LeaveEditMode();
		MBAgentRendererSceneController.DestructAgentRendererSceneController(_editorScene, _agentRendererSceneController, deleteThisFrame: false);
		_agentRendererSceneController = null;
		_editorScene = null;
	}

	public static void EnterEditMissionMode(Mission mission)
	{
		MBAPI.IMBEditor.EnterEditMissionMode(mission.Pointer);
		_isEditorMissionOn = true;
	}

	public static void LeaveEditMissionMode()
	{
		MBAPI.IMBEditor.LeaveEditMissionMode();
		_isEditorMissionOn = false;
	}

	public static bool IsEditorMissionOn()
	{
		if (_isEditorMissionOn)
		{
			return IsEditModeOn;
		}
		return false;
	}

	public static void ActivateSceneEditorPresentation()
	{
		Monster.GetBoneIndexWithId = MBActionSet.GetBoneIndexWithId;
		Monster.GetBoneHasParentBone = MBActionSet.GetBoneHasParentBone;
		MBObjectManager.Init();
		MBObjectManager.Instance.RegisterType<Monster>("Monster", "Monsters", 2u);
		MBObjectManager.Instance.LoadXML("Monsters", skipXmlFilterForEditor: true);
		MBAPI.IMBEditor.ActivateSceneEditorPresentation();
	}

	public static void DeactivateSceneEditorPresentation()
	{
		MBAPI.IMBEditor.DeactivateSceneEditorPresentation();
		MBObjectManager.Instance.Destroy();
	}

	public static void TickSceneEditorPresentation(float dt)
	{
		MBAPI.IMBEditor.TickSceneEditorPresentation(dt);
		LoadingWindow.DisableGlobalLoadingWindow();
	}

	public static SceneView GetEditorSceneView()
	{
		return MBAPI.IMBEditor.GetEditorSceneView();
	}

	public static bool HelpersEnabled()
	{
		return MBAPI.IMBEditor.HelpersEnabled();
	}

	public static bool BorderHelpersEnabled()
	{
		return MBAPI.IMBEditor.BorderHelpersEnabled();
	}

	public static void ZoomToPosition(Vec3 pos)
	{
		MBAPI.IMBEditor.ZoomToPosition(pos);
	}

	public static bool IsReplayManagerReplaying()
	{
		return MBAPI.IMBEditor.IsReplayManagerReplaying();
	}

	public static bool IsReplayManagerRendering()
	{
		return MBAPI.IMBEditor.IsReplayManagerRendering();
	}

	public static bool IsReplayManagerRecording()
	{
		return MBAPI.IMBEditor.IsReplayManagerRecording();
	}

	public static void AddEditorWarning(string msg)
	{
		MBAPI.IMBEditor.AddEditorWarning(msg);
	}

	public static void AddEntityWarning(WeakGameEntity entityId, string msg)
	{
		MBAPI.IMBEditor.AddEntityWarning(entityId.Pointer, msg);
	}

	public static void AddNavMeshWarning(Scene scene, PathFaceRecord record, string msg)
	{
		MBAPI.IMBEditor.AddNavMeshWarning(scene.Pointer, in record, msg);
	}

	public static string GetAllPrefabsAndChildWithTag(string tag)
	{
		return MBAPI.IMBEditor.GetAllPrefabsAndChildWithTag(tag);
	}

	public static void ExitEditMode()
	{
		MBAPI.IMBEditor.ExitEditMode();
	}

	public static void SetUpgradeLevelVisibility(List<string> levels)
	{
		string text = "";
		for (int i = 0; i < levels.Count - 1; i++)
		{
			text = text + levels[i] + "|";
		}
		text += levels[levels.Count - 1];
		MBAPI.IMBEditor.SetUpgradeLevelVisibility(text);
	}

	public static void SetLevelVisibility(List<string> levels)
	{
	}

	public static void ToggleEnableEditorPhysics()
	{
		MBAPI.IMBEditor.ToggleEnableEditorPhysics();
	}
}
