using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.View.Scripts;

public class PopupSceneSwitchCameraSequence : PopupSceneSequence
{
	public string EntityName = "";

	private GameEntity _switchEntity;

	protected override void OnInit()
	{
		_switchEntity = base.GameEntity.Scene.GetFirstEntityWithName(EntityName);
	}

	public override void OnInitialState()
	{
		if (_switchEntity != null)
		{
			base.GameEntity.Scene.FindEntityWithTag("customcamera")?.RemoveTag("customcamera");
			_switchEntity.AddTag("customcamera");
		}
	}

	public override void OnPositiveState()
	{
	}

	public override void OnNegativeState()
	{
	}
}
