using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.MountAndBlade.Objects.Usables;

namespace SandBox.Missions;

public class EavesdroppingMissionLogic : MissionLogic
{
	public class EavesdropSound
	{
		public TextObject Line;

		public int Priority;

		public CharacterObject Character;

		public string SoundPath;

		public EavesdropSound(TextObject line, int priority, CharacterObject character, string soundPath)
		{
			Line = line;
			Priority = priority;
			Character = character;
			SoundPath = BasePath.Name + "Modules/StoryMode/ModuleData/Languages/" + soundPath + ".ogg";
		}
	}

	private const string EavesdroppingPointTag = "eavesdropping_point";

	private const string CustomCameraTag = "customcamera";

	private const string StartEavesdroppingEventId = "start_eavesdropping";

	private readonly Dictionary<EventTriggeringUsableMachine, Camera> _eavesdroppingPoints = new Dictionary<EventTriggeringUsableMachine, Camera>();

	private readonly Queue<EavesdropSound> _eavesdropSoundQueue = new Queue<EavesdropSound>();

	private SoundEvent _currentSoundEvent;

	private Timer _waitTimer;

	public bool EavesdropStarted;

	public Camera CurrentEavesdroppingCamera;

	private EventTriggeringUsableMachine _currentEventTriggeringUsableMachine;

	private readonly CharacterObject _disguiseShadowingTargetCharacter;

	private readonly CharacterObject _disguiseOfficerCharacter;

	public EavesdroppingMissionLogic(CharacterObject disguiseShadowingTargetCharacter, CharacterObject disguiseOfficerCharacter)
	{
		_disguiseShadowingTargetCharacter = disguiseShadowingTargetCharacter;
		_disguiseOfficerCharacter = disguiseOfficerCharacter;
		Game.Current.EventManager.RegisterEvent<GenericMissionEvent>(OnGenericMissionEventTriggered);
	}

	protected override void OnEndMission()
	{
		Game.Current.EventManager.UnregisterEvent<GenericMissionEvent>(OnGenericMissionEventTriggered);
	}

	private void OnGenericMissionEventTriggered(GenericMissionEvent missionEvent)
	{
		if (!EavesdropStarted && missionEvent.EventId == "start_eavesdropping")
		{
			string[] array = missionEvent.Parameter.Split(new char[1] { ' ' });
			GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag(array[0]);
			StartEavesdropping(gameEntity.GetFirstScriptOfType<EventTriggeringUsableMachine>());
		}
	}

	private void StartEavesdropping(EventTriggeringUsableMachine eventTriggeringUsableMachine)
	{
		_eavesdropSoundQueue.Enqueue(new EavesdropSound(new TextObject("{=YAWCkOYa}The tracks look fresh, and I've seen some smoke on the horizon. They can't move too quickly if they're still looting and raiding. No, I'm pretty sure we'll be able to rescue the little ones... or die trying."), 0, _disguiseShadowingTargetCharacter, "VoicedLines/EN/PC/tutorial_npc_brother_009"));
		_eavesdropSoundQueue.Enqueue(new EavesdropSound(new TextObject("{=R5kLv5kg}I am what they call Palaic. Palaic is a language that is no longer spoken, except by a few old people. Even the word 'Palaic' is imperial. We are a people who have forgotten who we are.[if:convo_focused_voice]"), 0, _disguiseOfficerCharacter, "VoicedLines/EN/PC/storymode_imperial_mentor_arzagos_009"));
		_eavesdropSoundQueue.Enqueue(new EavesdropSound(new TextObject("{=phavdGYA}Are you sure about that?"), 0, _disguiseShadowingTargetCharacter, "VoicedLines/EN/PC/tutorial_npc_brother_005"));
		_eavesdropSoundQueue.Enqueue(new EavesdropSound(new TextObject("{=dPb2Vph3}My informants will tell me once you pledged your support...[ib:normal2][if:convo_nonchalant]"), 0, _disguiseOfficerCharacter, "VoicedLines/EN/PC/storymode_imperial_mentor_arzagos_044"));
		_eavesdropSoundQueue.Enqueue(new EavesdropSound(new TextObject("{=9ACSEvzD}Let's go on then."), 0, _disguiseShadowingTargetCharacter, "VoicedLines/EN/PC/tutorial_npc_brother_004"));
		_waitTimer = new Timer(base.Mission.CurrentTime, 1.7f);
		EavesdropStarted = true;
		CurrentEavesdroppingCamera = _eavesdroppingPoints[eventTriggeringUsableMachine];
		_currentEventTriggeringUsableMachine = eventTriggeringUsableMachine;
	}

	public override void AfterStart()
	{
		base.AfterStart();
		List<GameEntity> entities = new List<GameEntity>();
		Mission.Current.Scene.GetAllEntitiesWithScriptComponent<EventTriggeringUsableMachine>(ref entities);
		foreach (GameEntity item in entities)
		{
			if (item.HasTag("eavesdropping_point"))
			{
				EventTriggeringUsableMachine firstScriptOfType = item.GetFirstScriptOfType<EventTriggeringUsableMachine>();
				Vec3 dofParams = Vec3.Invalid;
				Camera camera = Camera.CreateCamera();
				item.GetFirstChildEntityWithTag("customcamera").GetCameraParamsFromCameraScript(camera, ref dofParams);
				camera.SetFovVertical(camera.GetFovVertical(), Screen.AspectRatio, camera.Near, camera.Far);
				_eavesdroppingPoints.Add(firstScriptOfType, camera);
			}
		}
	}

	public override void OnMissionTick(float dt)
	{
		if (!EavesdropStarted)
		{
			return;
		}
		Timer waitTimer = _waitTimer;
		if (waitTimer == null || !waitTimer.Check(base.Mission.CurrentTime) || (_currentSoundEvent != null && _currentSoundEvent.IsPlaying()))
		{
			return;
		}
		_currentSoundEvent?.Stop();
		if (_eavesdropSoundQueue.IsEmpty())
		{
			_waitTimer = null;
			EavesdropStarted = false;
			CurrentEavesdroppingCamera = null;
			foreach (GenericMissionEventScript scriptComponent in _currentEventTriggeringUsableMachine.GameEntity.GetScriptComponents<GenericMissionEventScript>())
			{
				if (scriptComponent.EventId == "start_eavesdropping")
				{
					scriptComponent.IsDisabled = true;
				}
			}
			for (int i = 0; i < _currentEventTriggeringUsableMachine.StandingPoints.Count; i++)
			{
				if (_currentEventTriggeringUsableMachine.StandingPoints[i].HasUser)
				{
					_currentEventTriggeringUsableMachine.StandingPoints[i].UserAgent.StopUsingGameObject();
				}
			}
			_currentEventTriggeringUsableMachine = null;
		}
		else
		{
			EavesdropSound eavesdropSound = _eavesdropSoundQueue.Dequeue();
			MBInformationManager.AddQuickInformation(eavesdropSound.Line, eavesdropSound.Priority, eavesdropSound.Character);
			_currentSoundEvent = SoundEvent.CreateEventFromExternalFile("event:/Extra/voiceover", eavesdropSound.SoundPath, Mission.Current.Scene, is3d: true, isBlocking: false);
			_currentSoundEvent.Play();
		}
	}
}
