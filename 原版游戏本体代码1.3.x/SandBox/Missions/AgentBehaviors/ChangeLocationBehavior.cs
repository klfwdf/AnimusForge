using System;
using System.Collections.Generic;
using SandBox.Missions.MissionLogics;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors
{
	// Token: 0x020000A1 RID: 161
	public class ChangeLocationBehavior : AgentBehavior
	{
		// Token: 0x060006CC RID: 1740 RVA: 0x0002E660 File Offset: 0x0002C860
		public ChangeLocationBehavior(AgentBehaviorGroup behaviorGroup)
			: base(behaviorGroup)
		{
			this._missionAgentHandler = base.Mission.GetMissionBehavior<MissionAgentHandler>();
			this._initializeTime = base.Mission.CurrentTime;
		}

		// Token: 0x060006CD RID: 1741 RVA: 0x0002E68C File Offset: 0x0002C88C
		public override void Tick(float dt, bool isSimulation)
		{
			if (this._selectedDoor == null)
			{
				Passage passage = this.SelectADoor();
				if (passage != null)
				{
					this._selectedDoor = passage;
					base.Navigator.SetTarget(this._selectedDoor, false, Agent.AIScriptedFrameFlags.None);
					return;
				}
			}
			else if (this._selectedDoor.ToLocation.CharacterCount >= this._selectedDoor.ToLocation.ProsperityMax)
			{
				base.Navigator.SetTarget(null, false, Agent.AIScriptedFrameFlags.None);
				base.Navigator.ForceThink(0f);
				this._selectedDoor = null;
			}
		}

		// Token: 0x060006CE RID: 1742 RVA: 0x0002E710 File Offset: 0x0002C910
		private Passage SelectADoor()
		{
			Passage result = null;
			List<Passage> list = new List<Passage>();
			foreach (UsableMachine usableMachine in this._missionAgentHandler.TownPassageProps)
			{
				Passage passage = (Passage)usableMachine;
				if (passage.GetVacantStandingPointForAI(base.OwnerAgent) != null && passage.ToLocation.CharacterCount < passage.ToLocation.ProsperityMax)
				{
					list.Add(passage);
				}
			}
			if (list.Count > 0)
			{
				result = list[MBRandom.RandomInt(list.Count)];
			}
			return result;
		}

		// Token: 0x060006CF RID: 1743 RVA: 0x0002E7B8 File Offset: 0x0002C9B8
		protected override void OnActivate()
		{
			base.OnActivate();
			this._selectedDoor = null;
		}

		// Token: 0x060006D0 RID: 1744 RVA: 0x0002E7C7 File Offset: 0x0002C9C7
		protected override void OnDeactivate()
		{
			base.OnDeactivate();
			this._selectedDoor = null;
		}

		// Token: 0x060006D1 RID: 1745 RVA: 0x0002E7D6 File Offset: 0x0002C9D6
		public override string GetDebugInfo()
		{
			if (this._selectedDoor != null)
			{
				return "Go to " + this._selectedDoor.ToLocation.StringId;
			}
			return "Change Location no target";
		}

		// Token: 0x060006D2 RID: 1746 RVA: 0x0002E800 File Offset: 0x0002CA00
		public override float GetAvailability(bool isSimulation)
		{
			float result = 0f;
			bool flag = false;
			bool flag2 = false;
			LocationCharacter locationCharacter = CampaignMission.Current.Location.GetLocationCharacter(base.OwnerAgent.Origin);
			if (base.Mission.CurrentTime < 5f || locationCharacter.FixedLocation || !this._missionAgentHandler.HasPassages())
			{
				return 0f;
			}
			foreach (UsableMachine usableMachine in this._missionAgentHandler.TownPassageProps)
			{
				Passage passage = usableMachine as Passage;
				if (passage.ToLocation.CanAIEnter(locationCharacter) && passage.ToLocation.CharacterCount < passage.ToLocation.ProsperityMax)
				{
					flag = true;
					if (passage.PilotStandingPoint.GameEntity.GetGlobalFrame().origin.Distance(base.OwnerAgent.Position) < 1f)
					{
						flag2 = true;
						break;
					}
				}
			}
			if (flag)
			{
				if (!flag2)
				{
					result = (CampaignMission.Current.Location.IsIndoor ? 0.1f : 0.05f);
				}
				else if (base.Mission.CurrentTime - this._initializeTime > 10f)
				{
					result = 0.01f;
				}
			}
			return result;
		}

		// Token: 0x04000397 RID: 919
		private readonly MissionAgentHandler _missionAgentHandler;

		// Token: 0x04000398 RID: 920
		private readonly float _initializeTime;

		// Token: 0x04000399 RID: 921
		private Passage _selectedDoor;
	}
}
