using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class DeathNotificationItemVM : MapNotificationItemBaseVM
{
	public DeathNotificationItemVM(DeathMapNotification data)
		: base(data)
	{
		DeathNotificationItemVM deathNotificationItemVM = this;
		base.NotificationIdentifier = "death";
		bool victimDiedAtSea = (data.VictimHero.PartyBelongedTo != null && data.VictimHero.PartyBelongedTo.IsCurrentlyAtSea) || (data.VictimHero.PartyBelongedToAsPrisoner != null && data.VictimHero.PartyBelongedToAsPrisoner.IsMobile && data.VictimHero.PartyBelongedToAsPrisoner.MobileParty.IsCurrentlyAtSea);
		if (data.VictimHero == Hero.MainHero)
		{
			_onInspect = delegate
			{
				deathNotificationItemVM.NavigationHandler?.OpenCharacterDeveloper(Hero.MainHero);
				deathNotificationItemVM.ExecuteRemove();
			};
		}
		else if (data.KillDetail == KillCharacterAction.KillCharacterActionDetail.DiedInBattle)
		{
			_onInspect = delegate
			{
				SceneNotificationData data2 = ((!victimDiedAtSea) ? ((SceneNotificationData)new ClanMemberWarDeathSceneNotificationItem(data.VictimHero, data.CreationTime)) : ((SceneNotificationData)new NavalDeathSceneNotificationItem(data.VictimHero, data.CreationTime, data.KillDetail)));
				MBInformationManager.ShowSceneNotification(data2);
				deathNotificationItemVM.ExecuteRemove();
			};
		}
		else
		{
			_onInspect = delegate
			{
				SceneNotificationData data2 = ((!victimDiedAtSea) ? ((SceneNotificationData)new ClanMemberPeaceDeathSceneNotificationItem(data.VictimHero, data.CreationTime, data.KillDetail)) : ((SceneNotificationData)new NavalDeathSceneNotificationItem(data.VictimHero, data.CreationTime, data.KillDetail)));
				MBInformationManager.ShowSceneNotification(data2);
				deathNotificationItemVM.ExecuteRemove();
			};
		}
	}
}
