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

namespace SandBox.GauntletUI.Missions;

[OverrideView(typeof(MissionNameMarkerUIHandler))]
public class MissionGauntletNameMarkerView : MissionNameMarkerUIHandler
{
	private GauntletLayer _gauntletLayer;

	private MissionNameMarkerVM _dataSource;

	private List<MissionNameMarkerProvider> _nameMarkerProviders;

	private int _lastVisualTrackerVersion;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_nameMarkerProviders = MissionNameMarkerFactory.CollectProviders();
		for (int i = 0; i < _nameMarkerProviders.Count; i++)
		{
			_nameMarkerProviders[i].Initialize(base.Mission, SetMarkersDirty);
		}
		_dataSource = new MissionNameMarkerVM(_nameMarkerProviders, base.MissionScreen.CombatCamera);
		_gauntletLayer = new GauntletLayer("MissionNameMarker", 1);
		_gauntletLayer.LoadMovie("NameMarker", _dataSource);
		base.MissionScreen.AddLayer(_gauntletLayer);
		if (Campaign.Current != null)
		{
			_lastVisualTrackerVersion = Campaign.Current.VisualTrackerManager.TrackedObjectsVersion;
			CampaignEvents.ConversationEnded.AddNonSerializedListener(this, OnConversationEnd);
		}
		MissionNameMarkerFactory.OnProvidersChanged += OnMarkersChanged;
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		for (int i = 0; i < _nameMarkerProviders.Count; i++)
		{
			_nameMarkerProviders[i].Destroy(base.Mission);
		}
		base.MissionScreen.RemoveLayer(_gauntletLayer);
		_gauntletLayer = null;
		_dataSource.OnFinalize();
		_dataSource = null;
		if (Campaign.Current != null)
		{
			CampaignEvents.ConversationEnded.ClearListeners(this);
		}
		InformationManager.HideAllMessages();
		MissionNameMarkerFactory.OnProvidersChanged -= OnMarkersChanged;
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (base.IsViewCreated)
		{
			if (base.IsViewSuspended != _gauntletLayer.IsActive)
			{
				ScreenManager.SetSuspendLayer(_gauntletLayer, base.IsViewSuspended);
			}
			for (int i = 0; i < _nameMarkerProviders.Count; i++)
			{
				_nameMarkerProviders[i].Tick(dt);
			}
			if (base.Input.IsGameKeyDown(5))
			{
				_dataSource.IsEnabled = true;
			}
			else
			{
				_dataSource.IsEnabled = false;
			}
			if (Campaign.Current != null && _lastVisualTrackerVersion != Campaign.Current.VisualTrackerManager.TrackedObjectsVersion)
			{
				SetMarkersDirty();
				_lastVisualTrackerVersion = Campaign.Current.VisualTrackerManager.TrackedObjectsVersion;
			}
			_dataSource.Tick(dt);
		}
	}

	private void OnMarkersChanged()
	{
		MissionNameMarkerFactory.UpdateProviders(_nameMarkerProviders.ToArray(), out var addedProviders, out var removedProviders);
		for (int i = 0; i < removedProviders.Count; i++)
		{
			_nameMarkerProviders.Remove(removedProviders[i]);
		}
		for (int j = 0; j < addedProviders.Count; j++)
		{
			_nameMarkerProviders.Add(addedProviders[j]);
		}
		SetMarkersDirty();
	}

	public override void SetMarkersDirty()
	{
		_dataSource?.SetTargetsDirty();
	}

	public override void OnAgentBuild(Agent affectedAgent, Banner banner)
	{
		base.OnAgentBuild(affectedAgent, banner);
		if (base.Mission.Mode != MissionMode.Battle)
		{
			SetMarkersDirty();
		}
	}

	public override void OnAgentDeleted(Agent affectedAgent)
	{
		if (base.Mission.Mode != MissionMode.Battle)
		{
			SetMarkersDirty();
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		if (base.Mission.Mode != MissionMode.Battle)
		{
			SetMarkersDirty();
		}
	}

	private void OnConversationEnd(IEnumerable<CharacterObject> conversationCharacters)
	{
		if (base.Mission.Mode != MissionMode.Battle)
		{
			SetMarkersDirty();
		}
	}

	public override void OnPhotoModeActivated()
	{
		base.OnPhotoModeActivated();
		if (_gauntletLayer != null)
		{
			_gauntletLayer.UIContext.ContextAlpha = 0f;
		}
	}

	public override void OnPhotoModeDeactivated()
	{
		base.OnPhotoModeDeactivated();
		if (_gauntletLayer != null)
		{
			_gauntletLayer.UIContext.ContextAlpha = 1f;
		}
	}

	protected override void OnResumeView()
	{
		base.OnResumeView();
	}

	protected override void OnSuspendView()
	{
		base.OnSuspendView();
	}
}
