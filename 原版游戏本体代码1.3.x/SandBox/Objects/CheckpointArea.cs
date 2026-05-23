using System;
using SandBox.Missions;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects
{
	// Token: 0x02000034 RID: 52
	public class CheckpointArea : VolumeBox
	{
		// Token: 0x17000025 RID: 37
		// (get) Token: 0x060001E8 RID: 488 RVA: 0x0000C731 File Offset: 0x0000A931
		// (set) Token: 0x060001E9 RID: 489 RVA: 0x0000C739 File Offset: 0x0000A939
		[EditorVisibleScriptComponentVariable(false)]
		public GameEntity SpawnPoint { get; private set; }

		// Token: 0x060001EA RID: 490 RVA: 0x0000C744 File Offset: 0x0000A944
		public override void AfterMissionStart()
		{
			this._checkpointMissionLogic = Mission.Current.GetMissionBehavior<CheckpointMissionLogic>();
			foreach (WeakGameEntity weakEntity in base.GameEntity.GetChildren())
			{
				if (weakEntity.HasTag("sp_checkpoint"))
				{
					this.SpawnPoint = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(weakEntity);
					break;
				}
			}
		}

		// Token: 0x060001EB RID: 491 RVA: 0x0000C7C0 File Offset: 0x0000A9C0
		protected override void OnTick(float dt)
		{
			if (this._checkpointMissionLogic != null)
			{
				Agent main = Agent.Main;
				if (main != null && main.IsActive() && base.IsPointIn(Agent.Main.Position))
				{
					this._checkpointMissionLogic.OnCheckpointUsed(this.UniqueId);
				}
			}
		}

		// Token: 0x060001EC RID: 492 RVA: 0x0000C800 File Offset: 0x0000AA00
		public override ScriptComponentBehavior.TickRequirement GetTickRequirement()
		{
			return ScriptComponentBehavior.TickRequirement.Tick;
		}

		// Token: 0x040000B2 RID: 178
		public const string CheckpointSpawnPointTag = "sp_checkpoint";

		// Token: 0x040000B3 RID: 179
		public int UniqueId;

		// Token: 0x040000B5 RID: 181
		[EditorVisibleScriptComponentVariable(false)]
		private CheckpointMissionLogic _checkpointMissionLogic;
	}
}
