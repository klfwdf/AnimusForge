using System;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.Core.ViewModelCollection.Tutorial
{
	// Token: 0x0200000F RID: 15
	public class TutorialNotificationElementChangeEvent : EventBase
	{
		// Token: 0x17000045 RID: 69
		// (get) Token: 0x060000BE RID: 190 RVA: 0x0000346A File Offset: 0x0000166A
		// (set) Token: 0x060000BF RID: 191 RVA: 0x00003472 File Offset: 0x00001672
		public string NewNotificationElementID { get; private set; }

		// Token: 0x060000C0 RID: 192 RVA: 0x0000347B File Offset: 0x0000167B
		public TutorialNotificationElementChangeEvent(string newNotificationElementID)
		{
			this.NewNotificationElementID = newNotificationElementID ?? string.Empty;
		}
	}
}
