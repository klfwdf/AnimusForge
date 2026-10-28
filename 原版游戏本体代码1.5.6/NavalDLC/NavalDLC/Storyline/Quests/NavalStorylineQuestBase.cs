using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace NavalDLC.Storyline.Quests;

public abstract class NavalStorylineQuestBase : QuestBase
{
	public sealed override bool IsRemainingTimeHidden => true;

	public override string SpecialQuestType => "NavalStoryline";

	public abstract NavalStorylineData.NavalStorylineStage Stage { get; }

	public abstract bool WillProgressStoryline { get; }

	protected abstract string MainPartyTemplateStringId { get; }

	public PartyTemplateObject Template
	{
		get
		{
			if (string.IsNullOrEmpty(MainPartyTemplateStringId))
			{
				return null;
			}
			return Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>(MainPartyTemplateStringId);
		}
	}

	protected NavalStorylineQuestBase(string questId, Hero questGiver, CampaignTime duration, int rewardGold)
		: base(questId, questGiver, duration, rewardGold)
	{
	}

	protected sealed override void RegisterEvents()
	{
		NavalDLCEvents.OnNavalStorylineActivityChangedEvent.AddNonSerializedListener(this, OnNavalStorylineActivityChanged);
		NavalDLCEvents.IsNavalQuestPartyEvent.AddNonSerializedListener(this, IsNavalQuestParty);
		RegisterEventsInternal();
	}

	private void IsNavalQuestParty(PartyBase partyBase, NavalStorylinePartyData data)
	{
		if (partyBase == PartyBase.MainParty)
		{
			data.IsQuestParty = true;
			data.Template = Template;
			if (data.Template != null)
			{
				data.PartySize = (int)NavalDLCHelpers.GetMaxPartySizeLimitFromTemplate(data.Template).ResultNumber + 2;
			}
		}
		IsNavalQuestPartyInternal(partyBase, data);
	}

	protected virtual void IsNavalQuestPartyInternal(PartyBase partyBase, NavalStorylinePartyData data)
	{
	}

	private void OnNavalStorylineActivityChanged(bool activity)
	{
		if (base.IsOngoing && !activity)
		{
			ResetQuest();
		}
		OnNavalStorylineActivityChangedInternal(activity);
	}

	protected virtual void OnNavalStorylineActivityChangedInternal(bool activity)
	{
	}

	protected abstract void RegisterEventsInternal();

	public void ResetQuest()
	{
		CompleteQuestWithCancel();
	}

	protected sealed override void OnStartQuest()
	{
		if (WillProgressStoryline)
		{
			NavalStorylineData.OnStorylineProgress(this);
		}
		OnStartQuestInternal();
	}

	protected sealed override void OnFinalize()
	{
		OnFinalizeInternal();
	}

	protected sealed override void InitializeQuestOnGameLoad()
	{
		InitializeQuestOnGameLoadInternal();
	}

	protected virtual void InitializeQuestOnGameLoadInternal()
	{
	}

	protected virtual void OnStartQuestInternal()
	{
	}

	protected virtual void OnFinalizeInternal()
	{
	}

	public sealed override void OnCanceled()
	{
		OnCanceledInternal();
	}

	protected virtual void OnCanceledInternal()
	{
	}

	public sealed override void OnFailed()
	{
		OnFailedInternal();
	}

	protected virtual void OnFailedInternal()
	{
	}

	protected sealed override void OnCompleteWithSuccess()
	{
		OnCompleteWithSuccessInternal();
	}

	protected virtual void OnCompleteWithSuccessInternal()
	{
	}
}
