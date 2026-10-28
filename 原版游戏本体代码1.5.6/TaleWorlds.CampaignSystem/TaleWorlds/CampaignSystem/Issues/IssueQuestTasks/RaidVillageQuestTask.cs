using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues.IssueQuestTasks;

public class RaidVillageQuestTask : QuestTaskBase
{
	[SaveableField(50)]
	private readonly Village _targetVillage;

	public RaidVillageQuestTask(Village village, Action onSucceededAction, Action onFailedAction, Action onCanceledAction, DialogFlow dialogFlow = null)
		: base(dialogFlow, onSucceededAction, onFailedAction, onCanceledAction)
	{
		_targetVillage = village;
	}

	public void OnVillageLooted(Village village)
	{
		if (_targetVillage == village)
		{
			Finish((_targetVillage.Owner.MapEvent.AttackerSide.LeaderParty != MobileParty.MainParty.Party) ? FinishStates.Fail : FinishStates.Success);
		}
	}

	public void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
	{
		if (!FactionManager.IsAtWarAgainstFaction(newKingdom, _targetVillage.Settlement.MapFaction))
		{
			Finish(FinishStates.Cancel);
		}
	}

	public override void SetReferences()
	{
		CampaignEvents.VillageLooted.AddNonSerializedListener(this, OnVillageLooted);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
	}
}
