using System;
using SandBox.Missions;
using SandBox.View.Missions;
using SandBox.ViewModelCollection.Missions.NameMarker.Targets.Hideout;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View;

namespace SandBox.GauntletUI.Missions
{
	// Token: 0x02000022 RID: 34
	[OverrideView(typeof(MissionStealthFailCounterView))]
	public class MissionGauntletStealthFailCounterView : MissionStealthFailCounterView
	{
		// Token: 0x060001CE RID: 462 RVA: 0x0000BBE8 File Offset: 0x00009DE8
		public override void OnMissionScreenInitialize()
		{
			base.OnMissionScreenInitialize();
			this._countdownCounterVM = new MissionStealthFailCounterVM();
			this._countdownLayer = new GauntletLayer("MissionStealthFailCounter", 10, false);
			this._countdownLayer.LoadMovie("MissionStealthFailCounter", this._countdownCounterVM);
			base.MissionScreen.AddLayer(this._countdownLayer);
		}

		// Token: 0x060001CF RID: 463 RVA: 0x0000BC41 File Offset: 0x00009E41
		public override void AfterStart()
		{
			this._stealthFailCounterMissionLogic = base.Mission.GetMissionBehavior<StealthFailCounterMissionLogic>();
		}

		// Token: 0x060001D0 RID: 464 RVA: 0x0000BC54 File Offset: 0x00009E54
		public override void OnMissionScreenFinalize()
		{
			base.OnMissionScreenFinalize();
			this._countdownCounterVM.OnFinalize();
			base.MissionScreen.RemoveLayer(this._countdownLayer);
			this._countdownLayer = null;
		}

		// Token: 0x060001D1 RID: 465 RVA: 0x0000BC7F File Offset: 0x00009E7F
		public override void OnMissionScreenTick(float dt)
		{
			base.OnMissionScreenTick(dt);
			if (this._stealthFailCounterMissionLogic != null)
			{
				this._countdownCounterVM.UpdateFailCounter(this._stealthFailCounterMissionLogic.FailCounterElapsedTime, this._stealthFailCounterMissionLogic.FailCounterSeconds);
			}
		}

		// Token: 0x04000096 RID: 150
		private GauntletLayer _countdownLayer;

		// Token: 0x04000097 RID: 151
		private MissionStealthFailCounterVM _countdownCounterVM;

		// Token: 0x04000098 RID: 152
		private StealthFailCounterMissionLogic _stealthFailCounterMissionLogic;
	}
}
