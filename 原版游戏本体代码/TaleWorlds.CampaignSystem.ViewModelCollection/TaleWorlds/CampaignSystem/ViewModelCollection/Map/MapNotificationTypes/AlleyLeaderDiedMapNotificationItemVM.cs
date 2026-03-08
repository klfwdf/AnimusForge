using System;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x0200003C RID: 60
	public class AlleyLeaderDiedMapNotificationItemVM : MapNotificationItemBaseVM
	{
		// Token: 0x060005CF RID: 1487 RVA: 0x0001E8D3 File Offset: 0x0001CAD3
		public AlleyLeaderDiedMapNotificationItemVM(AlleyLeaderDiedMapNotification data)
			: base(data)
		{
			this._alley = data.Alley;
			base.NotificationIdentifier = "alley_leader_died";
			this._onInspect = new Action(this.CreateAlleyLeaderDiedPopUp);
		}

		// Token: 0x060005D0 RID: 1488 RVA: 0x0001E908 File Offset: 0x0001CB08
		private void CreateAlleyLeaderDiedPopUp()
		{
			object obj = new TextObject("{=6QoSHiWC}An alley without a leader", null);
			TextObject textObject = new TextObject("{=FzbeSkBb}One of your alleys has lost its leader or is lacking troops. It will be abandoned after {DAYS} days have passed. You can assign a new clan member from the clan screen or travel to the alley to add more troops, if you wish to keep it. Any troops left in the alley will be lost when it is abandoned.", null);
			textObject.SetTextVariable("DAYS", (int)Campaign.Current.Models.AlleyModel.DestroyAlleyAfterDaysWhenLeaderIsDeath.ToDays);
			TextObject textObject2 = new TextObject("{=jVLJTuwl}Learn more", null);
			InformationManager.ShowInquiry(new InquiryData(obj.ToString(), textObject.ToString(), true, true, textObject2.ToString(), GameTexts.FindText("str_dismiss", null).ToString(), new Action(this.OpenClanScreenAfterAlleyLeaderDeath), new Action(base.ExecuteRemove), "", 0f, null, null, null), false, false);
		}

		// Token: 0x060005D1 RID: 1489 RVA: 0x0001E9B5 File Offset: 0x0001CBB5
		private void OpenClanScreenAfterAlleyLeaderDeath()
		{
			if (base.NavigationHandler != null && this._alley != null)
			{
				base.NavigationHandler.OpenClan(this._alley);
				base.ExecuteRemove();
			}
		}

		// Token: 0x04000279 RID: 633
		private Alley _alley;
	}
}
