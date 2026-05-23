using System;
using System.Collections.Generic;
using SandBox.View.Missions.NameMarkers;
using SandBox.ViewModelCollection.Missions.NameMarker;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Missions
{
	// Token: 0x02000020 RID: 32
	[OverrideView(typeof(MissionNameMarkerUIHandler))]
	public class MissionGauntletNameMarkerView : MissionNameMarkerUIHandler
	{
		// Token: 0x060001BD RID: 445 RVA: 0x0000B740 File Offset: 0x00009940
		public override void OnMissionScreenInitialize()
		{
			base.OnMissionScreenInitialize();
			this._nameMarkerProviders = MissionNameMarkerFactory.CollectProviders();
			for (int i = 0; i < this._nameMarkerProviders.Count; i++)
			{
				this._nameMarkerProviders[i].Initialize(base.Mission, new Action(this.SetMarkersDirty));
			}
			this._dataSource = new MissionNameMarkerVM(this._nameMarkerProviders, base.MissionScreen.CombatCamera);
			this._gauntletLayer = new GauntletLayer("MissionNameMarker", 1, false);
			this._gauntletLayer.LoadMovie("NameMarker", this._dataSource);
			base.MissionScreen.AddLayer(this._gauntletLayer);
			if (Campaign.Current != null)
			{
				this._lastVisualTrackerVersion = Campaign.Current.VisualTrackerManager.TrackedObjectsVersion;
				CampaignEvents.ConversationEnded.AddNonSerializedListener(this, new Action<IEnumerable<CharacterObject>>(this.OnConversationEnd));
			}
			MissionNameMarkerFactory.OnProvidersChanged += this.OnMarkersChanged;
		}

		// Token: 0x060001BE RID: 446 RVA: 0x0000B834 File Offset: 0x00009A34
		public override void OnMissionScreenFinalize()
		{
			base.OnMissionScreenFinalize();
			for (int i = 0; i < this._nameMarkerProviders.Count; i++)
			{
				this._nameMarkerProviders[i].Destroy(base.Mission);
			}
			base.MissionScreen.RemoveLayer(this._gauntletLayer);
			this._gauntletLayer = null;
			this._dataSource.OnFinalize();
			this._dataSource = null;
			if (Campaign.Current != null)
			{
				CampaignEvents.ConversationEnded.ClearListeners(this);
			}
			InformationManager.HideAllMessages();
			MissionNameMarkerFactory.OnProvidersChanged -= this.OnMarkersChanged;
		}

		// Token: 0x060001BF RID: 447 RVA: 0x0000B8C8 File Offset: 0x00009AC8
		public override void OnMissionScreenTick(float dt)
		{
			base.OnMissionScreenTick(dt);
			for (int i = 0; i < this._nameMarkerProviders.Count; i++)
			{
				this._nameMarkerProviders[i].Tick(dt);
			}
			if (base.Input.IsGameKeyDown(5))
			{
				this._dataSource.IsEnabled = true;
			}
			else
			{
				this._dataSource.IsEnabled = false;
			}
			if (Campaign.Current != null && this._lastVisualTrackerVersion != Campaign.Current.VisualTrackerManager.TrackedObjectsVersion)
			{
				this.SetMarkersDirty();
				this._lastVisualTrackerVersion = Campaign.Current.VisualTrackerManager.TrackedObjectsVersion;
			}
			this._dataSource.Tick(dt);
		}

		// Token: 0x060001C0 RID: 448 RVA: 0x0000B974 File Offset: 0x00009B74
		private void OnMarkersChanged()
		{
			List<MissionNameMarkerProvider> list;
			List<MissionNameMarkerProvider> list2;
			MissionNameMarkerFactory.UpdateProviders(this._nameMarkerProviders.ToArray(), out list, out list2);
			for (int i = 0; i < list2.Count; i++)
			{
				this._nameMarkerProviders.Remove(list2[i]);
			}
			for (int j = 0; j < list.Count; j++)
			{
				this._nameMarkerProviders.Add(list[j]);
			}
			this.SetMarkersDirty();
		}

		// Token: 0x060001C1 RID: 449 RVA: 0x0000B9E2 File Offset: 0x00009BE2
		public override void SetMarkersDirty()
		{
			MissionNameMarkerVM dataSource = this._dataSource;
			if (dataSource == null)
			{
				return;
			}
			dataSource.SetTargetsDirty();
		}

		// Token: 0x060001C2 RID: 450 RVA: 0x0000B9F4 File Offset: 0x00009BF4
		public override void OnAgentBuild(Agent affectedAgent, Banner banner)
		{
			base.OnAgentBuild(affectedAgent, banner);
			if (base.Mission.Mode != MissionMode.Battle)
			{
				this.SetMarkersDirty();
			}
		}

		// Token: 0x060001C3 RID: 451 RVA: 0x0000BA12 File Offset: 0x00009C12
		public override void OnAgentDeleted(Agent affectedAgent)
		{
			if (base.Mission.Mode != MissionMode.Battle)
			{
				this.SetMarkersDirty();
			}
		}

		// Token: 0x060001C4 RID: 452 RVA: 0x0000BA28 File Offset: 0x00009C28
		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
		{
			if (base.Mission.Mode != MissionMode.Battle)
			{
				this.SetMarkersDirty();
			}
		}

		// Token: 0x060001C5 RID: 453 RVA: 0x0000BA3E File Offset: 0x00009C3E
		private void OnConversationEnd(IEnumerable<CharacterObject> conversationCharacters)
		{
			if (base.Mission.Mode != MissionMode.Battle)
			{
				this.SetMarkersDirty();
			}
		}

		// Token: 0x060001C6 RID: 454 RVA: 0x0000BA54 File Offset: 0x00009C54
		public override void OnPhotoModeActivated()
		{
			base.OnPhotoModeActivated();
			if (this._gauntletLayer != null)
			{
				this._gauntletLayer.UIContext.ContextAlpha = 0f;
			}
		}

		// Token: 0x060001C7 RID: 455 RVA: 0x0000BA79 File Offset: 0x00009C79
		public override void OnPhotoModeDeactivated()
		{
			base.OnPhotoModeDeactivated();
			if (this._gauntletLayer != null)
			{
				this._gauntletLayer.UIContext.ContextAlpha = 1f;
			}
		}

		// Token: 0x060001C8 RID: 456 RVA: 0x0000BA9E File Offset: 0x00009C9E
		protected override void OnResumeView()
		{
			base.OnResumeView();
			ScreenManager.SetSuspendLayer(this._gauntletLayer, false);
		}

		// Token: 0x060001C9 RID: 457 RVA: 0x0000BAB2 File Offset: 0x00009CB2
		protected override void OnSuspendView()
		{
			base.OnSuspendView();
			ScreenManager.SetSuspendLayer(this._gauntletLayer, true);
		}

		// Token: 0x0400008D RID: 141
		private GauntletLayer _gauntletLayer;

		// Token: 0x0400008E RID: 142
		private MissionNameMarkerVM _dataSource;

		// Token: 0x0400008F RID: 143
		private List<MissionNameMarkerProvider> _nameMarkerProviders;

		// Token: 0x04000090 RID: 144
		private int _lastVisualTrackerVersion;
	}
}
