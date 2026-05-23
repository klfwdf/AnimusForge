using System;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x02000042 RID: 66
	public class EducationNotificationItemVM : MapNotificationItemBaseVM
	{
		// Token: 0x060005E6 RID: 1510 RVA: 0x0001EFA0 File Offset: 0x0001D1A0
		public EducationNotificationItemVM(EducationMapNotification data)
			: base(data)
		{
			base.NotificationIdentifier = "education";
			base.ForceInspection = true;
			this._child = data.Child;
			this._age = data.Age;
			this._onInspect = new Action(this.OnInspect);
			CampaignEvents.ChildEducationCompletedEvent.AddNonSerializedListener(this, new Action<Hero, int>(this.OnEducationCompletedForChild));
		}

		// Token: 0x060005E7 RID: 1511 RVA: 0x0001F008 File Offset: 0x0001D208
		private void OnInspect()
		{
			EducationMapNotification educationMapNotification = (EducationMapNotification)base.Data;
			if (educationMapNotification != null && !educationMapNotification.IsValid())
			{
				InformationManager.ShowInquiry(new InquiryData("", new TextObject("{=wGWYNYYX}This education stage is no longer relevant.", null).ToString(), true, false, GameTexts.FindText("str_ok", null).ToString(), "", null, null, "", 0f, null, null, null), false, false);
				base.ExecuteRemove();
				return;
			}
			Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<EducationState>(new object[] { this._child }), 0);
		}

		// Token: 0x060005E8 RID: 1512 RVA: 0x0001F0A8 File Offset: 0x0001D2A8
		private void OnEducationCompletedForChild(Hero child, int age)
		{
			if (child == this._child && age >= this._age)
			{
				base.ExecuteRemove();
			}
		}

		// Token: 0x060005E9 RID: 1513 RVA: 0x0001F0C2 File Offset: 0x0001D2C2
		public override void OnFinalize()
		{
			base.OnFinalize();
			CampaignEvents.ChildEducationCompletedEvent.ClearListeners(this);
		}

		// Token: 0x0400027E RID: 638
		private readonly Hero _child;

		// Token: 0x0400027F RID: 639
		private readonly int _age;
	}
}
