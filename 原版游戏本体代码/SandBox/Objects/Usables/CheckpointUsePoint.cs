using System;
using SandBox.Missions;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects.Usables
{
	// Token: 0x0200004A RID: 74
	public class CheckpointUsePoint : UsableMachine
	{
		// Token: 0x1700004A RID: 74
		// (get) Token: 0x060002B8 RID: 696 RVA: 0x0000FB13 File Offset: 0x0000DD13
		// (set) Token: 0x060002B9 RID: 697 RVA: 0x0000FB1B File Offset: 0x0000DD1B
		[EditorVisibleScriptComponentVariable(false)]
		public GameEntity SpawnPoint { get; private set; }

		// Token: 0x060002BA RID: 698 RVA: 0x0000FB24 File Offset: 0x0000DD24
		protected override void OnInit()
		{
			base.OnInit();
			base.SetScriptComponentToTick(ScriptComponentBehavior.TickRequirement.Tick);
		}

		// Token: 0x060002BB RID: 699 RVA: 0x0000FB34 File Offset: 0x0000DD34
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

		// Token: 0x060002BC RID: 700 RVA: 0x0000FBB0 File Offset: 0x0000DDB0
		protected override void OnTick(float dt)
		{
			base.OnTick(dt);
			if (this._checkpointMissionLogic != null)
			{
				Agent main = Agent.Main;
				if (main != null && main.IsActive())
				{
					for (int i = 0; i < base.StandingPoints.Count; i++)
					{
						if (base.StandingPoints[i].HasUser)
						{
							this._checkpointMissionLogic.OnCheckpointUsed(this.UniqueId);
						}
					}
				}
			}
		}

		// Token: 0x060002BD RID: 701 RVA: 0x0000FC19 File Offset: 0x0000DE19
		public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
		{
			return new TextObject("{=G2IaEr2Z}Use", null);
		}

		// Token: 0x060002BE RID: 702 RVA: 0x0000FC26 File Offset: 0x0000DE26
		public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
		{
			return new TextObject("{=eO7p1Q3C}Checkpoint", null);
		}

		// Token: 0x0400012E RID: 302
		public const string CheckpointSpawnPointTag = "sp_checkpoint";

		// Token: 0x0400012F RID: 303
		public int UniqueId;

		// Token: 0x04000131 RID: 305
		[EditorVisibleScriptComponentVariable(false)]
		private CheckpointMissionLogic _checkpointMissionLogic;
	}
}
