using System;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace SandBox.Tournaments.AgentControllers
{
	// Token: 0x02000033 RID: 51
	public class TownHorseRaceAgentController : AgentController
	{
		// Token: 0x060001E3 RID: 483 RVA: 0x0000C4D5 File Offset: 0x0000A6D5
		public override void OnInitialize()
		{
			this._controller = base.Mission.GetMissionBehavior<TownHorseRaceMissionController>();
			this._checkPointIndex = 0;
			this._tourCount = 0;
		}

		// Token: 0x060001E4 RID: 484 RVA: 0x0000C4F8 File Offset: 0x0000A6F8
		public void DisableMovement()
		{
			if (base.Owner.IsAIControlled)
			{
				WorldPosition worldPosition = base.Owner.GetWorldPosition();
				base.Owner.SetScriptedPositionAndDirection(ref worldPosition, base.Owner.Frame.rotation.f.AsVec2.RotationInRadians, false, Agent.AIScriptedFrameFlags.None);
			}
		}

		// Token: 0x060001E5 RID: 485 RVA: 0x0000C554 File Offset: 0x0000A754
		public void Start()
		{
			if (this._checkPointIndex < this._controller.CheckPoints.Count)
			{
				TownHorseRaceMissionController.CheckPoint checkPoint = this._controller.CheckPoints[this._checkPointIndex];
				checkPoint.AddToCheckList(base.Owner);
				if (base.Owner.IsAIControlled)
				{
					WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, checkPoint.GetBestTargetPosition(), false);
					base.Owner.SetScriptedPosition(ref worldPosition, false, Agent.AIScriptedFrameFlags.NeverSlowDown);
				}
			}
		}

		// Token: 0x060001E6 RID: 486 RVA: 0x0000C5D8 File Offset: 0x0000A7D8
		public void OnEnterCheckPoint(VolumeBox checkPoint)
		{
			this._controller.CheckPoints[this._checkPointIndex].RemoveFromCheckList(base.Owner);
			this._checkPointIndex++;
			if (this._checkPointIndex < this._controller.CheckPoints.Count)
			{
				if (base.Owner.IsAIControlled)
				{
					WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, this._controller.CheckPoints[this._checkPointIndex].GetBestTargetPosition(), false);
					base.Owner.SetScriptedPosition(ref worldPosition, false, Agent.AIScriptedFrameFlags.NeverSlowDown);
				}
				this._controller.CheckPoints[this._checkPointIndex].AddToCheckList(base.Owner);
				return;
			}
			this._tourCount++;
			if (this._tourCount < 2)
			{
				this._checkPointIndex = 0;
				if (base.Owner.IsAIControlled)
				{
					WorldPosition worldPosition2 = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, this._controller.CheckPoints[this._checkPointIndex].GetBestTargetPosition(), false);
					base.Owner.SetScriptedPosition(ref worldPosition2, false, Agent.AIScriptedFrameFlags.NeverSlowDown);
				}
				this._controller.CheckPoints[this._checkPointIndex].AddToCheckList(base.Owner);
			}
		}

		// Token: 0x040000AF RID: 175
		private TownHorseRaceMissionController _controller;

		// Token: 0x040000B0 RID: 176
		private int _checkPointIndex;

		// Token: 0x040000B1 RID: 177
		private int _tourCount;
	}
}
