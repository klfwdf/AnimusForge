using System;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.Objects;

namespace SandBox.Objects.AreaMarkers
{
	// Token: 0x02000046 RID: 70
	public class StealthAreaMarker : AreaMarker
	{
		// Token: 0x17000047 RID: 71
		// (get) Token: 0x0600029F RID: 671 RVA: 0x0000F59A File Offset: 0x0000D79A
		// (set) Token: 0x060002A0 RID: 672 RVA: 0x0000F5A2 File Offset: 0x0000D7A2
		public GameEntity ReinforcementAllyGroupSpawnPoint { get; private set; }

		// Token: 0x17000048 RID: 72
		// (get) Token: 0x060002A1 RID: 673 RVA: 0x0000F5AB File Offset: 0x0000D7AB
		// (set) Token: 0x060002A2 RID: 674 RVA: 0x0000F5B3 File Offset: 0x0000D7B3
		public GameEntity WaitPoint { get; private set; }

		// Token: 0x060002A3 RID: 675 RVA: 0x0000F5BC File Offset: 0x0000D7BC
		public override void AfterMissionStart()
		{
			foreach (WeakGameEntity weakEntity in base.GameEntity.GetChildren())
			{
				if (weakEntity.HasTag("reinforcement_ally_group_spawn_point_tag"))
				{
					this.ReinforcementAllyGroupSpawnPoint = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(weakEntity);
				}
				if (weakEntity.HasTag("wait_point_tag"))
				{
					this.WaitPoint = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(weakEntity);
				}
			}
		}

		// Token: 0x04000128 RID: 296
		private const string ReinforcementAllyGroupSpawnPointTag = "reinforcement_ally_group_spawn_point_tag";

		// Token: 0x04000129 RID: 297
		private const string WaitPointTag = "wait_point_tag";

		// Token: 0x0400012A RID: 298
		public string ReinforcementAllyGroupId;
	}
}
