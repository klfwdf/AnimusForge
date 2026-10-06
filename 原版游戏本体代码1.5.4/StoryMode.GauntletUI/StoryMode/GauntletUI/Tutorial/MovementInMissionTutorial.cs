using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("MovementInMissionTutorial")]
public class MovementInMissionTutorial : TutorialItemBase
{
	private bool _playerMovedForward;

	private bool _playerMovedBackward;

	private bool _playerMovedLeft;

	private bool _playerMovedRight;

	public MovementInMissionTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = string.Empty;
		base.MouseRequired = false;
	}

	public override bool IsConditionsMetForCompletion()
	{
		if (_playerMovedBackward && _playerMovedLeft && _playerMovedRight)
		{
			return _playerMovedForward;
		}
		return false;
	}

	public override void OnPlayerMovementFlagChanged(MissionPlayerMovementFlagsChangeEvent obj)
	{
		base.OnPlayerMovementFlagChanged(obj);
		_playerMovedRight = _playerMovedRight || (obj.MovementFlag & Agent.MovementControlFlag.StrafeRight) == Agent.MovementControlFlag.StrafeRight;
		_playerMovedLeft = _playerMovedLeft || (obj.MovementFlag & Agent.MovementControlFlag.StrafeLeft) == Agent.MovementControlFlag.StrafeLeft;
		_playerMovedForward = _playerMovedForward || (obj.MovementFlag & Agent.MovementControlFlag.Forward) == Agent.MovementControlFlag.Forward;
		_playerMovedBackward = _playerMovedBackward || (obj.MovementFlag & Agent.MovementControlFlag.Backward) == Agent.MovementControlFlag.Backward;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.Mission;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (Mission.Current != null && Mission.Current.Mode != MissionMode.Deployment && !TutorialHelper.PlayerIsInAConversation)
		{
			return TutorialHelper.CurrentContext == TutorialContexts.Mission;
		}
		return false;
	}
}
