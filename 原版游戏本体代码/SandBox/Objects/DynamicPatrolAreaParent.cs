using System;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects
{
	// Token: 0x02000036 RID: 54
	public class DynamicPatrolAreaParent : MissionObject
	{
		// Token: 0x060001EF RID: 495 RVA: 0x0000C8AC File Offset: 0x0000AAAC
		protected override void OnEditorTick(float dt)
		{
		}

		// Token: 0x040000B6 RID: 182
		public bool DrawPath = true;

		// Token: 0x040000B7 RID: 183
		public int UniqueId = -1;
	}
}
