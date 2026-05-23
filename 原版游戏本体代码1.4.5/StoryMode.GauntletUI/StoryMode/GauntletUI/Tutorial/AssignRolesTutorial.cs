using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("AssignRolesTutorial")]
public class AssignRolesTutorial : TutorialItemBase
{
	private bool _playerAssignedRoleToClanMember;

	public AssignRolesTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Top;
		base.HighlightedVisualElementID = "RoleAssignmentWidget";
		base.MouseRequired = true;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.ClanScreen;
	}

	public override void OnClanRoleAssignedThroughClanScreen(ClanRoleAssignedThroughClanScreenEvent obj)
	{
		_playerAssignedRoleToClanMember = true;
	}

	public override bool IsConditionsMetForActivation()
	{
		return TutorialHelper.PlayerHasUnassignedRolesAndMember;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _playerAssignedRoleToClanMember;
	}
}
