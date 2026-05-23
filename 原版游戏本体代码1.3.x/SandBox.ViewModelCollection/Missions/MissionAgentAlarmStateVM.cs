using System;
using System.Collections.Generic;
using SandBox.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;

namespace SandBox.ViewModelCollection.Missions
{
	// Token: 0x0200002B RID: 43
	public class MissionAgentAlarmStateVM : ViewModel
	{
		// Token: 0x06000374 RID: 884 RVA: 0x0000EF51 File Offset: 0x0000D151
		public MissionAgentAlarmStateVM()
		{
			this.Targets = new MBBindingList<MissionAgentAlarmTargetVM>();
			this._stealthBoxes = new List<StealthBox>();
		}

		// Token: 0x06000375 RID: 885 RVA: 0x0000EF70 File Offset: 0x0000D170
		public void Initialize(Mission mission, Camera camera)
		{
			this._mission = mission;
			this._camera = camera;
			this._isInitialized = true;
			this._areStealthBoxesDirty = true;
			this.RefreshTargets();
			StealthBox.OnBoxInitialized += this.OnStealthBoxInitialized;
			StealthBox.OnBoxRemoved += this.OnStealthBoxRemoved;
		}

		// Token: 0x06000376 RID: 886 RVA: 0x0000EFC1 File Offset: 0x0000D1C1
		public override void OnFinalize()
		{
			base.OnFinalize();
			StealthBox.OnBoxInitialized -= this.OnStealthBoxInitialized;
			StealthBox.OnBoxRemoved -= this.OnStealthBoxRemoved;
		}

		// Token: 0x06000377 RID: 887 RVA: 0x0000EFEB File Offset: 0x0000D1EB
		private void OnStealthBoxInitialized(StealthBox stealthBox)
		{
			this._areStealthBoxesDirty = true;
		}

		// Token: 0x06000378 RID: 888 RVA: 0x0000EFF4 File Offset: 0x0000D1F4
		private void OnStealthBoxRemoved(StealthBox stealthBox)
		{
			this._areStealthBoxesDirty = true;
		}

		// Token: 0x06000379 RID: 889 RVA: 0x0000F000 File Offset: 0x0000D200
		private void RefreshStealthBoxEntities()
		{
			this._stealthBoxes.Clear();
			Mission mission = Mission.Current;
			if (((mission != null) ? mission.Scene : null) == null)
			{
				return;
			}
			List<GameEntity> list = new List<GameEntity>();
			Mission.Current.Scene.GetAllEntitiesWithScriptComponent<StealthBox>(ref list);
			for (int i = 0; i < list.Count; i++)
			{
				StealthBox firstScriptOfTypeRecursive = list[i].GetFirstScriptOfTypeRecursive<StealthBox>();
				if (firstScriptOfTypeRecursive != null)
				{
					this._stealthBoxes.Add(firstScriptOfTypeRecursive);
				}
			}
		}

		// Token: 0x0600037A RID: 890 RVA: 0x0000F078 File Offset: 0x0000D278
		public void Update()
		{
			if (!this._isInitialized)
			{
				return;
			}
			if (this._disguiseMissionLogic == null)
			{
				Mission mission = this._mission;
				this._disguiseMissionLogic = ((mission != null) ? mission.GetMissionBehavior<DisguiseMissionLogic>() : null);
			}
			DisguiseMissionLogic disguiseMissionLogic = this._disguiseMissionLogic;
			bool isStealthModeEnabled = disguiseMissionLogic != null && disguiseMissionLogic.IsInStealthMode;
			this.IsMainAgentInSafeArea = this.IsMainAgentInStealthArea();
			for (int i = 0; i < this.Targets.Count; i++)
			{
				MissionAgentAlarmTargetVM missionAgentAlarmTargetVM = this.Targets[i];
				if (this._disguiseMissionLogic == null)
				{
					missionAgentAlarmTargetVM.IsStealthModeEnabled = true;
					missionAgentAlarmTargetVM.IsMainAgentInVisibilityRange = SandBoxUIHelper.IsAgentInVisibilityRangeApproximate(missionAgentAlarmTargetVM.TargetAgent, Agent.Main);
					Vec3 origin = this._camera.Frame.origin;
					Vec3 eyeGlobalPosition = missionAgentAlarmTargetVM.TargetAgent.GetEyeGlobalPosition();
					Mission mission2 = Mission.Current;
					float num;
					bool? flag = ((mission2 != null) ? new bool?(!mission2.Scene.RayCastForClosestEntityOrTerrain(origin, eyeGlobalPosition, out num, 0.035f, BodyFlags.CommonFocusRayCastExcludeFlags)) : null);
					missionAgentAlarmTargetVM.IsInVision = flag ?? false;
					missionAgentAlarmTargetVM.IsSuspected = missionAgentAlarmTargetVM.AlarmProgress > 0;
					missionAgentAlarmTargetVM.UpdateScreenPosition(this._camera);
					missionAgentAlarmTargetVM.UpdateValues();
				}
				else
				{
					missionAgentAlarmTargetVM.IsStealthModeEnabled = isStealthModeEnabled;
					DisguiseMissionLogic.ShadowingAgentOffenseInfo agentOffenseInfo = this._disguiseMissionLogic.GetAgentOffenseInfo(missionAgentAlarmTargetVM.TargetAgent);
					if (agentOffenseInfo != null)
					{
						missionAgentAlarmTargetVM.IsMainAgentInVisibilityRange = SandBoxUIHelper.IsAgentInVisibilityRangeApproximate(missionAgentAlarmTargetVM.TargetAgent, Agent.Main);
						missionAgentAlarmTargetVM.IsInVision = agentOffenseInfo.CanPlayerCameraSeeTheAgent;
						missionAgentAlarmTargetVM.IsSuspected = missionAgentAlarmTargetVM.AlarmProgress > 0;
					}
					missionAgentAlarmTargetVM.UpdateScreenPosition(this._camera);
					missionAgentAlarmTargetVM.UpdateValues();
				}
			}
		}

		// Token: 0x0600037B RID: 891 RVA: 0x0000F218 File Offset: 0x0000D418
		private bool IsMainAgentInStealthArea()
		{
			Agent main = Agent.Main;
			if (main == null)
			{
				return false;
			}
			Mission mission = Mission.Current;
			if (((mission != null) ? mission.Scene : null) == null)
			{
				return false;
			}
			if (this._areStealthBoxesDirty)
			{
				this.RefreshStealthBoxEntities();
				this._areStealthBoxesDirty = false;
			}
			for (int i = 0; i < this._stealthBoxes.Count; i++)
			{
				if (this._stealthBoxes[i].IsAgentInside(main))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600037C RID: 892 RVA: 0x0000F290 File Offset: 0x0000D490
		public void OnAgentRemoved(Agent agent)
		{
			MissionAgentAlarmTargetVM agentTargetFromAgent = this.GetAgentTargetFromAgent(agent);
			if (agentTargetFromAgent != null)
			{
				this.Targets.Remove(agentTargetFromAgent);
			}
		}

		// Token: 0x0600037D RID: 893 RVA: 0x0000F2B8 File Offset: 0x0000D4B8
		private void RefreshTargets()
		{
			this.Targets.Clear();
			foreach (Agent agent in Mission.Current.Agents)
			{
				if (agent != null && SandBoxUIHelper.CanAgentBeAlarmed(agent))
				{
					this.Targets.Add(new MissionAgentAlarmTargetVM(agent, new Action<MissionAgentAlarmTargetVM>(this.OnRemoveTarget)));
				}
			}
		}

		// Token: 0x0600037E RID: 894 RVA: 0x0000F33C File Offset: 0x0000D53C
		public void OnAgentBuild(Agent agent, Banner banner)
		{
			this.RefreshTargets();
		}

		// Token: 0x0600037F RID: 895 RVA: 0x0000F344 File Offset: 0x0000D544
		public void OnAgentTeamChanged(Team prevTeam, Team newTeam, Agent agent)
		{
			if (agent != null && agent == Agent.Main)
			{
				this.RefreshTargets();
				return;
			}
			MissionAgentAlarmTargetVM agentTargetFromAgent = this.GetAgentTargetFromAgent(agent);
			if (agentTargetFromAgent == null && SandBoxUIHelper.CanAgentBeAlarmed(agent))
			{
				this.Targets.Add(new MissionAgentAlarmTargetVM(agent, new Action<MissionAgentAlarmTargetVM>(this.OnRemoveTarget)));
				return;
			}
			if (agentTargetFromAgent != null && (newTeam == Team.Invalid || (newTeam == null || newTeam.IsPlayerAlly)))
			{
				this.Targets.Remove(agentTargetFromAgent);
			}
		}

		// Token: 0x06000380 RID: 896 RVA: 0x0000F3BA File Offset: 0x0000D5BA
		private void OnRemoveTarget(MissionAgentAlarmTargetVM targetToRemove)
		{
			this.Targets.Remove(targetToRemove);
		}

		// Token: 0x06000381 RID: 897 RVA: 0x0000F3CC File Offset: 0x0000D5CC
		private MissionAgentAlarmTargetVM GetAgentTargetFromAgent(Agent agent)
		{
			for (int i = 0; i < this.Targets.Count; i++)
			{
				MissionAgentAlarmTargetVM missionAgentAlarmTargetVM = this.Targets[i];
				if (missionAgentAlarmTargetVM.TargetAgent == agent)
				{
					return missionAgentAlarmTargetVM;
				}
			}
			return null;
		}

		// Token: 0x17000115 RID: 277
		// (get) Token: 0x06000382 RID: 898 RVA: 0x0000F408 File Offset: 0x0000D608
		// (set) Token: 0x06000383 RID: 899 RVA: 0x0000F410 File Offset: 0x0000D610
		[DataSourceProperty]
		public MBBindingList<MissionAgentAlarmTargetVM> Targets
		{
			get
			{
				return this._targets;
			}
			set
			{
				if (value != this._targets)
				{
					this._targets = value;
					base.OnPropertyChangedWithValue<MBBindingList<MissionAgentAlarmTargetVM>>(value, "Targets");
				}
			}
		}

		// Token: 0x17000116 RID: 278
		// (get) Token: 0x06000384 RID: 900 RVA: 0x0000F42E File Offset: 0x0000D62E
		// (set) Token: 0x06000385 RID: 901 RVA: 0x0000F436 File Offset: 0x0000D636
		[DataSourceProperty]
		public bool IsMainAgentInSafeArea
		{
			get
			{
				return this._isMainAgentInSafeArea;
			}
			set
			{
				if (value != this._isMainAgentInSafeArea)
				{
					this._isMainAgentInSafeArea = value;
					base.OnPropertyChangedWithValue(value, "IsMainAgentInSafeArea");
				}
			}
		}

		// Token: 0x040001C0 RID: 448
		private bool _isInitialized;

		// Token: 0x040001C1 RID: 449
		private Mission _mission;

		// Token: 0x040001C2 RID: 450
		private Camera _camera;

		// Token: 0x040001C3 RID: 451
		private DisguiseMissionLogic _disguiseMissionLogic;

		// Token: 0x040001C4 RID: 452
		private bool _areStealthBoxesDirty;

		// Token: 0x040001C5 RID: 453
		private List<StealthBox> _stealthBoxes;

		// Token: 0x040001C6 RID: 454
		private bool _isMainAgentInSafeArea;

		// Token: 0x040001C7 RID: 455
		private MBBindingList<MissionAgentAlarmTargetVM> _targets;
	}
}
