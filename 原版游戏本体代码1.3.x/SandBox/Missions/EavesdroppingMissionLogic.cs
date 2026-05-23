using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.MountAndBlade.Objects.Usables;

namespace SandBox.Missions
{
	// Token: 0x0200005D RID: 93
	public class EavesdroppingMissionLogic : MissionLogic
	{
		// Token: 0x060003A1 RID: 929 RVA: 0x0001524C File Offset: 0x0001344C
		public EavesdroppingMissionLogic(CharacterObject disguiseShadowingTargetCharacter, CharacterObject disguiseOfficerCharacter)
		{
			this._disguiseShadowingTargetCharacter = disguiseShadowingTargetCharacter;
			this._disguiseOfficerCharacter = disguiseOfficerCharacter;
			Game.Current.EventManager.RegisterEvent<GenericMissionEvent>(new Action<GenericMissionEvent>(this.OnGenericMissionEventTriggered));
		}

		// Token: 0x060003A2 RID: 930 RVA: 0x0001529E File Offset: 0x0001349E
		protected override void OnEndMission()
		{
			Game.Current.EventManager.UnregisterEvent<GenericMissionEvent>(new Action<GenericMissionEvent>(this.OnGenericMissionEventTriggered));
		}

		// Token: 0x060003A3 RID: 931 RVA: 0x000152BC File Offset: 0x000134BC
		private void OnGenericMissionEventTriggered(GenericMissionEvent missionEvent)
		{
			if (!this.EavesdropStarted && missionEvent.EventId == "start_eavesdropping")
			{
				string[] array = missionEvent.Parameter.Split(new char[] { ' ' });
				GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag(array[0]);
				this.StartEavesdropping(gameEntity.GetFirstScriptOfType<EventTriggeringUsableMachine>());
			}
		}

		// Token: 0x060003A4 RID: 932 RVA: 0x0001531C File Offset: 0x0001351C
		private void StartEavesdropping(EventTriggeringUsableMachine eventTriggeringUsableMachine)
		{
			this._eavesdropSoundQueue.Enqueue(new EavesdroppingMissionLogic.EavesdropSound(new TextObject("{=YAWCkOYa}The tracks look fresh, and I've seen some smoke on the horizon. They can't move too quickly if they're still looting and raiding. No, I'm pretty sure we'll be able to rescue the little ones... or die trying.", null), 0, this._disguiseShadowingTargetCharacter, "VoicedLines/EN/PC/tutorial_npc_brother_009"));
			this._eavesdropSoundQueue.Enqueue(new EavesdroppingMissionLogic.EavesdropSound(new TextObject("{=R5kLv5kg}I am what they call Palaic. Palaic is a language that is no longer spoken, except by a few old people. Even the word 'Palaic' is imperial. We are a people who have forgotten who we are.[if:convo_focused_voice]", null), 0, this._disguiseOfficerCharacter, "VoicedLines/EN/PC/storymode_imperial_mentor_arzagos_009"));
			this._eavesdropSoundQueue.Enqueue(new EavesdroppingMissionLogic.EavesdropSound(new TextObject("{=phavdGYA}Are you sure about that?", null), 0, this._disguiseShadowingTargetCharacter, "VoicedLines/EN/PC/tutorial_npc_brother_005"));
			this._eavesdropSoundQueue.Enqueue(new EavesdroppingMissionLogic.EavesdropSound(new TextObject("{=dPb2Vph3}My informants will tell me once you pledged your support...[ib:normal2][if:convo_nonchalant]", null), 0, this._disguiseOfficerCharacter, "VoicedLines/EN/PC/storymode_imperial_mentor_arzagos_044"));
			this._eavesdropSoundQueue.Enqueue(new EavesdroppingMissionLogic.EavesdropSound(new TextObject("{=9ACSEvzD}Let's go on then.", null), 0, this._disguiseShadowingTargetCharacter, "VoicedLines/EN/PC/tutorial_npc_brother_004"));
			this._waitTimer = new Timer(base.Mission.CurrentTime, 1.7f, true);
			this.EavesdropStarted = true;
			this.CurrentEavesdroppingCamera = this._eavesdroppingPoints[eventTriggeringUsableMachine];
			this._currentEventTriggeringUsableMachine = eventTriggeringUsableMachine;
		}

		// Token: 0x060003A5 RID: 933 RVA: 0x00015428 File Offset: 0x00013628
		public override void AfterStart()
		{
			base.AfterStart();
			List<GameEntity> list = new List<GameEntity>();
			Mission.Current.Scene.GetAllEntitiesWithScriptComponent<EventTriggeringUsableMachine>(ref list);
			foreach (GameEntity gameEntity in list)
			{
				if (gameEntity.HasTag("eavesdropping_point"))
				{
					EventTriggeringUsableMachine firstScriptOfType = gameEntity.GetFirstScriptOfType<EventTriggeringUsableMachine>();
					Vec3 invalid = Vec3.Invalid;
					Camera camera = Camera.CreateCamera();
					gameEntity.GetFirstChildEntityWithTag("customcamera").GetCameraParamsFromCameraScript(camera, ref invalid);
					camera.SetFovVertical(camera.GetFovVertical(), Screen.AspectRatio, camera.Near, camera.Far);
					this._eavesdroppingPoints.Add(firstScriptOfType, camera);
				}
			}
		}

		// Token: 0x060003A6 RID: 934 RVA: 0x000154F4 File Offset: 0x000136F4
		public override void OnMissionTick(float dt)
		{
			if (this.EavesdropStarted)
			{
				Timer waitTimer = this._waitTimer;
				if (waitTimer != null && waitTimer.Check(base.Mission.CurrentTime) && (this._currentSoundEvent == null || !this._currentSoundEvent.IsPlaying()))
				{
					SoundEvent currentSoundEvent = this._currentSoundEvent;
					if (currentSoundEvent != null)
					{
						currentSoundEvent.Stop();
					}
					if (this._eavesdropSoundQueue.IsEmpty<EavesdroppingMissionLogic.EavesdropSound>())
					{
						this._waitTimer = null;
						this.EavesdropStarted = false;
						this.CurrentEavesdroppingCamera = null;
						using (IEnumerator<ScriptComponentBehavior> enumerator = this._currentEventTriggeringUsableMachine.GameEntity.GetScriptComponents().GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								GenericMissionEventScript genericMissionEventScript;
								if ((genericMissionEventScript = enumerator.Current as GenericMissionEventScript) != null && genericMissionEventScript.EventId == "start_eavesdropping")
								{
									genericMissionEventScript.IsDisabled = true;
								}
							}
						}
						for (int i = 0; i < this._currentEventTriggeringUsableMachine.StandingPoints.Count; i++)
						{
							if (this._currentEventTriggeringUsableMachine.StandingPoints[i].HasUser)
							{
								this._currentEventTriggeringUsableMachine.StandingPoints[i].UserAgent.StopUsingGameObject(true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
							}
						}
						this._currentEventTriggeringUsableMachine = null;
						return;
					}
					EavesdroppingMissionLogic.EavesdropSound eavesdropSound = this._eavesdropSoundQueue.Dequeue();
					MBInformationManager.AddQuickInformation(eavesdropSound.Line, eavesdropSound.Priority, eavesdropSound.Character, null, "");
					this._currentSoundEvent = SoundEvent.CreateEventFromExternalFile("event:/Extra/voiceover", eavesdropSound.SoundPath, Mission.Current.Scene, true, false);
					this._currentSoundEvent.Play();
				}
			}
		}

		// Token: 0x040001DC RID: 476
		private const string EavesdroppingPointTag = "eavesdropping_point";

		// Token: 0x040001DD RID: 477
		private const string CustomCameraTag = "customcamera";

		// Token: 0x040001DE RID: 478
		private const string StartEavesdroppingEventId = "start_eavesdropping";

		// Token: 0x040001DF RID: 479
		private readonly Dictionary<EventTriggeringUsableMachine, Camera> _eavesdroppingPoints = new Dictionary<EventTriggeringUsableMachine, Camera>();

		// Token: 0x040001E0 RID: 480
		private readonly Queue<EavesdroppingMissionLogic.EavesdropSound> _eavesdropSoundQueue = new Queue<EavesdroppingMissionLogic.EavesdropSound>();

		// Token: 0x040001E1 RID: 481
		private SoundEvent _currentSoundEvent;

		// Token: 0x040001E2 RID: 482
		private Timer _waitTimer;

		// Token: 0x040001E3 RID: 483
		public bool EavesdropStarted;

		// Token: 0x040001E4 RID: 484
		public Camera CurrentEavesdroppingCamera;

		// Token: 0x040001E5 RID: 485
		private EventTriggeringUsableMachine _currentEventTriggeringUsableMachine;

		// Token: 0x040001E6 RID: 486
		private readonly CharacterObject _disguiseShadowingTargetCharacter;

		// Token: 0x040001E7 RID: 487
		private readonly CharacterObject _disguiseOfficerCharacter;

		// Token: 0x02000158 RID: 344
		public class EavesdropSound
		{
			// Token: 0x06000E0A RID: 3594 RVA: 0x00064289 File Offset: 0x00062489
			public EavesdropSound(TextObject line, int priority, CharacterObject character, string soundPath)
			{
				this.Line = line;
				this.Priority = priority;
				this.Character = character;
				this.SoundPath = BasePath.Name + "Modules/StoryMode/ModuleData/Languages/" + soundPath + ".ogg";
			}

			// Token: 0x040006C0 RID: 1728
			public TextObject Line;

			// Token: 0x040006C1 RID: 1729
			public int Priority;

			// Token: 0x040006C2 RID: 1730
			public CharacterObject Character;

			// Token: 0x040006C3 RID: 1731
			public string SoundPath;
		}
	}
}
