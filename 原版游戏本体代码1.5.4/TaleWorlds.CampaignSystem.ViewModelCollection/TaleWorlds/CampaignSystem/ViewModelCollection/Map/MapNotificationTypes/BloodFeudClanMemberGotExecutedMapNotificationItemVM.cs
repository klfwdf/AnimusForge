using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class BloodFeudClanMemberGotExecutedMapNotificationItemVM : MapNotificationItemBaseVM
{
	public BloodFeudClanMemberGotExecutedMapNotificationItemVM(BloodFeudClanMemberGotExecutedMapNotification data)
		: base(data)
	{
		base.NotificationIdentifier = "blood_feud_clan_member_executed";
		_onInspect = OnInspect;
	}

	private void OnInspect()
	{
		BloodFeudClanMemberGotExecutedMapNotification bloodFeudClanMemberGotExecutedMapNotification = (BloodFeudClanMemberGotExecutedMapNotification)base.Data;
		MBInformationManager.ShowSceneNotification(HeroExecutionSceneNotificationData.CreateForInformingPlayer(bloodFeudClanMemberGotExecutedMapNotification.ExecutingHero, bloodFeudClanMemberGotExecutedMapNotification.ExecutedHero, bloodFeudClanMemberGotExecutedMapNotification.ExecutionDate, SceneNotificationData.RelevantContextType.Map, null, isVisualOnly: true, useExecutioner: true, shouldAutoConfirm: true));
		ExecuteRemove();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
	}
}
