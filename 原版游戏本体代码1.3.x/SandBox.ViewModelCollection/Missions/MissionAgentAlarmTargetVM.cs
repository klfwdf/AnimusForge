using System;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Missions
{
	// Token: 0x0200002C RID: 44
	public class MissionAgentAlarmTargetVM : ViewModel
	{
		// Token: 0x17000117 RID: 279
		// (get) Token: 0x06000386 RID: 902 RVA: 0x0000F454 File Offset: 0x0000D654
		public bool HasCautiousness
		{
			get
			{
				return this.TargetAgent.AIStateFlags.HasAnyFlag(Agent.AIStateFlag.Alarmed) || this.AlarmedBehaviorGroup.AlarmFactor > 0f;
			}
		}

		// Token: 0x17000118 RID: 280
		// (get) Token: 0x06000387 RID: 903 RVA: 0x0000F47D File Offset: 0x0000D67D
		public AlarmedBehaviorGroup AlarmedBehaviorGroup
		{
			get
			{
				if (this._alarmedBehaviorGroupCache == null)
				{
					AgentNavigator agentNavigator = this.TargetAgent.GetComponent<CampaignAgentComponent>().AgentNavigator;
					this._alarmedBehaviorGroupCache = ((agentNavigator != null) ? agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>() : null);
				}
				return this._alarmedBehaviorGroupCache;
			}
		}

		// Token: 0x06000388 RID: 904 RVA: 0x0000F4AF File Offset: 0x0000D6AF
		public MissionAgentAlarmTargetVM(Agent agent, Action<MissionAgentAlarmTargetVM> onRemove)
		{
			this.TargetAgent = agent;
			this._onRemove = onRemove;
		}

		// Token: 0x06000389 RID: 905 RVA: 0x0000F4C8 File Offset: 0x0000D6C8
		public void UpdateValues()
		{
			string agentAlarmState = MissionAgentAlarmTargetVM.GetAgentAlarmState(this.TargetAgent.AIStateFlags);
			AlarmedBehaviorGroup alarmedBehaviorGroup = this.AlarmedBehaviorGroup;
			float num = ((alarmedBehaviorGroup != null) ? alarmedBehaviorGroup.AlarmFactor : 0f);
			if (num > 1f)
			{
				num = MathF.Min(num, 2f);
				num -= 1f;
				num = MathF.Lerp(0.3f, 1f, num, 1E-05f);
			}
			if (!this.IsInVision || !this.IsStealthModeEnabled || ((float)this.AlarmProgress <= 0f && !this.IsMainAgentInVisibilityRange))
			{
				this.AlarmProgress = 0;
				this.AlarmState = MissionAgentAlarmTargetVM.AlarmStateEnum.Invalid.ToString();
				return;
			}
			this.AlarmState = agentAlarmState;
			this.AlarmProgress = (int)(num * 100f);
		}

		// Token: 0x0600038A RID: 906 RVA: 0x0000F590 File Offset: 0x0000D790
		private static string GetAgentAlarmState(Agent.AIStateFlag stateFlag)
		{
			if ((stateFlag & Agent.AIStateFlag.Alarmed) == Agent.AIStateFlag.Alarmed)
			{
				return MissionAgentAlarmTargetVM.AlarmStateEnum.Alarmed.ToString();
			}
			if ((stateFlag & Agent.AIStateFlag.Alarmed) == Agent.AIStateFlag.Cautious)
			{
				return MissionAgentAlarmTargetVM.AlarmStateEnum.Cautious.ToString();
			}
			if ((stateFlag & Agent.AIStateFlag.Alarmed) == Agent.AIStateFlag.PatrollingCautious)
			{
				return MissionAgentAlarmTargetVM.AlarmStateEnum.PatrollingCautious.ToString();
			}
			return MissionAgentAlarmTargetVM.AlarmStateEnum.None.ToString();
		}

		// Token: 0x0600038B RID: 907 RVA: 0x0000F5F0 File Offset: 0x0000D7F0
		public void UpdateScreenPosition(Camera missionCamera)
		{
			Vec3 position = this.TargetAgent.Position;
			position.z += this.TargetAgent.GetEyeGlobalHeight() + 0.35f;
			this._latestX = 0f;
			this._latestY = 0f;
			this._latestW = 0f;
			MBWindowManager.WorldToScreenInsideUsableArea(missionCamera, position, ref this._latestX, ref this._latestY, ref this._latestW);
			this._wPosAfterPositionCalculation = ((this._latestW < 0f) ? (-1f) : 1.1f);
			this.WSign = (int)this._wPosAfterPositionCalculation;
			this.ScreenPosition = new Vec2(this._latestX, this._latestY);
			int wsign = this.WSign;
		}

		// Token: 0x0600038C RID: 908 RVA: 0x0000F6AC File Offset: 0x0000D8AC
		public void ExecuteRemove()
		{
			Action<MissionAgentAlarmTargetVM> onRemove = this._onRemove;
			if (onRemove == null)
			{
				return;
			}
			onRemove(this);
		}

		// Token: 0x17000119 RID: 281
		// (get) Token: 0x0600038D RID: 909 RVA: 0x0000F6BF File Offset: 0x0000D8BF
		// (set) Token: 0x0600038E RID: 910 RVA: 0x0000F6C7 File Offset: 0x0000D8C7
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

		// Token: 0x1700011A RID: 282
		// (get) Token: 0x0600038F RID: 911 RVA: 0x0000F6E5 File Offset: 0x0000D8E5
		// (set) Token: 0x06000390 RID: 912 RVA: 0x0000F6ED File Offset: 0x0000D8ED
		[DataSourceProperty]
		public bool IsMainAgentInVisibilityRange
		{
			get
			{
				return this._isMainAgentInVisibilityRange;
			}
			set
			{
				if (value != this._isMainAgentInVisibilityRange)
				{
					this._isMainAgentInVisibilityRange = value;
					base.OnPropertyChangedWithValue(value, "IsMainAgentInVisibilityRange");
				}
			}
		}

		// Token: 0x1700011B RID: 283
		// (get) Token: 0x06000391 RID: 913 RVA: 0x0000F70B File Offset: 0x0000D90B
		// (set) Token: 0x06000392 RID: 914 RVA: 0x0000F713 File Offset: 0x0000D913
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

		// Token: 0x1700011C RID: 284
		// (get) Token: 0x06000393 RID: 915 RVA: 0x0000F731 File Offset: 0x0000D931
		// (set) Token: 0x06000394 RID: 916 RVA: 0x0000F739 File Offset: 0x0000D939
		[DataSourceProperty]
		public bool IsSuspected
		{
			get
			{
				return this._isSuspected;
			}
			set
			{
				if (value != this._isSuspected)
				{
					this._isSuspected = value;
					base.OnPropertyChangedWithValue(value, "IsSuspected");
				}
			}
		}

		// Token: 0x1700011D RID: 285
		// (get) Token: 0x06000395 RID: 917 RVA: 0x0000F757 File Offset: 0x0000D957
		// (set) Token: 0x06000396 RID: 918 RVA: 0x0000F75F File Offset: 0x0000D95F
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

		// Token: 0x1700011E RID: 286
		// (get) Token: 0x06000397 RID: 919 RVA: 0x0000F77D File Offset: 0x0000D97D
		// (set) Token: 0x06000398 RID: 920 RVA: 0x0000F785 File Offset: 0x0000D985
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

		// Token: 0x1700011F RID: 287
		// (get) Token: 0x06000399 RID: 921 RVA: 0x0000F7A8 File Offset: 0x0000D9A8
		// (set) Token: 0x0600039A RID: 922 RVA: 0x0000F7B0 File Offset: 0x0000D9B0
		[DataSourceProperty]
		public int WSign
		{
			get
			{
				return this._wSign;
			}
			set
			{
				if (value != this._wSign)
				{
					this._wSign = value;
					base.OnPropertyChangedWithValue(value, "WSign");
				}
			}
		}

		// Token: 0x17000120 RID: 288
		// (get) Token: 0x0600039B RID: 923 RVA: 0x0000F7CE File Offset: 0x0000D9CE
		// (set) Token: 0x0600039C RID: 924 RVA: 0x0000F7D6 File Offset: 0x0000D9D6
		[DataSourceProperty]
		public Vec2 ScreenPosition
		{
			get
			{
				return this._screenPosition;
			}
			set
			{
				if (value.x != this._screenPosition.x || value.y != this._screenPosition.y)
				{
					this._screenPosition = value;
					base.OnPropertyChangedWithValue(value, "ScreenPosition");
				}
			}
		}

		// Token: 0x040001C8 RID: 456
		public readonly Agent TargetAgent;

		// Token: 0x040001C9 RID: 457
		private readonly Action<MissionAgentAlarmTargetVM> _onRemove;

		// Token: 0x040001CA RID: 458
		private float _latestX;

		// Token: 0x040001CB RID: 459
		private float _latestY;

		// Token: 0x040001CC RID: 460
		private float _latestW;

		// Token: 0x040001CD RID: 461
		private float _wPosAfterPositionCalculation;

		// Token: 0x040001CE RID: 462
		private AlarmedBehaviorGroup _alarmedBehaviorGroupCache;

		// Token: 0x040001CF RID: 463
		private bool _isStealthModeEnabled;

		// Token: 0x040001D0 RID: 464
		private bool _isMainAgentInVisibilityRange;

		// Token: 0x040001D1 RID: 465
		private bool _isInVision;

		// Token: 0x040001D2 RID: 466
		private bool _isSuspected;

		// Token: 0x040001D3 RID: 467
		private string _alarmState;

		// Token: 0x040001D4 RID: 468
		private int _wSign;

		// Token: 0x040001D5 RID: 469
		private int _alarmProgress;

		// Token: 0x040001D6 RID: 470
		private Vec2 _screenPosition;

		// Token: 0x0200009A RID: 154
		private enum AlarmStateEnum
		{
			// Token: 0x040003A2 RID: 930
			Invalid = -1,
			// Token: 0x040003A3 RID: 931
			None,
			// Token: 0x040003A4 RID: 932
			Default,
			// Token: 0x040003A5 RID: 933
			Cautious,
			// Token: 0x040003A6 RID: 934
			PatrollingCautious,
			// Token: 0x040003A7 RID: 935
			Alarmed
		}
	}
}
