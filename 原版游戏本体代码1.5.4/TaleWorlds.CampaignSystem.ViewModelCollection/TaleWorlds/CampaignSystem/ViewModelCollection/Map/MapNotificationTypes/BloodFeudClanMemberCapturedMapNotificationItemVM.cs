using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class BloodFeudClanMemberCapturedMapNotificationItemVM : MapNotificationItemBaseVM
{
	public BloodFeudClanMemberCapturedMapNotificationItemVM(BloodFeudClanMemberCapturedMapNotification data)
		: base(data)
	{
		base.NotificationIdentifier = "blood_feud_clan_member_captured";
		_onInspect = OnInspect;
		CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, OnHeroPrisonerReleased);
		CampaignEvents.OnHeroChangedClanEvent.AddNonSerializedListener(this, OnHeroChangedClan);
		CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
	}

	private void OnInspect()
	{
		Settlement settlement = ((BloodFeudClanMemberCapturedMapNotification)base.Data).Settlement;
		if (settlement != null)
		{
			if (!Campaign.Current.VisualTrackerManager.CheckTracked(settlement))
			{
				Campaign.Current.VisualTrackerManager.RegisterObject(settlement);
			}
			GoToMapPosition(settlement.Position);
		}
	}

	private void DailyTick()
	{
		int variable = (int)((BloodFeudClanMemberCapturedMapNotification)base.Data).ExecutionDate.RemainingDaysFromNow + 1;
		base.Data.DescriptionText.SetTextVariable("DAYS", variable);
		RefreshValues();
	}

	private void OnHeroPrisonerReleased(Hero prisoner, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification = true)
	{
		if (prisoner == ((BloodFeudClanMemberCapturedMapNotification)base.Data).CaptiveHero)
		{
			ExecuteRemove();
		}
	}

	private void OnHeroChangedClan(Hero hero, Clan oldClan)
	{
		if (hero == ((BloodFeudClanMemberCapturedMapNotification)base.Data).CaptiveHero && oldClan == Clan.PlayerClan)
		{
			ExecuteRemove();
		}
	}

	private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
		if (victim == ((BloodFeudClanMemberCapturedMapNotification)base.Data).CaptiveHero)
		{
			ExecuteRemove();
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
	}
}
