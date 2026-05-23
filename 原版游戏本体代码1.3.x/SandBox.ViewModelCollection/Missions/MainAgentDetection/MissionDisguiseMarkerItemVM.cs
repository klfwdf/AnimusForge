using System;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Missions.MainAgentDetection
{
	// Token: 0x02000042 RID: 66
	public class MissionDisguiseMarkerItemVM : ViewModel
	{
		// Token: 0x17000146 RID: 326
		// (get) Token: 0x0600043B RID: 1083 RVA: 0x00011264 File Offset: 0x0000F464
		public DisguiseMissionLogic.ShadowingAgentOffenseInfo OffenseInfo { get; }

		// Token: 0x0600043C RID: 1084 RVA: 0x0001126C File Offset: 0x0000F46C
		public MissionDisguiseMarkerItemVM(Camera missionCamera, DisguiseMissionLogic.ShadowingAgentOffenseInfo offenseInfo)
		{
			this._missionCamera = missionCamera;
			this.OffenseInfo = offenseInfo;
		}

		// Token: 0x0600043D RID: 1085 RVA: 0x00011282 File Offset: 0x0000F482
		public void RefreshVisuals()
		{
			DisguiseMissionLogic.ShadowingAgentOffenseInfo offenseInfo = this.OffenseInfo;
			this.OffenseTypeIdentifier = this.GetOffenseTypeIdentifier((offenseInfo != null) ? offenseInfo.OffenseType : StealthOffenseTypes.None);
			this.UpdateAlarmState();
		}

		// Token: 0x0600043E RID: 1086 RVA: 0x000112A8 File Offset: 0x0000F4A8
		public void UpdatePosition()
		{
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			Vec3 position = this.OffenseInfo.Agent.Position;
			position.z += this.OffenseInfo.Agent.GetEyeGlobalHeight() + 0.35f;
			if (position.IsValid)
			{
				MBWindowManager.WorldToScreenInsideUsableArea(this._missionCamera, position, ref num, ref num2, ref num3);
			}
			if (!position.IsValid || num3 < 0f || !MathF.IsValidValue(num) || !MathF.IsValidValue(num2))
			{
				num = -10000f;
				num2 = -10000f;
				num3 = 0f;
			}
			this.ScreenPosition = new Vec2(num, num2);
		}

		// Token: 0x0600043F RID: 1087 RVA: 0x00011358 File Offset: 0x0000F558
		private void UpdateAlarmState()
		{
			Agent agent = this.OffenseInfo.Agent;
			AgentNavigator agentNavigator = agent.GetComponent<CampaignAgentComponent>().AgentNavigator;
			AlarmedBehaviorGroup alarmedBehaviorGroup = ((agentNavigator != null) ? agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>() : null);
			Agent.AIStateFlag aistateFlags = agent.AIStateFlags;
			if (aistateFlags.HasFlag(Agent.AIStateFlag.Alarmed))
			{
				this._activeAlarmState = MissionDisguiseMarkerItemVM.AgentAlarmStateEnum.Alarmed;
			}
			else if (aistateFlags.HasFlag(Agent.AIStateFlag.Cautious))
			{
				this._activeAlarmState = MissionDisguiseMarkerItemVM.AgentAlarmStateEnum.Cautious;
			}
			else if (aistateFlags.HasFlag(Agent.AIStateFlag.PatrollingCautious))
			{
				this._activeAlarmState = MissionDisguiseMarkerItemVM.AgentAlarmStateEnum.PatrollingCautious;
			}
			else
			{
				this._activeAlarmState = MissionDisguiseMarkerItemVM.AgentAlarmStateEnum.None;
			}
			float num;
			if (aistateFlags.HasFlag(Agent.AIStateFlag.Alarmed))
			{
				num = 1f;
			}
			else
			{
				num = MathF.Clamp(alarmedBehaviorGroup.AlarmFactor / 2f, 0f, 1f);
			}
			this.AlarmState = this._activeAlarmState.ToString();
			this.AlarmProgress = (int)(num * 100f);
		}

		// Token: 0x06000440 RID: 1088 RVA: 0x00011448 File Offset: 0x0000F648
		private string GetOffenseTypeIdentifier(StealthOffenseTypes offenseType)
		{
			if (this.IsStealthModeEnabled || !this.IsInVision || !this.IsInVisibilityRange)
			{
				this._offenseType = MissionDisguiseMarkerItemVM.AgentStealthOffenseType.None;
				return this._offenseType.ToString();
			}
			switch (offenseType)
			{
			case StealthOffenseTypes.None:
				this._offenseType = MissionDisguiseMarkerItemVM.AgentStealthOffenseType.Default;
				break;
			case StealthOffenseTypes.IsVisible:
				this._offenseType = (this.IsSuspicious ? MissionDisguiseMarkerItemVM.AgentStealthOffenseType.Suspicious : MissionDisguiseMarkerItemVM.AgentStealthOffenseType.Visible);
				break;
			case StealthOffenseTypes.IsInPersonalZone:
				this._offenseType = MissionDisguiseMarkerItemVM.AgentStealthOffenseType.Suspicious;
				break;
			}
			return this._offenseType.ToString();
		}

		// Token: 0x17000147 RID: 327
		// (get) Token: 0x06000441 RID: 1089 RVA: 0x000114CF File Offset: 0x0000F6CF
		// (set) Token: 0x06000442 RID: 1090 RVA: 0x000114D7 File Offset: 0x0000F6D7
		[DataSourceProperty]
		public Vec2 ScreenPosition
		{
			get
			{
				return this._screenPosition;
			}
			set
			{
				if (value != this._screenPosition)
				{
					this._screenPosition = value;
					base.OnPropertyChangedWithValue(value, "ScreenPosition");
				}
			}
		}

		// Token: 0x17000148 RID: 328
		// (get) Token: 0x06000443 RID: 1091 RVA: 0x000114FA File Offset: 0x0000F6FA
		// (set) Token: 0x06000444 RID: 1092 RVA: 0x00011502 File Offset: 0x0000F702
		[DataSourceProperty]
		public int AlarmProgress
		{
			get
			{
				return this._alarmProgress;
			}
			set
			{
				if (value != this._alarmProgress)
				{
					this._alarmProgress = value;
					base.OnPropertyChangedWithValue(value, "AlarmProgress");
				}
			}
		}

		// Token: 0x17000149 RID: 329
		// (get) Token: 0x06000445 RID: 1093 RVA: 0x00011520 File Offset: 0x0000F720
		// (set) Token: 0x06000446 RID: 1094 RVA: 0x00011528 File Offset: 0x0000F728
		[DataSourceProperty]
		public string AlarmState
		{
			get
			{
				return this._alarmState;
			}
			set
			{
				if (value != this._alarmState)
				{
					this._alarmState = value;
					base.OnPropertyChangedWithValue<string>(value, "AlarmState");
				}
			}
		}

		// Token: 0x1700014A RID: 330
		// (get) Token: 0x06000447 RID: 1095 RVA: 0x0001154B File Offset: 0x0000F74B
		// (set) Token: 0x06000448 RID: 1096 RVA: 0x00011553 File Offset: 0x0000F753
		[DataSourceProperty]
		public string OffenseTypeIdentifier
		{
			get
			{
				return this._offenseTypeIdentifier;
			}
			set
			{
				if (value != this._offenseTypeIdentifier)
				{
					this._offenseTypeIdentifier = value;
					base.OnPropertyChangedWithValue<string>(value, "OffenseTypeIdentifier");
				}
			}
		}

		// Token: 0x1700014B RID: 331
		// (get) Token: 0x06000449 RID: 1097 RVA: 0x00011576 File Offset: 0x0000F776
		// (set) Token: 0x0600044A RID: 1098 RVA: 0x0001157E File Offset: 0x0000F77E
		[DataSourceProperty]
		public bool IsStealthModeEnabled
		{
			get
			{
				return this._isStealthModeEnabled;
			}
			set
			{
				if (value != this._isStealthModeEnabled)
				{
					this._isStealthModeEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsStealthModeEnabled");
				}
			}
		}

		// Token: 0x1700014C RID: 332
		// (get) Token: 0x0600044B RID: 1099 RVA: 0x0001159C File Offset: 0x0000F79C
		// (set) Token: 0x0600044C RID: 1100 RVA: 0x000115A4 File Offset: 0x0000F7A4
		[DataSourceProperty]
		public bool IsSuspicious
		{
			get
			{
				return this._isSuspicious;
			}
			set
			{
				if (value != this._isSuspicious)
				{
					this._isSuspicious = value;
					base.OnPropertyChangedWithValue(value, "IsSuspicious");
				}
			}
		}

		// Token: 0x1700014D RID: 333
		// (get) Token: 0x0600044D RID: 1101 RVA: 0x000115C2 File Offset: 0x0000F7C2
		// (set) Token: 0x0600044E RID: 1102 RVA: 0x000115CA File Offset: 0x0000F7CA
		[DataSourceProperty]
		public bool IsTarget
		{
			get
			{
				return this._isTarget;
			}
			set
			{
				if (value != this._isTarget)
				{
					this._isTarget = value;
					base.OnPropertyChangedWithValue(value, "IsTarget");
				}
			}
		}

		// Token: 0x1700014E RID: 334
		// (get) Token: 0x0600044F RID: 1103 RVA: 0x000115E8 File Offset: 0x0000F7E8
		// (set) Token: 0x06000450 RID: 1104 RVA: 0x000115F0 File Offset: 0x0000F7F0
		[DataSourceProperty]
		public bool IsInVision
		{
			get
			{
				return this._isInVision;
			}
			set
			{
				if (value != this._isInVision)
				{
					this._isInVision = value;
					base.OnPropertyChangedWithValue(value, "IsInVision");
				}
			}
		}

		// Token: 0x1700014F RID: 335
		// (get) Token: 0x06000451 RID: 1105 RVA: 0x0001160E File Offset: 0x0000F80E
		// (set) Token: 0x06000452 RID: 1106 RVA: 0x00011616 File Offset: 0x0000F816
		[DataSourceProperty]
		public bool IsInVisibilityRange
		{
			get
			{
				return this._isInVisibilityRange;
			}
			set
			{
				if (value != this._isInVisibilityRange)
				{
					this._isInVisibilityRange = value;
					base.OnPropertyChangedWithValue(value, "IsInVisibilityRange");
				}
			}
		}

		// Token: 0x04000226 RID: 550
		private Camera _missionCamera;

		// Token: 0x04000228 RID: 552
		private MissionDisguiseMarkerItemVM.AgentAlarmStateEnum _activeAlarmState;

		// Token: 0x04000229 RID: 553
		private MissionDisguiseMarkerItemVM.AgentStealthOffenseType _offenseType;

		// Token: 0x0400022A RID: 554
		private Vec2 _screenPosition;

		// Token: 0x0400022B RID: 555
		private int _alarmProgress;

		// Token: 0x0400022C RID: 556
		private string _alarmState;

		// Token: 0x0400022D RID: 557
		private string _offenseTypeIdentifier;

		// Token: 0x0400022E RID: 558
		private bool _isStealthModeEnabled;

		// Token: 0x0400022F RID: 559
		private bool _isSuspicious;

		// Token: 0x04000230 RID: 560
		private bool _isTarget;

		// Token: 0x04000231 RID: 561
		private bool _isInVision;

		// Token: 0x04000232 RID: 562
		private bool _isInVisibilityRange;

		// Token: 0x020000A1 RID: 161
		public enum AgentAlarmStateEnum
		{
			// Token: 0x040003B1 RID: 945
			None = -1,
			// Token: 0x040003B2 RID: 946
			Alarmed,
			// Token: 0x040003B3 RID: 947
			Cautious,
			// Token: 0x040003B4 RID: 948
			PatrollingCautious,
			// Token: 0x040003B5 RID: 949
			Suspicious,
			// Token: 0x040003B6 RID: 950
			Visible
		}

		// Token: 0x020000A2 RID: 162
		public enum AgentStealthOffenseType
		{
			// Token: 0x040003B8 RID: 952
			None = -1,
			// Token: 0x040003B9 RID: 953
			Default,
			// Token: 0x040003BA RID: 954
			Visible,
			// Token: 0x040003BB RID: 955
			Suspicious
		}
	}
}
