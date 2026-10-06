using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("ArmyCohesionStep1")]
public class ArmyCohesionStep1Tutorial : TutorialItemBase
{
	private bool _playerOpenedArmyManagement;

	private bool _playerArmyNeedsCohesion;

	public ArmyCohesionStep1Tutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = "ArmyOverlayArmyManagementButton";
		base.MouseRequired = true;
	}

	public override bool IsConditionsMetForCompletion()
	{
		if (_playerArmyNeedsCohesion)
		{
			return _playerOpenedArmyManagement;
		}
		return false;
	}

	public override void OnTutorialContextChanged(TutorialContextChangedEvent obj)
	{
		_playerOpenedArmyManagement = _playerArmyNeedsCohesion && obj.NewContext == TutorialContexts.ArmyManagement;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.MapWindow;
	}

	public override bool IsConditionsMetForActivation()
	{
		_playerArmyNeedsCohesion |= MobileParty.MainParty.Army?.Cohesion < TutorialHelper.MaxCohesionForCohesionTutorial;
		if (TutorialHelper.CurrentContext == TutorialContexts.MapWindow && MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty)
		{
			return MobileParty.MainParty.Army.Cohesion < TutorialHelper.MaxCohesionForCohesionTutorial;
		}
		return false;
	}
}
