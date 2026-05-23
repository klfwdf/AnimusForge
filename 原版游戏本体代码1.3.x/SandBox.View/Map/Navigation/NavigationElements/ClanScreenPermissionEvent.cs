using System;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;

namespace SandBox.View.Map.Navigation.NavigationElements
{
	// Token: 0x0200006B RID: 107
	public class ClanScreenPermissionEvent : EventBase
	{
		// Token: 0x17000090 RID: 144
		// (get) Token: 0x0600049B RID: 1179 RVA: 0x00024F32 File Offset: 0x00023132
		// (set) Token: 0x0600049C RID: 1180 RVA: 0x00024F3A File Offset: 0x0002313A
		public Action<bool, TextObject> IsClanScreenAvailable { get; private set; }

		// Token: 0x0600049D RID: 1181 RVA: 0x00024F43 File Offset: 0x00023143
		public ClanScreenPermissionEvent(Action<bool, TextObject> isClanScreenAvailable)
		{
			this.IsClanScreenAvailable = isClanScreenAvailable;
		}
	}
}
