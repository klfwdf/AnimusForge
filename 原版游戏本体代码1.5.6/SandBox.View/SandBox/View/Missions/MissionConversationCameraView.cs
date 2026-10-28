using System;
using SandBox.Conversation.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.View.Missions;

public class MissionConversationCameraView : MissionView
{
	private const string CustomCameraMultiAgentTag = "custom_camera_multi_agent";

	private MissionMainAgentController _missionMainAgentController;

	private ConversationMissionLogic _conversationMissionLogic;

	private Camera _customConversationCamera;

	private GameEntity _customMultiAgentConversationCameraEntity;

	private Agent _speakerAgent;

	private Agent _listenerAgent;

	public bool IsCameraOverridden => _customConversationCamera != null;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_conversationMissionLogic = base.Mission.GetMissionBehavior<ConversationMissionLogic>();
	}

	public override void AfterStart()
	{
		_missionMainAgentController = base.Mission.GetMissionBehavior<MissionMainAgentController>();
		_customMultiAgentConversationCameraEntity = base.Mission.Scene.FindEntityWithTag("custom_camera_multi_agent");
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (_conversationMissionLogic == null || !_conversationMissionLogic.IsMultiAgentConversation)
		{
			return;
		}
		ConversationManager conversationManager = Campaign.Current.ConversationManager;
		if (conversationManager.ConversationAgents == null || conversationManager.ConversationAgents.Count <= 0)
		{
			return;
		}
		Agent speakerAgent = (Agent)conversationManager.SpeakerAgent;
		Agent listenerAgent = (Agent)conversationManager.ListenerAgent;
		if (_speakerAgent == null || _listenerAgent == null)
		{
			return;
		}
		_speakerAgent = speakerAgent;
		_listenerAgent = listenerAgent;
		if (_speakerAgent != Agent.Main && _listenerAgent != Agent.Main)
		{
			if (base.MissionScreen.CustomCamera == null)
			{
				GameEntity customMultiAgentConversationCameraEntity = _customMultiAgentConversationCameraEntity;
				Vec3 dofParams = Vec3.Invalid;
				Camera camera = Camera.CreateCamera();
				customMultiAgentConversationCameraEntity.GetCameraParamsFromCameraScript(camera, ref dofParams);
				camera.SetFovVertical(camera.GetFovVertical(), Screen.AspectRatio, camera.Near, camera.Far);
				base.MissionScreen.CustomCamera = camera;
			}
			UpdateAgentLooksForConversation();
		}
		else
		{
			base.MissionScreen.CustomCamera = null;
		}
	}

	public override bool UpdateOverridenCamera(float dt)
	{
		MissionMode mode = base.Mission.Mode;
		if ((mode == MissionMode.Conversation || mode == MissionMode.Barter) && !base.MissionScreen.IsCheatGhostMode)
		{
			if (_conversationMissionLogic?.CustomConversationCameraEntity != null)
			{
				if (_customConversationCamera == null && _conversationMissionLogic?.CustomConversationCameraEntity != null)
				{
					Vec3 dofParams = Vec3.Invalid;
					_customConversationCamera = Camera.CreateCamera();
					_conversationMissionLogic?.CustomConversationCameraEntity.GetCameraParamsFromCameraScript(_customConversationCamera, ref dofParams);
					_customConversationCamera.SetFovVertical(_customConversationCamera.GetFovVertical(), Screen.AspectRatio, _customConversationCamera.Near, _customConversationCamera.Far);
				}
				SetConversationLookToPointOfInterest(_customConversationCamera.Position);
				base.MissionScreen.CustomCamera = _customConversationCamera;
			}
			else
			{
				UpdateAgentLooksForConversation();
			}
		}
		else if (_missionMainAgentController.CustomLookDir.IsNonZero)
		{
			_missionMainAgentController.CustomLookDir = Vec3.Zero;
		}
		return false;
	}

	private void UpdateAgentLooksForConversation()
	{
		Agent agent = null;
		ConversationManager conversationManager = Campaign.Current.ConversationManager;
		if (conversationManager.ConversationAgents != null && conversationManager.ConversationAgents.Count > 0)
		{
			_speakerAgent = (Agent)conversationManager.SpeakerAgent;
			_listenerAgent = (Agent)conversationManager.ListenerAgent;
			agent = Agent.Main.GetLookAgent();
			if (_speakerAgent == null)
			{
				return;
			}
			foreach (IAgent conversationAgent in conversationManager.ConversationAgents)
			{
				if (conversationAgent != _speakerAgent)
				{
					MakeAgentLookToSpeaker((Agent)conversationAgent);
				}
			}
			MakeSpeakerLookToListener();
		}
		SetFocusedObjectForCameraFocus();
		if (Agent.Main.GetLookAgent() != agent && _speakerAgent != null)
		{
			SpeakerAgentIsChanged();
		}
	}

	private void SpeakerAgentIsChanged()
	{
		Mission.Current.ConversationCharacterChanged();
	}

	private void SetFocusedObjectForCameraFocus()
	{
		if (base.MissionScreen.CustomCamera != null)
		{
			Agent agent = _speakerAgent;
			Agent agent2 = _listenerAgent;
			MatrixFrame identity = MatrixFrame.Identity;
			Vec3 vec = agent2.Position - agent.Position;
			vec.RotateAboutZ(-System.MathF.PI / 3f);
			vec += agent.Position;
			Vec3 vec2 = agent.Position - agent2.Position;
			vec2.RotateAboutZ(-System.MathF.PI / 3f);
			vec2 += agent2.Position;
			if (vec.Distance(Agent.Main.Position) > vec2.Distance(Agent.Main.Position))
			{
				agent = _listenerAgent;
				agent2 = _speakerAgent;
				vec = vec2;
			}
			vec.z += Agent.Main.GetEyeGlobalHeight();
			Vec3 u = -((agent2.Position - agent.Position) / 2f + agent.Position - vec).NormalizedCopy();
			identity.origin = vec;
			identity.rotation.s = Vec3.Side;
			identity.rotation.f = Vec3.Up;
			identity.rotation.u = u;
			identity.rotation.Orthonormalize();
			base.MissionScreen.CustomCamera.SetFovHorizontal(System.MathF.PI / 2f, Screen.AspectRatio, 0.1f, 2000f);
			base.MissionScreen.CustomCamera.Frame = identity;
			Agent.Main.AgentVisuals.SetVisible(value: false);
		}
		else if (_speakerAgent == Agent.Main)
		{
			_missionMainAgentController.InteractionComponent.SetCurrentFocusedObject(_listenerAgent, null, -1, isInteractable: true);
			_missionMainAgentController.CustomLookDir = (_listenerAgent.Position - Agent.Main.Position).NormalizedCopy();
			Agent.Main.SetLookAgent(_listenerAgent);
		}
		else
		{
			_missionMainAgentController.InteractionComponent.SetCurrentFocusedObject(_speakerAgent, null, -1, isInteractable: true);
			_missionMainAgentController.CustomLookDir = (_speakerAgent.Position - Agent.Main.Position).NormalizedCopy();
			Agent.Main.SetLookAgent(_speakerAgent);
		}
	}

	private void MakeAgentLookToSpeaker(Agent agent)
	{
		Vec3 position = agent.Position;
		Vec3 position2 = _speakerAgent.Position;
		position.z = agent.AgentVisuals.GetGlobalStableEyePoint(isHumanoid: true).z;
		position2.z = _speakerAgent.AgentVisuals.GetGlobalStableEyePoint(isHumanoid: true).z;
		agent.SetLookToPointOfInterest(_speakerAgent.AgentVisuals.GetGlobalStableEyePoint(isHumanoid: true));
		agent.AgentVisuals.GetSkeleton().ForceUpdateBoneFrames();
		agent.LookDirection = (position2 - position).NormalizedCopy();
		agent.SetLookAgent(_speakerAgent);
	}

	private void MakeSpeakerLookToListener()
	{
		Vec3 position = _speakerAgent.Position;
		Vec3 position2 = _listenerAgent.Position;
		position.z = _speakerAgent.AgentVisuals.GetGlobalStableEyePoint(isHumanoid: true).z;
		position2.z = _listenerAgent.AgentVisuals.GetGlobalStableEyePoint(isHumanoid: true).z;
		_speakerAgent.SetLookToPointOfInterest(_listenerAgent.AgentVisuals.GetGlobalStableEyePoint(isHumanoid: true));
		_speakerAgent.AgentVisuals.GetSkeleton().ForceUpdateBoneFrames();
		_speakerAgent.LookDirection = (position2 - position).NormalizedCopy();
		_speakerAgent.SetLookAgent(_listenerAgent);
	}

	private void SetConversationLookToPointOfInterest(Vec3 pointOfInterest)
	{
		ConversationManager conversationManager = Campaign.Current.ConversationManager;
		if (conversationManager.ConversationAgents != null && conversationManager.ConversationAgents.Count > 0)
		{
			_speakerAgent = (Agent)conversationManager.SpeakerAgent;
			_listenerAgent = (Agent)conversationManager.ListenerAgent;
			MakeAgentLookToSpeaker(_listenerAgent);
			MakeSpeakerLookToListener();
			Agent.Main.TeleportToPosition(pointOfInterest);
			Agent.Main.AgentVisuals.SetVisible(value: false);
			MakeAgentLookToSpeaker(Agent.Main);
		}
	}
}
