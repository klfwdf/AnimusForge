using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("CreateArmyStep3")]
public class CreateArmyStep3Tutorial : TutorialItemBase
{
	private bool _playerClosedArmyManagement;

	public CreateArmyStep3Tutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = string.Empty;
		base.MouseRequired = true;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _playerClosedArmyManagement;
	}

	public override void OnTutorialContextChanged(TutorialContextChangedEvent obj)
	{
		_playerClosedArmyManagement = obj.NewContext != TutorialContexts.ArmyManagement;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.ArmyManagement;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (TutorialHelper.CurrentContext == TutorialContexts.ArmyManagement && Campaign.Current.CurrentMenuContext == null && Clan.PlayerClan.Kingdom != null)
		{
			return MobileParty.MainParty.Army == null;
		}
		return false;
	}
}
