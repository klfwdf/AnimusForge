using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("CreateArmyStep1")]
public class CreateArmyStep1Tutorial : TutorialItemBase
{
	private bool _playerOpenedGatherArmy;

	public CreateArmyStep1Tutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.TopRight;
		base.HighlightedVisualElementID = "MapGatherArmyButton";
		base.MouseRequired = true;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _playerOpenedGatherArmy;
	}

	public override void OnTutorialContextChanged(TutorialContextChangedEvent obj)
	{
		_playerOpenedGatherArmy = obj.NewContext == TutorialContexts.ArmyManagement;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.MapWindow;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (TutorialHelper.CurrentContext == TutorialContexts.MapWindow && Campaign.Current.CurrentMenuContext == null && Clan.PlayerClan.Kingdom != null && MobileParty.MainParty.Army == null)
		{
			return Clan.PlayerClan.Influence >= 30f;
		}
		return false;
	}
}
