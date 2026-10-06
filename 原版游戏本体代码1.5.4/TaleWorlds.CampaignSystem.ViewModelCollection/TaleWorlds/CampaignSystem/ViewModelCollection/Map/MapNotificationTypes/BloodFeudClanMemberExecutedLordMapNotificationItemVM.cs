using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class BloodFeudClanMemberExecutedLordMapNotificationItemVM : MapNotificationItemBaseVM
{
	public BloodFeudClanMemberExecutedLordMapNotificationItemVM(BloodFeudClanMemberExecutedLordMapNotification data)
		: base(data)
	{
		base.NotificationIdentifier = "blood_feud_clan_member_executed_lord";
		base.ForceInspection = true;
		_onInspect = OnInspect;
	}

	private void OnInspect()
	{
		BloodFeudClanMemberExecutedLordMapNotification bloodFeudClanMemberExecutedLordMapNotification = (BloodFeudClanMemberExecutedLordMapNotification)base.Data;
		MBInformationManager.ShowSceneNotification(HeroExecutionSceneNotificationData.CreateForInformingPlayer(bloodFeudClanMemberExecutedLordMapNotification.Executor, bloodFeudClanMemberExecutedLordMapNotification.ExecutedHero, bloodFeudClanMemberExecutedLordMapNotification.ExecutionDate, SceneNotificationData.RelevantContextType.Map, null, isVisualOnly: true, useExecutioner: false, shouldAutoConfirm: true));
		ExecuteRemove();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
	}
}
