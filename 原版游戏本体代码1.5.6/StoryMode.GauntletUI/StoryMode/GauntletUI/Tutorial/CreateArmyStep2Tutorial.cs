using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("CreateArmyStep2")]
public class CreateArmyStep2Tutorial : TutorialItemBase
{
	private bool _playerAddedPartyToArmy;

	public CreateArmyStep2Tutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.TopRight;
		base.HighlightedVisualElementID = "GatherArmyPartiesPanel";
		base.MouseRequired = true;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _playerAddedPartyToArmy;
	}

	public override void OnPartyAddedToArmyByPlayer(PartyAddedToArmyByPlayerEvent obj)
	{
		_playerAddedPartyToArmy = true;
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
