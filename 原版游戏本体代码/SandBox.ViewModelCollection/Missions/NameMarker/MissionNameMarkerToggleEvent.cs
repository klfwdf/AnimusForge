using System;
using TaleWorlds.Library.EventSystem;

namespace SandBox.ViewModelCollection.Missions.NameMarker
{
	// Token: 0x02000035 RID: 53
	public class MissionNameMarkerToggleEvent : EventBase
	{
		// Token: 0x1700013A RID: 314
		// (get) Token: 0x060003F9 RID: 1017 RVA: 0x0001064C File Offset: 0x0000E84C
		// (set) Token: 0x060003FA RID: 1018 RVA: 0x00010654 File Offset: 0x0000E854
		public bool NewState { get; private set; }

		// Token: 0x060003FB RID: 1019 RVA: 0x0001065D File Offset: 0x0000E85D
		public MissionNameMarkerToggleEvent(bool newState)
		{
			this.NewState = newState;
		}
	}
}
