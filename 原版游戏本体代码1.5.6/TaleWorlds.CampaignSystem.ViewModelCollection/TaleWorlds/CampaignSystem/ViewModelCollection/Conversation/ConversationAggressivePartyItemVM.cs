using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Conversation;

public class ConversationAggressivePartyItemVM : ViewModel
{
	public readonly MobileParty Party;

	private MBBindingList<QuestMarkerVM> _quests;

	private CharacterImageIdentifierVM _leaderVisual;

	private int _healthyAmount;

	[DataSourceProperty]
	public CharacterImageIdentifierVM LeaderVisual
	{
		get
		{
			return _leaderVisual;
		}
		set
		{
			if (value != _leaderVisual)
			{
				_leaderVisual = value;
				OnPropertyChangedWithValue(value, "LeaderVisual");
			}
		}
	}

	[DataSourceProperty]
	public int HealthyAmount
	{
		get
		{
			return _healthyAmount;
		}
		set
		{
			if (value != _healthyAmount)
			{
				_healthyAmount = value;
				OnPropertyChangedWithValue(value, "HealthyAmount");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<QuestMarkerVM> Quests
	{
		get
		{
			return _quests;
		}
		set
		{
			if (value != _quests)
			{
				_quests = value;
				OnPropertyChangedWithValue(value, "Quests");
			}
		}
	}

	public ConversationAggressivePartyItemVM(MobileParty party, CharacterObject leader = null)
	{
		Party = party;
		if (leader != null)
		{
			LeaderVisual = new CharacterImageIdentifierVM(CampaignUIHelper.GetCharacterCode(leader));
		}
		else if (party != null)
		{
			CharacterObject visualPartyLeader = CampaignUIHelper.GetVisualPartyLeader(party.Party);
			if (visualPartyLeader != null)
			{
				LeaderVisual = new CharacterImageIdentifierVM(CampaignUIHelper.GetCharacterCode(visualPartyLeader));
			}
		}
		HealthyAmount = party?.Party.NumberOfHealthyMembers ?? 0;
		RefreshQuests();
	}

	private void RefreshQuests()
	{
		Quests = new MBBindingList<QuestMarkerVM>();
		if (Party != null)
		{
			List<QuestBase> questsRelatedToParty = CampaignUIHelper.GetQuestsRelatedToParty(Party);
			CampaignUIHelper.IssueQuestFlags issueQuestFlags = CampaignUIHelper.IssueQuestFlags.None;
			for (int i = 0; i < questsRelatedToParty.Count; i++)
			{
				issueQuestFlags |= CampaignUIHelper.GetQuestType(questsRelatedToParty[i], Party.LeaderHero);
			}
			if (Party.LeaderHero?.Issue != null)
			{
				issueQuestFlags |= CampaignUIHelper.GetIssueType(Party.LeaderHero.Issue);
			}
			if ((issueQuestFlags & CampaignUIHelper.IssueQuestFlags.TrackedIssue) != CampaignUIHelper.IssueQuestFlags.None)
			{
				Quests.Add(new QuestMarkerVM(CampaignUIHelper.IssueQuestFlags.TrackedIssue));
			}
			else if ((issueQuestFlags & CampaignUIHelper.IssueQuestFlags.ActiveIssue) != CampaignUIHelper.IssueQuestFlags.None)
			{
				Quests.Add(new QuestMarkerVM(CampaignUIHelper.IssueQuestFlags.ActiveIssue));
			}
			else if ((issueQuestFlags & CampaignUIHelper.IssueQuestFlags.AvailableIssue) != CampaignUIHelper.IssueQuestFlags.None)
			{
				Quests.Add(new QuestMarkerVM(CampaignUIHelper.IssueQuestFlags.AvailableIssue));
			}
			if ((issueQuestFlags & CampaignUIHelper.IssueQuestFlags.TrackedStoryQuest) != CampaignUIHelper.IssueQuestFlags.None)
			{
				Quests.Add(new QuestMarkerVM(CampaignUIHelper.IssueQuestFlags.TrackedStoryQuest));
			}
			else if ((issueQuestFlags & CampaignUIHelper.IssueQuestFlags.ActiveStoryQuest) != CampaignUIHelper.IssueQuestFlags.None)
			{
				Quests.Add(new QuestMarkerVM(CampaignUIHelper.IssueQuestFlags.ActiveStoryQuest));
			}
		}
	}

	public void ExecuteShowPartyTooltip()
	{
		if (Party != null)
		{
			InformationManager.ShowTooltip(typeof(MobileParty), Party, true, true);
		}
	}

	public void ExecuteHideTooltip()
	{
		MBInformationManager.HideInformations();
	}
}
