using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.View.Screens;

[GameStateScreen(typeof(EditorState))]
public class SceneEditorScreen : ScreenBase, IGameStateListener
{
	private SceneEditorLayer _editorLayer;

	public SceneEditorScreen(EditorState editorState)
	{
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		_editorLayer = new SceneEditorLayer();
		_editorLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: true, InputUsageMask.Invalid);
		AddLayer(_editorLayer);
		ManagedParameters.Instance.Initialize(ModuleHelper.GetXmlPath("Native", "managed_core_parameters"));
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		MouseManager.ActivateMouseCursor(CursorType.System);
		MBEditor.ActivateSceneEditorPresentation();
	}

	protected override void OnDeactivate()
	{
		MBEditor.DeactivateSceneEditorPresentation();
		MouseManager.ActivateMouseCursor(CursorType.Default);
		base.OnDeactivate();
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		if (_editorLayer != null)
		{
			bool mouseVisible = Screen.GetMouseVisible();
			_editorLayer.InputRestrictions.SetMouseVisibility(mouseVisible);
		}
		MBEditor.TickSceneEditorPresentation(dt);
	}

	void IGameStateListener.OnActivate()
	{
	}

	void IGameStateListener.OnDeactivate()
	{
	}

	void IGameStateListener.OnInitialize()
	{
	}

	void IGameStateListener.OnFinalize()
	{
	}
}
