using System;
using TaleWorlds.Library.EventSystem;

namespace SandBox.Missions
{
	// Token: 0x02000059 RID: 89
	public class CheckpointLoadedMissionEvent : EventBase
	{
		// Token: 0x06000380 RID: 896 RVA: 0x00014354 File Offset: 0x00012554
		public CheckpointLoadedMissionEvent(int loadedCheckpointUniqueId)
		{
			this.LoadedCheckpointUniqueId = loadedCheckpointUniqueId;
		}

		// Token: 0x040001CA RID: 458
		public readonly int LoadedCheckpointUniqueId;
	}
}
